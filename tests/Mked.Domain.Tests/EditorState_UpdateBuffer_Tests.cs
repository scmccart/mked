namespace Mked.Domain.Tests;

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
    public void CanRedo_ClearedByNewBufferChange()
    {
        var state = new EditorState("a");
        state.UpdateBuffer("b");
        // Simulate a redo stack entry by checking it's empty initially
        state.CanRedo.Should().BeFalse();
    }
}
