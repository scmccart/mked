namespace Mked.Domain;

/// <summary>
/// Mutable entity representing an active editing session. Stores the current buffer,
/// cursor position, dirty flag, and a command-object undo stack.
/// </summary>
public sealed class EditorState
{
    private interface IEditorCommand
    {
        public void Apply(EditorState state);
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
    private bool _isDirty;

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
    /// Cached to avoid O(n) string comparison on every access.
    /// </summary>
    public bool IsDirty => _isDirty;

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

    /// <summary>
    /// Inserts <paramref name="text"/> into the buffer at <paramref name="position"/>,
    /// pushes an undo command, clears the redo stack, and notifies observers.
    /// </summary>
    public void Insert(CursorPosition position, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string newBuffer = BufferOperations.Insert(Buffer, position, text);
        _undoStack.Push(new BufferCommand(Buffer));
        _redoStack.Clear();
        SetBufferInternal(newBuffer);
        foreach (var observer in _observers)
            observer.OnBufferChanged(Buffer);
    }

    /// <summary>
    /// Deletes the characters within <paramref name="range"/> from the buffer,
    /// pushes an undo command, clears the redo stack, and notifies observers.
    /// </summary>
    public void Delete(TextRange range)
    {
        string newBuffer = BufferOperations.Delete(Buffer, range);
        _undoStack.Push(new BufferCommand(Buffer));
        _redoStack.Clear();
        SetBufferInternal(newBuffer);
        foreach (var observer in _observers)
            observer.OnBufferChanged(Buffer);
    }

    /// <summary>
    /// Moves the cursor one character to the left. No-op at the start of the buffer.
    /// Does not push to the undo stack.
    /// </summary>
    public void MoveCursorLeft()
    {
        CursorPosition next = CursorNavigation.MoveLeft(Buffer, Cursor);
        if (next == Cursor) return;
        SetCursorInternal(next);
        foreach (var observer in _observers)
            observer.OnCursorMoved(Cursor);
    }

    /// <summary>
    /// Moves the cursor one character to the right. No-op at the end of the buffer.
    /// Does not push to the undo stack.
    /// </summary>
    public void MoveCursorRight()
    {
        CursorPosition next = CursorNavigation.MoveRight(Buffer, Cursor);
        if (next == Cursor) return;
        SetCursorInternal(next);
        foreach (var observer in _observers)
            observer.OnCursorMoved(Cursor);
    }

    /// <summary>
    /// Moves the cursor up one line. No-op when already on the first line.
    /// Does not push to the undo stack.
    /// </summary>
    public void MoveCursorUp()
    {
        CursorPosition next = CursorNavigation.MoveUp(Buffer, Cursor);
        if (next == Cursor) return;
        SetCursorInternal(next);
        foreach (var observer in _observers)
            observer.OnCursorMoved(Cursor);
    }

    /// <summary>
    /// Moves the cursor down one line. No-op when already on the last line.
    /// Does not push to the undo stack.
    /// </summary>
    public void MoveCursorDown()
    {
        CursorPosition next = CursorNavigation.MoveDown(Buffer, Cursor);
        if (next == Cursor) return;
        SetCursorInternal(next);
        foreach (var observer in _observers)
            observer.OnCursorMoved(Cursor);
    }

    /// <summary>
    /// Moves the cursor left past whitespace and then past a word boundary.
    /// Does not push to the undo stack.
    /// </summary>
    public void MoveCursorWordLeft()
    {
        CursorPosition next = CursorNavigation.MoveWordLeft(Buffer, Cursor);
        if (next == Cursor) return;
        SetCursorInternal(next);
        foreach (var observer in _observers)
            observer.OnCursorMoved(Cursor);
    }

    /// <summary>
    /// Moves the cursor right past a word and then past whitespace.
    /// Does not push to the undo stack.
    /// </summary>
    public void MoveCursorWordRight()
    {
        CursorPosition next = CursorNavigation.MoveWordRight(Buffer, Cursor);
        if (next == Cursor) return;
        SetCursorInternal(next);
        foreach (var observer in _observers)
            observer.OnCursorMoved(Cursor);
    }

    /// <summary>
    /// Moves the cursor to column 1 of the current line. No-op when already at column 1.
    /// Does not push to the undo stack.
    /// </summary>
    public void MoveCursorToLineStart()
    {
        CursorPosition next = CursorNavigation.MoveToLineStart(Buffer, Cursor);
        if (next == Cursor) return;
        SetCursorInternal(next);
        foreach (var observer in _observers)
            observer.OnCursorMoved(Cursor);
    }

    /// <summary>
    /// Moves the cursor to one past the last character on the current line.
    /// No-op when already at the end of the line. Does not push to the undo stack.
    /// </summary>
    public void MoveCursorToLineEnd()
    {
        CursorPosition next = CursorNavigation.MoveToLineEnd(Buffer, Cursor);
        if (next == Cursor) return;
        SetCursorInternal(next);
        foreach (var observer in _observers)
            observer.OnCursorMoved(Cursor);
    }

    /// <summary>
    /// Reverts the most recent buffer or cursor change. No-op when <see cref="CanUndo"/> is <see langword="false"/>.
    /// </summary>
    public void Undo()
    {
        if (!CanUndo) return;
        IEditorCommand cmd = _undoStack.Pop();
        _redoStack.Push(new BufferCommand(Buffer));
        cmd.Apply(this);
        foreach (var observer in _observers)
        {
            observer.OnBufferChanged(Buffer);
            observer.OnCursorMoved(Cursor);
        }
    }

    /// <summary>
    /// Reapplies the most recently undone change. No-op when <see cref="CanRedo"/> is <see langword="false"/>.
    /// </summary>
    public void Redo()
    {
        if (!CanRedo) return;
        IEditorCommand cmd = _redoStack.Pop();
        _undoStack.Push(new BufferCommand(Buffer));
        cmd.Apply(this);
        foreach (var observer in _observers)
        {
            observer.OnBufferChanged(Buffer);
            observer.OnCursorMoved(Cursor);
        }
    }

    private void SetBufferInternal(string buffer)
    {
        Buffer = buffer;
        _isDirty = buffer != _initialBuffer;
    }

    private void SetCursorInternal(CursorPosition position) => Cursor = position;
}
