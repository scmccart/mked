namespace Mked.Controls.Tests.Unit;

public sealed class MarkdownViewer_Render_Tests
{
    private static string Write(string markdown, Action<MarkdownViewer>? configure = null)
    {
        var console = new TestConsole().Width(80);
        var viewer = new MarkdownViewer(markdown);
        if (configure is not null)
        {
            var temp = viewer;
            configure(temp);
            viewer = temp;
        }
        console.Write(viewer);
        return console.Output;
    }

    private static List<Segment> GetSegments(string markdown, Action<MarkdownViewer>? configure = null)
    {
        var console = new TestConsole().Width(80);
        var viewer = new MarkdownViewer(markdown);
        if (configure is not null)
        {
            var temp = viewer;
            configure(temp);
            viewer = temp;
        }
        return viewer.GetSegments(console).ToList();
    }

    // ─── Headings ─────────────────────────────────────────────────────────────

    [Fact]
    public void H1_OutputContainsText()
    {
        Write("# Hello").Should().Contain("Hello");
    }

    [Fact]
    public void H1_SegmentIsBoldBlue()
    {
        var segs = GetSegments("# Hello");
        segs.Should().Contain(s =>
            s.Style.Decoration.HasFlag(Decoration.Bold) &&
            s.Style.Foreground == Color.Blue);
    }

    [Fact]
    public void H2_SegmentIsGreen()
    {
        var segs = GetSegments("## Hello");
        segs.Should().Contain(s => s.Style.Foreground == Color.Green);
    }

    [Fact]
    public void H3_SegmentIsYellow()
    {
        var segs = GetSegments("### Hello");
        segs.Should().Contain(s => s.Style.Foreground == Color.Yellow);
    }

    [Fact]
    public void H4_SegmentIsGrey()
    {
        var segs = GetSegments("#### Hello");
        segs.Should().Contain(s => s.Style.Foreground == Color.Grey);
    }

    // ─── Inline styles ────────────────────────────────────────────────────────

    [Fact]
    public void Bold_SegmentHasBoldDecoration()
    {
        var segs = GetSegments("**bold**");
        segs.Should().Contain(s => s.Style.Decoration.HasFlag(Decoration.Bold));
    }

    [Fact]
    public void Italic_SegmentHasItalicDecoration()
    {
        var segs = GetSegments("*italic*");
        segs.Should().Contain(s => s.Style.Decoration.HasFlag(Decoration.Italic));
    }

    [Fact]
    public void InlineCode_SegmentHasDimDecoration()
    {
        var segs = GetSegments("`code`");
        segs.Should().Contain(s => s.Style.Decoration.HasFlag(Decoration.Dim));
    }

    // ─── Code blocks ──────────────────────────────────────────────────────────

    [Fact]
    public void FencedCodeBlock_OutputContainsCode()
    {
        Write("```\nvar x = 1;\n```").Should().Contain("var x = 1;");
    }

    [Fact]
    public void FencedCodeBlock_SegmentsAreDim()
    {
        var segs = GetSegments("```\nvar x = 1;\n```");
        segs.Where(s => s.Text.Contains("var x")).Should().NotBeEmpty();
        segs.Where(s => s.Text.Contains("var x")).Should()
            .AllSatisfy(s => s.Style.Decoration.HasFlag(Decoration.Dim).Should().BeTrue());
    }

    [Fact]
    public void FencedCodeBlock_LinesAreIndented()
    {
        Write("```\nhello\n```").Should().Contain("  hello");
    }

    // ─── Blockquote ───────────────────────────────────────────────────────────

    [Fact]
    public void Blockquote_OutputContainsVerticalBar()
    {
        Write("> quoted text").Should().Contain("│");
    }

    [Fact]
    public void Blockquote_OutputContainsContent()
    {
        Write("> quoted text").Should().Contain("quoted text");
    }

    [Fact]
    public void Blockquote_PrefixIsDim()
    {
        var segs = GetSegments("> quoted");
        segs.Should().Contain(s => s.Text.Contains('│') && s.Style.Decoration.HasFlag(Decoration.Dim));
    }

    // ─── Lists ────────────────────────────────────────────────────────────────

    [Fact]
    public void UnorderedList_OutputContainsBullet()
    {
        Write("- item one").Should().Contain("•");
    }

    [Fact]
    public void UnorderedList_OutputContainsContent()
    {
        Write("- item one").Should().Contain("item one");
    }

    [Fact]
    public void UnorderedList_NestedItemUsesCircle()
    {
        Write("- top\n  - nested").Should().Contain("◦");
    }

    [Fact]
    public void OrderedList_OutputContainsNumbers()
    {
        var output = Write("1. first\n2. second");
        output.Should().Contain("1.");
        output.Should().Contain("2.");
    }

    // ─── Links ────────────────────────────────────────────────────────────────

    [Fact]
    public void Link_DefaultMode_OutputContainsTextAndUrl()
    {
        var output = Write("[text](https://example.com)");
        output.Should().Contain("text");
        output.Should().Contain("https://example.com");
    }

    [Fact]
    public void Link_PlainLinks_UrlAbsent()
    {
        var console = new TestConsole().Width(80);
        var viewer = new MarkdownViewer("[text](https://example.com)") { PlainLinks = true };
        console.Write(viewer);
        console.Output.Should().Contain("text");
        console.Output.Should().NotContain("https://example.com");
    }

    // ─── Horizontal rule ──────────────────────────────────────────────────────

    [Fact]
    public void ThematicBreak_OutputIsNotEmpty()
    {
        Write("---").Should().NotBeEmpty();
    }

    // ─── Table ────────────────────────────────────────────────────────────────

    [Fact]
    public void Table_OutputContainsHeaders()
    {
        var output = Write("| A | B |\n|---|---|\n| 1 | 2 |");
        output.Should().Contain("A");
        output.Should().Contain("B");
    }

    [Fact]
    public void Table_OutputContainsData()
    {
        Write("| A | B |\n|---|---|\n| 1 | 2 |").Should().Contain("1");
    }

    [Fact]
    public void Table_HeaderSegmentsAreBold()
    {
        var segs = GetSegments("| Name |\n|------|\n| val |");
        segs.Should().Contain(s =>
            s.Text.Contains("Name") && s.Style.Decoration.HasFlag(Decoration.Bold));
    }

    // ─── Frontmatter ──────────────────────────────────────────────────────────

    [Fact]
    public void Frontmatter_DefaultHidden()
    {
        const string source = "---\ntitle: Test\n---\n\n# Body";
        Write(source).Should().NotContain("title: Test");
    }

    [Fact]
    public void Frontmatter_ShowFrontmatter_IsPresent()
    {
        const string source = "---\ntitle: Test\n---\n\n# Body";
        var console = new TestConsole().Width(80);
        var viewer = new MarkdownViewer(source) { ShowFrontmatter = true };
        console.Write(viewer);
        console.Output.Should().Contain("title: Test");
    }

    [Fact]
    public void Frontmatter_ShowFrontmatter_IsDim()
    {
        const string source = "---\ntitle: Test\n---\n\n# Body";
        var console = new TestConsole().Width(80);
        var viewer = new MarkdownViewer(source) { ShowFrontmatter = true };
        var segs = viewer.GetSegments(console).ToList();
        segs.Should().Contain(s =>
            s.Text.Contains("title") && s.Style.Decoration.HasFlag(Decoration.Dim));
    }
}
