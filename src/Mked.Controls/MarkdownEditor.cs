using Markdig.Extensions.Yaml;

namespace Mked.Controls;

/// <summary>
/// An interactive, embeddable Markdown editor control. Owns the editing state, syntax
/// highlighting pipeline, undo/redo history, and viewport scroll, and renders as a
/// bounded <see cref="IRenderable"/> region. The host drives it by calling
/// <see cref="HandleKey"/> per keystroke and reading <see cref="Buffer"/> /
/// <see cref="IsDirty"/> for file operations.
/// </summary>
public sealed class MarkdownEditor : IRenderable, IEditorObserver
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseYamlFrontMatter()
        .Build();

    private static readonly IHighlightLayer[] Layers =
    [
        new HeadingHighlightLayer(),
        new EmphasisHighlightLayer(),
        new LinkHighlightLayer(),
        new FrontMatterDimLayer(),
        new CodeFenceLayer(),
    ];

    private readonly EditorState _state;

    // Highlight cache — invalidated when Buffer reference changes.
    private string? _highlightedBuffer;
    private IReadOnlyList<StyledSpan> _cachedSpans = [];

    // Scroll state — updated during Render to keep cursor visible.
    private int _topLineIndex;

    /// <summary>Initialises a new <see cref="MarkdownEditor"/> with an optional initial buffer.</summary>
    public MarkdownEditor(string initialBuffer = "")
    {
        _state = new EditorState(initialBuffer);
        _state.Subscribe(this);
    }

    // ─── Public API ──────────────────────────────────────────────────────────────

    /// <summary>The current text content of the buffer.</summary>
    public string Buffer => _state.Buffer;

    /// <summary>The current cursor position as a 1-based (line, column) tuple.</summary>
    public (int Line, int Column) Cursor => (_state.Cursor.Line, _state.Cursor.Column);

    /// <summary>Returns <see langword="true"/> when the buffer has unsaved changes.</summary>
    public bool IsDirty => _state.IsDirty;

    /// <summary>Number of whitespace-delimited words in the buffer (v1 approximation).</summary>
    public int WordCount => CountWords(_state.Buffer);

    /// <summary>Returns <see langword="true"/> when the last buffer operation can be undone.</summary>
    public bool CanUndo => _state.CanUndo;

    /// <summary>Returns <see langword="true"/> when the last undone operation can be reapplied.</summary>
    public bool CanRedo => _state.CanRedo;

    /// <summary>
    /// Controls whether the block cursor is rendered. Set to <see langword="false"/> when this
    /// pane does not have focus in a split-view layout.
    /// </summary>
    public bool HasFocus { get; set; } = true;

    /// <summary>
    /// Height of the viewport in terminal rows. The host should update this whenever the
    /// terminal is resized. When <see langword="null"/> all buffer lines are rendered.
    /// </summary>
    public int? ViewportHeight { get; set; }

    /// <summary>
    /// Raised after each buffer mutation with the new buffer content.
    /// The host can subscribe to update a live preview pane.
    /// </summary>
    public event Action<string>? BufferChanged;

    /// <summary>
    /// Replaces the buffer with <paramref name="buffer"/> and resets the dirty flag and scroll
    /// position. Call when opening or creating a document.
    /// </summary>
    public void LoadDocument(string buffer)
    {
        _state.UpdateBuffer(buffer);
        _state.MarkClean();
        _topLineIndex = 0;
    }

    /// <summary>Marks the current buffer as the clean baseline. Call after a successful save.</summary>
    public void MarkClean() => _state.MarkClean();

    /// <summary>Returns a snapshot of the status line for this editor frame.</summary>
    public IRenderable StatusLine() =>
        new EditorStatusLine((_state.Cursor.Line, _state.Cursor.Column), _state.IsDirty, WordCount);

    /// <summary>
    /// Processes a keystroke and applies editing or navigation to the internal state.
    /// Returns <see langword="true"/> if the buffer, cursor, or scroll changed (host should redraw).
    /// Returns <see langword="false"/> for unhandled keys (Save, Quit, Open, New, TogglePreview)
    /// so the host can respond to them.
    /// </summary>
    public bool HandleKey(ConsoleKeyInfo key)
    {
        switch (key)
        {
            // ── Undo / Redo ────────────────────────────────────────────────────
            case { Key: ConsoleKey.Z, Modifiers: ConsoleModifiers.Control } when _state.CanUndo:
                _state.Undo();
                return true;

            case { Key: ConsoleKey.Y, Modifiers: ConsoleModifiers.Control } when _state.CanRedo:
                _state.Redo();
                return true;

            // ── Word movement ──────────────────────────────────────────────────
            case { Key: ConsoleKey.LeftArrow, Modifiers: ConsoleModifiers.Control }:
            {
                var p = _state.Cursor;
                _state.MoveCursorWordLeft();
                return _state.Cursor != p;
            }

            case { Key: ConsoleKey.RightArrow, Modifiers: ConsoleModifiers.Control }:
            {
                var p = _state.Cursor;
                _state.MoveCursorWordRight();
                return _state.Cursor != p;
            }

            // ── Arrow keys ─────────────────────────────────────────────────────
            case { Key: ConsoleKey.LeftArrow }:
            {
                var p = _state.Cursor;
                _state.MoveCursorLeft();
                return _state.Cursor != p;
            }

            case { Key: ConsoleKey.RightArrow }:
            {
                var p = _state.Cursor;
                _state.MoveCursorRight();
                return _state.Cursor != p;
            }

            case { Key: ConsoleKey.UpArrow }:
            {
                var p = _state.Cursor;
                _state.MoveCursorUp();
                return _state.Cursor != p;
            }

            case { Key: ConsoleKey.DownArrow }:
            {
                var p = _state.Cursor;
                _state.MoveCursorDown();
                return _state.Cursor != p;
            }

            // ── Line start / end ───────────────────────────────────────────────
            case { Key: ConsoleKey.Home }:
            {
                var p = _state.Cursor;
                _state.MoveCursorToLineStart();
                return _state.Cursor != p;
            }

            case { Key: ConsoleKey.End }:
            {
                var p = _state.Cursor;
                _state.MoveCursorToLineEnd();
                return _state.Cursor != p;
            }

            // ── Deletion ───────────────────────────────────────────────────────
            case { Key: ConsoleKey.Backspace }:
            {
                CursorPosition target = CursorNavigation.MoveLeft(_state.Buffer, _state.Cursor);
                if (target == _state.Cursor) return false;
                _state.Delete(new TextRange(target, _state.Cursor));
                return true;
            }

            case { Key: ConsoleKey.Delete }:
            {
                CursorPosition next = CursorNavigation.MoveRight(_state.Buffer, _state.Cursor);
                if (next == _state.Cursor) return false;
                _state.Delete(new TextRange(_state.Cursor, next));
                return true;
            }

            // ── Enter (newline) ────────────────────────────────────────────────
            case { Key: ConsoleKey.Enter }:
                _state.Insert(_state.Cursor, "\n");
                _state.MoveCursorRight();
                return true;

            // ── Printable characters ───────────────────────────────────────────
            case { KeyChar: char c } when !char.IsControl(c):
                _state.Insert(_state.Cursor, c.ToString());
                _state.MoveCursorRight();
                return true;

            default:
                return false;
        }
    }

    // ─── IEditorObserver ─────────────────────────────────────────────────────────

    void IEditorObserver.OnBufferChanged(string newBuffer)
    {
        RefreshHighlights(newBuffer);
        BufferChanged?.Invoke(newBuffer);
    }

    void IEditorObserver.OnCursorMoved(CursorPosition position) { }

    // ─── IRenderable ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Measurement Measure(RenderOptions options, int maxWidth) =>
        new Measurement(0, maxWidth);

    /// <inheritdoc/>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        RefreshHighlights(_state.Buffer);

        int? viewportHeight = ViewportHeight;
        if (viewportHeight.HasValue)
        {
            // Keep the cursor row within the visible viewport window.
            int editorH = viewportHeight.Value;
            int cursorRow = _state.Cursor.Line - 1; // 0-based
            if (cursorRow - 1 < _topLineIndex)
                _topLineIndex = Math.Max(0, cursorRow - 1);
            else if (cursorRow >= _topLineIndex + editorH)
                _topLineIndex = cursorRow - editorH + 1;
            _topLineIndex = Math.Max(0, _topLineIndex);
        }

        var widget = new MarkdownEditorWidget(
            _state.Buffer,
            (_state.Cursor.Line, _state.Cursor.Column),
            _cachedSpans,
            _topLineIndex,
            viewportHeight,
            HasFocus);

        return widget.Render(options, maxWidth);
    }

    // ─── Private helpers ──────────────────────────────────────────────────────────

    private void RefreshHighlights(string buffer)
    {
        if (ReferenceEquals(buffer, _highlightedBuffer))
            return;

        var doc = Markdown.Parse(buffer, Pipeline);
        var spans = Layers.SelectMany(l => l.Annotate(buffer, doc));
        _cachedSpans = HighlightMapper.Map(spans, buffer);
        _highlightedBuffer = buffer;
    }

    private static int CountWords(string buffer) =>
        buffer.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Length;
}
