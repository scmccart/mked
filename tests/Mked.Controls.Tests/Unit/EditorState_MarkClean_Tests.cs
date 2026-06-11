namespace Mked.Controls.Tests;

public class EditorState_MarkClean_Tests
{
    [Fact]
    public void MarkClean_AfterUpdate_ResetsDirtyFlagToFalse()
    {
        var state = new EditorState("initial");
        state.UpdateBuffer("changed");
        state.IsDirty.Should().BeTrue();

        state.MarkClean();

        state.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void MarkClean_SetsNewBaseline_FurtherUpdateReturnsDirty()
    {
        var state = new EditorState("v1");
        state.UpdateBuffer("v2");
        state.MarkClean();
        state.IsDirty.Should().BeFalse();

        state.UpdateBuffer("v3");

        state.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void MarkClean_UndoBackToNewBaseline_IsNotDirty()
    {
        var state = new EditorState("v1");
        state.UpdateBuffer("v2");
        state.MarkClean();    // v2 is now the clean baseline

        state.UpdateBuffer("v3");
        state.IsDirty.Should().BeTrue();

        state.Undo();         // reverts to v2

        state.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void MarkClean_UndoBackPastNewBaseline_IsDirty()
    {
        var state = new EditorState("v1");
        state.UpdateBuffer("v2");
        state.MarkClean();    // v2 is now the clean baseline

        state.Undo();         // reverts to v1 (which is no longer the baseline)

        state.IsDirty.Should().BeTrue();
    }
}
