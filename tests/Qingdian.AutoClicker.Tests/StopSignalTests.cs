using System.Threading;
using WindowsAutoClicker;
using Xunit;

namespace Qingdian.AutoClicker.Tests
{
    public sealed class StopSignalTests
    {
        [Fact]
        public void MissingHotkeyMessageStillStopsFromKeyState()
        {
            bool down = false;
            var signal = new StopSignal(() => down);
            signal.Reset();
            Assert.False(signal.Check());
            down = true;
            Assert.True(signal.Check());
        }

        [Fact]
        public void WorkerLatchesPressUntilUiResumesEvenAfterRelease()
        {
            int down = 1;
            var signal = new StopSignal(() => Volatile.Read(ref down) != 0);
            var worker = new Thread(signal.Poll);
            worker.Start();
            Assert.True(worker.Join(3000));
            Volatile.Write(ref down, 0);
            Assert.True(signal.Check());
            Assert.True(signal.Check());
            signal.Reset();
            Assert.False(signal.Check());
        }

        [Fact]
        public void HeldStopKeyPreventsRestart()
        {
            var signal = new StopSignal(() => true);
            signal.Reset();
            Assert.True(signal.Check());
        }

        [Fact]
        public void StopBeforeFirstRoundDoesNotIncrementSession()
        {
            bool down = false;
            var signal = new StopSignal(() => down);
            var session = new ClickSession();
            session.Start(0);
            down = true;
            if (signal.Check()) session.Stop();
            session.RecordClick();
            Assert.Equal(0, session.Rounds);
            Assert.False(session.IsRunning);
        }
    }
}
