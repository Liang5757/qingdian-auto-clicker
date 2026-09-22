using System;
using System.IO;
using System.Windows.Forms;
using WindowsAutoClicker;
using Xunit;

namespace Qingdian.AutoClicker.Tests
{
    public sealed class TrayTests
    {
        [Theory]
        [InlineData(CloseReason.UserClosing, false, true)]
        [InlineData(CloseReason.UserClosing, true, false)]
        [InlineData(CloseReason.WindowsShutDown, false, false)]
        [InlineData(CloseReason.TaskManagerClosing, false, false)]
        [InlineData(CloseReason.ApplicationExitCall, false, false)]
        public void OnlyUserCloseWithoutExitRequestHides(CloseReason reason, bool exiting, bool expected)
        {
            Assert.Equal(expected, MainForm.ShouldHideOnClose(reason, exiting));
        }

        [Fact]
        public void StartupCommandQuotesTheWholeExecutablePath()
        {
            Assert.Equal("\"C:\\Program Files\\轻点\\Qingdian.AutoClicker.exe\"", StartupRegistration.Command(@"C:\Program Files\轻点\Qingdian.AutoClicker.exe"));
        }

        [Fact]
        public void HiddenStartupDefaultsOffAndPersistsWithoutChangingClickSettings()
        {
            string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "settings.xml");
            try
            {
                var store = new SettingsStore(path); string warning;
                File.WriteAllText(path, "<Settings><Interval>500</Interval><Button>1</Button></Settings>");
                var settings = store.Load(out warning);
                Assert.Null(warning); Assert.False(settings.StartHidden);
                settings.StartHidden = true;
                Assert.True(store.TrySave(settings, out warning));
                settings = store.Load(out warning);
                Assert.True(settings.StartHidden); Assert.Equal(500, settings.Interval); Assert.Equal(1, settings.Button);
            }
            finally { Directory.Delete(dir, true); }
        }
    }
}
