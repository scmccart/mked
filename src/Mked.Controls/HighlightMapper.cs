namespace Mked.Controls;

/// <summary>Converts <see cref="HighlightSpan"/> values to <see cref="StyledSpan"/> values.</summary>
internal static class HighlightMapper
{
    /// <summary>
    /// Maps each <see cref="HighlightSpan"/> in <paramref name="spans"/> to a <see cref="StyledSpan"/>
    /// using character offsets computed from <paramref name="buffer"/>.
    /// </summary>
    public static IReadOnlyList<StyledSpan> Map(IEnumerable<HighlightSpan> spans, string buffer)
    {
        var result = new List<StyledSpan>();
        foreach (HighlightSpan span in spans)
        {
            int startOffset = BufferOperations.ToOffset(buffer, span.Range.Start);
            int endOffset = BufferOperations.ToOffset(buffer, span.Range.End);
            int length = endOffset - startOffset;
            if (length <= 0)
                continue;
            Style style = MapKind(span.Kind);
            result.Add(new StyledSpan(startOffset, length, style));
        }
        return result;
    }

    private static Style MapKind(HighlightKind kind) => kind switch
    {
        HighlightKind.Heading => new Style(Color.Blue, decoration: Decoration.Bold),
        HighlightKind.Bold => new Style(decoration: Decoration.Bold),
        HighlightKind.Italic => new Style(decoration: Decoration.Italic),
        HighlightKind.InlineCode => new Style(Color.Grey),
        HighlightKind.LinkText => new Style(Color.Cyan1),
        HighlightKind.LinkUrl => new Style(Color.Grey, decoration: Decoration.Underline),
        HighlightKind.FrontmatterBlock => new Style(Color.Grey),
        HighlightKind.CodeFence => new Style(Color.Grey),
        _ => Style.Plain,
    };
}
