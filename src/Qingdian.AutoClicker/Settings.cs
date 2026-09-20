using System;

namespace WindowsAutoClicker
{
    public class Settings
    {
        public int Interval = 100;
        public int Button = 0;
        public bool DoubleClick = false;
        public bool FixedPosition = false;
        public int X = 0;
        public int Y = 0;
        public int Limit = 0;
        public void Validate()
        {
            Interval = Math.Max(20, Math.Min(3600000, Interval));
            Button = Math.Max(0, Math.Min(2, Button));
            Limit = Math.Max(0, Math.Min(100000000, Limit));
            X = Math.Max(-100000, Math.Min(100000, X));
            Y = Math.Max(-100000, Math.Min(100000, Y));
        }
    }

}
