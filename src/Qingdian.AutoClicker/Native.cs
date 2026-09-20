using System;
using System.Runtime.InteropServices;

namespace WindowsAutoClicker
{
    internal static class Native
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct MouseInput
        {
            public int dx, dy;
            public uint mouseData, flags, time;
            public UIntPtr extra;
        }
        [StructLayout(LayoutKind.Explicit)]
        internal struct InputUnion
        {
            [FieldOffset(0)] public MouseInput mouse;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct Input
        {
            public uint type;
            public InputUnion data;
        }
        [DllImport("user32.dll", SetLastError = true)] internal static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr window, int id);
        [DllImport("user32.dll", SetLastError = true)] internal static extern uint SendInput(uint count, Input[] inputs, int size);
        [DllImport("user32.dll", SetLastError = true)] internal static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();

        internal static Input[] ClickInputs(int button, bool doubleClick)
        {
            uint[] down = { 0x0002, 0x0008, 0x0020 };
            uint[] up = { 0x0004, 0x0010, 0x0040 };
            Input[] inputs = new Input[doubleClick ? 4 : 2];
            for (int i = 0; i < inputs.Length; i++)
                inputs[i].data.mouse.flags = (i % 2 == 0) ? down[button] : up[button];
            return inputs;
        }
    }

}
