namespace Mked.Controls.Tests.Unit;

public sealed class EditorStatusLine_Tests
{
    private static string Write(EditorStatusLine statusLine)
    {
        var console = new TestConsole().Width(80);
        console.Write(statusLine);
        return console.Output;
    }

    private static List<Segment> GetSegments(EditorStatusLine statusLine)
    {
        var console = new TestConsole().Width(80);
        return statusLine.GetSegments(console).ToList();
    }

    // ─── Dirty indicator ──────────────────────────────────────────────────────

    [Fact]
    public void Dirty_True_BulletSegmentPresent()
    {
        var statusLine = new EditorStatusLine(cursor: (1, 1), isDirty: true, wordCount: 0);

        var segs = GetSegments(statusLine);

        segs.Should().Contain(s => s.Text.Contains('●'));
    }

    [Fact]
    public void Dirty_False_NoBulletSegment()
    {
        var statusLine = new EditorStatusLine(cursor: (1, 1), isDirty: false, wordCount: 0);

        var segs = GetSegments(statusLine);

        segs.Should().NotContain(s => s.Text.Contains('●'));
    }

    // ─── Position and word count ───────────────────────────────────────────────

    [Fact]
    public void Output_ContainsLineAndColumnNumbers()
    {
        var statusLine = new EditorStatusLine(cursor: (5, 12), isDirty: false, wordCount: 0);

        string output = Write(statusLine);

        output.Should().Contain("Ln 5");
        output.Should().Contain("Col 12");
    }

    [Fact]
    public void Output_ContainsWordCount()
    {
        var statusLine = new EditorStatusLine(cursor: (1, 1), isDirty: false, wordCount: 42);

        string output = Write(statusLine);

        output.Should().Contain("42");
        output.Should().Contain("words");
    }
}
