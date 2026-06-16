namespace Mked.Controls.Tests;

public class EditorState_Reset_Tests
{
    [Fact]
    public void Reset_SetsBufferToNewContent()
    {
        var state = new EditorState("old content");

        state.Reset("new content");

        state.Buffer.Should().Be("new content");
    }

    [Fact]
    public void Reset_SetsCursorToLineOneColumnOne()
    {
        var state = new EditorState("line1\nline2\nline3");
        // Move cursor away from (1,1)
        state.UpdateCursor(new CursorPosition(3, 2));

        state.Reset("fresh buffer");

        state.Cursor.Should().Be(new CursorPosition(1, 1));
    }

    [Fact]
    public void Reset_ClearsUndoStack()
    {
        var state = new EditorState("v1");
        state.UpdateBuffer("v2");
        state.UpdateBuffer("v3");
        state.CanUndo.Should().BeTrue();

        state.Reset("fresh");

        state.CanUndo.Should().BeFalse();
    }

    [Fact]
    public void Reset_ClearsRedoStack()
    {
        var state = new EditorState("v1");
        state.UpdateBuffer("v2");
        state.Undo();
        state.CanRedo.Should().BeTrue();

        state.Reset("fresh");

        state.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void Reset_IsNotDirty()
    {
        var state = new EditorState("v1");
        state.UpdateBuffer("v2");
        state.IsDirty.Should().BeTrue();

        state.Reset("loaded doc");

        state.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void Reset_SetsNewCleanBaseline_SubsequentEditIsDirty()
    {
        var state = new EditorState("initial");
        state.Reset("loaded doc");
        state.IsDirty.Should().BeFalse();

        state.UpdateBuffer("modified");

        state.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void Reset_NotifiesObserverOfBufferChange()
    {
        var state = new EditorState("old");
        var spy = new SpyObserver();
        state.Subscribe(spy);

        state.Reset("new");

        spy.LastBuffer.Should().Be("new");
        spy.BufferCallCount.Should().Be(1);
    }

    [Fact]
    public void Reset_NotifiesObserverOfCursorMove()
    {
        var state = new EditorState("line1\nline2");
        state.UpdateCursor(new CursorPosition(2, 3));
        var spy = new SpyObserver();
        state.Subscribe(spy);

        state.Reset("new doc");

        spy.LastCursor.Should().Be(new CursorPosition(1, 1));
        spy.CursorCallCount.Should().Be(1);
    }

    [Fact]
    public void Reset_ThrowsOnNullBuffer()
    {
        var state = new EditorState("existing");

        Action act = () => state.Reset(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
