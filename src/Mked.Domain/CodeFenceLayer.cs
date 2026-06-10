using Markdig.Syntax;

namespace Mked.Domain;

/// <summary>Annotates fenced code blocks.</summary>
public sealed class CodeFenceLayer : IHighlightLayer
{
    /// <inheritdoc/>
    public IEnumerable<HighlightSpan> Annotate(string source, MarkdownDocument document)
    {
        foreach (Block block in document.Blocks)
        {
            if (block is FencedCodeBlock fenced)
            {
                CursorPosition start = BufferOperations.FromOffset(source, fenced.Span.Start);
                CursorPosition end = BufferOperations.FromOffset(source, fenced.Span.End + 1);
                yield return new HighlightSpan(new TextRange(start, end), HighlightKind.CodeFence);
            }
        }
    }
}
