namespace Mked.Controls;

/// <summary>Renders a raw text buffer with a block cursor, syntax-highlight overlays, and an optional text selection.</summary>
public sealed class MarkdownEditorWidget : IRenderable
{

    private readonly string _buffer;
    private readonly (int Line, int Column) _cursor;
    private readonly IReadOnlyList<StyledSpan> _highlights;
    private readonly int _topLineIndex;
    private readonly int? _viewportHeight;
    private readonly bool _showCursor;
    private readonly int _selectionStartOffset;
    private readonly int _selectionEndOffset;

    /// <summary>
    /// Initialises a new <see cref="MarkdownEditorWidget"/>.
    /// </summary>
    /// <param name="buffer">The raw text buffer to render.</param>
    /// <param name="cursor">1-based (line, column) cursor position.</param>
    /// <param name="highlights">Syntax-highlight overlays expressed as character-offset spans.</param>
    /// <param name="topLineIndex">0-based index of the first line to display.</param>
    /// <param name="viewportHeight">Maximum number of lines to render; <see langword="null"/> renders all lines.</param>
    /// <param name="showCursor">When <see langword="false"/> the block-cursor cell is not rendered with invert decoration.</param>
    /// <param name="selectionStartOffset">0-based start offset of the selection, or <c>-1</c> when no selection is active.</param>
    /// <param name="selectionEndOffset">0-based exclusive end offset of the selection, or <c>-1</c> when no selection is active.</param>
    public MarkdownEditorWidget(
        string buffer,
        (int Line, int Column) cursor,
        IReadOnlyList<StyledSpan> highlights,
        int topLineIndex = 0,
        int? viewportHeight = null,
        bool showCursor = true,
        int selectionStartOffset = -1,
        int selectionEndOffset = -1)
    {
        _buffer = buffer;
        _cursor = cursor;
        _highlights = highlights;
        _topLineIndex = topLineIndex;
        _viewportHeight = viewportHeight;
        _showCursor = showCursor;
        _selectionStartOffset = selectionStartOffset;
        _selectionEndOffset = selectionEndOffset;
    }

    /// <inheritdoc/>
    public Measurement Measure(RenderOptions options, int maxWidth) =>
        new Measurement(0, maxWidth);

    /// <inheritdoc/>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        string[] lines = _buffer.Split('\n');

        int startLine = _topLineIndex;
        int endLine = _viewportHeight.HasValue
            ? Math.Min(startLine + _viewportHeight.Value, lines.Length)
            : lines.Length;

        startLine = Math.Clamp(startLine, 0, lines.Length);
        endLine = Math.Clamp(endLine, 0, lines.Length);

        bool firstLine = true;
        for (int lineIdx = startLine; lineIdx < endLine; lineIdx++)
        {
            if (!firstLine)
                yield return Segment.LineBreak;
            firstLine = false;

            string line = lines[lineIdx];
            int lineStartOffset = ComputeLineStartOffset(lines, lineIdx);
            bool isCursorLine = _showCursor && (lineIdx + 1) == _cursor.Line;
            int cursorColIndex = _cursor.Column - 1;

            // Clip to maxWidth: an over-wide line would wrap in the terminal, making the
            // frame taller than the viewport and scrolling earlier rows off-screen.
            int visibleLength = Math.Min(line.Length, maxWidth);

            int pos = 0;
            while (pos <= visibleLength)
            {
                bool isCursorPos = isCursorLine && pos == cursorColIndex;

                if (pos == visibleLength)
                {
                    if (isCursorPos && line.Length < maxWidth)
                        yield return new Segment(" ", Style.Plain.Decoration(Decoration.Invert));
                    break;
                }

                int offset = lineStartOffset + pos;
                Style charStyle = GetEffectiveStyle(offset, isCursorPos);

                int runEnd = pos + 1;
                while (runEnd < visibleLength)
                {
                    bool isRunCursorPos = isCursorLine && runEnd == cursorColIndex;
                    if (isRunCursorPos)
                        break;

                    Style nextStyle = GetEffectiveStyle(lineStartOffset + runEnd, false);
                    if (!StyleEquals(nextStyle, charStyle))
                        break;

                    runEnd++;
                }

                yield return new Segment(line.Substring(pos, runEnd - pos), charStyle);
                pos = runEnd;
            }
        }

        if (_viewportHeight.HasValue)
        {
            int padCount = _viewportHeight.Value - (endLine - startLine);
            for (int i = 0; i < padCount; i++)
            {
                if (!firstLine)
                    yield return Segment.LineBreak;
                firstLine = false;
                // Emit an empty-text segment so the widget never ends on a LineBreak.
                // This prevents composed renderables (e.g. VerticalLayout) from adding
                // a double line break when they insert their own separator.
                yield return new Segment(string.Empty);
            }
        }
    }

    private static int ComputeLineStartOffset(string[] lines, int lineIdx)
    {
        int offset = 0;
        for (int i = 0; i < lineIdx; i++)
            offset += lines[i].Length + 1;
        return offset;
    }

    /// <summary>
    /// Returns the effective style for a character cell, combining syntax highlighting,
    /// optional selection background, and the block-cursor invert (which always wins).
    /// </summary>
    private Style GetEffectiveStyle(int offset, bool isCursorPos)
    {
        if (isCursorPos)
            return Style.Plain.Decoration(Decoration.Invert | Decoration.Bold);

        Style style = GetStyleAtOffset(offset);

        // Apply reverse-video when offset falls within the selection range.
        if (_selectionStartOffset >= 0
            && offset >= _selectionStartOffset
            && offset < _selectionEndOffset)
        {
            style = new Style(style.Foreground, style.Background, style.Decoration | Decoration.Invert);
        }

        return style;
    }

    private Style GetStyleAtOffset(int offset)
    {
        Style result = Style.Plain;
        bool found = false;
        for (int i = 0; i < _highlights.Count; i++)
        {
            StyledSpan span = _highlights[i];
            if (offset >= span.StartOffset && offset < span.StartOffset + span.Length)
            {
                result = span.SpectreStyle;
                found = true;
            }
        }
        return found ? result : Style.Plain;
    }

    private static bool StyleEquals(Style a, Style b)
    {
        return a.Foreground == b.Foreground
            && a.Background == b.Background
            && a.Decoration == b.Decoration;
    }
}
