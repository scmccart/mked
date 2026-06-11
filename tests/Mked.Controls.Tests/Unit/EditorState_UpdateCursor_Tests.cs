namespace Mked.Controls.Tests;

public class EditorState_UpdateCursor_Tests
{
    [Fact]
    public void Cursor_ChangesToNewPosition()
    {
        var state = new EditorState(string.Empty);
        var newPos = new CursorPosition(5, 10);
        state.UpdateCursor(newPos);

        state.Cursor.Should().Be(newPos);
    }

    [Fact]
    public void CanUndo_TrueAfterCursorMove()
    {
        var state = new EditorState(string.Empty);
        state.UpdateCursor(new CursorPosition(2, 1));

        state.CanUndo.Should().BeTrue();
    }

    // ── Undo/Redo preserve command type ──────────────────────────────────────

    [Fact]
    public void Undo_AfterCursorMove_RestoresPreviousCursorPosition()
    {
        var state = new EditorState(string.Empty);
        var original = state.Cursor;
        state.UpdateCursor(new CursorPosition(3, 5));

        state.Undo();

        state.Cursor.Should().Be(original);
    }

    [Fact]
    public void Undo_AfterCursorMove_PushesCursorCommandOntoRedoStack()
    {
        var state = new EditorState(string.Empty);
        var moved = new CursorPosition(3, 5);
        state.UpdateCursor(moved);

        state.Undo();
        state.Redo();

        // Redo reapplies the cursor move, not a buffer change.
        state.Cursor.Should().Be(moved);
    }

    [Fact]
    public void Undo_AfterCursorMove_DoesNotChangeBuffer()
    {
        var state = new EditorState("hello");
        state.UpdateCursor(new CursorPosition(1, 3));

        state.Undo();

        state.Buffer.Should().Be("hello");
    }

    [Fact]
    public void Redo_AfterCursorMoveUndo_RestoresMovedCursorNotBuffer()
    {
        var state = new EditorState("hello");
        var moved = new CursorPosition(1, 4);
        state.UpdateCursor(moved);
        state.Undo();

        state.Redo();

        state.Cursor.Should().Be(moved);
        state.Buffer.Should().Be("hello");
    }
}
