namespace Mked.Application;

/// <summary>Creates a blank editing session for <c>mked edit</c> with no file argument.</summary>
public sealed class NewDocumentUseCase
{
    /// <summary>Returns a fresh <see cref="EditorState"/> with an empty buffer.</summary>
    public static EditorState Execute() => new EditorState(string.Empty);
}
