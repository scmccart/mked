namespace Mked.Controls.Tests.Unit;

public sealed class MarkdownEditorWidget_Tests
{
    private static string Write(MarkdownEditorWidget widget)
    {
        var console = new TestConsole().Width(80);
        console.Write(widget);
        return console.Output;
    }

    private static List<Segment> GetSegments(MarkdownEditorWidget widget)
    {
        var console = new TestConsole().Width(80);
        return widget.GetSegments(console).ToList();
    }

    // ─── Single-line buffer ───────────────────────────────────────────────────

    [Fact]
    public void SingleLineBuffer_RendersLineText()
    {
        var widget = new MarkdownEditorWidget(
            buffer: "hello",
            cursor: (1, 6),
            highlights: Array.Empty<StyledSpan>());

        Write(widget).Should().Contain("hello");
    }

    // ─── Cursor rendering ─────────────────────────────────────────────────────

    [Fact]
    public void Cursor_RendersInvertedCharacterAtCorrectColumn()
    {
        var widget = new MarkdownEditorWidget(
            buffer: "hello",
            cursor: (1, 2),
            highlights: Array.Empty<StyledSpan>());

        var segs = GetSegments(widget);

        segs.Should().Contain(s =>
            s.Text.Contains('e') && s.Style.Decoration.HasFlag(Decoration.Invert));
    }

    [Fact]
    public void Cursor_AtEndOfLine_RendersInvertedSpace()
    {
        var widget = new MarkdownEditorWidget(
            buffer: "hello",
            cursor: (1, 6),
            highlights: Array.Empty<StyledSpan>());

        var segs = GetSegments(widget);

        segs.Should().Contain(s =>
            s.Text == " " && s.Style.Decoration.HasFlag(Decoration.Invert));
    }

    // ─── Highlight spans ──────────────────────────────────────────────────────

    [Fact]
    public void StyledSpan_CoveringFirstFiveChars_CausesThoseSegmentsToHaveThatStyle()
    {
        var spanStyle = new Style(foreground: Color.Red);
        var highlights = new[] { new StyledSpan(0, 5, spanStyle) };

        var widget = new MarkdownEditorWidget(
            buffer: "hello world",
            cursor: (1, 12),
            highlights: highlights);

        var segs = GetSegments(widget);

        segs.Should().Contain(s =>
            s.Text == "hello" && s.Style.Foreground == Color.Red);
    }

    // ─── Viewport clipping ────────────────────────────────────────────────────

    [Fact]
    public void Viewport_TopLineIndex1_ViewportHeight2_RendersOnlyLines1And2()
    {
        var widget = new MarkdownEditorWidget(
            buffer: "line0\nline1\nline2\nline3",
            cursor: (1, 1),
            highlights: Array.Empty<StyledSpan>(),
            topLineIndex: 1,
            viewportHeight: 2);

        string output = Write(widget);

        output.Should().Contain("line1");
        output.Should().Contain("line2");
        output.Should().NotContain("line0");
        output.Should().NotContain("line3");
    }

    // ─── Empty buffer ─────────────────────────────────────────────────────────

    [Fact]
    public void EmptyBuffer_RendersInvertedSpace()
    {
        var widget = new MarkdownEditorWidget(
            buffer: string.Empty,
            cursor: (1, 1),
            highlights: Array.Empty<StyledSpan>());

        var segs = GetSegments(widget);

        segs.Should().Contain(s =>
            s.Text == " " && s.Style.Decoration.HasFlag(Decoration.Invert));
    }
}
