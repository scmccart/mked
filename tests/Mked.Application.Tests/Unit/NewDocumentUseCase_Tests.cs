namespace Mked.Application.Tests.UnitTests;

public sealed class NewDocumentUseCase_Tests
{
    [Fact]
    public void Execute_ReturnsEditorStateWithEmptyBuffer()
    {
        var state = NewDocumentUseCase.Execute();

        state.Buffer.Should().BeEmpty();
    }

    [Fact]
    public void Execute_ReturnsEditorStateWithIsDirtyFalse()
    {
        var state = NewDocumentUseCase.Execute();

        state.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void Execute_ReturnsEditorStateWithCursorAtLineOneColumnOne()
    {
        var state = NewDocumentUseCase.Execute();

        state.Cursor.Should().Be(new CursorPosition(1, 1));
    }

    [Fact]
    public void Execute_ReturnsEditorStateWithCanUndoFalse()
    {
        var state = NewDocumentUseCase.Execute();

        state.CanUndo.Should().BeFalse();
    }

    [Fact]
    public void Execute_CalledTwice_ReturnsDistinctInstances()
    {
        var first = NewDocumentUseCase.Execute();
        var second = NewDocumentUseCase.Execute();

        first.Should().NotBeSameAs(second);
    }
}
