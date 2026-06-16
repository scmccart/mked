using Markdig.Extensions.Yaml;

namespace Mked.Controls;

/// <summary>Annotates the YAML front-matter block when one is present.</summary>
internal sealed class FrontMatterDimLayer : IHighlightLayer
{
    /// <inheritdoc/>
    public IEnumerable<HighlightSpan> Annotate(string source, MarkdownDocument document)
    {
        if (document.Count > 0 && document[0] is YamlFrontMatterBlock yaml)
        {
            CursorPosition start = BufferOperations.FromOffset(source, yaml.Span.Start);
            CursorPosition end = BufferOperations.FromOffset(source, yaml.Span.End + 1);
            yield return new HighlightSpan(new TextRange(start, end), HighlightKind.FrontmatterBlock);
        }
    }
}
