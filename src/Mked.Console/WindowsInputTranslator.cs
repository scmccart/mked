namespace Mked.Console;

/// <summary>
/// Pure (no I/O) translator from raw Win32 <see cref="WindowsConsoleInterop.InputRecord"/> values
/// to <see cref="InputEvent"/> instances. Static so it is trivially unit-testable without any
/// real console handle.
/// </summary>
internal static class WindowsInputTranslator
{
    /// <summary>
    /// Translates one Win32 input record to an <see cref="InputEvent"/>.
    /// Returns <see langword="null"/> for unhandled event types (mouse move, focus, resize, etc.).
    /// Key-down records with <c>wRepeatCount &gt; 1</c> yield multiple events; the caller should
    /// call this in a loop using <paramref name="repeatIndex"/> from 0 to <c>repeatCount - 1</c>.
    /// </summary>
    internal static InputEvent? Translate(
        in WindowsConsoleInterop.InputRecord record, int repeatIndex = 0)
    {
        _ = repeatIndex; // repeat fanning is handled by WindowsConsoleInputSource

        switch (record.EventType)
        {
            case WindowsConsoleInterop.KEY_EVENT:
            {
                ref readonly var k = ref record.KeyEvent;
                if (k.bKeyDown == 0) return null; // key-up events are ignored

                bool shift = (k.dwControlKeyState & WindowsConsoleInterop.SHIFT_PRESSED) != 0;
                bool alt = (k.dwControlKeyState &
                    (WindowsConsoleInterop.LEFT_ALT_PRESSED | WindowsConsoleInterop.RIGHT_ALT_PRESSED)) != 0;
                bool ctrl = (k.dwControlKeyState &
                    (WindowsConsoleInterop.LEFT_CTRL_PRESSED | WindowsConsoleInterop.RIGHT_CTRL_PRESSED)) != 0;

                var key = new ConsoleKeyInfo(k.UnicodeChar, (ConsoleKey)k.wVirtualKeyCode, shift, alt, ctrl);
                return InputEvent.OfKey(key);
            }

            case WindowsConsoleInterop.MOUSE_EVENT:
            {
                ref readonly var m = ref record.MouseEvent;

                // ── Mouse wheel ───────────────────────────────────────────────
                if (m.dwEventFlags == WindowsConsoleInterop.MOUSE_WHEELED)
                {
                    // High word of dwButtonState: positive = wheel rolled forward (toward screen) = scroll up.
                    // Our convention: WheelDelta +1 = scroll down, -1 = scroll up.
                    int highWord = (short)(m.dwButtonState >> 16); // high word is a signed WHEEL_DELTA
                    int delta = highWord > 0 ? -1 : 1;
                    return InputEvent.OfWheel(delta);
                }

                // ── Left-button press ─────────────────────────────────────────
                // Emit on press only (dwButtonState bit set). Releases (bit cleared) and pure
                // mouse-move records (MOUSE_MOVED) are discarded so the translator stays stateless.
                bool isClick = m.dwEventFlags == 0 || m.dwEventFlags == WindowsConsoleInterop.DOUBLE_CLICK;
                if (isClick && (m.dwButtonState & WindowsConsoleInterop.FROM_LEFT_1ST_BUTTON_PRESSED) != 0)
                    return InputEvent.OfClick(m.MouseX, m.MouseY); // MouseX/Y are already 0-based

                return null;
            }

            default:
                return null;
        }
    }
}
