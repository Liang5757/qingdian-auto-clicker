using System;

namespace WindowsAutoClicker
{
    // Counts complete click rounds. The UI thread owns scheduling and native input.
    internal sealed class ClickSession
    {
        private int limit;
        public long Rounds { get; private set; }
        public bool IsRunning { get; private set; }

        public void Start(int roundLimit)
        {
            if (roundLimit < 0) throw new ArgumentOutOfRangeException(nameof(roundLimit));
            if (IsRunning) return;
            limit = roundLimit;
            Rounds = 0;
            IsRunning = true;
        }

        public void RecordClick()
        {
            if (!IsRunning) return;
            Rounds++;
            if (limit > 0 && Rounds >= limit) Stop();
        }

        public void Stop() { IsRunning = false; }
    }
}
