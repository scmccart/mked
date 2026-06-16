namespace Mked.Controls.Tests;

public class EditorState_Insert_Tests
{
    [Fact]
    public void Insert_IntoEmptyBuffer_FiresOnBufferChangedWithInsertedText()
    {
        var state = new EditorState(string.Empty);
        var spy = new SpyObserver();
        state.Subscribe(spy);

        state.Insert(new CursorPosition(1, 1), "hello");

        spy.LastBuffer.Should().Be("hello");
        spy.BufferCallCount.Should().Be(1);
    }

    [Fact]
    public void Insert_AtPosition11_PrependsText()
    {
        var state = new EditorState("world");

        state.Insert(new CursorPosition(1, 1), "hello ");

        state.Buffer.Should().Be("hello world");
    }

    [Fact]
    public void Insert_MidLine_InsertsAtCorrectOffset()
    {
        var state = new EditorState("helo");

        state.Insert(new CursorPosition(1, 3), "l");

        state.Buffer.Should().Be("hello");
    }

    [Fact]
    public void Insert_PushesToUndoStack_CanUndoBecomesTrue()
    {
        var state = new EditorState("before");

        state.Insert(new CursorPosition(1, 1), "x");

        state.CanUndo.Should().BeTrue();
    }

    [Fact]
    public void Insert_ClearsRedoStack_CanRedoIsFalse()
    {
        // After Insert the redo stack should be empty.
        var state = new EditorState("a");

        state.Insert(new CursorPosition(1, 1), "x");

        state.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void Insert_Observer_ReceivesCorrectBufferContent()
    {
        var state = new EditorState("world");
        var spy = new SpyObserver();
        state.Subscribe(spy);

        state.Insert(new CursorPosition(1, 1), "hello ");

        spy.LastBuffer.Should().Be("hello world");
        spy.BufferCallCount.Should().Be(1);
    }
}
