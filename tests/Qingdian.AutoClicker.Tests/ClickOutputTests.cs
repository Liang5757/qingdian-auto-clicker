using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WindowsAutoClicker;
using Xunit;

namespace Qingdian.AutoClicker.Tests
{
    public sealed class ClickOutputTests
    {
        [Fact]
        public void QueuedTicksAfterStopCannotSendAnything()
        {
            int calls = 0;
            var signal = new StopSignal(() => false);
            var output = new ClickOutput(signal, inputs => { calls++; return (uint)inputs.Length; }, _ => { });
            output.Start(0, false);
            Assert.True(output.TryClick());
            output.Stop("test");
            Parallel.For(0, 1000, _ => Assert.False(output.TryClick()));
            Assert.Equal(1, calls);
            Assert.Equal(1, output.Rounds);
        }

        [Fact]
        public void HookRequestStopsOutputBeforeUiProcessesStop()
        {
            int calls = 0;
            var signal = new StopSignal(() => false); // Key already released; polling cannot see it.
            var output = new ClickOutput(signal, inputs => { calls++; return (uint)inputs.Length; }, _ => { });
            output.Start(1, false);
            signal.Request(); // Event captured on the independent monitor thread.
            Assert.False(output.TryClick());
            Assert.Equal(0, calls);
            Assert.Equal(0, output.AcceptedEvents);
        }

        [Fact]
        public async Task StopWaitsForInFlightBatchAndPreventsEverySubsequentBatch()
        {
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            using (var stopping = new ManualResetEventSlim())
            {
                int calls = 0;
                var signal = new StopSignal(() => false);
                var output = new ClickOutput(signal, inputs =>
                {
                    Interlocked.Increment(ref calls);
                    entered.Set();
                    if (!release.Wait(3000)) throw new TimeoutException();
                    return (uint)inputs.Length;
                }, _ => { });
                output.Start(0, false);
                Task send = Task.Run(() => output.TryClick());
                Assert.True(entered.Wait(3000));
                Task stop = Task.Run(() => { stopping.Set(); output.Stop("concurrent"); });
                Assert.True(stopping.Wait(3000));
                Assert.NotSame(stop, await Task.WhenAny(stop, Task.Delay(30)));
                release.Set();
                Task completed = Task.WhenAll(send, stop);
                Assert.Same(completed, await Task.WhenAny(completed, Task.Delay(3000)));
                await completed;
                Assert.False(output.TryClick());
                Assert.Equal(1, calls);
            }
        }

        [Theory]
        [InlineData(0u, 1)]
        [InlineData(1u, 2)]
        [InlineData(2u, 1)]
        [InlineData(3u, 2)]
        public void PartialDoubleClickOnlyReleasesAnAcceptedUnpairedDown(uint accepted, int expectedCalls)
        {
            var captured = new List<Native.Input[]>();
            var output = new ClickOutput(new StopSignal(() => false), inputs =>
            {
                captured.Add(inputs);
                return captured.Count == 1 ? accepted : 1;
            }, _ => { });
            output.Start(1, true);
            Assert.False(output.TryClick());
            Assert.Equal(expectedCalls, captured.Count);
            if (expectedCalls == 2)
            {
                Assert.Single(captured[1]);
                Assert.Equal(0x10u, captured[1][0].data.mouse.flags); // RIGHTUP only
            }
            Assert.NotNull(output.Failure);
            Assert.Equal(0, output.Rounds);
            Assert.False(output.TryClick());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void EveryOutgoingEventHasTheProcessMarkerAndNoMovement(int button)
        {
            var output = new ClickOutput(new StopSignal(() => false), inputs =>
            {
                Assert.NotEqual(UIntPtr.Zero, Native.InputMarker);
                Assert.All(inputs, input =>
                {
                    Assert.Equal(Native.InputMarker, input.data.mouse.extra);
                    Assert.Equal(0, input.data.mouse.dx);
                    Assert.Equal(0, input.data.mouse.dy);
                    Assert.Equal(0u, input.data.mouse.flags & (0x1u | 0x8000u));
                });
                return (uint)inputs.Length;
            }, _ => { });
            output.Start(button, true);
            Assert.True(output.TryClick());
            Assert.Equal(4, output.AcceptedEvents);
        }

        [Fact]
        public void RestartRequiresExplicitSignalReset()
        {
            int calls = 0;
            var signal = new StopSignal(() => false);
            var output = new ClickOutput(signal, inputs => { calls++; return (uint)inputs.Length; }, _ => { });
            output.Start(0, false);
            signal.Request();
            output.Stop("test");
            output.Start(0, false);
            Assert.False(output.TryClick());
            signal.Reset();
            output.Start(0, false);
            Assert.True(output.TryClick());
            Assert.Equal(1, calls);
        }
    }
}
