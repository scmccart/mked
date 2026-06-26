namespace Mked.Console.Tests.Unit;

public class TerminalInputParser_Tests
{
    private static List<byte> Bytes(params byte[] data) => new(data);
    private static List<byte> Seq(string s) => new(System.Text.Encoding.Latin1.GetBytes(s));

    private static InputEvent Parse(List<byte> buf)
    {
        var parser = new TerminalInputParser();
        bool ok = parser.TryParse(buf, out var ev);
        ok.Should().BeTrue("expected a complete event to be parsed");
        return ev;
    }

    // ─── Mouse wheel ──────────────────────────────────────────────────────────

    [Fact]
    public void WheelUp_SgrMouse_ReturnsWheelMinus1()
    {
        var buf = Seq("\x1b[<64;1;1M");
        var ev = Parse(buf);
        ev.Kind.Should().Be(InputEventKind.Wheel);
        ev.WheelDelta.Should().Be(-1);
    }

    [Fact]
    public void WheelDown_SgrMouse_ReturnsWheelPlus1()
    {
        var buf = Seq("\x1b[<65;1;1M");
        var ev = Parse(buf);
        ev.Kind.Should().Be(InputEventKind.Wheel);
        ev.WheelDelta.Should().Be(+1);
    }

    // ─── Left-button click ────────────────────────────────────────────────────

    [Fact]
    public void LeftClick_SgrPress_ReturnsClickEvent()
    {
        // btn=0 (left), col=5, row=3, M=press → Click(4, 2) [0-based]
        var buf = Seq("\x1b[<0;5;3M");
        var ev = Parse(buf);
        ev.Kind.Should().Be(InputEventKind.Click);
        ev.ClickX.Should().Be(4);
        ev.ClickY.Should().Be(2);
    }

    [Fact]
    public void LeftClick_SgrRelease_IsDiscarded()
    {
        // 'm' = release → discard.
        var buf = Seq("\x1b[<0;5;3m");
        var parser = new TerminalInputParser();
        bool ok = parser.TryParse(buf, out _);
        ok.Should().BeFalse("button release events are discarded");
    }

    [Fact]
    public void MiddleClick_IsDiscarded()
    {
        // btn=1 = middle button → discard.
        var buf = Seq("\x1b[<1;5;3M");
        var parser = new TerminalInputParser();
        bool ok = parser.TryParse(buf, out _);
        ok.Should().BeFalse("middle-button clicks are discarded");
    }

    [Fact]
    public void RightClick_IsDiscarded()
    {
        // btn=2 = right button → discard.
        var buf = Seq("\x1b[<2;5;3M");
        var parser = new TerminalInputParser();
        bool ok = parser.TryParse(buf, out _);
        ok.Should().BeFalse("right-button clicks are discarded");
    }

    [Fact]
    public void DragEvent_IsDiscarded()
    {
        // btn=32 = motion/drag flag set → discard.
        var buf = Seq("\x1b[<32;5;3M");
        var parser = new TerminalInputParser();
        bool ok = parser.TryParse(buf, out _);
        ok.Should().BeFalse("drag/motion events are discarded");
    }

    // ─── Arrow keys ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("\x1b[A", ConsoleKey.UpArrow)]
    [InlineData("\x1b[B", ConsoleKey.DownArrow)]
    [InlineData("\x1b[C", ConsoleKey.RightArrow)]
    [InlineData("\x1b[D", ConsoleKey.LeftArrow)]
    public void CsiArrow_ProducesExpectedKey(string seq, ConsoleKey expected)
    {
        var buf = Seq(seq);
        var ev = Parse(buf);
        ev.Kind.Should().Be(InputEventKind.Key);
        ev.Key.Key.Should().Be(expected);
        ev.Key.Modifiers.Should().Be(0);
    }

    [Theory]
    [InlineData("\x1b[1;2A", ConsoleKey.UpArrow,    ConsoleModifiers.Shift)]
    [InlineData("\x1b[1;2B", ConsoleKey.DownArrow,  ConsoleModifiers.Shift)]
    [InlineData("\x1b[1;2C", ConsoleKey.RightArrow, ConsoleModifiers.Shift)]
    [InlineData("\x1b[1;2D", ConsoleKey.LeftArrow,  ConsoleModifiers.Shift)]
    public void CsiArrowWithShift_ProducesShiftModifier(string seq, ConsoleKey expected, ConsoleModifiers mods)
    {
        var buf = Seq(seq);
        var ev = Parse(buf);
        ev.Kind.Should().Be(InputEventKind.Key);
        ev.Key.Key.Should().Be(expected);
        ev.Key.Modifiers.Should().Be(mods);
    }

    // ─── Navigation keys ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("\x1b[5~",  ConsoleKey.PageUp)]
    [InlineData("\x1b[6~",  ConsoleKey.PageDown)]
    [InlineData("\x1b[H",   ConsoleKey.Home)]
    [InlineData("\x1b[F",   ConsoleKey.End)]
    [InlineData("\x1b[1~",  ConsoleKey.Home)]
    [InlineData("\x1b[4~",  ConsoleKey.End)]
    public void NavigationSequence_ProducesExpectedKey(string seq, ConsoleKey expected)
    {
        var buf = Seq(seq);
        var ev = Parse(buf);
        ev.Kind.Should().Be(InputEventKind.Key);
        ev.Key.Key.Should().Be(expected);
    }

    // ─── SS3 (ESC O x) ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("\x1bOA", ConsoleKey.UpArrow)]
    [InlineData("\x1bOB", ConsoleKey.DownArrow)]
    [InlineData("\x1bOC", ConsoleKey.RightArrow)]
    [InlineData("\x1bOD", ConsoleKey.LeftArrow)]
    public void Ss3Arrow_ProducesExpectedKey(string seq, ConsoleKey expected)
    {
        var buf = Seq(seq);
        var ev = Parse(buf);
        ev.Kind.Should().Be(InputEventKind.Key);
        ev.Key.Key.Should().Be(expected);
    }

    // ─── Plain bytes ──────────────────────────────────────────────────────────

    [Fact]
    public void Enter_ProducesEnterKey()
    {
        var buf = Bytes(0x0d);
        var ev = Parse(buf);
        ev.Key.Key.Should().Be(ConsoleKey.Enter);
    }

    [Fact]
    public void Backspace_ProducesBackspaceKey()
    {
        var buf = Bytes(0x7f);
        var ev = Parse(buf);
        ev.Key.Key.Should().Be(ConsoleKey.Backspace);
    }

    [Fact]
    public void Tab_ProducesTabKey()
    {
        var buf = Bytes(0x09);
        var ev = Parse(buf);
        ev.Key.Key.Should().Be(ConsoleKey.Tab);
    }

    [Fact]
    public void CtrlC_ProducesCtrlCKey()
    {
        var buf = Bytes(0x03); // Ctrl+C = byte 3
        var ev = Parse(buf);
        ev.Key.Key.Should().Be(ConsoleKey.C);
        ev.Key.Modifiers.Should().Be(ConsoleModifiers.Control);
    }

    [Fact]
    public void PrintableChar_PreservesCharValue()
    {
        var buf = Bytes((byte)'a');
        var ev = Parse(buf);
        ev.Kind.Should().Be(InputEventKind.Key);
        ev.Key.KeyChar.Should().Be('a');
    }

    // ─── Partial sequence buffering ───────────────────────────────────────────

    [Fact]
    public void PartialEscapeSequence_Buffered_CompletesOnSecondCall()
    {
        var parser = new TerminalInputParser();

        // First call: just ESC — no complete event yet.
        var buf = Bytes(0x1b);
        bool first = parser.TryParse(buf, out _);
        first.Should().BeFalse("incomplete sequence should not produce event");
        buf.Should().BeEmpty("consumed bytes should be removed from the list");

        // Second call: rest of the arrow sequence.
        buf.AddRange(new byte[] { (byte)'[', (byte)'A' });
        bool second = parser.TryParse(buf, out var ev);
        second.Should().BeTrue();
        ev.Key.Key.Should().Be(ConsoleKey.UpArrow);
    }

    // ─── Buffer is drained after parse ───────────────────────────────────────

    [Fact]
    public void ConsumedBytes_RemovedFromBuffer()
    {
        var buf = Seq("\x1b[Aextra");
        var parser = new TerminalInputParser();
        _ = parser.TryParse(buf, out _);
        // "extra" should remain
        buf.Should().Equal(new byte[] { (byte)'e', (byte)'x', (byte)'t', (byte)'r', (byte)'a' });
    }
}
