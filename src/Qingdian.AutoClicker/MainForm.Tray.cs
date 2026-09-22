using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowsAutoClicker
{
    internal sealed partial class MainForm
    {
        private NotifyIcon tray;
        private ContextMenuStrip trayMenu;
        private ToolStripMenuItem trayStart, trayStop;
        private Icon idleIcon, runningIcon;
        private bool exitRequested, trayHintShown;
        [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr icon);

        private void InitializeTray()
        {
            idleIcon = CreateTrayIcon(Blue); runningIcon = CreateTrayIcon(Color.FromArgb(25, 175, 104));
            Icon = idleIcon;
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("打开轻点", null, delegate { RestoreWindow(); });
            trayMenu.Items.Add(new ToolStripSeparator());
            trayStart = new ToolStripMenuItem("开始  F9", null, delegate { StartClicking(); });
            trayStop = new ToolStripMenuItem("停止  F10 / Esc", null, delegate { StopClicking("已停止 · 托盘菜单"); });
            trayMenu.Items.Add(trayStart); trayMenu.Items.Add(trayStop);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("退出轻点", null, delegate { ExitApplication(); });
            tray = new NotifyIcon { Icon = idleIcon, Text = "轻点 · 已停止", ContextMenuStrip = trayMenu, Visible = true };
            tray.DoubleClick += delegate { RestoreWindow(); };
            UpdateTray();
        }

        private static Icon CreateTrayIcon(Color color)
        {
            using (var bitmap = new Bitmap(32, 32))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var brush = new SolidBrush(color))
            using (var pen = new Pen(Color.White, 2))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.FillEllipse(brush, 1, 1, 30, 30);
                graphics.DrawLines(pen, new[] { new Point(11, 7), new Point(11, 23), new Point(15, 19), new Point(19, 25), new Point(22, 23), new Point(18, 17), new Point(24, 17), new Point(11, 7) });
                IntPtr handle = bitmap.GetHicon();
                try { using (var icon = Icon.FromHandle(handle)) return (Icon)icon.Clone(); }
                finally { DestroyIcon(handle); }
            }
        }

        private void UpdateTray()
        {
            if (tray == null || resourcesDisposed) return;
            tray.Icon = running ? runningIcon : idleIcon;
            tray.Text = running ? "轻点 · 正在连点 · F10 / Esc 停止" : "轻点 · 已停止 · F9 开始";
            trayStart.Enabled = !running && f10Registered;
            trayStop.Enabled = running;
        }

        private void HideToTray()
        {
            // Hide preserves the native handle and registered hotkeys.
            Hide();
            if (!trayHintShown)
            {
                trayHintShown = true;
                tray.ShowBalloonTip(3000, "轻点正在后台驻留", "F9 开始，F10 / Esc 停止。双击托盘图标打开；右键菜单退出。", ToolTipIcon.Info);
            }
        }

        private void RestoreWindow()
        {
            Show(); WindowState = FormWindowState.Normal; Activate();
        }

        private void ExitApplication()
        {
            exitRequested = true;
            StopClicking("已停止 · 退出程序");
            Close();
        }

        private void HandleClosing(object sender, FormClosingEventArgs e)
        {
            if (ShouldHideOnClose(e.CloseReason, exitRequested))
            {
                e.Cancel = true;
                SaveSettings(); HideToTray();
                return;
            }
            StopClicking("已停止 · 退出程序"); SaveSettings();
        }

        internal static bool ShouldHideOnClose(CloseReason reason, bool exiting)
        {
            // Never cancel shutdown, task-manager close, or explicit application exit.
            return reason == CloseReason.UserClosing && !exiting;
        }
    }
}
