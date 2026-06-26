namespace Mked.Console;

/// <summary>
/// Writes a Markdown document to a <see cref="TextWriter"/> as plain text — no pager, no ANSI codes.
/// Used when <see cref="RendererSelector.IsPlainMode(ViewSettings)"/> returns <see langword="true"/>.
/// </summary>
public static class PlainTextRenderer
{
    /// <summary>
    /// Pipeline that parses YAML frontmatter as a discrete block.
    /// <c>Markdown.ToPlainText</c> has no renderer for <c>YamlFrontMatterBlock</c>,
    /// so the block is silently omitted from output — giving the expected hide-frontmatter behaviour.
    /// </summary>
    private static readonly MarkdownPipeline PipelineHideFrontmatter =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().UseYamlFrontMatter().Build();

    /// <summary>
    /// Pipeline without the YAML extension — frontmatter is treated as regular content
    /// and its text is emitted by the plain-text pass.
    /// </summary>
    private static readonly MarkdownPipeline PipelineShowFrontmatter =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    /// <summary>
    /// Strips all Markdown syntax from <paramref name="source"/> and writes the resulting
    /// plain text to <paramref name="output"/>.
    /// YAML frontmatter is omitted unless <paramref name="showFrontmatter"/> is <see langword="true"/>.
    /// </summary>
    public static Task RenderAsync(string source, bool showFrontmatter, TextWriter output)
    {
        var pipeline = showFrontmatter ? PipelineShowFrontmatter : PipelineHideFrontmatter;
        var text = Markdown.ToPlainText(source, pipeline);
        return output.WriteAsync(text);
    }
}
