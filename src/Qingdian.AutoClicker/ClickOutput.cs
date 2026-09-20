using System;
using System.Runtime.InteropServices;

namespace WindowsAutoClicker
{
    // All production mouse input passes through this gate. Stop and send are serialized.
    internal sealed class ClickOutput
    {
        private readonly object gate = new object();
        private readonly Func<Native.Input[], uint> send;
        private readonly Action<string> log;
        private readonly StopSignal stop;
        private Native.Input[] inputs;
        private bool enabled;
        internal long Rounds { get; private set; }
        internal long AcceptedEvents { get; private set; }
        internal string Failure { get; private set; }

        internal ClickOutput(StopSignal stop, Func<Native.Input[], uint> send, Action<string> log)
        {
            this.stop = stop;
            this.send = send;
            this.log = log;
        }

        internal static uint SendNative(Native.Input[] inputs)
        {
            return Native.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Native.Input)));
        }

        internal void Start(int button, bool doubleClick)
        {
            lock (gate)
            {
                inputs = Native.ClickInputs(button, doubleClick, Native.InputMarker);
                Rounds = AcceptedEvents = 0;
                Failure = null;
                enabled = true;
                log("output.started button=" + button + " double=" + doubleClick + " marker=" + Native.InputMarker.ToUInt64().ToString("X"));
            }
        }

        internal bool TryClick()
        {
            lock (gate)
            {
                if (!enabled) return false;
                if (stop.Check()) { enabled = false; return false; }
                uint sent = send(inputs);
                AcceptedEvents += sent;
                if (sent != inputs.Length)
                {
                    enabled = false;
                    Failure = "点击发送失败，已停止。";
                    // Only release if the accepted prefix ends in a DOWN event.
                    // A zero-event failure must not release a physically held mouse button.
                    uint released = 0;
                    if (sent < inputs.Length && (sent & 1) != 0)
                    {
                        released = send(new[] { inputs[1] });
                        AcceptedEvents += released;
                        if (released != 1) Failure = "鼠标松开事件发送失败，请手动按下并松开鼠标按键。";
                    }
                    log("output.failed accepted=" + sent + " release=" + released);
                    return false;
                }
                Rounds++;
                return true;
            }
        }

        internal void Stop(string reason)
        {
            lock (gate)
            {
                enabled = false;
                log("output.stopped reason=" + reason + " rounds=" + Rounds + " acceptedEvents=" + AcceptedEvents);
            }
        }
    }
}
