namespace Mked.Controls;

/// <summary>
/// A single syntax-highlighting pass over a parsed Markdown document.
/// Implementations are stateless and may be called on any thread.
/// </summary>
public interface IHighlightLayer
{
    /// <summary>
    /// Returns all <see cref="HighlightSpan"/> values produced by this layer for
    /// the given <paramref name="source"/> and its pre-parsed <paramref name="document"/>.
    /// </summary>
    public IEnumerable<HighlightSpan> Annotate(string source, MarkdownDocument document);
}
