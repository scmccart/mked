namespace Mked.Controls.Tests;

public class EditorState_UpdateBuffer_Tests
{
    [Fact]
    public void Buffer_ChangesToNewValue()
    {
        var state = new EditorState("before");
        state.UpdateBuffer("after");

        state.Buffer.Should().Be("after");
    }

    [Fact]
    public void IsDirty_TrueAfterBufferChange()
    {
        var state = new EditorState("original");
        state.UpdateBuffer("modified");

        state.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void CanUndo_TrueAfterBufferChange()
    {
        var state = new EditorState("a");
        state.UpdateBuffer("b");

        state.CanUndo.Should().BeTrue();
    }

    [Fact]
    public void CanRedo_FalseAfterBufferChange()
    {
        var state = new EditorState("a");
        state.UpdateBuffer("b");

        state.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void Undo_ClampsCursorIntoNewBuffer()
    {
        // Reproduces the reviewer scenario: type one char so the cursor advances past the
        // end of the pre-edit buffer, then Undo — the cursor must be clamped back into range.
        var state = new EditorState("");
        state.Insert(state.Cursor, "x");
        state.MoveCursorRight(); // cursor now at (1, 2) — one past the single char
        state.Undo();            // buffer restores to ""; cursor must clamp to (1, 1)

        state.Cursor.Should().Be(new CursorPosition(1, 1));
    }
}
