namespace Mked.Domain.Tests;

public class EditorState_Construction_Tests
{
    [Fact]
    public void Buffer_MatchesInitialValue()
    {
        var state = new EditorState("hello");

        state.Buffer.Should().Be("hello");
    }

    [Fact]
    public void IsDirty_IsFalseInitially()
    {
        var state = new EditorState("hello");

        state.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void CanUndo_IsFalseInitially()
    {
        var state = new EditorState("hello");

        state.CanUndo.Should().BeFalse();
    }

    [Fact]
    public void CanRedo_IsFalseInitially()
    {
        var state = new EditorState("hello");

        state.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void Cursor_StartsAtLineOneColumnOne()
    {
        var state = new EditorState(string.Empty);

        state.Cursor.Should().Be(new CursorPosition(1, 1));
    }
}
