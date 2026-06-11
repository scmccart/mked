namespace Mked.Console;

/// <summary>
/// The <c>mked edit</c> command. Opens a file (or a blank document) in an interactive
/// Markdown editor with syntax highlighting, undo/redo, and save support.
/// </summary>
public sealed class EditCommand : AsyncCommand<EditSettings>
{
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
            var result = await new OpenFileUseCase(new FileSystemReader()).ExecuteAsync(settings.Path);
            if (result is Result<OpenedFile, MkedError>.Err(var openErr))
            {
                AnsiConsole.MarkupLine($"[red bold]Error:[/] {Markup.Escape(FormatError(openErr))}");
                return 1;
            }
            var file = ((Result<OpenedFile, MkedError>.Ok)result).Value;
            editor.LoadDocument(file.Source);
            session.FilePath = settings.Path;
        }

        // Track the latest buffer for the preview pane without re-parsing on every frame.
        string previewSource = editor.Buffer;
        editor.BufferChanged += md => previewSource = md;

        while (!session.Cancelled && !cancellationToken.IsCancellationRequested)
        {
            session.PendingAction = HostAction.None;

            int h = AnsiConsole.Profile.Height;
            editor.ViewportHeight = h - 1;

            // Build the initial preview instance; rebuilt in the dirty path when source or
            // scroll position changes.
            var preview = new MarkdownViewer(previewSource)
            {
                ShowFrontmatter = false,
                ViewportHeight = editor.ViewportHeight,
                TopLineIndex = session.PreviewTopLine,
            };

            await AnsiConsole.Live(BuildLayout(editor, preview, session)).StartAsync(async liveCtx =>
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                liveCtx.UpdateTarget(BuildLayout(editor, preview, session));

                int lastW = AnsiConsole.Profile.Width;
                int lastH = h;

                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        bool dirty = false;

                        while (System.Console.KeyAvailable)
                        {
                            var key = System.Console.ReadKey(intercept: true);

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
                                    dirty = true;
                                    continue;
                            }

                            // ── Ctrl+Tab — flip focus between editor and preview (split only) ─
                            if (key is { Key: ConsoleKey.Tab, Modifiers: ConsoleModifiers.Control }
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
                            editor.ViewportHeight = newH - 1;
                            h = newH;
                            lastW = newW;
                            lastH = newH;
                            dirty = true;
                        }

                        if (dirty)
                        {
                            preview = new MarkdownViewer(previewSource)
                            {
                                ShowFrontmatter = false,
                                ViewportHeight = editor.ViewportHeight,
                                TopLineIndex = session.PreviewTopLine,
                            };
                            liveCtx.UpdateTarget(BuildLayout(editor, preview, session));
                        }

                        await Task.Delay(16, cts.Token);
                    }
                }
                catch (OperationCanceledException) { }
            });

            // Handle host-level actions outside the live display so prompts render normally.
            switch (session.PendingAction)
            {
                case HostAction.Save:
                    await SaveAsync(session, editor);
                    break;

                case HostAction.Quit:
                    await HandleQuitAsync(session, editor);
                    break;

                case HostAction.New:
                    await HandleNewAsync(session, editor);
                    break;

                case HostAction.Open:
                    await HandleOpenAsync(session, editor);
                    break;
            }
        }

        return 0;
    }

    // ─── File operations ──────────────────────────────────────────────────────

    private static async Task SaveAsync(EditSession session, MarkdownEditor editor)
    {
        if (session.FilePath is null)
            session.FilePath = AnsiConsole.Ask<string>("Save as: ");

        var result = await new SaveFileUseCase(new FileSystemWriter())
            .ExecuteAsync(session.FilePath, editor.Buffer);
        if (result is Result<Unit, MkedError>.Err(var err))
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(FormatError(err))}[/]");
        else
            editor.MarkClean();
    }

    private static async Task HandleQuitAsync(EditSession session, MarkdownEditor editor)
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
            await SaveAsync(session, editor);
            session.Cancelled = true;
        }
        else if (choice == "Quit without saving")
        {
            session.Cancelled = true;
        }
    }

    private static async Task HandleNewAsync(EditSession session, MarkdownEditor editor)
    {
        if (editor.IsDirty)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Unsaved changes. Save before opening a new document?")
                    .AddChoices("Save and new", "Discard and new", "Cancel"));

            if (choice == "Save and new")
                await SaveAsync(session, editor);
            else if (choice == "Cancel")
                return;
        }

        editor.LoadDocument(string.Empty);
        session.FilePath = null;
    }

    private static async Task HandleOpenAsync(EditSession session, MarkdownEditor editor)
    {
        if (editor.IsDirty)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Unsaved changes. Save before opening another file?")
                    .AddChoices("Save and open", "Discard and open", "Cancel"));

            if (choice == "Save and open")
                await SaveAsync(session, editor);
            else if (choice == "Cancel")
                return;
        }

        string path = AnsiConsole.Ask<string>("Open file: ");
        var result = await new OpenFileUseCase(new FileSystemReader()).ExecuteAsync(path);

        if (result is Result<OpenedFile, MkedError>.Ok(var openedFile))
        {
            editor.LoadDocument(openedFile.Source);
            session.FilePath = path;
        }
        else if (result is Result<OpenedFile, MkedError>.Err(var err))
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(FormatError(err))}[/]");
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

        splitLayout["Editor"].Update(editor);
        splitLayout["Preview"].Update(preview);
        splitLayout["Status"].Update(statusLine);
        splitLayout["Status"].Size(1);

        return splitLayout;
    }

    private static string FormatError(MkedError error) => error switch
    {
        MkedError.IoError e => $"{e.Path}: {e.Reason}",
        MkedError.ValidationError e => $"{e.Field}: {e.Message}",
        _ => error.ToString() ?? "Unknown error",
    };
}
