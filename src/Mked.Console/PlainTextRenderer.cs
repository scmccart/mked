using System.Text.RegularExpressions;

namespace Mked.Console;

/// <summary>
/// Writes a Markdown document to a <see cref="TextWriter"/> as plain text — no pager, no ANSI codes.
/// Used when <see cref="RendererSelector.IsPlainMode(ViewSettings)"/> returns <see langword="true"/>.
/// </summary>
public static partial class PlainTextRenderer
{
    /// <summary>
    /// Writes <paramref name="source"/> to <paramref name="output"/>.
    /// YAML frontmatter is omitted unless <paramref name="showFrontmatter"/> is <see langword="true"/>.
    /// </summary>
    public static Task RenderAsync(string source, bool showFrontmatter, TextWriter output)
    {
        var text = showFrontmatter ? source : StripFrontmatter(source);
        return output.WriteAsync(text);
    }

    [GeneratedRegex(@"^---\r?\n.*?\r?\n---\r?\n", RegexOptions.Singleline)]
    private static partial Regex FrontmatterPattern();

    private static string StripFrontmatter(string source) =>
        FrontmatterPattern().Replace(source, string.Empty);
}
