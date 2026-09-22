using System;
using System.Threading;
using System.Windows.Forms;

namespace WindowsAutoClicker
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool created;
            using (var singleton = new Mutex(true, "Local\\Qingdian.AutoClicker", out created))
            {
                if (!created)
                {
                    MessageBox.Show("轻点已经在运行，请查看系统托盘（任务栏右侧隐藏图标）。", "轻点");
                    return;
                }
                Native.SetProcessDPIAware();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }
    }
}
