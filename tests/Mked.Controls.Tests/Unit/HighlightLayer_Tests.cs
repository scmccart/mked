using Markdig;
using Markdig.Syntax;

namespace Mked.Controls.Tests;

public class HighlightLayer_Tests
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseYamlFrontMatter()
        .Build();

    private static MarkdownDocument Parse(string source) => Markdown.Parse(source, Pipeline);

    // ── HeadingHighlightLayer ────────────────────────────────────────────────

    [Fact]
    public void HeadingLayer_H1_ProducesTwoSpans()
    {
        const string source = "# Hello";
        var doc = Parse(source);
        var layer = new HeadingHighlightLayer();

        IEnumerable<HighlightSpan> spans = layer.Annotate(source, doc);

        spans.Should().HaveCount(2);
    }

    [Fact]
    public void HeadingLayer_H1_BothSpansHaveHeadingKind()
    {
        const string source = "# Hello";
        var doc = Parse(source);
        var layer = new HeadingHighlightLayer();

        IEnumerable<HighlightSpan> spans = layer.Annotate(source, doc);

        spans.Should().AllSatisfy(s => s.Kind.Should().Be(HighlightKind.Heading));
    }

    [Fact]
    public void HeadingLayer_H2_MarkerSpanStartsAtCol1()
    {
        const string source = "## World";
        var doc = Parse(source);
        var layer = new HeadingHighlightLayer();

        HighlightSpan first = layer.Annotate(source, doc).First();

        first.Range.Start.Should().Be(new CursorPosition(1, 1));
    }

    [Fact]
    public void HeadingLayer_NoHeading_ReturnsEmpty()
    {
        const string source = "Just a paragraph.";
        var doc = Parse(source);
        var layer = new HeadingHighlightLayer();

        layer.Annotate(source, doc).Should().BeEmpty();
    }

    [Fact]
    public void HeadingLayer_MultipleHeadings_ProducesSpansForEach()
    {
        const string source = "# One\n\n## Two";
        var doc = Parse(source);
        var layer = new HeadingHighlightLayer();

        IEnumerable<HighlightSpan> spans = layer.Annotate(source, doc);

        // 2 spans per heading × 2 headings = 4
        spans.Should().HaveCount(4);
    }

    // ── EmphasisHighlightLayer ────────────────────────────────────────────────

    [Fact]
    public void EmphasisLayer_Bold_ProducesBoldSpan()
    {
        const string source = "**bold**";
        var doc = Parse(source);
        var layer = new EmphasisHighlightLayer();

        IEnumerable<HighlightSpan> spans = layer.Annotate(source, doc);

        spans.Should().ContainSingle(s => s.Kind == HighlightKind.Bold);
    }

    [Fact]
    public void EmphasisLayer_Italic_ProducesItalicSpan()
    {
        const string source = "*italic*";
        var doc = Parse(source);
        var layer = new EmphasisHighlightLayer();

        IEnumerable<HighlightSpan> spans = layer.Annotate(source, doc);

        spans.Should().ContainSingle(s => s.Kind == HighlightKind.Italic);
    }

    [Fact]
    public void EmphasisLayer_BoldSpan_CoversFullDelimiters()
    {
        // "**bold**" is 8 chars: offset 0..7 → positions (1,1)..(1,9)
        const string source = "**bold**";
        var doc = Parse(source);
        var layer = new EmphasisHighlightLayer();

        HighlightSpan span = layer.Annotate(source, doc).Single(s => s.Kind == HighlightKind.Bold);

        span.Range.Start.Should().Be(new CursorPosition(1, 1));
        span.Range.End.Should().Be(new CursorPosition(1, 9));
    }

    [Fact]
    public void EmphasisLayer_NoParagraph_ReturnsEmpty()
    {
        const string source = "# Heading only";
        var doc = Parse(source);
        var layer = new EmphasisHighlightLayer();

        layer.Annotate(source, doc).Should().BeEmpty();
    }

    [Fact]
    public void EmphasisLayer_BoldAndItalic_ProducesBothKinds()
    {
        const string source = "**bold** and *italic*";
        var doc = Parse(source);
        var layer = new EmphasisHighlightLayer();

        IEnumerable<HighlightSpan> spans = layer.Annotate(source, doc);

        spans.Should().Contain(s => s.Kind == HighlightKind.Bold);
        spans.Should().Contain(s => s.Kind == HighlightKind.Italic);
    }

    // ── LinkHighlightLayer ────────────────────────────────────────────────────

    [Fact]
    public void LinkLayer_SimpleLink_ProducesLinkTextAndLinkUrl()
    {
        const string source = "[click here](https://example.com)";
        var doc = Parse(source);
        var layer = new LinkHighlightLayer();

        IEnumerable<HighlightSpan> spans = layer.Annotate(source, doc);

        spans.Should().Contain(s => s.Kind == HighlightKind.LinkText);
        spans.Should().Contain(s => s.Kind == HighlightKind.LinkUrl);
    }

    [Fact]
    public void LinkLayer_NoLink_ReturnsEmpty()
    {
        const string source = "Just plain text.";
        var doc = Parse(source);
        var layer = new LinkHighlightLayer();

        layer.Annotate(source, doc).Should().BeEmpty();
    }

    [Fact]
    public void LinkLayer_LinkTextSpan_StartsAtFirstChar()
    {
        const string source = "[hi](https://x.com)";
        var doc = Parse(source);
        var layer = new LinkHighlightLayer();

        HighlightSpan textSpan = layer.Annotate(source, doc).First(s => s.Kind == HighlightKind.LinkText);

        textSpan.Range.Start.Should().Be(new CursorPosition(1, 1));
    }

    // ── FrontMatterDimLayer ───────────────────────────────────────────────────

    [Fact]
    public void FrontMatterLayer_WithFrontMatter_ProducesOneSpan()
    {
        const string source = "---\ntitle: Test\n---\n\n# Body";
        var doc = Parse(source);
        var layer = new FrontMatterDimLayer();

        IEnumerable<HighlightSpan> spans = layer.Annotate(source, doc);

        spans.Should().ContainSingle(s => s.Kind == HighlightKind.FrontmatterBlock);
    }

    [Fact]
    public void FrontMatterLayer_WithFrontMatter_SpanStartsAtOrigin()
    {
        const string source = "---\ntitle: Test\n---\n\n# Body";
        var doc = Parse(source);
        var layer = new FrontMatterDimLayer();

        HighlightSpan span = layer.Annotate(source, doc).Single();

        span.Range.Start.Should().Be(new CursorPosition(1, 1));
    }

    [Fact]
    public void FrontMatterLayer_NoFrontMatter_ReturnsEmpty()
    {
        const string source = "# Just a heading";
        var doc = Parse(source);
        var layer = new FrontMatterDimLayer();

        layer.Annotate(source, doc).Should().BeEmpty();
    }

    // ── CodeFenceLayer ────────────────────────────────────────────────────────

    [Fact]
    public void CodeFenceLayer_FencedBlock_ProducesOneSpan()
    {
        const string source = "```csharp\nvar x = 1;\n```";
        var doc = Parse(source);
        var layer = new CodeFenceLayer();

        IEnumerable<HighlightSpan> spans = layer.Annotate(source, doc);

        spans.Should().ContainSingle(s => s.Kind == HighlightKind.CodeFence);
    }

    [Fact]
    public void CodeFenceLayer_FencedBlock_SpanStartsAtOrigin()
    {
        const string source = "```\ncode\n```";
        var doc = Parse(source);
        var layer = new CodeFenceLayer();

        HighlightSpan span = layer.Annotate(source, doc).Single();

        span.Range.Start.Should().Be(new CursorPosition(1, 1));
    }

    [Fact]
    public void CodeFenceLayer_NoFence_ReturnsEmpty()
    {
        const string source = "Just text.";
        var doc = Parse(source);
        var layer = new CodeFenceLayer();

        layer.Annotate(source, doc).Should().BeEmpty();
    }

    [Fact]
    public void CodeFenceLayer_MultipleFences_ProducesSpanPerFence()
    {
        const string source = "```\nfirst\n```\n\n```\nsecond\n```";
        var doc = Parse(source);
        var layer = new CodeFenceLayer();

        IEnumerable<HighlightSpan> spans = layer.Annotate(source, doc);

        spans.Should().HaveCount(2);
    }
}
