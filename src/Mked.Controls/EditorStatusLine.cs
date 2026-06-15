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
        var bar = Style.Plain;

        var posText  = $"  Ln {_cursor.Line}, Col {_cursor.Column}   ";
        var dotText  = "●";
        var wordText = $"  {_wordCount} words  ";

        yield return new Segment(posText, bar);

        // Only render the dirty marker when there are unsaved changes.
        int used = posText.Length + wordText.Length;
        if (_isDirty)
        {
            yield return new Segment(dotText, bar);
            used += dotText.Length;
        }

        yield return new Segment(wordText, bar);

        // Fill the rest of the row so the background spans the full width.
        int pad = Math.Max(0, maxWidth - used);
        if (pad > 0)
            yield return new Segment(new string(' ', pad), bar);
    }
}
