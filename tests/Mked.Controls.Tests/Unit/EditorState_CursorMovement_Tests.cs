namespace Mked.Controls.Tests;

public class EditorState_CursorMovement_Tests
{
    // ── MoveCursorLeft ────────────────────────────────────────────────────────

    [Fact]
    public void MoveCursorLeft_FromColumn2_MovesToColumn1()
    {
        var state = new EditorState("hi");
        state.UpdateCursor(new CursorPosition(1, 2));

        state.MoveCursorLeft();

        state.Cursor.Should().Be(new CursorPosition(1, 1));
    }

    [Fact]
    public void MoveCursorLeft_AtColumn1_StaysAtColumn1()
    {
        var state = new EditorState("hi");
        // Cursor starts at (1,1); clear the undo side-effect from UpdateCursor
        // by leaving cursor at the initial position without UpdateCursor.

        state.MoveCursorLeft();

        state.Cursor.Should().Be(new CursorPosition(1, 1));
    }

    // ── MoveCursorRight ───────────────────────────────────────────────────────

    [Fact]
    public void MoveCursorRight_AtEndOfLastLine_Stays()
    {
        var state = new EditorState("hi");
        // Position cursor at end of last line (column 3 for "hi")
        state.UpdateCursor(new CursorPosition(1, 3));

        state.MoveCursorRight();

        state.Cursor.Should().Be(new CursorPosition(1, 3));
    }

    // ── MoveCursorUp ──────────────────────────────────────────────────────────

    [Fact]
    public void MoveCursorUp_OnLine1_StaysOnLine1()
    {
        var state = new EditorState("line1\nline2");

        state.MoveCursorUp();

        state.Cursor.Line.Should().Be(1);
    }

    // ── MoveCursorDown ────────────────────────────────────────────────────────

    [Fact]
    public void MoveCursorDown_OnLastLine_Stays()
    {
        var state = new EditorState("line1\nline2");
        state.UpdateCursor(new CursorPosition(2, 1));

        state.MoveCursorDown();

        state.Cursor.Line.Should().Be(2);
    }

    // ── MoveCursorToLineStart / End ───────────────────────────────────────────

    [Fact]
    public void MoveCursorToLineStart_ColumnBecomesOne()
    {
        var state = new EditorState("hello");
        state.UpdateCursor(new CursorPosition(1, 4));

        state.MoveCursorToLineStart();

        state.Cursor.Column.Should().Be(1);
    }

    [Fact]
    public void MoveCursorToLineEnd_ColumnBecomesLineLengthPlusOne()
    {
        var state = new EditorState("hello");

        state.MoveCursorToLineEnd();

        // "hello".Length + 1 == 6
        state.Cursor.Column.Should().Be(6);
    }

    // ── Observer notification ─────────────────────────────────────────────────

    [Fact]
    public void MoveCursorLeft_FiresOnCursorMoved()
    {
        var state = new EditorState("hi");
        state.UpdateCursor(new CursorPosition(1, 2));
        var obs = new Mock<IEditorObserver>();
        state.Subscribe(obs.Object);

        state.MoveCursorLeft();

        obs.Verify(o => o.OnCursorMoved(It.IsAny<CursorPosition>()), Times.Once());
    }

    [Fact]
    public void MoveCursorRight_FiresOnCursorMoved()
    {
        var state = new EditorState("hi");
        var obs = new Mock<IEditorObserver>();
        state.Subscribe(obs.Object);

        state.MoveCursorRight();

        obs.Verify(o => o.OnCursorMoved(It.IsAny<CursorPosition>()), Times.Once());
    }

    // ── Undo stack not touched ────────────────────────────────────────────────

    [Fact]
    public void CursorMovement_DoesNotPushToUndoStack_CanUndoStaysFalse()
    {
        var state = new EditorState("hello\nworld");

        state.MoveCursorRight();
        state.MoveCursorDown();
        state.MoveCursorLeft();
        state.MoveCursorUp();
        state.MoveCursorToLineStart();
        state.MoveCursorToLineEnd();
        state.MoveCursorWordLeft();
        state.MoveCursorWordRight();

        state.CanUndo.Should().BeFalse();
    }

    // ── Word movement ─────────────────────────────────────────────────────────

    [Fact]
    public void MoveCursorWordLeft_MovesBeforeWord()
    {
        var state = new EditorState("hello world");
        // Position at start of "world" (column 7)
        state.UpdateCursor(new CursorPosition(1, 7));

        state.MoveCursorWordLeft();

        // Should land at start of "hello" (column 1)
        state.Cursor.Should().Be(new CursorPosition(1, 1));
    }

    [Fact]
    public void MoveCursorWordRight_MovesAfterWord()
    {
        var state = new EditorState("hello world");
        // Cursor starts at (1,1)

        state.MoveCursorWordRight();

        // After "hello" + space → should be at column 7 (start of "world")
        state.Cursor.Should().Be(new CursorPosition(1, 7));
    }
}
