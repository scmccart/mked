using Markdig.Syntax.Inlines;

namespace Mked.Controls;

/// <summary>Annotates bold and italic emphasis spans.</summary>
internal sealed class EmphasisHighlightLayer : IHighlightLayer
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
            if (inline is EmphasisInline emphasis)
            {
                HighlightKind kind = emphasis.DelimiterCount == 2 ? HighlightKind.Bold : HighlightKind.Italic;
                CursorPosition start = BufferOperations.FromOffset(source, emphasis.Span.Start);
                CursorPosition end = BufferOperations.FromOffset(source, emphasis.Span.End + 1);
                yield return new HighlightSpan(new TextRange(start, end), kind);
            }

            if (inline is ContainerInline childContainer)
            {
                foreach (HighlightSpan span in WalkInlines(source, childContainer))
                    yield return span;
            }
        }
    }
}
