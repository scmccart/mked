namespace Mked.Controls;

/// <summary>Single-line status bar displaying cursor position, dirty state, and word count.</summary>
public sealed class EditorStatusLine : IRenderable
{
    private readonly (int Line, int Column) _cursor;
    private readonly bool _isDirty;
    private readonly int _wordCount;

    /// <summary>
    /// Initialises a new <see cref="EditorStatusLine"/>.
    /// </summary>
    /// <param name="cursor">1-based (line, column) cursor position.</param>
    /// <param name="isDirty">Whether the buffer has unsaved changes.</param>
    /// <param name="wordCount">Number of words in the buffer.</param>
    public EditorStatusLine(
        (int Line, int Column) cursor,
        bool isDirty,
        int wordCount)
    {
        _cursor = cursor;
        _isDirty = isDirty;
        _wordCount = wordCount;
    }

    /// <inheritdoc/>
    public Measurement Measure(RenderOptions options, int maxWidth) =>
        new Measurement(0, maxWidth);

    /// <inheritdoc/>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        yield return new Segment($"  Ln {_cursor.Line}, Col {_cursor.Column}   ", Style.Plain);

        Style dotStyle = _isDirty
            ? new Style(foreground: Color.Yellow)
            : new Style(foreground: Color.Grey);
        yield return new Segment("●", dotStyle);

        yield return new Segment($"  {_wordCount} words  ", Style.Plain);
    }
}
