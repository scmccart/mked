using System.Diagnostics.CodeAnalysis;

namespace Mked.Console;

/// <summary>
/// The <c>mked edit</c> command. Opens a file (or a blank document) in an interactive
/// Markdown editor with syntax highlighting, undo/redo, and save support.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class EditCommand(OpenFileUseCase openFile, SaveFileUseCase saveFile)
    : AsyncCommand<EditSettings>
{
    private readonly OpenFileUseCase _openFile = openFile;
    private readonly SaveFileUseCase _saveFile = saveFile;

    private enum HostAction { None, Save, New, Open, Quit }

    private sealed class EditSession
    {
        public string? FilePath;
        public bool SplitEnabled;
        public bool Cancelled;
        public HostAction PendingAction;
        public int PreviewTopLine;
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(
        CommandContext context, EditSettings settings, CancellationToken cancellationToken)
    {
        var session = new EditSession { SplitEnabled = settings.Split };
        var editor = new MarkdownEditor();

        if (settings.Path is not null)
        {
            var result = await _openFile.ExecuteAsync(settings.Path);
            if (result is Result<OpenedFile, MkedError>.Err(var openErr))
                return ErrorPresenter.Show(openErr);
            var file = ((Result<OpenedFile, MkedError>.Ok)result).Value;
            editor.LoadDocument(file.Source);
            session.FilePath = settings.Path;
        }

        // Track the latest buffer for the preview pane without re-parsing on every frame.
        string previewSource = editor.Buffer;
        bool previewSourceChanged = false;
        editor.BufferChanged += md => { previewSource = md; previewSourceChanged = true; };

        using var outerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var lifecycle = new TerminalLifecycle(outerCts);
        using var input = ConsoleInputSource.Create();

        while (!session.Cancelled && !outerCts.Token.IsCancellationRequested)
        {
            session.PendingAction = HostAction.None;

            int h = AnsiConsole.Profile.Height;
            editor.ViewportHeight = ComputeViewportHeight(h, session.SplitEnabled);

            // Build the initial preview instance. In the dirty path below, `with`-copies are used
            // for scroll/resize changes so the parsed AST and render cache are reused; a full
            // reconstruction only happens when the source text actually changes.
            var preview = new MarkdownViewer(previewSource)
            {
                ShowFrontmatter = false,
                ViewportHeight = editor.ViewportHeight,
                TopLineIndex = session.PreviewTopLine,
            };
            previewSourceChanged = false;

            await AnsiConsole.Live(BuildLayout(editor, preview, session)).StartAsync(async liveCtx =>
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(outerCts.Token);
                liveCtx.UpdateTarget(BuildLayout(editor, preview, session));

                int lastW = AnsiConsole.Profile.Width;
                int lastH = h;

                try
                {
                    using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(16));
                    while (!cts.Token.IsCancellationRequested)
                    {
                        bool dirty = false;

                        while (input.TryRead(out var ev))
                        {
                            // ── Mouse wheel ───────────────────────────────────────────────────
                            if (ev.Kind == InputEventKind.Wheel)
                            {
                                int delta = ev.WheelDelta * ConsoleInputSource.LinesPerNotch;
                                if (session.SplitEnabled && !editor.HasFocus)
                                {
                                    int viewportH = editor.ViewportHeight ?? h;
                                    int maxLine = Math.Max(0, preview.ScrollInfo.TotalLineCount - viewportH);
                                    session.PreviewTopLine = Math.Clamp(session.PreviewTopLine + delta, 0, maxLine);
                                }
                                else
                                {
                                    editor.Scroll(delta);
                                }
                                dirty = true;
                                continue;
                            }

                            // ── Mouse click — move editor cursor or switch pane focus ─────────
                            if (ev.Kind == InputEventKind.Click)
                            {
                                int W = AnsiConsole.Profile.Width;
                                bool split = session.SplitEnabled;

                                if (split && ev.ClickX >= W / 2)
                                {
                                    // Right half → switch focus to the preview pane.
                                    editor.HasFocus = false;
                                }
                                else
                                {
                                    // Left half (or non-split) → focus editor and move cursor.
                                    if (split) editor.HasFocus = true;

                                    // In split mode the editor pane is wrapped in a Panel with a
                                    // 1-cell rounded border on every side, so content starts at
                                    // screen (row 1, col 1). In non-split mode there is no border.
                                    int originRow = split ? 1 : 0;
                                    int originCol = split ? 1 : 0;
                                    int relRow = ev.ClickY - originRow;
                                    int relCol = ev.ClickX - originCol;

                                    // Ignore clicks on the status bar row (below the viewport).
                                    if (relRow >= 0 && ev.ClickY < h)
                                    {
                                        int line = editor.TopLineIndex + relRow + 1;  // 1-based
                                        int col  = Math.Max(0, relCol) + 1;           // 1-based
                                        editor.MoveCursorTo(line, col);
                                    }
                                }
                                dirty = true;
                                continue;
                            }

                            var key = ev.Key;

                            // ── Host-level keys — always handled regardless of focus ──────────
                            switch (key)
                            {
                                case { Key: ConsoleKey.S, Modifiers: ConsoleModifiers.Control }:
                                    session.PendingAction = HostAction.Save;
                                    await cts.CancelAsync();
                                    return;

                                case { Key: ConsoleKey.Q, Modifiers: ConsoleModifiers.Control }:
                                    session.PendingAction = HostAction.Quit;
                                    await cts.CancelAsync();
                                    return;

                                case { Key: ConsoleKey.N, Modifiers: ConsoleModifiers.Control }:
                                    session.PendingAction = HostAction.New;
                                    await cts.CancelAsync();
                                    return;

                                case { Key: ConsoleKey.O, Modifiers: ConsoleModifiers.Control }:
                                    session.PendingAction = HostAction.Open;
                                    await cts.CancelAsync();
                                    return;

                                case { Key: ConsoleKey.P, Modifiers: ConsoleModifiers.Control }:
                                    session.SplitEnabled = !session.SplitEnabled;
                                    // When closing the split, always return focus to the editor.
                                    if (!session.SplitEnabled)
                                        editor.HasFocus = true;
                                    editor.ViewportHeight = ComputeViewportHeight(h, session.SplitEnabled);
                                    dirty = true;
                                    continue;
                            }

                            // ── Shift+Tab — flip focus between editor and preview (split only) ─
                            if (key is { Key: ConsoleKey.Tab, Modifiers: ConsoleModifiers.Shift }
                                && session.SplitEnabled)
                            {
                                editor.HasFocus = !editor.HasFocus;
                                dirty = true;
                                continue;
                            }

                            // ── Preview focused — route scroll keys to the preview pane ───────
                            if (session.SplitEnabled && !editor.HasFocus)
                            {
                                int viewportH = editor.ViewportHeight ?? h;
                                int maxLine = Math.Max(0, preview.ScrollInfo.TotalLineCount - viewportH);

                                switch (key.Key)
                                {
                                    case ConsoleKey.UpArrow:
                                        session.PreviewTopLine = Math.Max(session.PreviewTopLine - 1, 0);
                                        dirty = true;
                                        break;

                                    case ConsoleKey.DownArrow:
                                        session.PreviewTopLine = Math.Min(session.PreviewTopLine + 1, maxLine);
                                        dirty = true;
                                        break;

                                    case ConsoleKey.PageUp:
                                        session.PreviewTopLine = Math.Max(session.PreviewTopLine - Math.Max(1, viewportH / 2), 0);
                                        dirty = true;
                                        break;

                                    case ConsoleKey.PageDown:
                                        session.PreviewTopLine = Math.Min(session.PreviewTopLine + Math.Max(1, viewportH / 2), maxLine);
                                        dirty = true;
                                        break;

                                    case ConsoleKey.Home:
                                        session.PreviewTopLine = 0;
                                        dirty = true;
                                        break;

                                    case ConsoleKey.End:
                                        session.PreviewTopLine = maxLine;
                                        dirty = true;
                                        break;
                                }

                                continue;
                            }

                            // ── Editor focused — delegate editing / navigation / undo-redo ────
                            if (editor.HandleKey(key))
                                dirty = true;
                        }

                        int newW = AnsiConsole.Profile.Width;
                        int newH = AnsiConsole.Profile.Height;
                        if (newW != lastW || newH != lastH)
                        {
                            editor.ViewportHeight = ComputeViewportHeight(newH, session.SplitEnabled);
                            h = newH;
                            lastW = newW;
                            lastH = newH;
                            dirty = true;
                        }

                        if (dirty)
                        {
                            // Re-parse only when the buffer actually changed; for scroll /
                            // resize ticks reuse the existing AST and render cache via with.
                            preview = previewSourceChanged
                                ? new MarkdownViewer(previewSource)
                                {
                                    ShowFrontmatter = false,
                                    ViewportHeight = editor.ViewportHeight,
                                    TopLineIndex = session.PreviewTopLine,
                                }
                                : preview with
                                {
                                    ViewportHeight = editor.ViewportHeight,
                                    TopLineIndex = session.PreviewTopLine,
                                };
                            previewSourceChanged = false;
                            liveCtx.UpdateTarget(BuildLayout(editor, preview, session));
                        }

                        await timer.WaitForNextTickAsync(cts.Token);
                    }
                }
                catch (OperationCanceledException) { }
            });

            // Handle host-level actions outside the live display so prompts render normally.
            switch (session.PendingAction)
            {
                case HostAction.Save:
                    await SaveAsync(session, editor, _saveFile);
                    break;

                case HostAction.Quit:
                    await HandleQuitAsync(session, editor, _saveFile);
                    break;

                case HostAction.New:
                    await HandleNewAsync(session, editor, _saveFile);
                    break;

                case HostAction.Open:
                    await HandleOpenAsync(session, editor, _openFile, _saveFile);
                    break;
            }
        }

        return 0;
    }

    // ─── File operations ──────────────────────────────────────────────────────

    private static async Task SaveAsync(EditSession session, MarkdownEditor editor, SaveFileUseCase saveFile)
    {
        if (session.FilePath is null)
            session.FilePath = AnsiConsole.Ask<string>("Save as: ");

        var result = await saveFile.ExecuteAsync(session.FilePath, editor.Buffer);
        if (result is Result<Unit, MkedError>.Err(var err))
            ErrorPresenter.Show(err);
        else
            editor.MarkClean();
    }

    private static async Task HandleQuitAsync(EditSession session, MarkdownEditor editor, SaveFileUseCase saveFile)
    {
        if (!editor.IsDirty)
        {
            session.Cancelled = true;
            return;
        }

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Unsaved changes. Save before quitting?")
                .AddChoices("Save and quit", "Quit without saving", "Cancel"));

        if (choice == "Save and quit")
        {
            await SaveAsync(session, editor, saveFile);
            session.Cancelled = true;
        }
        else if (choice == "Quit without saving")
        {
            session.Cancelled = true;
        }
    }

    private static async Task HandleNewAsync(EditSession session, MarkdownEditor editor, SaveFileUseCase saveFile)
    {
        if (editor.IsDirty)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Unsaved changes. Save before opening a new document?")
                    .AddChoices("Save and new", "Discard and new", "Cancel"));

            if (choice == "Save and new")
                await SaveAsync(session, editor, saveFile);
            else if (choice == "Cancel")
                return;
        }

        editor.LoadDocument(string.Empty);
        session.FilePath = null;
    }

    private static async Task HandleOpenAsync(EditSession session, MarkdownEditor editor, OpenFileUseCase openFile, SaveFileUseCase saveFile)
    {
        if (editor.IsDirty)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Unsaved changes. Save before opening another file?")
                    .AddChoices("Save and open", "Discard and open", "Cancel"));

            if (choice == "Save and open")
                await SaveAsync(session, editor, saveFile);
            else if (choice == "Cancel")
                return;
        }

        string path = AnsiConsole.Ask<string>("Open file: ");
        var result = await openFile.ExecuteAsync(path);

        if (result is Result<OpenedFile, MkedError>.Ok(var openedFile))
        {
            editor.LoadDocument(openedFile.Source);
            session.FilePath = path;
        }
        else if (result is Result<OpenedFile, MkedError>.Err(var err))
        {
            ErrorPresenter.Show(err);
        }
    }

    // ─── Layout ───────────────────────────────────────────────────────────────

    private static Layout BuildLayout(MarkdownEditor editor, MarkdownViewer preview, EditSession session)
    {
        var statusLine = editor.StatusLine();

        if (!session.SplitEnabled)
        {
            var layout = new Layout("Root")
                .SplitRows(
                    new Layout("Editor"),
                    new Layout("Status"));
            layout["Editor"].Update(editor);
            layout["Status"].Update(statusLine);
            layout["Status"].Size(1);
            return layout;
        }

        var splitLayout = new Layout("Root")
            .SplitRows(
                new Layout("Main").SplitColumns(
                    new Layout("Editor"),
                    new Layout("Preview")),
                new Layout("Status"));

        splitLayout["Editor"].Update(new Panel(editor).Expand().Border(BoxBorder.Rounded));
        splitLayout["Preview"].Update(new Panel(preview).Expand().Border(BoxBorder.Rounded));
        splitLayout["Status"].Update(statusLine);
        splitLayout["Status"].Size(1);

        return splitLayout;
    }

    /// <summary>
    /// Returns the editor/preview content height for the given terminal height and split state.
    /// In split mode each pane is wrapped in a rounded <see cref="Panel"/> that consumes two rows
    /// (top and bottom border), so the usable height is two less than in the non-split case.
    /// </summary>
    private static int ComputeViewportHeight(int terminalHeight, bool split) =>
        split ? terminalHeight - 3 : terminalHeight - 1;

}
