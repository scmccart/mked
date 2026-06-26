namespace Mked.Controls;

/// <summary>
/// Mutable entity representing an active editing session. Stores the current buffer,
/// cursor position, dirty flag, and a command-object undo stack.
/// </summary>
internal sealed class EditorState
{
    private interface IEditorCommand
    {
        public void Apply(EditorState state);

        /// <summary>Captures the current state as the inverse of this command.</summary>
        public IEditorCommand CaptureInverse(EditorState state);

        /// <summary>Fires the observer callbacks appropriate for this command type.</summary>
        public void Notify(EditorState state);
    }

    private sealed class BufferCommand(string before) : IEditorCommand
    {
        public void Apply(EditorState state) => state.SetBufferInternal(before);
        public IEditorCommand CaptureInverse(EditorState state) => new BufferCommand(state.Buffer);
        public void Notify(EditorState state)
        {
            foreach (var observer in state._observers)
                observer.OnBufferChanged(state.Buffer);
        }
    }

    private sealed class CursorCommand(CursorPosition before) : IEditorCommand
    {
        public void Apply(EditorState state) => state.SetCursorInternal(before);
        public IEditorCommand CaptureInverse(EditorState state) => new CursorCommand(state.Cursor);
        public void Notify(EditorState state)
        {
            foreach (var observer in state._observers)
                observer.OnCursorMoved(state.Cursor);
        }
    }

    private string _cleanBuffer;
    private readonly List<IEditorObserver> _observers = [];
    private readonly Stack<IEditorCommand> _undoStack = new();
    private readonly Stack<IEditorCommand> _redoStack = new();
    private bool _isDirty;
    private CursorPosition? _anchor;

    /// <summary>Creates an <see cref="EditorState"/> with the given initial buffer.</summary>
    public EditorState(string initialBuffer)
    {
        ArgumentNullException.ThrowIfNull(initialBuffer);
        _cleanBuffer = initialBuffer;
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

    /// <summary>
    /// The fixed end of the current selection. When non-null and offset-different from
    /// <see cref="Cursor"/>, the selection is the range between the anchor and the cursor.
    /// </summary>
    public CursorPosition? Anchor => _anchor;

    /// <summary>
    /// Returns <see langword="true"/> when a non-empty selection exists (anchor set and
    /// at a different buffer offset than <see cref="Cursor"/>).
    /// </summary>
    public bool HasSelection =>
        _anchor is { } a &&
        BufferOperations.ToOffset(Buffer, a) != BufferOperations.ToOffset(Buffer, Cursor);

    /// <summary>
    /// Returns the normalised selection range (Start offset ≤ End offset).
    /// Only valid when <see cref="HasSelection"/> is <see langword="true"/>.
    /// </summary>
    public TextRange SelectionRange
    {
        get
        {
            var anchor = _anchor!.Value;
            int anchorOffset = BufferOperations.ToOffset(Buffer, anchor);
            int cursorOffset = BufferOperations.ToOffset(Buffer, Cursor);
            return anchorOffset <= cursorOffset
                ? new TextRange(anchor, Cursor)
                : new TextRange(Cursor, anchor);
        }
    }

    /// <summary>
    /// Returns the selected text. Empty string when <see cref="HasSelection"/> is
    /// <see langword="false"/>.
    /// </summary>
    public string SelectedText =>
        HasSelection ? BufferOperations.Substring(Buffer, SelectionRange) : string.Empty;

    /// <summary>
    /// Sets the selection anchor to the current cursor position if no anchor is already set.
    /// Call before every shift-move so the first shift-press locks the anchor.
    /// </summary>
    public void BeginSelection()
    {
        _anchor ??= Cursor;
    }

    /// <summary>Clears the selection anchor. The cursor position is unchanged.</summary>
    public void ClearSelection() => _anchor = null;

    /// <summary>
    /// Records the current buffer as the clean baseline, resetting <see cref="IsDirty"/> to
    /// <see langword="false"/>. Call after a successful save or when loading a new document.
    /// </summary>
    public void MarkClean()
    {
        _cleanBuffer = Buffer;
        _isDirty = false;
    }

    /// <summary>
    /// Replaces the buffer with <paramref name="newBuffer"/> as a fresh baseline, mirroring the
    /// effect of constructing a new <see cref="EditorState"/>: the undo and redo stacks are
    /// cleared, the cursor is reset to (1, 1), and the dirty flag is set to
    /// <see langword="false"/>. Notifies all observers of the buffer and cursor change.
    /// Call when opening or creating a document.
    /// </summary>
    public void Reset(string newBuffer)
    {
        ArgumentNullException.ThrowIfNull(newBuffer);
        _undoStack.Clear();
        _redoStack.Clear();
        _anchor = null;
        _cleanBuffer = newBuffer;
        Buffer = newBuffer;
        Cursor = new CursorPosition(1, 1);
        _isDirty = false;
        foreach (var observer in _observers)
        {
            observer.OnBufferChanged(Buffer);
            observer.OnCursorMoved(Cursor);
        }
    }

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
    /// moves the cursor to <paramref name="range"/>.Start, pushes an undo command,
    /// clears the redo stack, and notifies observers.
    /// </summary>
    public void Delete(TextRange range)
    {
        string newBuffer = BufferOperations.Delete(Buffer, range);
        _undoStack.Push(new BufferCommand(Buffer));
        _redoStack.Clear();
        SetBufferInternal(newBuffer);
        SetCursorInternal(range.Start);
        foreach (var observer in _observers)
        {
            observer.OnBufferChanged(Buffer);
            observer.OnCursorMoved(Cursor);
        }
    }

    /// <summary>
    /// Replaces the characters within <paramref name="range"/> with <paramref name="text"/>,
    /// positions the cursor at the end of the inserted text, clears the selection anchor,
    /// pushes a single undo command, clears the redo stack, and notifies observers.
    /// Passing an empty range performs a pure insertion (equivalent to
    /// <see cref="Insert"/> followed by a cursor advance of <c>text.Length</c> characters).
    /// </summary>
    public void ReplaceRange(TextRange range, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string newBuffer = BufferOperations.ReplaceRange(Buffer, range, text);
        int startOffset = Math.Min(
            BufferOperations.ToOffset(Buffer, range.Start),
            BufferOperations.ToOffset(Buffer, range.End));
        _undoStack.Push(new BufferCommand(Buffer));
        _redoStack.Clear();
        SetBufferInternal(newBuffer);
        SetCursorInternal(BufferOperations.FromOffset(newBuffer, startOffset + text.Length));
        _anchor = null;
        foreach (var observer in _observers)
        {
            observer.OnBufferChanged(Buffer);
            observer.OnCursorMoved(Cursor);
        }
    }

    /// <summary>
    /// Deletes the selected text. No-op when <see cref="HasSelection"/> is
    /// <see langword="false"/>.
    /// </summary>
    public void DeleteSelection()
    {
        if (HasSelection)
            ReplaceRange(SelectionRange, string.Empty);
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
    /// Moves the cursor to the given 1-based (line, column), clamping to the valid range,
    /// and clears the selection anchor. No-op when the clamped position equals the current
    /// cursor and no anchor is set. Does not push to the undo stack.
    /// </summary>
    public void MoveCursorTo(CursorPosition raw)
    {
        CursorPosition next = CursorNavigation.Clamp(Buffer, raw);
        bool anchorCleared = _anchor is not null;
        _anchor = null;
        if (next == Cursor && !anchorCleared) return;
        SetCursorInternal(next);
        foreach (var observer in _observers)
            observer.OnCursorMoved(Cursor);
    }

    /// <summary>
    /// Reverts the most recent buffer or cursor change. Clears the selection anchor.
    /// No-op when <see cref="CanUndo"/> is <see langword="false"/>.
    /// </summary>
    public void Undo()
    {
        if (!CanUndo) return;
        _anchor = null;
        IEditorCommand cmd = _undoStack.Pop();
        _redoStack.Push(cmd.CaptureInverse(this));
        cmd.Apply(this);
        cmd.Notify(this);
    }

    /// <summary>
    /// Reapplies the most recently undone change. Clears the selection anchor.
    /// No-op when <see cref="CanRedo"/> is <see langword="false"/>.
    /// </summary>
    public void Redo()
    {
        if (!CanRedo) return;
        _anchor = null;
        IEditorCommand cmd = _redoStack.Pop();
        _undoStack.Push(cmd.CaptureInverse(this));
        cmd.Apply(this);
        cmd.Notify(this);
    }

    private void SetBufferInternal(string buffer)
    {
        Buffer = buffer;
        Cursor = CursorNavigation.Clamp(buffer, Cursor);
        _isDirty = buffer != _cleanBuffer;
    }

    private void SetCursorInternal(CursorPosition position) => Cursor = position;
}
