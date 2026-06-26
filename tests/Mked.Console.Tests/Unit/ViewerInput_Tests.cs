using Mked.Controls;

namespace Mked.Console.Tests.Unit;

public class ViewerInput_Tests
{
    // Minimal stub for MarkdownViewerScrollInfo.
    private static MarkdownViewerScrollInfo ScrollInfo(int totalLines, int[]? blockStarts = null)
    {
        var starts = (IReadOnlyList<int>)(blockStarts ?? []);
        return new MarkdownViewerScrollInfo(totalLines, starts);
    }

    private static InputEvent Key(ConsoleKey key, ConsoleModifiers mods = 0) =>
        InputEvent.OfKey(new ConsoleKeyInfo('\0', key, mods.HasFlag(ConsoleModifiers.Shift),
            mods.HasFlag(ConsoleModifiers.Alt), mods.HasFlag(ConsoleModifiers.Control)));

    // ─── Wheel ────────────────────────────────────────────────────────────────

    [Fact]
    public void WheelDown_IncreasesCurrentLine()
    {
        int line = 0;
        bool dirty = ViewerInput.Apply(InputEvent.OfWheel(+1), ref line, ScrollInfo(100), 20, out bool quit);
        dirty.Should().BeTrue();
        quit.Should().BeFalse();
        line.Should().Be(ConsoleInputSource.LinesPerNotch); // 3
    }

    [Fact]
    public void WheelUp_DecreasesCurrentLine()
    {
        int line = 10;
        ViewerInput.Apply(InputEvent.OfWheel(-1), ref line, ScrollInfo(100), 20, out _);
        line.Should().Be(10 - ConsoleInputSource.LinesPerNotch); // 7
    }

    [Fact]
    public void WheelDown_ClampsToMaxLine()
    {
        // totalLines=100, viewportHeight=20 → maxLine=80
        int line = 79;
        ViewerInput.Apply(InputEvent.OfWheel(+1), ref line, ScrollInfo(100), 20, out _);
        line.Should().Be(80); // clamped to maxLine
    }

    [Fact]
    public void WheelUp_ClampsToZero()
    {
        int line = 1;
        ViewerInput.Apply(InputEvent.OfWheel(-1), ref line, ScrollInfo(100), 20, out _);
        line.Should().Be(0); // 1 - 3 < 0, clamped
    }

    [Fact]
    public void WheelAtMax_ReturnsFalse()
    {
        // Already at maxLine — no change.
        int line = 80;
        bool dirty = ViewerInput.Apply(InputEvent.OfWheel(+1), ref line, ScrollInfo(100), 20, out _);
        dirty.Should().BeFalse();
        line.Should().Be(80);
    }

    // ─── Arrow keys ───────────────────────────────────────────────────────────

    [Fact]
    public void DownArrow_IncreasesLineByOne()
    {
        int line = 5;
        bool dirty = ViewerInput.Apply(Key(ConsoleKey.DownArrow), ref line, ScrollInfo(100), 20, out _);
        dirty.Should().BeTrue();
        line.Should().Be(6);
    }

    [Fact]
    public void UpArrow_DecreasesLineByOne()
    {
        int line = 5;
        ViewerInput.Apply(Key(ConsoleKey.UpArrow), ref line, ScrollInfo(100), 20, out _);
        line.Should().Be(4);
    }

    [Fact]
    public void J_SameAsDownArrow()
    {
        int line = 5;
        ViewerInput.Apply(Key(ConsoleKey.J), ref line, ScrollInfo(100), 20, out _);
        line.Should().Be(6);
    }

    [Fact]
    public void K_SameAsUpArrow()
    {
        int line = 5;
        ViewerInput.Apply(Key(ConsoleKey.K), ref line, ScrollInfo(100), 20, out _);
        line.Should().Be(4);
    }

    // ─── Block jump ───────────────────────────────────────────────────────────

    [Fact]
    public void ShiftDown_JumpsToNextBlockStart()
    {
        int line = 5;
        var scroll = ScrollInfo(100, blockStarts: [0, 10, 25, 40]);
        ViewerInput.Apply(Key(ConsoleKey.DownArrow, ConsoleModifiers.Shift), ref line, scroll, 20, out _);
        line.Should().Be(10);
    }

    [Fact]
    public void ShiftUp_JumpsToPrevBlockStart()
    {
        int line = 12;
        var scroll = ScrollInfo(100, blockStarts: [0, 10, 25, 40]);
        ViewerInput.Apply(Key(ConsoleKey.UpArrow, ConsoleModifiers.Shift), ref line, scroll, 20, out _);
        line.Should().Be(10);
    }

    // ─── Page scroll ──────────────────────────────────────────────────────────

    [Fact]
    public void PageDown_ScrollsHalfViewport()
    {
        int line = 0;
        ViewerInput.Apply(Key(ConsoleKey.PageDown), ref line, ScrollInfo(100), 20, out _);
        line.Should().Be(10); // h/2 = 10
    }

    [Fact]
    public void PageUp_ScrollsHalfViewportBack()
    {
        int line = 30;
        ViewerInput.Apply(Key(ConsoleKey.PageUp), ref line, ScrollInfo(100), 20, out _);
        line.Should().Be(20);
    }

    // ─── Top / bottom ─────────────────────────────────────────────────────────

    [Fact]
    public void ShiftG_ScrollsToBottom()
    {
        int line = 0;
        ViewerInput.Apply(Key(ConsoleKey.G, ConsoleModifiers.Shift), ref line, ScrollInfo(100), 20, out _);
        line.Should().Be(80); // maxLine = 100 - 20
    }

    [Fact]
    public void G_ScrollsToTop()
    {
        int line = 50;
        ViewerInput.Apply(Key(ConsoleKey.G), ref line, ScrollInfo(100), 20, out _);
        line.Should().Be(0);
    }

    // ─── Quit ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Q_SetsQuit()
    {
        int line = 0;
        bool dirty = ViewerInput.Apply(Key(ConsoleKey.Q), ref line, ScrollInfo(100), 20, out bool quit);
        quit.Should().BeTrue();
        dirty.Should().BeFalse();
    }

    [Fact]
    public void CtrlC_SetsQuit()
    {
        int line = 0;
        ViewerInput.Apply(Key(ConsoleKey.C, ConsoleModifiers.Control), ref line, ScrollInfo(100), 20, out bool quit);
        quit.Should().BeTrue();
    }

    // ─── Unhandled key ────────────────────────────────────────────────────────

    [Fact]
    public void UnhandledKey_ReturnsFalse()
    {
        int line = 0;
        bool dirty = ViewerInput.Apply(Key(ConsoleKey.F1), ref line, ScrollInfo(100), 20, out bool quit);
        dirty.Should().BeFalse();
        quit.Should().BeFalse();
        line.Should().Be(0);
    }

    // ─── Click events ignored (viewer has no cursor) ──────────────────────────

    [Fact]
    public void Click_IsIgnored_ReturnsFalse()
    {
        int line = 5;
        bool dirty = ViewerInput.Apply(InputEvent.OfClick(10, 3), ref line, ScrollInfo(100), 20, out bool quit);
        dirty.Should().BeFalse();
        quit.Should().BeFalse();
        line.Should().Be(5); // unchanged
    }
}
