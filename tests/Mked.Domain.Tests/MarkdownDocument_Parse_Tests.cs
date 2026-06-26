namespace Mked.Domain.Tests;

public class MarkdownDocument_Parse_Tests
{
    [Fact]
    public void NullSource_ThrowsArgumentNullException()
    {
        Action act = () => MarkdownDocument.Parse(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EmptyString_ReturnsEmptyDocument()
    {
        var doc = MarkdownDocument.Parse(string.Empty);

        doc.IsEmpty.Should().BeTrue();
        doc.Blocks.Should().BeEmpty();
    }

    [Fact]
    public void Heading_IsNotEmpty()
    {
        var doc = MarkdownDocument.Parse("# Hello");

        doc.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void MultipleBlocks_ExposesMoreThanOneBlock()
    {
        var doc = MarkdownDocument.Parse("# Title\n\nParagraph text.");

        doc.Blocks.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public void YamlFrontMatter_FrontmatterIsSome()
    {
        var source = "---\ntitle: Test\n---\n\n# Body";
        var doc = MarkdownDocument.Parse(source);

        doc.Frontmatter.IsSome.Should().BeTrue();
    }

    [Fact]
    public void YamlFrontMatter_FrontmatterContainsYamlText()
    {
        var source = "---\ntitle: Test\n---\n\n# Body";
        var doc = MarkdownDocument.Parse(source);

        var text = doc.Frontmatter.UnwrapOr(string.Empty);
        text.Should().Contain("title: Test");
    }

    [Fact]
    public void NoFrontMatter_FrontmatterIsNone()
    {
        var doc = MarkdownDocument.Parse("# No front matter here");

        doc.Frontmatter.IsNone.Should().BeTrue();
    }

    [Fact]
    public void Parse_StoresOriginalSource()
    {
        MarkdownDocument.Parse("# Hello").Source.Should().Be("# Hello");
    }

    [Fact]
    public void EmptyDocument_FrontmatterIsNone()
    {
        var doc = MarkdownDocument.Parse(string.Empty);

        doc.Frontmatter.IsNone.Should().BeTrue();
    }
}
