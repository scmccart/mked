namespace Mked.Domain;

/// <summary>Observer that receives notifications when editor state changes.</summary>
public interface IEditorObserver
{
    /// <summary>Called when the text buffer is replaced with <paramref name="newBuffer"/>.</summary>
    void OnBufferChanged(string newBuffer);

    /// <summary>Called when the cursor moves to <paramref name="position"/>.</summary>
    void OnCursorMoved(CursorPosition position);
}
