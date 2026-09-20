using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace WindowsAutoClicker
{
    internal sealed class MainForm : Form
    {
        private readonly NumericUpDown interval = Number(20, 3600000, 100);
        private readonly NumericUpDown limit = Number(0, 100000000, 0);
        private readonly NumericUpDown x = Number(-100000, 100000, 0);
        private readonly NumericUpDown y = Number(-100000, 100000, 0);
        private readonly ComboBox button = new ComboBox();
        private readonly CheckBox doubleClick = new CheckBox { Text = "双击（每轮点击两次）", AutoSize = true };
        private readonly CheckBox fixedPosition = new CheckBox { Text = "固定屏幕坐标", AutoSize = true };
        private readonly Button pick = new Button { Text = "3 秒后取点", AutoSize = true };
        private readonly Button start = new Button { Text = "开始  F9", Dock = DockStyle.Fill, Height = 42 };
        private readonly Button stop = new Button { Text = "停止  F10", Dock = DockStyle.Fill, Height = 42, Enabled = false };
        private readonly Label status = new Label { Text = "就绪 · 按 F9 开始", AutoSize = true, ForeColor = Color.FromArgb(36, 106, 80) };
        private readonly Label counter = new Label { Text = "已完成 0 轮", AutoSize = true };
        private readonly Label configWarning = new Label { AutoSize = true, ForeColor = Color.DarkOrange };
        private readonly TableLayoutPanel options = new TableLayoutPanel();
        private readonly Timer timer = new Timer();
        private readonly Timer pickTimer = new Timer { Interval = 100 };
        private readonly Timer stopTimer = new Timer { Interval = 20 };
        private readonly StopSignal stopSignal = new StopSignal(() => Native.GetAsyncKeyState((int)Keys.F10) < 0);
        private readonly System.Threading.Timer stopPoller;
        private readonly Stopwatch elapsed = new Stopwatch();
        private readonly Stopwatch pickElapsed = new Stopwatch();
        private readonly SettingsStore settingsStore = new SettingsStore();
        private bool running, f9Registered, f10Registered;
        private readonly ClickSession session = new ClickSession();
        private Settings active;
        private Native.Input[] inputs;

        private static NumericUpDown Number(int min, int max, int value)
        {
            return new NumericUpDown { Minimum = min, Maximum = max, Value = value, Width = 160, ThousandsSeparator = true };
        }

        internal MainForm()
        {
            Text = "轻点 · Windows 连点器 v1.0.1";
            Font = new Font("Microsoft YaHei UI", 10F);
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(600, 570);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(247, 249, 252);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(24), ColumnCount = 1, RowCount = 8 };
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.Controls.Add(new Label { Text = "轻点 / AUTO CLICKER", Font = new Font(Font.FontFamily, 19, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 0, 8) });
            layout.Controls.Add(new Label { Text = "F9 全局开始 · F10 全局停止", AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(0, 0, 0, 18) });
            options.AutoSize = true;
            options.Dock = DockStyle.Top;
            options.ColumnCount = 2;
            options.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            button.DropDownStyle = ComboBoxStyle.DropDownList;
            button.Items.AddRange(new object[] { "鼠标左键", "鼠标右键", "鼠标中键" });
            button.SelectedIndex = 0;
            button.Width = 160;
            AddOption("间隔（毫秒）", interval);
            AddOption("鼠标按键", button);
            AddOption("点击方式", doubleClick);
            AddOption("轮数（0=无限）", limit);
            AddOption("点击位置", fixedPosition);
            var coords = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
            x.Width = 115; y.Width = 115;
            coords.Controls.Add(new Label { Text = "X", AutoSize = true, Padding = new Padding(0, 5, 0, 0) }); coords.Controls.Add(x);
            coords.Controls.Add(new Label { Text = "Y", AutoSize = true, Padding = new Padding(0, 5, 0, 0) }); coords.Controls.Add(y);
            AddOption("屏幕坐标", coords);
            AddOption("延时取点", pick);
            layout.Controls.Add(options);
            var actions = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Top, Height = 54, Margin = new Padding(0, 16, 0, 8) };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            start.BackColor = Color.FromArgb(39, 99, 215); start.ForeColor = Color.White; start.FlatStyle = FlatStyle.Flat;
            actions.Controls.Add(start); actions.Controls.Add(stop); layout.Controls.Add(actions);
            layout.Controls.Add(status); layout.Controls.Add(counter); layout.Controls.Add(configWarning);
            layout.Controls.Add(new Label { Text = "启动后预留 1 秒移开鼠标；取消固定坐标即跟随鼠标。\n最小间隔 20 ms，实际速度受系统调度影响。", AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(0, 12, 0, 0) });
            Controls.Add(layout);
            start.Click += delegate { StartClicking(); };
            stop.Click += delegate { StopClicking("已停止"); };
            fixedPosition.CheckedChanged += delegate { UpdateCoordinates(); };
            pick.Click += delegate { BeginPick(); };
            timer.Tick += delegate { TickClick(); };
            pickTimer.Tick += delegate { TickPick(); };
            stopTimer.Tick += delegate { CheckEmergencyStop(); };
            stopPoller = new System.Threading.Timer(delegate { stopSignal.Poll(); }, null, 0, 10);
            FormClosing += delegate { StopClicking("已停止"); SaveSettings(); };
            LoadSettings(); UpdateCoordinates();
        }

        private void AddOption(string title, Control control)
        {
            int row = options.RowCount++;
            options.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            options.Controls.Add(new Label { Text = title, AutoSize = true, Margin = new Padding(0, 6, 8, 8) }, 0, row);
            control.Margin = new Padding(0, 3, 0, 7);
            options.Controls.Add(control, 1, row);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            f10Registered = Native.RegisterHotKey(Handle, 10, 0x4000, (uint)Keys.F10);
            f9Registered = Native.RegisterHotKey(Handle, 9, 0x4000, (uint)Keys.F9);
            if (!f10Registered) { start.Enabled = false; status.Text = "F10 被占用，无法安全启动。关闭冲突程序后重开。"; }
            else if (!f9Registered) status.Text = "F9 被占用，请使用开始按钮；F10 可用。";
        }
        protected override void OnHandleDestroyed(EventArgs e)
        {
            StopClicking("已停止");
            if (f9Registered) Native.UnregisterHotKey(Handle, 9);
            if (f10Registered) Native.UnregisterHotKey(Handle, 10);
            f9Registered = f10Registered = false;
            base.OnHandleDestroyed(e);
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312)
            {
                if (m.WParam.ToInt32() == 10) StopClicking("已停止 · F10 热键");
                if (m.WParam.ToInt32() == 9) StartClicking();
            }
            base.WndProc(ref m);
        }
        private Settings ReadSettings()
        {
            return new Settings { Interval = (int)interval.Value, Button = button.SelectedIndex, DoubleClick = doubleClick.Checked, FixedPosition = fixedPosition.Checked, X = (int)x.Value, Y = (int)y.Value, Limit = (int)limit.Value };
        }
        private void UpdateCoordinates() { x.Enabled = y.Enabled = fixedPosition.Checked; }
        private void StartClicking()
        {
            if (running || pickTimer.Enabled || !f10Registered) return;
            stopSignal.Reset();
            if (stopSignal.Check()) { status.Text = "请先松开 F10，再开始。"; return; }
            active = ReadSettings();
            if (active.FixedPosition && !OnScreen(new Point(active.X, active.Y)))
            {
                status.Text = "坐标不在当前屏幕内，请重新取点。"; return;
            }
            SaveSettings();
            if (stopSignal.Check()) { StopClicking("已取消启动 · F10"); return; }
            inputs = Native.ClickInputs(active.Button, active.DoubleClick);
            session.Start(active.Limit); running = true; options.Enabled = false; start.Enabled = false; stop.Enabled = true;
            counter.Text = "已完成 0 轮"; status.Text = "1 秒后开始 · F10 随时停止";
            elapsed.Restart(); timer.Interval = 1000; timer.Start();
            stopTimer.Start();
        }
        private static bool OnScreen(Point point)
        {
            foreach (Screen screen in Screen.AllScreens) if (screen.Bounds.Contains(point)) return true;
            return false;
        }
        private void TickClick()
        {
            if (!running) return;
            if (CheckEmergencyStop()) return;
            if (active.FixedPosition && (!OnScreen(new Point(active.X, active.Y)) || !Native.SetCursorPos(active.X, active.Y)))
            { StopClicking("坐标不可用，已停止"); return; }
            if (CheckEmergencyStop()) return;
            uint sent = Native.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Native.Input)));
            if (sent != inputs.Length)
            {
                // Release the selected button after a partial insertion, without issuing another down event.
                Native.SendInput(1, new Native.Input[] { inputs[1] }, Marshal.SizeOf(typeof(Native.Input)));
                StopClicking("点击发送失败，请检查目标程序权限。"); return;
            }
            session.RecordClick();
            counter.Text = "已完成 " + session.Rounds.ToString("N0") + " 轮 · " + elapsed.Elapsed.TotalSeconds.ToString("0.0") + " 秒";
            status.Text = "正在连点 · F10 停止";
            if (!session.IsRunning) { StopClicking("已完成设定轮数"); return; }
            timer.Interval = active.Interval;
        }
        private void StopClicking(string message)
        {
            timer.Stop(); pickTimer.Stop(); stopTimer.Stop(); elapsed.Stop(); session.Stop(); running = false;
            options.Enabled = true; start.Enabled = f10Registered; stop.Enabled = false;
            pick.Text = "3 秒后取点"; status.Text = message;
        }
        private void BeginPick()
        {
            stopSignal.Reset();
            if (stopSignal.Check()) { status.Text = "请先松开 F10，再取点。"; return; }
            options.Enabled = false; start.Enabled = false; stop.Enabled = true;
            status.Text = "请把鼠标移到目标位置 · F10 取消";
            pickElapsed.Restart(); pickTimer.Start();
            stopTimer.Start();
        }
        private bool CheckEmergencyStop()
        {
            if (!stopSignal.Check()) return false;
            StopClicking("已停止 · F10 按键检测");
            return true;
        }
        private void TickPick()
        {
            if (CheckEmergencyStop()) return;
            pick.Text = Math.Max(0, 3 - pickElapsed.Elapsed.TotalSeconds).ToString("0.0") + " 秒后取点";
            if (pickElapsed.ElapsedMilliseconds < 3000) return;
            Point point = Cursor.Position;
            x.Value = Math.Max(x.Minimum, Math.Min(x.Maximum, point.X));
            y.Value = Math.Max(y.Minimum, Math.Min(y.Maximum, point.Y));
            fixedPosition.Checked = true;
            StopClicking("已记录坐标：" + point.X + ", " + point.Y);
        }
        private void LoadSettings()
        {
            string warning;
            Settings value = settingsStore.Load(out warning);
            interval.Value = value.Interval; button.SelectedIndex = value.Button;
            doubleClick.Checked = value.DoubleClick; fixedPosition.Checked = value.FixedPosition;
            x.Value = value.X; y.Value = value.Y; limit.Value = value.Limit;
            configWarning.Text = warning ?? string.Empty;
        }
        private void SaveSettings()
        {
            string warning;
            settingsStore.TrySave(ReadSettings(), out warning);
            configWarning.Text = warning ?? string.Empty;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { timer.Stop(); pickTimer.Stop(); stopTimer.Stop(); stopPoller.Dispose(); }
            base.Dispose(disposing);
            if (disposing) { timer.Dispose(); pickTimer.Dispose(); stopTimer.Dispose(); }
        }
    }

}
