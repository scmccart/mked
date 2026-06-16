namespace Mked.Controls.Tests;

public class EditorState_Delete_Tests
{
    [Fact]
    public void Delete_SingleChar_FiresOnBufferChangedWithCharRemoved()
    {
        var state = new EditorState("hello");
        var spy = new SpyObserver();
        state.Subscribe(spy);
        var range = new TextRange(new CursorPosition(1, 1), new CursorPosition(1, 2));

        state.Delete(range);

        spy.LastBuffer.Should().Be("ello");
        spy.BufferCallCount.Should().Be(1);
    }

    [Fact]
    public void Delete_AcrossLines_RemovesSpanningText()
    {
        var state = new EditorState("line1\nline2");
        // Delete from column 6 of line 1 (the '\n') through column 1 of line 2
        var range = new TextRange(new CursorPosition(1, 6), new CursorPosition(2, 1));

        state.Delete(range);

        state.Buffer.Should().Be("line1line2");
    }

    [Fact]
    public void Delete_PushesToUndoStack_CanUndoBecomesTrue()
    {
        var state = new EditorState("hello");
        var range = new TextRange(new CursorPosition(1, 1), new CursorPosition(1, 2));

        state.Delete(range);

        state.CanUndo.Should().BeTrue();
    }

    [Fact]
    public void Delete_Observer_ReceivesCorrectBufferContent()
    {
        var state = new EditorState("abcd");
        var spy = new SpyObserver();
        state.Subscribe(spy);
        // Delete "bc" — positions (1,2) to (1,4)
        var range = new TextRange(new CursorPosition(1, 2), new CursorPosition(1, 4));

        state.Delete(range);

        spy.LastBuffer.Should().Be("ad");
        spy.BufferCallCount.Should().Be(1);
    }

    // ── Cursor repositioning & single undo step ───────────────────────────────

    [Fact]
    public void Delete_MovesCursorToRangeStart()
    {
        var state = new EditorState("hello world");
        // Delete " world" from (1,6) to (1,12)
        var start = new CursorPosition(1, 6);
        var range = new TextRange(start, new CursorPosition(1, 12));

        state.Delete(range);

        state.Cursor.Should().Be(start);
    }

    [Fact]
    public void Delete_ProducesSingleUndoEntry()
    {
        // One Delete should push exactly one entry: Undo once must restore the full
        // pre-delete state and leave CanUndo false.
        var state = new EditorState("abcde");
        var range = new TextRange(new CursorPosition(1, 2), new CursorPosition(1, 4));

        state.Delete(range);
        state.Undo();

        state.Buffer.Should().Be("abcde");
        state.CanUndo.Should().BeFalse();
    }

    [Fact]
    public void Delete_Observer_NotifiedOfCursorMoveToRangeStart()
    {
        var state = new EditorState("hello");
        var spy = new SpyObserver();
        state.Subscribe(spy);
        var start = new CursorPosition(1, 2);
        var range = new TextRange(start, new CursorPosition(1, 4));

        state.Delete(range);

        spy.LastCursor.Should().Be(start);
        spy.CursorCallCount.Should().Be(1);
    }
}
