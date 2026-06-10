using Spectre.Console.Rendering;

namespace Mked.Console;

/// <summary>
/// The <c>mked edit</c> command. Opens a file (or a blank document) in an interactive
/// Markdown editor with syntax highlighting, undo/redo, and save support.
/// </summary>
public sealed class EditCommand : AsyncCommand<EditSettings>
{
    private sealed class EditSession
    {
        public string? FilePath;
        public bool SplitEnabled;
        public bool Cancelled;
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(
        CommandContext context, EditSettings settings, CancellationToken cancellationToken)
    {
        EditorState state;
        var session = new EditSession
        {
            SplitEnabled = settings.Split,
        };

        if (settings.Path is not null)
        {
            var result = await new OpenFileUseCase(new FileSystemReader()).ExecuteAsync(settings.Path);
            if (result is Result<OpenedFile, MkedError>.Err(var openErr))
            {
                AnsiConsole.MarkupLine($"[red bold]Error:[/] {Markup.Escape(FormatError(openErr))}");
                return 1;
            }
            var file = ((Result<OpenedFile, MkedError>.Ok)result).Value;
            state = new EditorState(file.Source);
            session.FilePath = settings.Path;
        }
        else
        {
            state = NewDocumentUseCase.Execute();
        }

        IHighlightLayer[] layers =
        [
            new HeadingHighlightLayer(),
            new EmphasisHighlightLayer(),
            new LinkHighlightLayer(),
            new FrontMatterDimLayer(),
            new CodeFenceLayer(),
        ];

        int topLineIndex = 0;
        int lastW = System.Console.WindowWidth;
        int lastH = System.Console.WindowHeight;
        int h = lastH;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        System.Console.CursorVisible = false;
        bool dirty = true;

        try
        {
            while (!session.Cancelled && !cts.Token.IsCancellationRequested)
            {
                while (System.Console.KeyAvailable)
                {
                    var key = System.Console.ReadKey(intercept: true);
                    EditorAction action = MapKey(key);
                    if (await ApplyActionAsync(action, state, session))
                        dirty = true;
                }

                int newW = System.Console.WindowWidth;
                int newH = System.Console.WindowHeight;
                if (newW != lastW || newH != lastH)
                {
                    h = newH;
                    lastW = newW;
                    lastH = newH;
                    dirty = true;
                }

                if (dirty)
                {
                    int editorH = h - 1;
                    int cursorRow = state.Cursor.Line - 1;
                    if (cursorRow - 1 < topLineIndex)
                        topLineIndex = Math.Max(0, cursorRow - 1);
                    else if (cursorRow >= topLineIndex + editorH)
                        topLineIndex = cursorRow - editorH + 1;
                    topLineIndex = Math.Max(0, topLineIndex);

                    IReadOnlyList<StyledSpan> spans = RunHighlightPipeline(state.Buffer, layers);
                    System.Console.Write("\x1B[?2026h\x1B[H");
                    AnsiConsole.Write(new ErasedWidget(BuildWidget(state, spans, topLineIndex, h, session.SplitEnabled)));
                    System.Console.Write("\x1B[?2026l");
                    dirty = false;
                }

                await Task.Delay(16, cts.Token);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            System.Console.CursorVisible = true;
        }

        return 0;
    }

    // ─── Key mapping (pure) ───────────────────────────────────────────────────

    private static EditorAction MapKey(ConsoleKeyInfo key) => key switch
    {
        { Key: ConsoleKey.Z, Modifiers: ConsoleModifiers.Control } => new EditorAction.UndoAction(),
        { Key: ConsoleKey.Y, Modifiers: ConsoleModifiers.Control } => new EditorAction.RedoAction(),
        { Key: ConsoleKey.S, Modifiers: ConsoleModifiers.Control } => new EditorAction.SaveFile(),
        { Key: ConsoleKey.N, Modifiers: ConsoleModifiers.Control } => new EditorAction.NewFile(),
        { Key: ConsoleKey.O, Modifiers: ConsoleModifiers.Control } => new EditorAction.OpenFile(),
        { Key: ConsoleKey.P, Modifiers: ConsoleModifiers.Control } => new EditorAction.TogglePreview(),
        { Key: ConsoleKey.Q, Modifiers: ConsoleModifiers.Control } => new EditorAction.Quit(),
        { Key: ConsoleKey.LeftArrow, Modifiers: ConsoleModifiers.Control } => new EditorAction.MoveWordCursor(Direction.Left),
        { Key: ConsoleKey.RightArrow, Modifiers: ConsoleModifiers.Control } => new EditorAction.MoveWordCursor(Direction.Right),
        { Key: ConsoleKey.LeftArrow } => new EditorAction.MoveCursor(Direction.Left),
        { Key: ConsoleKey.RightArrow } => new EditorAction.MoveCursor(Direction.Right),
        { Key: ConsoleKey.UpArrow } => new EditorAction.MoveCursor(Direction.Up),
        { Key: ConsoleKey.DownArrow } => new EditorAction.MoveCursor(Direction.Down),
        { Key: ConsoleKey.Home } => new EditorAction.MoveToLineStart(),
        { Key: ConsoleKey.End } => new EditorAction.MoveToLineEnd(),
        { Key: ConsoleKey.Enter } => new EditorAction.InsertChar('\n'),
        { Key: ConsoleKey.Backspace } => new EditorAction.DeleteBackward(),
        { Key: ConsoleKey.Delete } => new EditorAction.DeleteForward(),
        { KeyChar: char c } when !char.IsControl(c) => new EditorAction.InsertChar(c),
        _ => new EditorAction.None(),
    };

    // ─── Action dispatch ──────────────────────────────────────────────────────

    private static async Task<bool> ApplyActionAsync(
        EditorAction action, EditorState state, EditSession session)
    {
        switch (action)
        {
            case EditorAction.InsertChar(var c):
                state.Insert(state.Cursor, c.ToString());
                state.MoveCursorRight();
                return true;

            case EditorAction.DeleteBackward:
            {
                CursorPosition target = CursorNavigation.MoveLeft(state.Buffer, state.Cursor);
                if (target != state.Cursor)
                {
                    // Delete repositions the cursor to range.Start automatically.
                    state.Delete(new TextRange(target, state.Cursor));
                    return true;
                }
                return false;
            }

            case EditorAction.DeleteForward:
            {
                CursorPosition next = CursorNavigation.MoveRight(state.Buffer, state.Cursor);
                if (next != state.Cursor)
                {
                    state.Delete(new TextRange(state.Cursor, next));
                    return true;
                }
                return false;
            }

            case EditorAction.MoveCursor(Direction.Left):
            { var p = state.Cursor; state.MoveCursorLeft(); return state.Cursor != p; }

            case EditorAction.MoveCursor(Direction.Right):
            { var p = state.Cursor; state.MoveCursorRight(); return state.Cursor != p; }

            case EditorAction.MoveCursor(Direction.Up):
            { var p = state.Cursor; state.MoveCursorUp(); return state.Cursor != p; }

            case EditorAction.MoveCursor(Direction.Down):
            { var p = state.Cursor; state.MoveCursorDown(); return state.Cursor != p; }

            case EditorAction.MoveWordCursor(Direction.Left):
            { var p = state.Cursor; state.MoveCursorWordLeft(); return state.Cursor != p; }

            case EditorAction.MoveWordCursor(Direction.Right):
            { var p = state.Cursor; state.MoveCursorWordRight(); return state.Cursor != p; }

            case EditorAction.MoveToLineStart:
            { var p = state.Cursor; state.MoveCursorToLineStart(); return state.Cursor != p; }

            case EditorAction.MoveToLineEnd:
            { var p = state.Cursor; state.MoveCursorToLineEnd(); return state.Cursor != p; }

            case EditorAction.UndoAction:
                if (state.CanUndo)
                {
                    state.Undo();
                    return true;
                }
                return false;

            case EditorAction.RedoAction:
                if (state.CanRedo)
                {
                    state.Redo();
                    return true;
                }
                return false;

            case EditorAction.SaveFile:
                await SaveAsync(session, state.Buffer);
                return true;

            case EditorAction.Quit:
                await HandleQuitAsync(state, session);
                return true;

            case EditorAction.NewFile:
                return await HandleNewFileAsync(state, session);

            case EditorAction.OpenFile:
                return await HandleOpenFileAsync(state, session);

            case EditorAction.TogglePreview:
                session.SplitEnabled = !session.SplitEnabled;
                return true;

            default:
                return false;
        }
    }

    // ─── Cursor-visibility helper ─────────────────────────────────────────────

    /// <summary>
    /// Temporarily restores the terminal cursor around an interactive <paramref name="prompt"/>
    /// and hides it again afterwards. Ensures visible caret during AnsiConsole.Ask/Prompt calls.
    /// </summary>
    private static T WithCursorVisible<T>(Func<T> prompt)
    {
        System.Console.CursorVisible = true;
        try { return prompt(); }
        finally { System.Console.CursorVisible = false; }
    }

    // ─── File operations ──────────────────────────────────────────────────────

    private static async Task SaveAsync(EditSession session, string content)
    {
        if (session.FilePath is null)
            session.FilePath = WithCursorVisible(() => AnsiConsole.Ask<string>("Save as: "));

        var result = await new SaveFileUseCase(new FileSystemWriter()).ExecuteAsync(session.FilePath, content);
        if (result is Result<Unit, MkedError>.Err(var err))
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(FormatError(err))}[/]");
    }

    private static async Task HandleQuitAsync(EditorState state, EditSession session)
    {
        if (state.IsDirty)
        {
            var choice = WithCursorVisible(() => AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Unsaved changes. Save before quitting?")
                    .AddChoices("Save and quit", "Quit without saving", "Cancel")));

            if (choice == "Save and quit")
            {
                await SaveAsync(session, state.Buffer);
                session.Cancelled = true;
            }
            else if (choice == "Quit without saving")
            {
                session.Cancelled = true;
            }
        }
        else
        {
            session.Cancelled = true;
        }
    }

    private static async Task<bool> HandleNewFileAsync(EditorState state, EditSession session)
    {
        if (state.IsDirty)
        {
            var choice = WithCursorVisible(() => AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Unsaved changes. Save before opening a new document?")
                    .AddChoices("Save and new", "Discard and new", "Cancel")));

            if (choice == "Save and new")
                await SaveAsync(session, state.Buffer);
            else if (choice == "Cancel")
                return false;
        }

        EditorState fresh = NewDocumentUseCase.Execute();
        state.UpdateBuffer(fresh.Buffer);
        state.UpdateCursor(fresh.Cursor);
        session.FilePath = null;
        return true;
    }

    private static async Task<bool> HandleOpenFileAsync(EditorState state, EditSession session)
    {
        if (state.IsDirty)
        {
            var choice = WithCursorVisible(() => AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Unsaved changes. Save before opening another file?")
                    .AddChoices("Save and open", "Discard and open", "Cancel")));

            if (choice == "Save and open")
                await SaveAsync(session, state.Buffer);
            else if (choice == "Cancel")
                return false;
        }

        string path = WithCursorVisible(() => AnsiConsole.Ask<string>("Open file: "));
        var result = await new OpenFileUseCase(new FileSystemReader()).ExecuteAsync(path);

        if (result is Result<OpenedFile, MkedError>.Ok(var openedFile))
        {
            state.UpdateBuffer(openedFile.Source);
            state.UpdateCursor(new CursorPosition(1, 1));
            session.FilePath = path;
            return true;
        }

        if (result is Result<OpenedFile, MkedError>.Err(var err))
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(FormatError(err))}[/]");

        return false;
    }

    // ─── Rendering helpers ────────────────────────────────────────────────────

    private static IRenderable BuildWidget(
        EditorState state,
        IReadOnlyList<StyledSpan> styledSpans,
        int topLineIndex,
        int viewportHeight,
        bool splitEnabled)
    {
        int wordCount = CountWords(state.Buffer);
        var editorWidget = new MarkdownEditorWidget(
            state.Buffer,
            (state.Cursor.Line, state.Cursor.Column),
            styledSpans,
            topLineIndex,
            viewportHeight - 1);
        var statusLine = new EditorStatusLine(
            (state.Cursor.Line, state.Cursor.Column),
            state.IsDirty,
            wordCount);

        if (!splitEnabled)
            return new VerticalLayout(editorWidget, statusLine);

        var preview = new MarkdownViewer(state.Buffer)
        {
            ShowFrontmatter = false,
            ViewportHeight = viewportHeight - 1,
        };

        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Main").SplitColumns(
                    new Layout("Editor"),
                    new Layout("Preview")),
                new Layout("Status"));

        layout["Editor"].Update(editorWidget);
        layout["Preview"].Update(preview);
        layout["Status"].Update(statusLine);
        layout["Status"].Size(1);

        return layout;
    }

    private static IReadOnlyList<StyledSpan> RunHighlightPipeline(string buffer, IHighlightLayer[] layers)
    {
        var doc = MarkdownDocument.Parse(buffer);
        var spans = layers.SelectMany(l => l.Annotate(buffer, doc));
        return HighlightMapper.Map(spans, buffer);
    }

    // Renders two widgets back-to-back with a single LineBreak between them and no
    // trailing newline, keeping total height == top.height + 1 + bottom.height.
    // Avoids Rows, which emits a trailing LineBreak that pushes rendered height past
    // the terminal boundary and causes LiveDisplay to scroll on every frame.
    // Wraps any renderable and inserts \x1B[K (erase-to-EOL) before each line break and
    // at the end, clearing leftover characters from longer previous renders without a full
    // screen clear (which causes flicker).
    private sealed class ErasedWidget(IRenderable inner) : IRenderable
    {
        private static readonly Segment Erase = new("\x1B[K", Style.Plain);

        public Measurement Measure(RenderOptions options, int maxWidth) =>
            inner.Measure(options, maxWidth);

        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            foreach (Segment seg in inner.Render(options, maxWidth))
            {
                if (seg.IsLineBreak)
                    yield return Erase;
                yield return seg;
            }
            yield return Erase;
        }
    }

    private sealed class VerticalLayout(IRenderable top, IRenderable bottom) : IRenderable
    {
        public Measurement Measure(RenderOptions options, int maxWidth) =>
            new Measurement(0, maxWidth);

        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            foreach (Segment seg in top.Render(options, maxWidth))
                yield return seg;
            yield return Segment.LineBreak;
            foreach (Segment seg in bottom.Render(options, maxWidth))
                yield return seg;
        }
    }

    private static int CountWords(string buffer) =>
        buffer.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Length;

    private static string FormatError(MkedError error) => error switch
    {
        MkedError.IoError e => $"{e.Path}: {e.Reason}",
        MkedError.ValidationError e => $"{e.Field}: {e.Message}",
        _ => error.ToString() ?? "Unknown error",
    };
}
