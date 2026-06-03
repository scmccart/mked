using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;

namespace Mked.Domain;

/// <summary>
/// A parsed Markdown document. Wraps the Markdig AST so the rest of the codebase
/// never takes a direct dependency on Markdig outside this boundary.
/// </summary>
public sealed class MarkdownDocument
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseYamlFrontMatter()
        .Build();

    private readonly Markdig.Syntax.MarkdownDocument _ast;
    private readonly string _source;

    private MarkdownDocument(string source, Markdig.Syntax.MarkdownDocument ast)
    {
        _source = source;
        _ast = ast;
    }

    /// <summary>
    /// Parses <paramref name="source"/> and returns a <see cref="MarkdownDocument"/>.
    /// Markdig is lenient; malformed input degrades gracefully rather than throwing.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source"/> is <see langword="null"/>.
    /// </exception>
    public static MarkdownDocument Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new MarkdownDocument(source, Markdig.Markdown.Parse(source, Pipeline));
    }

    /// <summary>The original Markdown source text passed to <see cref="Parse"/>.</summary>
    public string Source => _source;

    /// <summary>Returns <see langword="true"/> when the document contains no top-level blocks.</summary>
    public bool IsEmpty => _ast.Count == 0;

    /// <summary>The top-level AST blocks in document order.</summary>
    public IReadOnlyList<Block> Blocks => _ast;

    /// <summary>
    /// Raw YAML front matter text when a YAML block is the first block in the document;
    /// <see cref="Maybe{T}.None"/> otherwise.
    /// </summary>
    public Maybe<string> Frontmatter
    {
        get
        {
            if (_ast.Count > 0 && _ast[0] is YamlFrontMatterBlock yaml)
                return Maybe.Some(yaml.Lines.ToString());
            return Maybe.None<string>();
        }
    }
}
