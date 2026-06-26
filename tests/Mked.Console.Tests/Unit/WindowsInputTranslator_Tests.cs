namespace Mked.Console.Tests.Unit;

public class WindowsInputTranslator_Tests
{
    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static WindowsConsoleInterop.InputRecord KeyRecord(
        ushort vk, char ch, uint controlState = 0, int bKeyDown = 1, ushort repeat = 1)
    {
        var r = new WindowsConsoleInterop.InputRecord
        {
            EventType = WindowsConsoleInterop.KEY_EVENT,
        };
        r.KeyEvent.bKeyDown = bKeyDown;
        r.KeyEvent.wRepeatCount = repeat;
        r.KeyEvent.wVirtualKeyCode = vk;
        r.KeyEvent.UnicodeChar = ch;
        r.KeyEvent.dwControlKeyState = controlState;
        return r;
    }

    private static WindowsConsoleInterop.InputRecord WheelRecord(uint buttonState)
    {
        var r = new WindowsConsoleInterop.InputRecord
        {
            EventType = WindowsConsoleInterop.MOUSE_EVENT,
        };
        r.MouseEvent.dwEventFlags = WindowsConsoleInterop.MOUSE_WHEELED;
        r.MouseEvent.dwButtonState = buttonState;
        return r;
    }

    // ─── Key events ───────────────────────────────────────────────────────────

    [Fact]
    public void KeyDown_ProducesKeyEvent()
    {
        var record = KeyRecord((ushort)ConsoleKey.Enter, '\r');
        var ev = WindowsInputTranslator.Translate(in record);
        ev.Should().NotBeNull();
        ev!.Value.Kind.Should().Be(InputEventKind.Key);
        ev.Value.Key.Key.Should().Be(ConsoleKey.Enter);
    }

    [Fact]
    public void KeyUp_ReturnsNull()
    {
        var record = KeyRecord((ushort)ConsoleKey.A, 'a', bKeyDown: 0);
        WindowsInputTranslator.Translate(in record).Should().BeNull();
    }

    [Fact]
    public void ShiftModifier_SetOnShiftPressed()
    {
        var record = KeyRecord(
            (ushort)ConsoleKey.UpArrow, '\0',
            controlState: WindowsConsoleInterop.SHIFT_PRESSED);
        var ev = WindowsInputTranslator.Translate(in record);
        ev!.Value.Key.Modifiers.Should().HaveFlag(ConsoleModifiers.Shift);
    }

    [Fact]
    public void CtrlModifier_SetOnLeftCtrlPressed()
    {
        var record = KeyRecord(
            (ushort)ConsoleKey.C, 'c',
            controlState: WindowsConsoleInterop.LEFT_CTRL_PRESSED);
        var ev = WindowsInputTranslator.Translate(in record);
        ev!.Value.Key.Modifiers.Should().HaveFlag(ConsoleModifiers.Control);
    }

    [Fact]
    public void AltModifier_SetOnLeftAltPressed()
    {
        var record = KeyRecord(
            (ushort)ConsoleKey.A, 'a',
            controlState: WindowsConsoleInterop.LEFT_ALT_PRESSED);
        var ev = WindowsInputTranslator.Translate(in record);
        ev!.Value.Key.Modifiers.Should().HaveFlag(ConsoleModifiers.Alt);
    }

    // ─── Wheel events ─────────────────────────────────────────────────────────

    [Fact]
    public void WheelUp_PositiveHighWord_ReturnsMinus1()
    {
        // Positive high word = wheel rolled forward = scroll up in our convention = WheelDelta -1
        uint buttonState = 0x00780000; // WHEEL_DELTA = 120 in high word, positive
        var record = WheelRecord(buttonState);
        var ev = WindowsInputTranslator.Translate(in record);
        ev.Should().NotBeNull();
        ev!.Value.Kind.Should().Be(InputEventKind.Wheel);
        ev.Value.WheelDelta.Should().Be(-1);
    }

    [Fact]
    public void WheelDown_NegativeHighWord_ReturnsPlus1()
    {
        // Negative high word = wheel rolled backward = scroll down = WheelDelta +1
        uint buttonState = 0xFF880000; // -120 as signed int in high word
        var record = WheelRecord(buttonState);
        var ev = WindowsInputTranslator.Translate(in record);
        ev!.Value.Kind.Should().Be(InputEventKind.Wheel);
        ev.Value.WheelDelta.Should().Be(+1);
    }

    [Fact]
    public void MouseMove_IsDiscarded()
    {
        var r = new WindowsConsoleInterop.InputRecord { EventType = WindowsConsoleInterop.MOUSE_EVENT };
        r.MouseEvent.dwEventFlags = WindowsConsoleInterop.MOUSE_MOVED;
        WindowsInputTranslator.Translate(in r).Should().BeNull();
    }

    [Fact]
    public void UnhandledEventType_ReturnsNull()
    {
        var r = new WindowsConsoleInterop.InputRecord { EventType = 0x0010 }; // WINDOW_BUFFER_SIZE_EVENT
        WindowsInputTranslator.Translate(in r).Should().BeNull();
    }

    // ─── Click events ─────────────────────────────────────────────────────────

    [Fact]
    public void LeftButtonPress_ProducesClickEvent()
    {
        var r = new WindowsConsoleInterop.InputRecord { EventType = WindowsConsoleInterop.MOUSE_EVENT };
        r.MouseEvent.dwEventFlags = 0; // single click (no special flag)
        r.MouseEvent.dwButtonState = WindowsConsoleInterop.FROM_LEFT_1ST_BUTTON_PRESSED;
        r.MouseEvent.MouseX = 10;
        r.MouseEvent.MouseY = 3;

        var ev = WindowsInputTranslator.Translate(in r);
        ev.Should().NotBeNull();
        ev!.Value.Kind.Should().Be(InputEventKind.Click);
        ev.Value.ClickX.Should().Be(10);
        ev.Value.ClickY.Should().Be(3);
    }

    [Fact]
    public void DoubleClick_ProducesClickEvent()
    {
        var r = new WindowsConsoleInterop.InputRecord { EventType = WindowsConsoleInterop.MOUSE_EVENT };
        r.MouseEvent.dwEventFlags = WindowsConsoleInterop.DOUBLE_CLICK;
        r.MouseEvent.dwButtonState = WindowsConsoleInterop.FROM_LEFT_1ST_BUTTON_PRESSED;
        r.MouseEvent.MouseX = 5;
        r.MouseEvent.MouseY = 7;

        var ev = WindowsInputTranslator.Translate(in r);
        ev.Should().NotBeNull();
        ev!.Value.Kind.Should().Be(InputEventKind.Click);
        ev.Value.ClickX.Should().Be(5);
        ev.Value.ClickY.Should().Be(7);
    }

    [Fact]
    public void LeftButtonRelease_IsDiscarded()
    {
        // On release, dwButtonState bit is 0 (button no longer held).
        var r = new WindowsConsoleInterop.InputRecord { EventType = WindowsConsoleInterop.MOUSE_EVENT };
        r.MouseEvent.dwEventFlags = 0;
        r.MouseEvent.dwButtonState = 0; // bit cleared = release
        WindowsInputTranslator.Translate(in r).Should().BeNull();
    }
}
