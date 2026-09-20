using System;
using System.IO;

namespace WindowsAutoClicker
{
    internal sealed class DiagnosticLog
    {
        private readonly object gate = new object();
        internal string Path { get; private set; }
        internal DiagnosticLog(string path = null)
        {
            Path = path ?? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WindowsAutoClicker", "diagnostics.log");
        }
        internal void Write(string message)
        {
            lock (gate)
            {
                try
                {
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
                    if (File.Exists(Path) && new FileInfo(Path).Length > 524288)
                    {
                        File.Copy(Path, Path + ".1", true);
                        File.WriteAllText(Path, string.Empty);
                    }
                    File.AppendAllText(Path, DateTime.UtcNow.ToString("O") + " " + message + Environment.NewLine);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
