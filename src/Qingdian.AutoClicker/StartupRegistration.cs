using System;
using System.IO;
using System.Security;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WindowsAutoClicker
{
    internal static class StartupRegistration
    {
        private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "Qingdian.AutoClicker";
        internal static string Command(string executable) { return "\"" + executable + "\""; }

        internal static bool IsEnabled(out string warning)
        {
            warning = null;
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(KeyPath))
                    return string.Equals(key == null ? null : key.GetValue(ValueName) as string, Command(Application.ExecutablePath), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is SecurityException || ex is IOException)
            {
                warning = "无法读取开机启动设置。"; return false;
            }
        }

        internal static bool TrySet(bool enabled, out string warning)
        {
            warning = null;
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(KeyPath))
                {
                    if (key == null) { warning = "无法打开开机启动设置。"; return false; }
                    if (enabled) key.SetValue(ValueName, Command(Application.ExecutablePath), RegistryValueKind.String);
                    else key.DeleteValue(ValueName, false);
                }
                return true;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is SecurityException || ex is IOException)
            {
                warning = "开机启动设置失败，请检查当前用户权限。"; return false;
            }
        }
    }
}
