using System;

namespace WindowsAutoClicker
{
    public class Settings
    {
        public int Interval = 100;
        public int Button = 0;
        public bool DoubleClick = false;
        public int Limit = 0;
        public bool StartHidden = false;
        public void Validate()
        {
            Interval = Math.Max(20, Math.Min(3600000, Interval));
            Button = Math.Max(0, Math.Min(2, Button));
            Limit = Math.Max(0, Math.Min(100000000, Limit));
        }
    }

}
