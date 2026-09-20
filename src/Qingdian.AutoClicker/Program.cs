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
                    MessageBox.Show("轻点已经在运行，请查看任务栏。", "轻点");
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
