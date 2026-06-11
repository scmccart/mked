namespace Mked.Controls.Tests;

public class EditorState_Observer_Tests
{
    private sealed class SpyObserver : IEditorObserver
    {
        public string? LastBuffer { get; private set; }
        public CursorPosition? LastCursor { get; private set; }
        public int BufferCallCount { get; private set; }
        public int CursorCallCount { get; private set; }

        public void OnBufferChanged(string newBuffer)
        {
            LastBuffer = newBuffer;
            BufferCallCount++;
        }

        public void OnCursorMoved(CursorPosition position)
        {
            LastCursor = position;
            CursorCallCount++;
        }
    }

    [Fact]
    public void Subscribe_OnBufferChanged_NotifiedWithNewValue()
    {
        var state = new EditorState("initial");
        var spy = new SpyObserver();
        state.Subscribe(spy);

        state.UpdateBuffer("changed");

        spy.LastBuffer.Should().Be("changed");
        spy.BufferCallCount.Should().Be(1);
    }

    [Fact]
    public void Subscribe_OnCursorMoved_NotifiedWithNewPosition()
    {
        var state = new EditorState(string.Empty);
        var spy = new SpyObserver();
        state.Subscribe(spy);
        var pos = new CursorPosition(3, 7);

        state.UpdateCursor(pos);

        spy.LastCursor.Should().Be(pos);
        spy.CursorCallCount.Should().Be(1);
    }

    [Fact]
    public void MultipleObservers_AllNotifiedOnBufferChange()
    {
        var state = new EditorState("x");
        var spy1 = new SpyObserver();
        var spy2 = new SpyObserver();
        state.Subscribe(spy1);
        state.Subscribe(spy2);

        state.UpdateBuffer("y");

        spy1.BufferCallCount.Should().Be(1);
        spy2.BufferCallCount.Should().Be(1);
    }

    [Fact]
    public void UpdateBuffer_DoesNotNotifyUnregisteredObserver()
    {
        var state = new EditorState("x");
        var spy = new SpyObserver();

        state.UpdateBuffer("y");

        spy.BufferCallCount.Should().Be(0);
    }

    // ─── Selective Undo/Redo notifications ────────────────────────────────────

    [Fact]
    public void Undo_BufferCommand_NotifiesOnlyOnBufferChanged()
    {
        var state = new EditorState("before");
        var spy = new SpyObserver();
        state.Subscribe(spy);
        state.UpdateBuffer("after");
        spy.BufferCallCount.Should().Be(1);
        spy.CursorCallCount.Should().Be(0);

        state.Undo();

        spy.BufferCallCount.Should().Be(2);
        spy.CursorCallCount.Should().Be(0);
    }

    [Fact]
    public void Undo_CursorCommand_NotifiesOnlyOnCursorMoved()
    {
        var state = new EditorState("line1\nline2");
        var spy = new SpyObserver();
        state.Subscribe(spy);
        state.UpdateCursor(new CursorPosition(2, 1));
        spy.BufferCallCount.Should().Be(0);
        spy.CursorCallCount.Should().Be(1);

        state.Undo();

        spy.BufferCallCount.Should().Be(0);
        spy.CursorCallCount.Should().Be(2);
    }

    [Fact]
    public void Redo_BufferCommand_NotifiesOnlyOnBufferChanged()
    {
        var state = new EditorState("before");
        var spy = new SpyObserver();
        state.Subscribe(spy);
        state.UpdateBuffer("after");
        state.Undo();

        var bufCountBeforeRedo = spy.BufferCallCount;
        var curCountBeforeRedo = spy.CursorCallCount;

        state.Redo();

        spy.BufferCallCount.Should().Be(bufCountBeforeRedo + 1);
        spy.CursorCallCount.Should().Be(curCountBeforeRedo);
    }

    [Fact]
    public void Redo_CursorCommand_NotifiesOnlyOnCursorMoved()
    {
        var state = new EditorState("line1\nline2");
        var spy = new SpyObserver();
        state.Subscribe(spy);
        state.UpdateCursor(new CursorPosition(2, 1));
        state.Undo();

        var bufCountBeforeRedo = spy.BufferCallCount;
        var curCountBeforeRedo = spy.CursorCallCount;

        state.Redo();

        spy.BufferCallCount.Should().Be(bufCountBeforeRedo);
        spy.CursorCallCount.Should().Be(curCountBeforeRedo + 1);
    }
}
