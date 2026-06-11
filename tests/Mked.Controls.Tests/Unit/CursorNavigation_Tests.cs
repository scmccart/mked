namespace Mked.Controls.Tests;

public class CursorNavigation_Tests
{
    private const string MultiLine = "abc\ndef\nghi";
    //  line 1: "abc"  (cols 1-4, end=4)
    //  line 2: "def"  (cols 1-4, end=4)
    //  line 3: "ghi"  (cols 1-4, end=4)

    // ── MoveLeft ─────────────────────────────────────────────────────────────

    [Fact]
    public void MoveLeft_AtOrigin_StaysAtOrigin()
    {
        CursorPosition result = CursorNavigation.MoveLeft(MultiLine, new CursorPosition(1, 1));

        result.Should().Be(new CursorPosition(1, 1));
    }

    [Fact]
    public void MoveLeft_MidLine_DecrementsColumn()
    {
        CursorPosition result = CursorNavigation.MoveLeft(MultiLine, new CursorPosition(1, 3));

        result.Should().Be(new CursorPosition(1, 2));
    }

    [Fact]
    public void MoveLeft_AtLineStart_WrapsToEndOfPreviousLine()
    {
        // Line 2 col 1 → line 1 col 4 (one past 'c', i.e. end of "abc")
        CursorPosition result = CursorNavigation.MoveLeft(MultiLine, new CursorPosition(2, 1));

        result.Should().Be(new CursorPosition(1, 4));
    }

    [Fact]
    public void MoveLeft_EmptyBuffer_ReturnsOneOne()
    {
        CursorPosition result = CursorNavigation.MoveLeft(string.Empty, new CursorPosition(1, 1));

        result.Should().Be(new CursorPosition(1, 1));
    }

    // ── MoveRight ────────────────────────────────────────────────────────────

    [Fact]
    public void MoveRight_MidLine_IncrementsColumn()
    {
        CursorPosition result = CursorNavigation.MoveRight(MultiLine, new CursorPosition(1, 2));

        result.Should().Be(new CursorPosition(1, 3));
    }

    [Fact]
    public void MoveRight_AtEndOfLine_WrapsToStartOfNextLine()
    {
        // Line 1 has length 3; end position is col 4; moving right wraps to (2,1)
        CursorPosition result = CursorNavigation.MoveRight(MultiLine, new CursorPosition(1, 4));

        result.Should().Be(new CursorPosition(2, 1));
    }

    [Fact]
    public void MoveRight_AtEndOfLastLine_StaysAtEnd()
    {
        CursorPosition result = CursorNavigation.MoveRight(MultiLine, new CursorPosition(3, 4));

        result.Should().Be(new CursorPosition(3, 4));
    }

    [Fact]
    public void MoveRight_EmptyBuffer_ReturnsOneOne()
    {
        CursorPosition result = CursorNavigation.MoveRight(string.Empty, new CursorPosition(1, 1));

        result.Should().Be(new CursorPosition(1, 1));
    }

    // ── MoveUp ───────────────────────────────────────────────────────────────

    [Fact]
    public void MoveUp_FromFirstLine_StaysOnFirstLine()
    {
        CursorPosition result = CursorNavigation.MoveUp(MultiLine, new CursorPosition(1, 2));

        result.Line.Should().Be(1);
    }

    [Fact]
    public void MoveUp_FromSecondLine_MovesToFirstLine()
    {
        CursorPosition result = CursorNavigation.MoveUp(MultiLine, new CursorPosition(2, 2));

        result.Should().Be(new CursorPosition(1, 2));
    }

    [Fact]
    public void MoveUp_ClampsColumnToLineLength()
    {
        // "short\nlonger line" — from line 2 col 8, move up to line 1 which is only 5 chars
        CursorPosition result = CursorNavigation.MoveUp("short\nlonger line", new CursorPosition(2, 8));

        result.Should().Be(new CursorPosition(1, 6)); // col 6 = length+1
    }

    [Fact]
    public void MoveUp_EmptyBuffer_ReturnsOneOne()
    {
        CursorPosition result = CursorNavigation.MoveUp(string.Empty, new CursorPosition(1, 1));

        result.Should().Be(new CursorPosition(1, 1));
    }

    // ── MoveDown ─────────────────────────────────────────────────────────────

    [Fact]
    public void MoveDown_FromLastLine_StaysOnLastLine()
    {
        CursorPosition result = CursorNavigation.MoveDown(MultiLine, new CursorPosition(3, 2));

        result.Line.Should().Be(3);
    }

    [Fact]
    public void MoveDown_FromFirstLine_MovesToSecondLine()
    {
        CursorPosition result = CursorNavigation.MoveDown(MultiLine, new CursorPosition(1, 2));

        result.Should().Be(new CursorPosition(2, 2));
    }

    [Fact]
    public void MoveDown_ClampsColumnToLineLength()
    {
        // "longer line\nshort" — from line 1 col 8, move down to line 2 which is only 5 chars
        CursorPosition result = CursorNavigation.MoveDown("longer line\nshort", new CursorPosition(1, 8));

        result.Should().Be(new CursorPosition(2, 6)); // col 6 = length+1
    }

    [Fact]
    public void MoveDown_EmptyBuffer_ReturnsOneOne()
    {
        CursorPosition result = CursorNavigation.MoveDown(string.Empty, new CursorPosition(1, 1));

        result.Should().Be(new CursorPosition(1, 1));
    }

    // ── MoveWordLeft ─────────────────────────────────────────────────────────

    [Fact]
    public void MoveWordLeft_FromEndOfWord_JumpsToWordStart()
    {
        // "hello world" col 6 ('o') → after skipping 'hello', lands at col 1
        CursorPosition result = CursorNavigation.MoveWordLeft("hello world", new CursorPosition(1, 6));

        result.Should().Be(new CursorPosition(1, 1));
    }

    [Fact]
    public void MoveWordLeft_FromMidSpaces_SkipsSpacesThenWord()
    {
        // "hello   world" col 9 (inside spaces) → lands at col 1 (start of "hello")
        CursorPosition result = CursorNavigation.MoveWordLeft("hello   world", new CursorPosition(1, 9));

        result.Should().Be(new CursorPosition(1, 1));
    }

    [Fact]
    public void MoveWordLeft_AtOrigin_StaysAtOrigin()
    {
        CursorPosition result = CursorNavigation.MoveWordLeft("hello", new CursorPosition(1, 1));

        result.Should().Be(new CursorPosition(1, 1));
    }

    // ── MoveWordRight ────────────────────────────────────────────────────────

    [Fact]
    public void MoveWordRight_FromStartOfWord_JumpsAfterWordAndSpaces()
    {
        // "hello world" col 1 → moves past 'hello' then past ' ' → col 7 (start of 'world')
        CursorPosition result = CursorNavigation.MoveWordRight("hello world", new CursorPosition(1, 1));

        result.Should().Be(new CursorPosition(1, 7));
    }

    [Fact]
    public void MoveWordRight_AtEndOfBuffer_Stays()
    {
        // "hello" col 6 (past last char) → stays
        CursorPosition result = CursorNavigation.MoveWordRight("hello", new CursorPosition(1, 6));

        result.Should().Be(new CursorPosition(1, 6));
    }

    // ── MoveToLineStart ──────────────────────────────────────────────────────

    [Fact]
    public void MoveToLineStart_ReturnsCol1()
    {
        CursorPosition result = CursorNavigation.MoveToLineStart(MultiLine, new CursorPosition(2, 3));

        result.Should().Be(new CursorPosition(2, 1));
    }

    [Fact]
    public void MoveToLineStart_AlreadyAtCol1_StaysAtCol1()
    {
        CursorPosition result = CursorNavigation.MoveToLineStart(MultiLine, new CursorPosition(1, 1));

        result.Should().Be(new CursorPosition(1, 1));
    }

    // ── MoveToLineEnd ────────────────────────────────────────────────────────

    [Fact]
    public void MoveToLineEnd_ReturnsOnePastLastChar()
    {
        // "abc" has length 3; end should be col 4
        CursorPosition result = CursorNavigation.MoveToLineEnd(MultiLine, new CursorPosition(1, 1));

        result.Should().Be(new CursorPosition(1, 4));
    }

    [Fact]
    public void MoveToLineEnd_EmptyLine_ReturnsCol1()
    {
        CursorPosition result = CursorNavigation.MoveToLineEnd("abc\n\ndef", new CursorPosition(2, 1));

        result.Should().Be(new CursorPosition(2, 1));
    }

    // ── Clamp ────────────────────────────────────────────────────────────────

    [Fact]
    public void Clamp_EmptyBuffer_ReturnsOneOne()
    {
        CursorPosition result = CursorNavigation.Clamp(string.Empty, new CursorPosition(5, 10));

        result.Should().Be(new CursorPosition(1, 1));
    }

    [Fact]
    public void Clamp_LineBeyondLast_ClampsToLastLine()
    {
        CursorPosition result = CursorNavigation.Clamp(MultiLine, new CursorPosition(99, 1));

        result.Line.Should().Be(3);
    }

    [Fact]
    public void Clamp_ColumnBeyondLineEnd_ClampsToLineEndPlusOne()
    {
        // Line 1 "abc" length=3; clamp col 99 → col 4
        CursorPosition result = CursorNavigation.Clamp(MultiLine, new CursorPosition(1, 99));

        result.Should().Be(new CursorPosition(1, 4));
    }

    [Fact]
    public void Clamp_ValidPosition_ReturnsUnchanged()
    {
        CursorPosition result = CursorNavigation.Clamp(MultiLine, new CursorPosition(2, 2));

        result.Should().Be(new CursorPosition(2, 2));
    }

    // ── MoveLeft column clamping (Fix 4) ─────────────────────────────────────

    [Fact]
    public void MoveLeft_OutOfRangeColumn_ClampsResultToLineEnd()
    {
        // Line 1 "abc" has length 3; end=col 4. An out-of-range input col 99
        // should move left but the returned column must not exceed lineLength+1.
        CursorPosition result = CursorNavigation.MoveLeft(MultiLine, new CursorPosition(1, 99));

        result.Column.Should().BeLessThanOrEqualTo(4); // lineLength+1
    }

    [Fact]
    public void MoveLeft_ColumnOneAboveEnd_DecrementsNormally()
    {
        // Slightly past end (col 5 on a length-3 line) → should clamp to col 4.
        CursorPosition result = CursorNavigation.MoveLeft(MultiLine, new CursorPosition(1, 5));

        result.Should().Be(new CursorPosition(1, 4));
    }

    // ── MoveRight column clamping (Fix 5) ────────────────────────────────────

    [Fact]
    public void MoveRight_OutOfRangeColumnOnLastLine_ClampsToLineEnd()
    {
        // Line 3 "ghi" has length 3; end=col 4. Column 99 on the last line
        // must return col 4, not 99.
        CursorPosition result = CursorNavigation.MoveRight(MultiLine, new CursorPosition(3, 99));

        result.Should().Be(new CursorPosition(3, 4));
    }

    [Fact]
    public void MoveRight_AtEndOfLastLine_StaysAtEnd_Clamped()
    {
        // Already at valid end (col 4) — still returns col 4 (no change/no over-run).
        CursorPosition result = CursorNavigation.MoveRight(MultiLine, new CursorPosition(3, 4));

        result.Should().Be(new CursorPosition(3, 4));
    }
}
