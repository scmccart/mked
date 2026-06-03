namespace Mked.Controls.Tests.Unit;

public sealed class MarkdownViewer_Scroll_Tests
{
    private static readonly string ThreeBlockDoc = "# Heading\n\nParagraph one.\n\nParagraph two.";

    private static List<Segment> GetSegments(MarkdownViewer viewer)
    {
        var console = new TestConsole().Width(80);
        return viewer.GetSegments(console).ToList();
    }

    private static string Write(MarkdownViewer viewer)
    {
        var console = new TestConsole().Width(80);
        console.Write(viewer);
        return console.Output;
    }

    // ─── BlockCount ───────────────────────────────────────────────────────────

    [Fact]
    public void BlockCount_SingleBlock_IsOne()
    {
        // A single heading with nothing else produces exactly one block.
        new MarkdownViewer("# Hello").BlockCount.Should().Be(1);
    }

    [Fact]
    public void BlockCount_EmptyDocument_IsZero()
    {
        new MarkdownViewer(string.Empty).BlockCount.Should().Be(0);
    }

    [Fact]
    public void BlockCount_MoreContentProducesMoreBlocks()
    {
        int single = new MarkdownViewer("# H1").BlockCount;
        int multiple = new MarkdownViewer("# H1\n\nParagraph").BlockCount;
        multiple.Should().BeGreaterThan(single);
    }

    // ─── Scroll / clip ────────────────────────────────────────────────────────

    [Fact]
    public void TopLineIndex_AtBlock1Start_FirstBlockTextAbsent()
    {
        var baseViewer = new MarkdownViewer(ThreeBlockDoc);
        GetSegments(baseViewer); // populate cache
        int block1Line = baseViewer.ScrollInfo.BlockStartLines[1];

        var viewer = baseViewer with { TopLineIndex = block1Line, ViewportHeight = 5 };
        Write(viewer).Should().NotContain("Heading");
    }

    [Fact]
    public void TopLineIndex_AtBlock1Start_SecondBlockTextPresent()
    {
        var baseViewer = new MarkdownViewer(ThreeBlockDoc);
        GetSegments(baseViewer); // populate cache
        int block1Line = baseViewer.ScrollInfo.BlockStartLines[1];

        var viewer = baseViewer with { TopLineIndex = block1Line, ViewportHeight = 5 };
        Write(viewer).Should().Contain("Paragraph one");
    }

    [Fact]
    public void ViewportHeight_ClipsLinesRendered()
    {
        // With ViewportHeight = 1 on a multi-block document, only one line is rendered.
        var viewer = new MarkdownViewer(ThreeBlockDoc) { ViewportHeight = 1 };
        var segs = GetSegments(viewer);
        segs.Should().NotContain(s => s.IsLineBreak);
    }

    // ─── with-expression cache sharing ───────────────────────────────────────

    [Fact]
    public void WithCopy_SharesSameScrollInfoReference()
    {
        var viewer = new MarkdownViewer(ThreeBlockDoc);
        GetSegments(viewer); // populate cache

        var viewer2 = viewer with { TopLineIndex = 1 };

        ReferenceEquals(viewer.ScrollInfo, viewer2.ScrollInfo).Should().BeTrue();
    }

    [Fact]
    public void WithCopy_DifferentTopLineIndex_ShowsDifferentContent()
    {
        var viewer0 = new MarkdownViewer(ThreeBlockDoc) { TopLineIndex = 0, ViewportHeight = 3 };
        GetSegments(viewer0); // populate cache
        int block1Line = viewer0.ScrollInfo.BlockStartLines[1];

        var viewer1 = viewer0 with { TopLineIndex = block1Line };

        Write(viewer0).Should().Contain("Heading");
        Write(viewer1).Should().NotContain("Heading");
    }

    // ─── ScrollInfo accuracy ─────────────────────────────────────────────────

    [Fact]
    public void ScrollInfo_BlockStartLines_FirstEntryIsZero()
    {
        var viewer = new MarkdownViewer(ThreeBlockDoc);
        GetSegments(viewer); // trigger cache

        viewer.ScrollInfo.BlockStartLines[0].Should().Be(0);
    }

    [Fact]
    public void ScrollInfo_BlockStartLines_AreMonotonicallyNonDecreasing()
    {
        var viewer = new MarkdownViewer(ThreeBlockDoc);
        GetSegments(viewer); // trigger cache

        var starts = viewer.ScrollInfo.BlockStartLines;
        for (int i = 1; i < starts.Count; i++)
        {
            starts[i].Should().BeGreaterThanOrEqualTo(starts[i - 1]);
        }
    }

    [Fact]
    public void ScrollInfo_TotalLineCount_EqualsOrExceedsBlockCount()
    {
        var viewer = new MarkdownViewer(ThreeBlockDoc);
        GetSegments(viewer);

        viewer.ScrollInfo.TotalLineCount.Should().BeGreaterThanOrEqualTo(viewer.BlockCount);
    }

    [Fact]
    public void ScrollInfo_BlockStartLinesCount_EqualsBlockCount()
    {
        var viewer = new MarkdownViewer(ThreeBlockDoc);
        GetSegments(viewer);

        viewer.ScrollInfo.BlockStartLines.Count.Should().Be(viewer.BlockCount);
    }
}
