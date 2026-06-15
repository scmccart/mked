namespace Mked.Controls;

/// <summary>Annotates ATX heading markers and heading text.</summary>
public sealed class HeadingHighlightLayer : IHighlightLayer
{
    /// <inheritdoc/>
    public IEnumerable<HighlightSpan> Annotate(string source, MarkdownDocument document)
    {
        foreach (Block block in document)
        {
            if (block is not HeadingBlock heading)
                continue;

            int blockStart = heading.Span.Start;
            int blockEnd = heading.Span.End;

            // The '#' markers occupy (heading.Level) characters starting at blockStart.
            int markerEnd = blockStart + heading.Level; // exclusive, points at the space after '#'s

            CursorPosition markerStartPos = BufferOperations.FromOffset(source, blockStart);
            CursorPosition markerEndPos = BufferOperations.FromOffset(source, markerEnd);
            yield return new HighlightSpan(new TextRange(markerStartPos, markerEndPos), HighlightKind.Heading);

            // Skip past the '#' chars and the following space (if any) to find the text start.
            int textStart = markerEnd;
            while (textStart <= blockEnd && textStart < source.Length && source[textStart] == ' ')
                textStart++;

            if (textStart <= blockEnd)
            {
                CursorPosition textStartPos = BufferOperations.FromOffset(source, textStart);
                CursorPosition textEndPos = BufferOperations.FromOffset(source, blockEnd + 1);
                yield return new HighlightSpan(new TextRange(textStartPos, textEndPos), HighlightKind.Heading);
            }
        }
    }
}
