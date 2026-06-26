namespace Mked.Console.Tests.Unit;

public sealed class PlainTextRenderer_Tests
{
    // ─── Core stripping behaviour ─────────────────────────────────────────────

    [Fact]
    public async Task Heading_IsRenderedAsPlainText()
    {
        const string source = "# Hello\n\nSome text.\n";
        var writer = new StringWriter();

        await PlainTextRenderer.RenderAsync(source, showFrontmatter: false, writer);

        writer.ToString().Should().Be("Hello\nSome text.\n");
    }

    [Fact]
    public async Task EmptySource_WritesEmpty()
    {
        var writer = new StringWriter();

        await PlainTextRenderer.RenderAsync(string.Empty, showFrontmatter: false, writer);

        writer.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task BoldAndItalic_AreStripped()
    {
        const string source = "**bold** and *italic*\n";
        var writer = new StringWriter();

        await PlainTextRenderer.RenderAsync(source, showFrontmatter: false, writer);

        writer.ToString().Should().Be("bold and italic\n");
    }

    [Fact]
    public async Task Link_KeepsTextDropsUrl()
    {
        const string source = "[click here](https://example.com)\n";
        var writer = new StringWriter();

        await PlainTextRenderer.RenderAsync(source, showFrontmatter: false, writer);

        writer.ToString().Should().Be("click here\n");
    }

    [Fact]
    public async Task InlineCode_IsStripped()
    {
        const string source = "Use `var` for local variables.\n";
        var writer = new StringWriter();

        await PlainTextRenderer.RenderAsync(source, showFrontmatter: false, writer);

        writer.ToString().Should().Be("Use var for local variables.\n");
    }

    [Fact]
    public async Task FencedCodeBlock_RetainsCodeContent()
    {
        const string source = "```csharp\nint x = 1;\n```\n";
        var writer = new StringWriter();

        await PlainTextRenderer.RenderAsync(source, showFrontmatter: false, writer);

        writer.ToString().Should().Contain("int x = 1;");
        writer.ToString().Should().NotContain("```");
    }

    [Fact]
    public async Task UnorderedList_StripsMarkers()
    {
        const string source = "- Alpha\n- Beta\n- Gamma\n";
        var writer = new StringWriter();

        await PlainTextRenderer.RenderAsync(source, showFrontmatter: false, writer);

        var result = writer.ToString();
        result.Should().Contain("Alpha");
        result.Should().Contain("Beta");
        result.Should().Contain("Gamma");
        result.Should().NotContain("- ");
    }

    // ─── Frontmatter handling ─────────────────────────────────────────────────

    [Fact]
    public async Task WithFrontmatter_ShowFrontmatterFalse_DropsFrontmatter()
    {
        const string source = "---\ntitle: Test\n---\n# Hello\n";
        var writer = new StringWriter();

        await PlainTextRenderer.RenderAsync(source, showFrontmatter: false, writer);

        writer.ToString().Should().Be("Hello\n");
    }

    [Fact]
    public async Task WithFrontmatter_ShowFrontmatterTrue_IncludesFrontmatterAsText()
    {
        const string source = "---\ntitle: Test\n---\n# Hello\n";
        var writer = new StringWriter();

        await PlainTextRenderer.RenderAsync(source, showFrontmatter: true, writer);

        writer.ToString().Should().Be("title: Test\nHello\n");
    }
}
