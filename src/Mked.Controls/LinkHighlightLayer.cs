using Markdig.Syntax.Inlines;

namespace Mked.Controls;

/// <summary>Annotates link text and URL spans.</summary>
public sealed class LinkHighlightLayer : IHighlightLayer
{
    /// <inheritdoc/>
    public IEnumerable<HighlightSpan> Annotate(string source, MarkdownDocument document)
    {
        foreach (Block block in document)
        {
            foreach (HighlightSpan span in WalkBlock(source, block))
                yield return span;
        }
    }

    private static IEnumerable<HighlightSpan> WalkBlock(string source, Block block)
    {
        if (block is LeafBlock leaf && leaf.Inline is not null)
        {
            foreach (HighlightSpan span in WalkInlines(source, leaf.Inline))
                yield return span;
        }
        else if (block is ContainerBlock container)
        {
            foreach (Block child in container)
            {
                foreach (HighlightSpan span in WalkBlock(source, child))
                    yield return span;
            }
        }
    }

    private static IEnumerable<HighlightSpan> WalkInlines(string source, ContainerInline container)
    {
        foreach (Inline inline in container)
        {
            if (inline is LinkInline link)
            {
                int spanStart = link.Span.Start;
                int spanEnd = link.Span.End;

                // Find the '](' boundary to split [text] from (url).
                int boundary = source.IndexOf("](", spanStart, spanEnd - spanStart + 1,
                    StringComparison.Ordinal);

                if (boundary >= 0)
                {
                    // [text] portion: spanStart .. boundary (inclusive of ']')
                    CursorPosition textStart = BufferOperations.FromOffset(source, spanStart);
                    CursorPosition textEnd = BufferOperations.FromOffset(source, boundary + 1);
                    yield return new HighlightSpan(new TextRange(textStart, textEnd), HighlightKind.LinkText);

                    // (url) portion: boundary+1 .. spanEnd+1 (exclusive)
                    CursorPosition urlStart = BufferOperations.FromOffset(source, boundary + 1);
                    CursorPosition urlEnd = BufferOperations.FromOffset(source, spanEnd + 1);
                    yield return new HighlightSpan(new TextRange(urlStart, urlEnd), HighlightKind.LinkUrl);
                }
                else
                {
                    // Fallback: annotate the whole span as link text
                    CursorPosition start = BufferOperations.FromOffset(source, spanStart);
                    CursorPosition end = BufferOperations.FromOffset(source, spanEnd + 1);
                    yield return new HighlightSpan(new TextRange(start, end), HighlightKind.LinkText);
                }
            }

            if (inline is ContainerInline childContainer && inline is not LinkInline)
            {
                foreach (HighlightSpan span in WalkInlines(source, childContainer))
                    yield return span;
            }
        }
    }
}
