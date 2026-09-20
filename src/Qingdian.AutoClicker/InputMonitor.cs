using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace WindowsAutoClicker
{
    // Read-only hooks. No input suppression, no coordinate/typed-text logging, no UI callbacks.
    internal sealed class InputMonitor : IDisposable
    {
        private delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
        [StructLayout(LayoutKind.Sequential)]
        private struct MouseEvent
        {
            public int x, y;
            public uint data, flags, time;
            public UIntPtr extra;
        }
        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int id, HookProc callback, IntPtr module, uint thread);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
        [DllImport("user32.dll")] private static extern bool PostThreadMessage(uint thread, uint message, IntPtr w, IntPtr l);
        [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)] private static extern IntPtr GetModuleHandle(string name);
        private readonly StopSignal stop;
        private readonly Thread thread;
        private readonly ManualResetEventSlim ready = new ManualResetEventSlim();
        private readonly HookProc keyboardCallback, mouseCallback;
        private IntPtr keyboardHook, mouseHook;
        private uint threadId;
        private int disposed;
        private long ownDown, ownUp, unmarkedLeft, unmarkedRight, unmarkedMiddle;
        internal int KeyboardError { get; private set; }
        internal int MouseError { get; private set; }
        internal bool Ready { get; private set; }
        internal long OwnDown { get { return Interlocked.Read(ref ownDown); } }
        internal long OwnUp { get { return Interlocked.Read(ref ownUp); } }
        internal long UnmarkedLeft { get { return Interlocked.Read(ref unmarkedLeft); } }
        internal long UnmarkedRight { get { return Interlocked.Read(ref unmarkedRight); } }
        internal long UnmarkedMiddle { get { return Interlocked.Read(ref unmarkedMiddle); } }

        internal InputMonitor(StopSignal stop)
        {
            this.stop = stop;
            keyboardCallback = OnKeyboard;
            mouseCallback = OnMouse;
            thread = new Thread(Run) { IsBackground = true, Name = "Qingdian input monitor" };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Ready = ready.Wait(3000);
        }

        private void Run()
        {
            // Construct the message queue before publishing the thread id.
            using (var context = new ApplicationContext())
            using (var timer = new System.Windows.Forms.Timer { Interval = 10 })
            {
                timer.Tick += delegate { stop.Poll(); };
                timer.Start();
                threadId = GetCurrentThreadId();
                keyboardHook = SetWindowsHookEx(13, keyboardCallback, GetModuleHandle(null), 0);
                if (keyboardHook == IntPtr.Zero) KeyboardError = Marshal.GetLastWin32Error();
                mouseHook = SetWindowsHookEx(14, mouseCallback, GetModuleHandle(null), 0);
                if (mouseHook == IntPtr.Zero) MouseError = Marshal.GetLastWin32Error();
                ready.Set();
                try { if (Volatile.Read(ref disposed) == 0) Application.Run(context); }
                finally
                {
                    if (keyboardHook != IntPtr.Zero) UnhookWindowsHookEx(keyboardHook);
                    if (mouseHook != IntPtr.Zero) UnhookWindowsHookEx(mouseHook);
                }
            }
        }

        private IntPtr OnKeyboard(int code, IntPtr message, IntPtr data)
        {
            if (code >= 0 && (message.ToInt32() == 0x100 || message.ToInt32() == 0x104))
            {
                int key = Marshal.ReadInt32(data);
                if (key == (int)Keys.F10 || key == (int)Keys.Escape) stop.Request();
            }
            return CallNextHookEx(keyboardHook, code, message, data);
        }

        private IntPtr OnMouse(int code, IntPtr message, IntPtr data)
        {
            if (code >= 0)
            {
                int m = message.ToInt32();
                bool down = m == 0x201 || m == 0x204 || m == 0x207;
                bool up = m == 0x202 || m == 0x205 || m == 0x208;
                if (down || up)
                {
                    var value = (MouseEvent)Marshal.PtrToStructure(data, typeof(MouseEvent));
                    if ((value.flags & 1) != 0)
                    {
                        if (value.extra == Native.InputMarker)
                        {
                            if (down) Interlocked.Increment(ref ownDown); else Interlocked.Increment(ref ownUp);
                        }
                        else if (down)
                        {
                            if (m == 0x201) Interlocked.Increment(ref unmarkedLeft);
                            if (m == 0x204) Interlocked.Increment(ref unmarkedRight);
                            if (m == 0x207) Interlocked.Increment(ref unmarkedMiddle);
                        }
                    }
                }
            }
            return CallNextHookEx(mouseHook, code, message, data);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
            if (threadId != 0) PostThreadMessage(threadId, 0x12, IntPtr.Zero, IntPtr.Zero);
            if (thread.Join(1500)) ready.Dispose();
        }
    }
}
