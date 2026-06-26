using System.Runtime.InteropServices;

namespace Mked.Console;

/// <summary>
/// Native Win32 bindings for console input. All methods use <c>[LibraryImport]</c> for
/// NativeAOT / trim safety — no reflection or marshaler classes are required.
/// </summary>
internal static partial class WindowsConsoleInterop
{
    internal const int STD_INPUT_HANDLE = -10;
    internal const uint ENABLE_PROCESSED_INPUT = 0x0001;
    internal const uint ENABLE_MOUSE_INPUT = 0x0010;
    internal const uint ENABLE_EXTENDED_FLAGS = 0x0080;
    internal const uint ENABLE_QUICK_EDIT_MODE = 0x0040;

    internal const ushort KEY_EVENT = 0x0001;
    internal const ushort MOUSE_EVENT = 0x0002;
    internal const uint MOUSE_MOVED   = 0x0001;  // dwEventFlags: mouse moved (no button change)
    internal const uint DOUBLE_CLICK  = 0x0002;  // dwEventFlags: double-click
    internal const uint MOUSE_WHEELED = 0x0004;  // dwEventFlags: wheel rotated

    internal const uint FROM_LEFT_1ST_BUTTON_PRESSED = 0x0001; // dwButtonState: left button held

    internal const uint RIGHT_ALT_PRESSED = 0x0001;
    internal const uint LEFT_ALT_PRESSED = 0x0002;
    internal const uint RIGHT_CTRL_PRESSED = 0x0004;
    internal const uint LEFT_CTRL_PRESSED = 0x0008;
    internal const uint SHIFT_PRESSED = 0x0010;

    [LibraryImport("kernel32.dll")]
    internal static partial nint GetStdHandle(int nStdHandle);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetConsoleMode(nint hConsoleHandle, uint dwMode);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetNumberOfConsoleInputEvents(
        nint hConsoleInput, out uint lpcNumberOfEvents);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ReadConsoleInputW(
        nint hConsoleInput,
        out InputRecord lpBuffer,
        uint nLength,
        out uint lpNumberOfEventsRead);

    // ─── Structures ───────────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Explicit, Size = 20)]
    internal struct InputRecord
    {
        [FieldOffset(0)] public ushort EventType;
        [FieldOffset(4)] public KeyEventRecord KeyEvent;
        [FieldOffset(4)] public MouseEventRecord MouseEvent;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KeyEventRecord
    {
        public int bKeyDown;            // BOOL (4 bytes)
        public ushort wRepeatCount;
        public ushort wVirtualKeyCode;
        public ushort wVirtualScanCode;
        public char UnicodeChar;        // WCHAR (2 bytes, same offset as AsciiChar union)
        public uint dwControlKeyState;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MouseEventRecord
    {
        public short MouseX;            // COORD.X
        public short MouseY;            // COORD.Y
        public uint dwButtonState;
        public uint dwControlKeyState;
        public uint dwEventFlags;
    }
}
