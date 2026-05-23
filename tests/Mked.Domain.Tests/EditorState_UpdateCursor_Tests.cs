namespace Mked.Domain.Tests;

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
}
