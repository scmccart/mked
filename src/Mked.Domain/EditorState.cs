namespace Mked.Domain;

/// <summary>
/// Mutable entity representing an active editing session. Stores the current buffer,
/// cursor position, dirty flag, and a command-object undo stack.
/// </summary>
public sealed class EditorState
{
    private interface IEditorCommand
    {
        void Apply(EditorState state);
    }

    private sealed class BufferCommand(string before) : IEditorCommand
    {
        public void Apply(EditorState state) => state.SetBufferInternal(before);
    }

    private sealed class CursorCommand(CursorPosition before) : IEditorCommand
    {
        public void Apply(EditorState state) => state.SetCursorInternal(before);
    }

    private readonly string _initialBuffer;
    private readonly List<IEditorObserver> _observers = [];
    private readonly Stack<IEditorCommand> _undoStack = new();
    private readonly Stack<IEditorCommand> _redoStack = new();

    /// <summary>Creates an <see cref="EditorState"/> with the given initial buffer.</summary>
    public EditorState(string initialBuffer)
    {
        ArgumentNullException.ThrowIfNull(initialBuffer);
        _initialBuffer = initialBuffer;
        Buffer = initialBuffer;
        Cursor = new CursorPosition(1, 1);
    }

    /// <summary>The current text content of the buffer.</summary>
    public string Buffer { get; private set; }

    /// <summary>The current cursor location.</summary>
    public CursorPosition Cursor { get; private set; }

    /// <summary>
    /// Returns <see langword="true"/> when the buffer differs from its initial value.
    /// </summary>
    public bool IsDirty => Buffer != _initialBuffer;

    /// <summary>Returns <see langword="true"/> when <c>Undo</c> can be called.</summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>Returns <see langword="true"/> when <c>Redo</c> can be called.</summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>Registers <paramref name="observer"/> to receive future change notifications.</summary>
    public void Subscribe(IEditorObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        _observers.Add(observer);
    }

    /// <summary>Replaces the buffer with <paramref name="newBuffer"/> and notifies observers.</summary>
    public void UpdateBuffer(string newBuffer)
    {
        ArgumentNullException.ThrowIfNull(newBuffer);
        _undoStack.Push(new BufferCommand(Buffer));
        _redoStack.Clear();
        SetBufferInternal(newBuffer);
        foreach (var observer in _observers)
            observer.OnBufferChanged(Buffer);
    }

    /// <summary>Moves the cursor to <paramref name="position"/> and notifies observers.</summary>
    public void UpdateCursor(CursorPosition position)
    {
        _undoStack.Push(new CursorCommand(Cursor));
        _redoStack.Clear();
        SetCursorInternal(position);
        foreach (var observer in _observers)
            observer.OnCursorMoved(Cursor);
    }

    private void SetBufferInternal(string buffer) => Buffer = buffer;

    private void SetCursorInternal(CursorPosition position) => Cursor = position;
}
