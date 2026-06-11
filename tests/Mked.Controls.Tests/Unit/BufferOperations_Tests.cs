namespace Mked.Controls.Tests;

public class BufferOperations_Tests
{
    // ── ToOffset ────────────────────────────────────────────────────────────

    [Fact]
    public void ToOffset_EmptyBuffer_ReturnsZero()
    {
        int offset = BufferOperations.ToOffset(string.Empty, new CursorPosition(1, 1));

        offset.Should().Be(0);
    }

    [Fact]
    public void ToOffset_FirstLineFirstColumn_ReturnsZero()
    {
        int offset = BufferOperations.ToOffset("hello", new CursorPosition(1, 1));

        offset.Should().Be(0);
    }

    [Fact]
    public void ToOffset_MidLine_ReturnsCorrectOffset()
    {
        int offset = BufferOperations.ToOffset("hello", new CursorPosition(1, 3));

        offset.Should().Be(2);
    }

    [Fact]
    public void ToOffset_SecondLine_AccountsForNewline()
    {
        // "abc\ndef"  →  line 2, col 1  →  offset 4
        int offset = BufferOperations.ToOffset("abc\ndef", new CursorPosition(2, 1));

        offset.Should().Be(4);
    }

    [Fact]
    public void ToOffset_SecondLineMidCol_ReturnsCorrectOffset()
    {
        // "abc\ndef"  →  line 2, col 3  →  offset 6
        int offset = BufferOperations.ToOffset("abc\ndef", new CursorPosition(2, 3));

        offset.Should().Be(6);
    }

    [Fact]
    public void ToOffset_ColumnPastLineEnd_ClampsToLineLength()
    {
        // Line 1 is "abc" (length 3); col 99 clamps to col 3 → offset 3
        int offset = BufferOperations.ToOffset("abc\ndef", new CursorPosition(1, 99));

        offset.Should().Be(3);
    }

    // ── FromOffset ───────────────────────────────────────────────────────────

    [Fact]
    public void FromOffset_EmptyBuffer_ReturnsOneOne()
    {
        CursorPosition pos = BufferOperations.FromOffset(string.Empty, 0);

        pos.Should().Be(new CursorPosition(1, 1));
    }

    [Fact]
    public void FromOffset_ZeroOffset_ReturnsFirstLineFirstColumn()
    {
        CursorPosition pos = BufferOperations.FromOffset("hello", 0);

        pos.Should().Be(new CursorPosition(1, 1));
    }

    [Fact]
    public void FromOffset_MidFirstLine_ReturnsCorrectPosition()
    {
        CursorPosition pos = BufferOperations.FromOffset("hello", 2);

        pos.Should().Be(new CursorPosition(1, 3));
    }

    [Fact]
    public void FromOffset_StartOfSecondLine_ReturnsLine2Col1()
    {
        // "abc\ndef" offset 4 → line 2, col 1
        CursorPosition pos = BufferOperations.FromOffset("abc\ndef", 4);

        pos.Should().Be(new CursorPosition(2, 1));
    }

    [Fact]
    public void FromOffset_MidSecondLine_ReturnsCorrectPosition()
    {
        CursorPosition pos = BufferOperations.FromOffset("abc\ndef", 6);

        pos.Should().Be(new CursorPosition(2, 3));
    }

    // ── Roundtrip ────────────────────────────────────────────────────────────

    [Fact]
    public void ToOffset_ThenFromOffset_Roundtrips()
    {
        const string buffer = "first line\nsecond line\nthird";
        var original = new CursorPosition(2, 5);

        int offset = BufferOperations.ToOffset(buffer, original);
        CursorPosition roundtripped = BufferOperations.FromOffset(buffer, offset);

        roundtripped.Should().Be(original);
    }

    [Fact]
    public void FromOffset_ThenToOffset_Roundtrips()
    {
        const string buffer = "abc\ndef\nghi";
        const int originalOffset = 7;

        CursorPosition pos = BufferOperations.FromOffset(buffer, originalOffset);
        int roundtripped = BufferOperations.ToOffset(buffer, pos);

        roundtripped.Should().Be(originalOffset);
    }

    // ── Insert ───────────────────────────────────────────────────────────────

    [Fact]
    public void Insert_IntoEmptyBuffer_ReturnsText()
    {
        string result = BufferOperations.Insert(string.Empty, new CursorPosition(1, 1), "hello");

        result.Should().Be("hello");
    }

    [Fact]
    public void Insert_AtBeginningOfBuffer_PrependsText()
    {
        string result = BufferOperations.Insert("world", new CursorPosition(1, 1), "hello ");

        result.Should().Be("hello world");
    }

    [Fact]
    public void Insert_MidLine_SplicesCorrectly()
    {
        string result = BufferOperations.Insert("helo", new CursorPosition(1, 3), "l");

        result.Should().Be("hello");
    }

    [Fact]
    public void Insert_AtEndOfLine_AppendsToLine()
    {
        string result = BufferOperations.Insert("abc", new CursorPosition(1, 4), "!");

        result.Should().Be("abc!");
    }

    [Fact]
    public void Insert_WithNewlineInText_CreatesNewLines()
    {
        string result = BufferOperations.Insert("ac", new CursorPosition(1, 2), "b\n");

        result.Should().Be("ab\nc");
    }

    [Fact]
    public void Insert_AtStartOfSecondLine_InsertsCorrectly()
    {
        string result = BufferOperations.Insert("abc\ndef", new CursorPosition(2, 1), "XX");

        result.Should().Be("abc\nXXdef");
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    [Fact]
    public void Delete_EmptyBuffer_ReturnsEmpty()
    {
        string result = BufferOperations.Delete(string.Empty,
            new TextRange(new CursorPosition(1, 1), new CursorPosition(1, 2)));

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void Delete_SingleChar_RemovesIt()
    {
        string result = BufferOperations.Delete("hello",
            new TextRange(new CursorPosition(1, 2), new CursorPosition(1, 3)));

        result.Should().Be("hllo");
    }

    [Fact]
    public void Delete_FirstChar_RemovesIt()
    {
        string result = BufferOperations.Delete("hello",
            new TextRange(new CursorPosition(1, 1), new CursorPosition(1, 2)));

        result.Should().Be("ello");
    }

    [Fact]
    public void Delete_AcrossLines_RemovesSpan()
    {
        // "abc\ndef" — delete from (1,3) to (2,2) removes "c\nd"
        string result = BufferOperations.Delete("abc\ndef",
            new TextRange(new CursorPosition(1, 3), new CursorPosition(2, 2)));

        result.Should().Be("abef");
    }

    [Fact]
    public void Delete_SameStartAndEnd_ReturnsUnchanged()
    {
        string result = BufferOperations.Delete("hello",
            new TextRange(new CursorPosition(1, 2), new CursorPosition(1, 2)));

        result.Should().Be("hello");
    }
}
