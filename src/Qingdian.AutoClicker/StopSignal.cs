using System;
using System.Threading;

namespace WindowsAutoClicker
{
    // The worker only latches a request; it never accesses controls or sends input.
    internal sealed class StopSignal
    {
        private readonly Func<bool> isKeyDown;
        private int requested;

        internal StopSignal(Func<bool> isKeyDown)
        {
            this.isKeyDown = isKeyDown ?? throw new ArgumentNullException(nameof(isKeyDown));
        }

        internal void Poll()
        {
            if (isKeyDown()) Interlocked.Exchange(ref requested, 1);
        }

        internal bool Check()
        {
            Poll();
            return Volatile.Read(ref requested) != 0;
        }

        internal void Reset()
        {
            Interlocked.Exchange(ref requested, 0);
            Poll(); // A held stop key must prevent a new start.
        }
    }
}
