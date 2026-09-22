using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

namespace WindowsAutoClicker
{
    internal sealed partial class MainForm
    {
        private static readonly Color Blue = Color.FromArgb(24, 112, 245);
        private static readonly Color Muted = Color.FromArgb(113, 123, 142);
        private readonly CheckBox startHidden = new CheckBox { Text = "启动时隐藏到托盘", AutoSize = true };
        private readonly CheckBox autoStart = new CheckBox { Text = "开机启动", AutoSize = true };
        private bool loadingSettings;

        private static Label Caption(string text, float size = 10, bool bold = false)
        {
            return new Label { Text = text, AutoSize = true, ForeColor = Color.FromArgb(27, 34, 48),
                Font = new Font("Microsoft YaHei UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
                Margin = new Padding(0, 0, 0, 10) };
        }

        private static TableLayoutPanel Stack()
        {
            var panel = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 1, Margin = Padding.Empty };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            return panel;
        }

        private void BuildInterface()
        {
            Text = "轻点 · v" + typeof(MainForm).Assembly.GetName().Version.ToString(3);
            Font = new Font("Microsoft YaHei UI", 10F);
            ForeColor = Color.FromArgb(27, 34, 48);
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(720, 810);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(248, 250, 253);
            AutoScroll = true;
            var layout = Stack();
            layout.Padding = new Padding(28, 10, 28, 10);
            Controls.Add(layout);

            var heading = Caption("让重复点击，更简单", 20, true);
            heading.Dock = DockStyle.Fill; heading.TextAlign = ContentAlignment.MiddleCenter;
            heading.Margin = new Padding(0, 2, 0, 10);
            layout.Controls.Add(heading);
            var shortcut = Caption("F9 开始 · F10 / Esc 停止", 11);
            shortcut.ForeColor = Muted; shortcut.Dock = DockStyle.Fill; shortcut.TextAlign = ContentAlignment.MiddleCenter;
            shortcut.Margin = new Padding(0, 0, 0, 12); layout.Controls.Add(shortcut);

            var stateCard = new Surface { Dock = DockStyle.Top, Height = 110, Padding = new Padding(20, 14, 20, 14), Margin = new Padding(0, 0, 0, 16) };
            var stateGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            stateGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            stateGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            stateGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            status.Text = "●  就绪"; status.Dock = DockStyle.Fill; status.AutoSize = false;
            status.TextAlign = ContentAlignment.MiddleLeft; status.Font = new Font(Font, FontStyle.Bold);
            status.ForeColor = Color.FromArgb(25, 139, 86);
            var countBox = new Panel { Dock = DockStyle.Fill };
            counter.Text = "0"; counter.Font = new Font("Segoe UI", 21, FontStyle.Bold);
            counter.AutoSize = false; counter.Dock = DockStyle.Fill;
            counter.TextAlign = ContentAlignment.MiddleRight;
            var countCaption = Caption("已完成轮数", 9); countCaption.ForeColor = Muted;
            countCaption.AutoSize = false; countCaption.Dock = DockStyle.Bottom; countCaption.Height = 24;
            countCaption.TextAlign = ContentAlignment.MiddleRight;
            countBox.Controls.Add(counter); countBox.Controls.Add(countCaption);
            stateGrid.Controls.Add(status); stateGrid.Controls.Add(countBox); stateCard.Controls.Add(stateGrid); layout.Controls.Add(stateCard);

            var settingsCard = new Surface { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(16), Margin = new Padding(0, 0, 0, 14) };
            var content = Stack(); settingsCard.Controls.Add(content); layout.Controls.Add(settingsCard);
            options.ColumnCount = 2; options.RowCount = 2; options.AutoSize = true; options.Dock = DockStyle.Top; options.Margin = Padding.Empty;
            options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            button.Items.AddRange(new object[] { "左键", "右键", "中键" }); button.SelectedIndex = 0;
            options.Controls.Add(NumberField("点击间隔", interval, "毫秒 · 最小 20 ms"), 0, 0);
            options.Controls.Add(NumberField("点击轮数", limit, "0 表示持续运行"), 1, 0);
            options.Controls.Add(ChoiceField("鼠标按键", new[] { "左键", "右键", "中键" }, () => button.SelectedIndex, n => button.SelectedIndex = n,
                handler => button.SelectedIndexChanged += handler), 0, 1);
            options.Controls.Add(ChoiceField("点击方式", new[] { "单击", "双击" }, () => doubleClick.Checked ? 1 : 0, n => doubleClick.Checked = n == 1,
                handler => doubleClick.CheckedChanged += handler), 1, 1);
            content.Controls.Add(options);
            var hint = Caption("↖  始终点击鼠标当前位置", 10); hint.ForeColor = Muted;
            hint.BackColor = Color.FromArgb(242, 245, 250); hint.Padding = new Padding(8); hint.Dock = DockStyle.Fill;
            hint.Margin = new Padding(0, 4, 0, 16); content.Controls.Add(hint);
            var actions = new TableLayoutPanel { Dock = DockStyle.Top, Height = 50, ColumnCount = 2, Margin = Padding.Empty };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            StyleAction(start, "▶  开始连点     F9", Blue, Color.White);
            StyleAction(stop, "■  停止     F10", Color.FromArgb(255, 238, 240), Color.FromArgb(220, 43, 61));
            start.Margin = new Padding(0, 0, 6, 0); stop.Margin = new Padding(6, 0, 0, 0);
            actions.Controls.Add(start); actions.Controls.Add(stop); content.Controls.Add(actions);
            var delay = Caption("启动后预留 1 秒，移开鼠标即可", 9); delay.ForeColor = Muted;
            delay.Dock = DockStyle.Fill; delay.TextAlign = ContentAlignment.MiddleCenter; delay.Margin = new Padding(0, 12, 0, 0); content.Controls.Add(delay);

            var background = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Margin = new Padding(0, 0, 0, 8), WrapContents = true };
            startHidden.Margin = new Padding(0, 0, 24, 6); autoStart.Margin = new Padding(0, 0, 0, 6);
            background.Controls.Add(startHidden); background.Controls.Add(autoStart); layout.Controls.Add(background);
            var trayHint = Caption("关闭 / 最小化窗口将驻留托盘；退出请用托盘菜单。", 9);
            trayHint.ForeColor = Muted; layout.Controls.Add(trayHint);
            inputStatus.Visible = false; inputStatus.MaximumSize = new Size(650, 0); inputStatus.Font = new Font(Font.FontFamily, 9); inputStatus.Margin = new Padding(0, 0, 0, 10);
            layout.Controls.Add(inputStatus);
            var footer = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, Padding = new Padding(0, 12, 0, 0) };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            configWarning.ForeColor = Muted; configWarning.Font = new Font(Font.FontFamily, 9); configWarning.MaximumSize = new Size(460, 0);
            footer.Controls.Add(configWarning);
            var logLink = new LinkLabel { Text = "诊断日志 ↗", AutoSize = true, Anchor = AnchorStyles.Right, LinkColor = Blue, ActiveLinkColor = Blue, LinkBehavior = LinkBehavior.NeverUnderline };
            logLink.LinkClicked += delegate {
                diagnosticLog.Write("diagnostics.opened");
                try { if (File.Exists(diagnosticLog.Path)) Process.Start(new ProcessStartInfo(diagnosticLog.Path) { UseShellExecute = true }); }
                catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException) { status.Text = "无法打开日志，请检查文件关联。"; }
            };
            footer.Controls.Add(logLink); layout.Controls.Add(footer);
        }

        private Control NumberField(string title, NumericUpDown number, string helper)
        {
            var field = Stack(); field.Margin = new Padding(0, 0, 16, 8);
            field.Controls.Add(Caption(title, 11, true));
            number.Font = new Font("Segoe UI", 14); number.Dock = DockStyle.Top; number.BorderStyle = BorderStyle.FixedSingle;
            number.Margin = new Padding(0, 0, 0, 7); number.AccessibleName = title;
            field.Controls.Add(number);
            var note = Caption(helper, 9); note.ForeColor = Muted; note.Margin = Padding.Empty; field.Controls.Add(note);
            return field;
        }

        private Control ChoiceField(string title, string[] labels, Func<int> get, Action<int> set, Action<EventHandler> subscribe)
        {
            var field = Stack(); field.Margin = new Padding(0, 0, 16, 14); field.Controls.Add(Caption(title, 11, true));
            var row = new TableLayoutPanel { Height = 42, Dock = DockStyle.Top, ColumnCount = labels.Length, Margin = Padding.Empty };
            var choices = new RadioButton[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / labels.Length));
                var choice = new RadioButton { Text = labels[i], Appearance = Appearance.Button, FlatStyle = FlatStyle.Flat,
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Margin = Padding.Empty, AccessibleName = title + "：" + labels[i] };
                choice.FlatAppearance.CheckedBackColor = Blue;
                choice.FlatAppearance.BorderColor = Color.FromArgb(214, 222, 234);
                choice.CheckedChanged += delegate { if (choice.Checked) set(index); };
                choices[i] = choice; row.Controls.Add(choice);
            }
            Action refresh = () => { for (int i = 0; i < choices.Length; i++) { bool selected = get() == i; choices[i].Checked = selected; choices[i].BackColor = selected ? Blue : Color.FromArgb(247, 249, 252); choices[i].ForeColor = selected ? Color.White : ForeColor; } };
            subscribe(delegate { refresh(); }); refresh(); field.Controls.Add(row); return field;
        }

        private static void StyleAction(Button action, string text, Color back, Color fore)
        {
            action.Text = text; action.BackColor = back; action.ForeColor = fore; action.FlatStyle = FlatStyle.Flat;
            action.FlatAppearance.BorderSize = 0; action.Font = new Font("Microsoft YaHei UI", 13, FontStyle.Bold);
            action.Dock = DockStyle.Fill; action.Cursor = Cursors.Hand;
        }

        private void WireSettings()
        {
            loadingSettings = true;
            string warning;
            autoStart.Checked = StartupRegistration.IsEnabled(out warning);
            if (warning != null) configWarning.Text = warning;
            loadingSettings = false;
            EventHandler save = delegate { if (!loadingSettings) SaveSettings(); };
            interval.ValueChanged += save; limit.ValueChanged += save; button.SelectedIndexChanged += save;
            doubleClick.CheckedChanged += save; startHidden.CheckedChanged += save;
            autoStart.CheckedChanged += delegate {
                if (loadingSettings) return;
                string error;
                if (!StartupRegistration.TrySet(autoStart.Checked, out error))
                {
                    loadingSettings = true; autoStart.Checked = !autoStart.Checked; loadingSettings = false;
                    configWarning.Text = error;
                }
                else configWarning.Text = "设置已自动保存";
            };
        }

        private sealed class Surface : Panel
        {
            internal Surface() { DoubleBuffered = true; BackColor = Color.White; }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.FromArgb(221, 227, 237)))
                using (var path = new GraphicsPath())
                {
                    const int d = 16; int w = Width - 1, h = Height - 1;
                    if (w < d || h < d) return;
                    path.AddArc(0, 0, d, d, 180, 90); path.AddArc(w - d, 0, d, d, 270, 90);
                    path.AddArc(w - d, h - d, d, d, 0, 90); path.AddArc(0, h - d, d, d, 90, 90); path.CloseFigure();
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }
    }
}
