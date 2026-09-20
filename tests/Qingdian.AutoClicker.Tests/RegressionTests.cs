using System;
using System.IO;
using System.Runtime.InteropServices;
using WindowsAutoClicker;
using Xunit;

namespace Qingdian.AutoClicker.Tests
{
    public sealed class RegressionTests
    {
        [Theory]
        [InlineData(-1, 20)]
        [InlineData(100, 100)]
        [InlineData(int.MaxValue, 3600000)]
        public void CorruptIntervalIsClamped(int value, int expected)
        {
            var settings = new Settings { Interval = value, Button = 99, Limit = -1 };
            settings.Validate();
            Assert.Equal(expected, settings.Interval);
            Assert.Equal(2, settings.Button);
            Assert.Equal(0, settings.Limit);
        }

        [Fact]
        public void FiniteSessionStopsExactlyAtLimitAndIgnoresLateTicks()
        {
            var session = new ClickSession();
            session.Start(3);
            for (int i = 0; i < 10; i++) session.RecordClick();
            Assert.False(session.IsRunning);
            Assert.Equal(3, session.Rounds);
        }

        [Fact]
        public void UnlimitedSessionStopsAndRestartResetsCounter()
        {
            var session = new ClickSession();
            session.Start(0);
            for (int i = 0; i < 1000; i++) session.RecordClick();
            Assert.True(session.IsRunning);
            session.Start(1); // Repeated F9 must not reset an active session.
            Assert.Equal(1000, session.Rounds);
            session.Stop();
            session.RecordClick();
            Assert.Equal(1000, session.Rounds);
            session.Start(1);
            Assert.Equal(0, session.Rounds);
            session.RecordClick();
            Assert.False(session.IsRunning);
        }

        [Fact]
        public void InvalidLimitIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ClickSession().Start(-1));
        }

        [Theory]
        [InlineData(0, 2u, 4u)]
        [InlineData(1, 8u, 16u)]
        [InlineData(2, 32u, 64u)]
        public void NativeClicksHaveBalancedDownAndUp(int button, uint down, uint up)
        {
            foreach (bool doubleClick in new[] { false, true })
            {
                var inputs = Native.ClickInputs(button, doubleClick);
                Assert.Equal(doubleClick ? 4 : 2, inputs.Length);
                for (int i = 0; i < inputs.Length; i++)
                {
                    Assert.Equal(0u, inputs[i].type);
                    Assert.Equal(0, inputs[i].data.mouse.dx);
                    Assert.Equal(0, inputs[i].data.mouse.dy);
                    Assert.Equal(0u, inputs[i].data.mouse.flags & (0x0001u | 0x8000u)); // No MOVE or ABSOLUTE flags.
                    Assert.Equal(i % 2 == 0 ? down : up, inputs[i].data.mouse.flags);
                }
            }
            Assert.Equal(IntPtr.Size == 8 ? 40 : 28, Marshal.SizeOf(typeof(Native.Input)));
        }

        [Fact]
        public void LegacyFixedCoordinatesAreIgnoredAndRemovedOnSave()
        {
            WithDirectory(dir =>
            {
                string path = Path.Combine(dir, "settings.xml");
                File.WriteAllText(path, "<Settings><Interval>500</Interval><FixedPosition>true</FixedPosition><X>1919</X><Y>1128</Y><Limit>3</Limit></Settings>");
                string warning;
                var store = new SettingsStore(path);
                var settings = store.Load(out warning);
                Assert.Null(warning);
                Assert.Equal(500, settings.Interval);
                Assert.Equal(3, settings.Limit);
                Assert.True(store.TrySave(settings, out warning));
                string saved = File.ReadAllText(path);
                Assert.DoesNotContain("FixedPosition", saved);
                Assert.DoesNotContain("<X>", saved);
                Assert.DoesNotContain("<Y>", saved);
            });
        }

        [Fact]
        public void SettingsRoundTripAndAtomicReplacement()
        {
            WithDirectory(dir =>
            {
                var store = new SettingsStore(Path.Combine(dir, "settings.xml"));
                string warning;
                Assert.True(store.TrySave(new Settings { Interval = 150, DoubleClick = true }, out warning));
                Assert.Null(warning);
                Assert.Equal(150, store.Load(out warning).Interval);
                Assert.True(store.Load(out warning).DoubleClick);
                Assert.True(store.TrySave(new Settings { Interval = 300 }, out warning));
                Assert.Equal(300, store.Load(out warning).Interval);
                Assert.Single(Directory.GetFiles(dir));
            });
        }

        [Theory]
        [InlineData("not xml")]
        [InlineData("<!DOCTYPE Settings [<!ENTITY x SYSTEM 'file:///nonexistent'>]><Settings>&x;</Settings>")]
        public void InvalidOrExternalEntityXmlFallsBackSafely(string xml)
        {
            WithDirectory(dir =>
            {
                string path = Path.Combine(dir, "settings.xml");
                File.WriteAllText(path, xml);
                string warning;
                var value = new SettingsStore(path).Load(out warning);
                Assert.Equal(100, value.Interval);
                Assert.NotNull(warning);
                Assert.Equal(xml, File.ReadAllText(path));
            });
        }

        [Fact]
        public void MissingSettingsUseDefaultsWithoutWarning()
        {
            WithDirectory(dir =>
            {
                string warning;
                Assert.Equal(100, new SettingsStore(Path.Combine(dir, "missing.xml")).Load(out warning).Interval);
                Assert.Null(warning);
            });
        }

        [Fact]
        public void FailedSavePreservesExistingFileAndCleansTemporaryFile()
        {
            WithDirectory(dir =>
            {
                string path = Path.Combine(dir, "settings.xml");
                var store = new SettingsStore(path);
                string warning;
                Assert.True(store.TrySave(new Settings { Interval = 170 }, out warning));
                using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    Assert.False(store.TrySave(new Settings { Interval = 990 }, out warning));
                    Assert.NotNull(warning);
                }
                Assert.Equal(170, store.Load(out warning).Interval);
                Assert.Single(Directory.GetFiles(dir));
            });
        }

        private static void WithDirectory(Action<string> action)
        {
            string directory = Path.Combine(Path.GetTempPath(), "Qingdian.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try { action(directory); }
            finally { Directory.Delete(directory, true); }
        }
    }
}
