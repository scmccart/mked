namespace Mked.Controls.Tests.Unit;

public sealed class MarkdownEditorWidget_Selection_Tests
{
    private static List<Segment> GetSegments(MarkdownEditorWidget widget)
    {
        var console = new TestConsole().Width(80);
        return widget.GetSegments(console).ToList();
    }

    // ─── Selection background ─────────────────────────────────────────────────

    [Fact]
    public void SelectedRange_HasInvertDecoration()
    {
        // Select offsets 0..2 ("he" in "hello"), cursor at (1,6) (past selection)
        var widget = new MarkdownEditorWidget(
            buffer: "hello",
            cursor: (1, 6),
            highlights: Array.Empty<StyledSpan>(),
            selectionStartOffset: 0,
            selectionEndOffset: 2);

        var segs = GetSegments(widget);

        // At least one segment covering the selection should carry Invert decoration
        segs.Should().Contain(s =>
            s.Text.Length > 0
            && "he".Contains(s.Text[0])
            && s.Style.Decoration.HasFlag(Decoration.Invert));
    }

    [Fact]
    public void UnselectedRange_HasNoInvertDecoration()
    {
        // Select only first char ("h"), "ello" should carry no Invert decoration
        var widget = new MarkdownEditorWidget(
            buffer: "hello",
            cursor: (1, 6),
            highlights: Array.Empty<StyledSpan>(),
            selectionStartOffset: 0,
            selectionEndOffset: 1);

        var segs = GetSegments(widget);

        // Characters after offset 0 should not have Invert
        segs.Should().Contain(s =>
            s.Text.Length > 0
            && (s.Text.Contains('l') || s.Text.Contains('o'))
            && !s.Style.Decoration.HasFlag(Decoration.Invert));
    }

    [Fact]
    public void CursorCell_WithinSelection_RendersDistinctlyFromSelection()
    {
        // Selection covers entire "hello" (0..5), cursor at (1,2) → 'e'
        var widget = new MarkdownEditorWidget(
            buffer: "hello",
            cursor: (1, 2),
            highlights: Array.Empty<StyledSpan>(),
            selectionStartOffset: 0,
            selectionEndOffset: 5);

        var segs = GetSegments(widget);

        // The cursor cell ('e') must be a separate segment with Bold+Invert,
        // distinct from surrounding selection-only Invert cells so it is visible.
        segs.Should().Contain(s =>
            s.Text == "e"
            && s.Style.Decoration.HasFlag(Decoration.Invert)
            && s.Style.Decoration.HasFlag(Decoration.Bold));
    }

    [Fact]
    public void NoSelection_ContentCells_HaveNoInvertDecoration()
    {
        // selectionStartOffset = -1 means no selection; cursor sits after content
        var widget = new MarkdownEditorWidget(
            buffer: "hello",
            cursor: (1, 6),
            highlights: Array.Empty<StyledSpan>());

        var segs = GetSegments(widget);

        // Content characters should carry no Invert (only the cursor cell has Invert)
        segs.Should().Contain(s =>
            s.Text.Length > 0
            && (s.Text.Contains('h') || s.Text.Contains('o'))
            && !s.Style.Decoration.HasFlag(Decoration.Invert));
    }
}
