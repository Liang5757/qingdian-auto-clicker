using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsAutoClicker
{
    internal sealed partial class MainForm : Form
    {
        private readonly NumericUpDown interval = Number(20, 3600000, 100);
        private readonly NumericUpDown limit = Number(0, 100000000, 0);
        private readonly ComboBox button = new ComboBox();
        private readonly CheckBox doubleClick = new CheckBox { Text = "双击（每轮点击两次）", AutoSize = true };
        private readonly Button start = new Button { Text = "开始  F9", Dock = DockStyle.Fill, Height = 42 };
        private readonly Button stop = new Button { Text = "停止  F10", Dock = DockStyle.Fill, Height = 42, Enabled = false };
        private readonly Label status = new Label { Text = "就绪 · 按 F9 开始", AutoSize = true, ForeColor = Color.FromArgb(36, 106, 80) };
        private readonly Label counter = new Label { Text = "已完成 0 轮", AutoSize = true };
        private readonly Label configWarning = new Label { AutoSize = true, ForeColor = Color.DarkOrange };
        private readonly TableLayoutPanel options = new TableLayoutPanel();
        private readonly Timer timer = new Timer();
        private readonly Timer stopTimer = new Timer { Interval = 20 };
        private readonly StopSignal stopSignal = new StopSignal(() => Native.GetAsyncKeyState((int)Keys.F10) < 0 || Native.GetAsyncKeyState((int)Keys.Escape) < 0);
        private readonly InputMonitor inputMonitor;
        private readonly ClickOutput output;
        private readonly DiagnosticLog diagnosticLog = new DiagnosticLog();
        private readonly Timer diagnosticTimer = new Timer { Interval = 2000 };
        private readonly Label inputStatus = new Label { AutoSize = true, ForeColor = Color.DimGray, MaximumSize = new Size(540, 0) };
        private long lastUnmarked, lastObserved;
        private bool resourcesDisposed;
        private readonly Stopwatch elapsed = new Stopwatch();
        private readonly SettingsStore settingsStore = new SettingsStore();
        private bool running, f9Registered, f10Registered;
        private readonly ClickSession session = new ClickSession();
        private Settings active;

        private static NumericUpDown Number(int min, int max, int value)
        {
            return new NumericUpDown { Minimum = min, Maximum = max, Value = value, Width = 160, ThousandsSeparator = true };
        }

        internal MainForm()
        {
            output = new ClickOutput(stopSignal, ClickOutput.SendNative, diagnosticLog.Write);
            inputMonitor = new InputMonitor(stopSignal);
            diagnosticLog.Write("application.started version=" + Application.ProductVersion + " pid=" + Process.GetCurrentProcess().Id + " marker=" + Native.InputMarker.ToUInt64().ToString("X") + " hookReady=" + inputMonitor.Ready + " keyboardError=" + inputMonitor.KeyboardError + " mouseError=" + inputMonitor.MouseError);
            BuildInterface();
            InitializeTray();
            start.Click += delegate { StartClicking(); };
            stop.Click += delegate { StopClicking("已停止"); };
            timer.Tick += delegate { TickClick(); };
            stopTimer.Tick += delegate { CheckEmergencyStop(); };
            diagnosticTimer.Tick += delegate { UpdateInputDiagnostics(); };
            diagnosticTimer.Start();
            FormClosing += HandleClosing;
            Resize += delegate { if (WindowState == FormWindowState.Minimized) HideToTray(); };
            LoadSettings();
            WireSettings();
            Shown += delegate { if (startHidden.Checked) HideToTray(); };
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            f10Registered = Native.RegisterHotKey(Handle, 10, 0x4000, (uint)Keys.F10);
            f9Registered = Native.RegisterHotKey(Handle, 9, 0x4000, (uint)Keys.F9);
            diagnosticLog.Write("hotkeys.registered f9=" + f9Registered + " f10=" + f10Registered);
            if (!f10Registered) { start.Enabled = false; status.Text = "F10 被占用，无法安全启动。关闭冲突程序后重开。"; }
            else if (!f9Registered) status.Text = "F9 被占用，请使用开始按钮；F10 可用。";
            UpdateTray();
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
            return new Settings { Interval = (int)interval.Value, Button = button.SelectedIndex, DoubleClick = doubleClick.Checked, Limit = (int)limit.Value, StartHidden = startHidden.Checked };
        }
        private void StartClicking()
        {
            if (running || !f10Registered) return;
            stopSignal.Reset();
            if (stopSignal.Check()) { status.Text = "请先松开 F10 / Esc，再开始。"; return; }
            active = ReadSettings();
            SaveSettings();
            if (stopSignal.Check()) { StopClicking("已取消启动 · F10"); return; }
            output.Start(active.Button, active.DoubleClick);
            diagnosticLog.Write("run.started interval=" + active.Interval + " limit=" + active.Limit);
            session.Start(active.Limit); running = true; options.Enabled = false; start.Enabled = false; stop.Enabled = true;
            counter.Text = "0"; status.Text = "1 秒后开始 · F10 随时停止";
            elapsed.Restart(); timer.Interval = 1000; timer.Start();
            stopTimer.Start();
            UpdateTray();
        }
        private void TickClick()
        {
            if (!running) return;
            if (CheckEmergencyStop()) return;
            if (!output.TryClick())
            {
                StopClicking(output.Failure ?? "已停止 · F10 / Esc 输入检测");
                return;
            }
            session.RecordClick();
            counter.Text = output.Rounds.ToString("N0");
            status.Text = "正在连点 · F10 停止";
            if (!session.IsRunning) { StopClicking("已完成设定轮数"); return; }
            timer.Interval = active.Interval;
        }
        private void StopClicking(string message)
        {
            stopSignal.Request();
            output.Stop(message);
            timer.Stop(); stopTimer.Stop(); elapsed.Stop(); session.Stop(); running = false;
            diagnosticLog.Write("run.stopped observedDown=" + inputMonitor.OwnDown + " observedUp=" + inputMonitor.OwnUp);
            options.Enabled = true; start.Enabled = f10Registered; stop.Enabled = false;
            status.Text = message;
            UpdateTray();
        }
        private bool CheckEmergencyStop()
        {
            if (!stopSignal.Check()) return false;
            StopClicking("已停止 · F10 / Esc 输入检测");
            return true;
        }
        private void UpdateInputDiagnostics()
        {
            long unmarked = inputMonitor.UnmarkedLeft + inputMonitor.UnmarkedRight + inputMonitor.UnmarkedMiddle;
            long observed = inputMonitor.OwnDown + inputMonitor.OwnUp;
            long delta = unmarked - lastUnmarked;
            inputStatus.Visible = !inputMonitor.Ready || inputMonitor.MouseError != 0 || (!running && delta > 0);
            inputStatus.Text = !inputMonitor.Ready || inputMonitor.MouseError != 0
                ? "输入观测不可用；请保留日志排查。"
                : (!running && delta > 0
                    ? "本程序已停止；近 2 秒观测到 " + delta + " 次未带本程序标记的模拟点击。"
                    : "输入观测：本程序标记按下 " + inputMonitor.OwnDown + " / 松开 " + inputMonitor.OwnUp);
            if (unmarked != lastUnmarked || observed != lastObserved)
                diagnosticLog.Write("input.observed running=" + running + " ownDown=" + inputMonitor.OwnDown + " ownUp=" + inputMonitor.OwnUp + " unmarkedLeft=" + inputMonitor.UnmarkedLeft + " unmarkedRight=" + inputMonitor.UnmarkedRight + " unmarkedMiddle=" + inputMonitor.UnmarkedMiddle);
            lastUnmarked = unmarked;
            lastObserved = observed;
        }
        private void LoadSettings()
        {
            string warning;
            Settings value = settingsStore.Load(out warning);
            interval.Value = value.Interval; button.SelectedIndex = value.Button;
            doubleClick.Checked = value.DoubleClick;
            limit.Value = value.Limit;
            startHidden.Checked = value.StartHidden;
            configWarning.Text = warning ?? "设置已自动保存";
        }
        private void SaveSettings()
        {
            string warning;
            settingsStore.TrySave(ReadSettings(), out warning);
            configWarning.Text = warning ?? "设置已自动保存";
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && !resourcesDisposed)
            {
                resourcesDisposed = true;
                stopSignal.Request();
                output.Stop("dispose");
                timer.Stop(); stopTimer.Stop(); diagnosticTimer.Stop();
                if (tray != null) { tray.Visible = false; tray.Dispose(); }
                if (trayMenu != null) trayMenu.Dispose();
                if (idleIcon != null) idleIcon.Dispose();
                if (runningIcon != null) runningIcon.Dispose();
                inputMonitor.Dispose();
                diagnosticLog.Write("application.exiting");
            }
            base.Dispose(disposing);
            if (disposing) { timer.Dispose(); stopTimer.Dispose(); diagnosticTimer.Dispose(); button.Dispose(); doubleClick.Dispose(); }
        }
    }
}
