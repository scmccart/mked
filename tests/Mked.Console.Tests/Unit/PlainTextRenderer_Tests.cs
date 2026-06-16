namespace Mked.Console.Tests.Unit;

public sealed class PlainTextRenderer_Tests
{
    [Fact]
    public async Task NoFrontmatter_WritesSourceUnchanged()
    {
        const string source = "# Hello\n\nSome text.\n";
        var writer = new StringWriter();

        await PlainTextRenderer.RenderAsync(source, showFrontmatter: false, writer);

        writer.ToString().Should().Be(source);
    }

    [Fact]
    public async Task WithFrontmatter_ShowFrontmatterFalse_StripsYamlBlock()
    {
        const string source = "---\ntitle: Test\n---\n# Hello\n";
        var writer = new StringWriter();

        await PlainTextRenderer.RenderAsync(source, showFrontmatter: false, writer);

        writer.ToString().Should().Be("# Hello\n");
    }

    [Fact]
    public async Task WithFrontmatter_ShowFrontmatterTrue_KeepsFrontmatter()
    {
        const string source = "---\ntitle: Test\n---\n# Hello\n";
        var writer = new StringWriter();

        await PlainTextRenderer.RenderAsync(source, showFrontmatter: true, writer);

        writer.ToString().Should().Be(source);
    }

    [Fact]
    public async Task EmptySource_WritesEmpty()
    {
        var writer = new StringWriter();

        await PlainTextRenderer.RenderAsync(string.Empty, showFrontmatter: false, writer);

        writer.ToString().Should().BeEmpty();
    }
}
