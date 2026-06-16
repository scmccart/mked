namespace Mked.Controls;

/// <summary>Observer that receives notifications when editor state changes.</summary>
internal interface IEditorObserver
{
    /// <summary>Called when the text buffer is replaced with <paramref name="newBuffer"/>.</summary>
    public void OnBufferChanged(string newBuffer);

    /// <summary>Called when the cursor moves to <paramref name="position"/>.</summary>
    public void OnCursorMoved(CursorPosition position);
}
