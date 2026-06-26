namespace Mked.Controls.Tests;

public class BufferOperations_SelectionOps_Tests
{
    // ─── Substring ───────────────────────────────────────────────────────────

    [Fact]
    public void Substring_EmptyBuffer_ReturnsEmptyString()
    {
        var range = new TextRange(new CursorPosition(1, 1), new CursorPosition(1, 1));

        string result = BufferOperations.Substring(string.Empty, range);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Substring_EntireFirstLine_ReturnsLine()
    {
        var range = new TextRange(new CursorPosition(1, 1), new CursorPosition(1, 6));

        string result = BufferOperations.Substring("hello", range);

        result.Should().Be("hello");
    }

    [Fact]
    public void Substring_PartialLine_ReturnsSlice()
    {
        var range = new TextRange(new CursorPosition(1, 2), new CursorPosition(1, 4));

        string result = BufferOperations.Substring("hello", range);

        result.Should().Be("el");
    }

    [Fact]
    public void Substring_AcrossLines_IncludesNewline()
    {
        var range = new TextRange(new CursorPosition(1, 4), new CursorPosition(2, 4));

        string result = BufferOperations.Substring("hello\nworld", range);

        result.Should().Be("lo\nwor");
    }

    [Fact]
    public void Substring_ZeroLengthRange_ReturnsEmptyString()
    {
        var range = new TextRange(new CursorPosition(1, 3), new CursorPosition(1, 3));

        string result = BufferOperations.Substring("hello", range);

        result.Should().BeEmpty();
    }

    // ─── ReplaceRange ────────────────────────────────────────────────────────

    [Fact]
    public void ReplaceRange_EmptyBuffer_ReturnsInsertedText()
    {
        var range = new TextRange(new CursorPosition(1, 1), new CursorPosition(1, 1));

        string result = BufferOperations.ReplaceRange(string.Empty, range, "hello");

        result.Should().Be("hello");
    }

    [Fact]
    public void ReplaceRange_ReplaceEntireBuffer_ReturnsNewText()
    {
        var range = new TextRange(new CursorPosition(1, 1), new CursorPosition(1, 6));

        string result = BufferOperations.ReplaceRange("hello", range, "world");

        result.Should().Be("world");
    }

    [Fact]
    public void ReplaceRange_ReplacePartial_LeavesRemainder()
    {
        var range = new TextRange(new CursorPosition(1, 1), new CursorPosition(1, 4));

        string result = BufferOperations.ReplaceRange("hello", range, "X");

        result.Should().Be("Xlo");
    }

    [Fact]
    public void ReplaceRange_EmptyTextArg_ActsAsDelete()
    {
        var range = new TextRange(new CursorPosition(1, 2), new CursorPosition(1, 4));

        string result = BufferOperations.ReplaceRange("hello", range, string.Empty);

        result.Should().Be("hlo");
    }

    [Fact]
    public void ReplaceRange_EmptyRange_ActsAsInsert()
    {
        var range = new TextRange(new CursorPosition(1, 2), new CursorPosition(1, 2));

        string result = BufferOperations.ReplaceRange("abc", range, "XY");

        result.Should().Be("aXYbc");
    }

    [Fact]
    public void ReplaceRange_AcrossLines_ReplacesCorrectly()
    {
        var range = new TextRange(new CursorPosition(1, 6), new CursorPosition(2, 6));

        string result = BufferOperations.ReplaceRange("hello\nworld", range, " ");

        result.Should().Be("hello ");
    }
}
