using Markdig;
using Markdig.Extensions.Yaml;

namespace Mked.Controls;

/// <summary>Standalone scrollable Markdown rendering widget.</summary>
public sealed record class MarkdownViewer(string Markdown) : IRenderable
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseYamlFrontMatter()
        .Build();

    // Parsed once at construction; shared (by reference) across with-copies.
    private readonly Markdig.Syntax.MarkdownDocument _ast =
        Markdig.Markdown.Parse(Markdown, Pipeline);

    // Mutable holder shared across with-copies so the render cache is computed once.
    private readonly RenderStateHolder _state = new();

    /// <summary>Show the raw YAML front matter block above the document body.</summary>
    public bool ShowFrontmatter { get; init; }

    /// <summary>Render link text only, omitting URLs.</summary>
    public bool PlainLinks { get; init; }

    /// <summary>
    /// 0-based index of the first terminal line to display.
    /// Defaults to 0 (document top). Clamped to [0, <see cref="MarkdownViewerScrollInfo.TotalLineCount"/> - <see cref="ViewportHeight"/>].
    /// </summary>
    public int TopLineIndex { get; init; }

    /// <summary>
    /// Maximum number of terminal rows to render.
    /// <see langword="null"/> emits the entire document.
    /// </summary>
    public int? ViewportHeight { get; init; }

    /// <summary>Total top-level block count (excludes parser-internal housekeeping blocks).</summary>
    public int BlockCount => _ast.Count(b =>
        b is not Markdig.Syntax.BlankLineBlock
        and not Markdig.Syntax.LinkReferenceDefinitionGroup
        && (ShowFrontmatter || b is not YamlFrontMatterBlock));

    /// <summary>
    /// Scroll metadata computed on first <see cref="Render"/> or <see cref="Measure"/> call.
    /// Returns <see cref="MarkdownViewerScrollInfo.Empty"/> until then.
    /// Width-dependent; create a new instance when terminal width changes.
    /// </summary>
    public MarkdownViewerScrollInfo ScrollInfo => _state.ScrollInfo ?? MarkdownViewerScrollInfo.Empty;

    /// <inheritdoc/>
    public Measurement Measure(RenderOptions options, int maxWidth)
    {
        _state.EnsureCache(this, options, maxWidth);
        return new Measurement(0, maxWidth);
    }

    /// <inheritdoc/>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var (lines, _) = _state.EnsureCache(this, options, maxWidth);

        int maxStart = ViewportHeight.HasValue
            ? Math.Max(0, lines.Count - ViewportHeight.Value)
            : Math.Max(0, lines.Count - 1);
        int startLine = Math.Clamp(TopLineIndex, 0, maxStart);

        int endLine = ViewportHeight.HasValue
            ? Math.Min(startLine + ViewportHeight.Value, lines.Count)
            : lines.Count;

        for (int i = startLine; i < endLine; i++)
        {
            foreach (var seg in lines[i])
                yield return seg;
            if (i < endLine - 1)
                yield return Segment.LineBreak;
        }
    }

    private sealed class RenderStateHolder
    {
        private (List<List<Segment>> Lines, MarkdownViewerScrollInfo Info)? _cached;
        private (int Width, bool ShowFrontmatter, bool PlainLinks) _cacheKey;

        public MarkdownViewerScrollInfo? ScrollInfo => _cached?.Info;

        public (List<List<Segment>> Lines, MarkdownViewerScrollInfo Info) EnsureCache(
            MarkdownViewer viewer, RenderOptions options, int maxWidth)
        {
            var key = (maxWidth, viewer.ShowFrontmatter, viewer.PlainLinks);
            if (_cached is null || _cacheKey != key)
            {
                _cached = new MarkdownBlockRenderer(viewer.ShowFrontmatter, viewer.PlainLinks)
                    .Render(viewer._ast, options, maxWidth);
                _cacheKey = key;
            }
            return _cached.Value;
        }
    }
}
