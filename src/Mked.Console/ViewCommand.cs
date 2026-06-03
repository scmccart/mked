namespace Mked.Console;

/// <summary>
/// The <c>mked view</c> command. Renders a Markdown file in an interactive scrollable pager.
/// Supports plain file view, <c>--follow</c> (live file reload), and <c>--stream</c> (stdin).
/// </summary>
public sealed class ViewCommand : AsyncCommand<ViewSettings>
{
    private readonly OpenFileUseCase _openFile = new(new FileSystemReader());
    private readonly StreamInputUseCase _streamInput = new(new StdinInputReader());

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, ViewSettings settings, CancellationToken cancellationToken)
    {
        if (settings.Stream)
        {
            return await RunStreamModeAsync(settings);
        }

        if (settings.Path is null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] A file path is required.");
            return 1;
        }

        return settings.Follow
            ? await RunFollowModeAsync(settings)
            : await RunFileModeAsync(settings);
    }

    // ─── Plain file mode ──────────────────────────────────────────────────────

    private async Task<int> RunFileModeAsync(ViewSettings settings)
    {
        var result = await _openFile.ExecuteAsync(settings.Path!);
        if (result is not Result<OpenedFile, MkedError>.Ok(var file))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(FormatError(((Result<OpenedFile, MkedError>.Err)result).Error))}");
            return 1;
        }

        int h = AnsiConsole.Profile.Height;
        int currentBlock = 0;
        var baseViewer = BuildViewer(file.Source, settings);
        var viewer = baseViewer with { TopBlockIndex = 0, ViewportHeight = h };

        using var cts = new CancellationTokenSource();

        await AnsiConsole.Live(viewer).StartAsync(async liveCtx =>
        {
            liveCtx.UpdateTarget(viewer);
            int lastW = AnsiConsole.Profile.Width;
            int lastH = h;

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    bool dirty = false;

                    if (System.Console.KeyAvailable)
                    {
                        var key = System.Console.ReadKey(intercept: true);
                        var scrollInfo = viewer.ScrollInfo;
                        int maxBlock = Math.Max(0, viewer.BlockCount - 1);

                        switch (key.Key)
                        {
                            case ConsoleKey.DownArrow:
                            case ConsoleKey.J when key.Modifiers == 0:
                                currentBlock = Math.Min(currentBlock + 1, maxBlock);
                                dirty = true;
                                break;

                            case ConsoleKey.UpArrow:
                            case ConsoleKey.K when key.Modifiers == 0:
                                currentBlock = Math.Max(currentBlock - 1, 0);
                                dirty = true;
                                break;

                            case ConsoleKey.PageDown:
                            case ConsoleKey.D when key.Modifiers == ConsoleModifiers.Control:
                                currentBlock = Math.Min(currentBlock + Math.Max(1, h / 4), maxBlock);
                                dirty = true;
                                break;

                            case ConsoleKey.PageUp:
                            case ConsoleKey.U when key.Modifiers == ConsoleModifiers.Control:
                                currentBlock = Math.Max(currentBlock - Math.Max(1, h / 4), 0);
                                dirty = true;
                                break;

                            case ConsoleKey.G when key.Modifiers == ConsoleModifiers.Shift:
                                currentBlock = maxBlock;
                                dirty = true;
                                break;

                            case ConsoleKey.G when key.Modifiers == 0:
                                currentBlock = 0;
                                dirty = true;
                                break;

                            case ConsoleKey.Q:
                            case ConsoleKey.C when key.Modifiers == ConsoleModifiers.Control:
                                await cts.CancelAsync();
                                return;
                        }
                    }

                    int newW = AnsiConsole.Profile.Width;
                    int newH = AnsiConsole.Profile.Height;
                    if (newW != lastW || newH != lastH)
                    {
                        h = newH;
                        lastW = newW;
                        lastH = newH;
                        baseViewer = BuildViewer(file.Source, settings);
                        dirty = true;
                    }

                    if (dirty)
                    {
                        viewer = baseViewer with { TopBlockIndex = currentBlock, ViewportHeight = h };
                        liveCtx.UpdateTarget(viewer);
                    }

                    await Task.Delay(16, cts.Token);
                }
            }
            catch (OperationCanceledException) { }
        });

        return 0;
    }

    // ─── Follow mode ──────────────────────────────────────────────────────────

    private async Task<int> RunFollowModeAsync(ViewSettings settings)
    {
        var result = await _openFile.ExecuteAsync(settings.Path!);
        if (result is not Result<OpenedFile, MkedError>.Ok(var file))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(FormatError(((Result<OpenedFile, MkedError>.Err)result).Error))}");
            return 1;
        }

        int h = AnsiConsole.Profile.Height;
        int currentBlock = 0;
        var baseViewer = BuildViewer(file.Source, settings);
        var viewer = baseViewer with { TopBlockIndex = 0, ViewportHeight = h };

        using var cts = new CancellationTokenSource();
        using var watcher = new FileWatcherAdapter(settings.Path!);

        // Feed file-change notifications into a channel so we can consume them in the poll loop
        var reloadChannel = System.Threading.Channels.Channel.CreateBounded<bool>(
            new System.Threading.Channels.BoundedChannelOptions(1)
            {
                FullMode = System.Threading.Channels.BoundedChannelFullMode.DropWrite,
            });

        _ = Task.Run(async () =>
        {
            await foreach (var _ in watcher.WatchAsync(cts.Token))
            {
                reloadChannel.Writer.TryWrite(true);
            }

            reloadChannel.Writer.TryComplete();
        }, cts.Token);

        await AnsiConsole.Live(viewer).StartAsync(async liveCtx =>
        {
            liveCtx.UpdateTarget(viewer);
            int lastW = AnsiConsole.Profile.Width;
            int lastH = h;

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    bool dirty = false;

                    // Reload if file changed
                    if (reloadChannel.Reader.TryRead(out _))
                    {
                        var reloaded = await _openFile.ExecuteAsync(settings.Path!);
                        if (reloaded is Result<OpenedFile, MkedError>.Ok(var newFile))
                        {
                            baseViewer = BuildViewer(newFile.Source, settings);
                            dirty = true;
                        }
                    }

                    if (System.Console.KeyAvailable)
                    {
                        var key = System.Console.ReadKey(intercept: true);
                        int maxBlock = Math.Max(0, viewer.BlockCount - 1);

                        switch (key.Key)
                        {
                            case ConsoleKey.DownArrow:
                            case ConsoleKey.J when key.Modifiers == 0:
                                currentBlock = Math.Min(currentBlock + 1, maxBlock);
                                dirty = true;
                                break;

                            case ConsoleKey.UpArrow:
                            case ConsoleKey.K when key.Modifiers == 0:
                                currentBlock = Math.Max(currentBlock - 1, 0);
                                dirty = true;
                                break;

                            case ConsoleKey.PageDown:
                            case ConsoleKey.D when key.Modifiers == ConsoleModifiers.Control:
                                currentBlock = Math.Min(currentBlock + Math.Max(1, h / 4), maxBlock);
                                dirty = true;
                                break;

                            case ConsoleKey.PageUp:
                            case ConsoleKey.U when key.Modifiers == ConsoleModifiers.Control:
                                currentBlock = Math.Max(currentBlock - Math.Max(1, h / 4), 0);
                                dirty = true;
                                break;

                            case ConsoleKey.G when key.Modifiers == ConsoleModifiers.Shift:
                                currentBlock = Math.Max(0, viewer.BlockCount - 1);
                                dirty = true;
                                break;

                            case ConsoleKey.G when key.Modifiers == 0:
                                currentBlock = 0;
                                dirty = true;
                                break;

                            case ConsoleKey.Q:
                            case ConsoleKey.C when key.Modifiers == ConsoleModifiers.Control:
                                await cts.CancelAsync();
                                return;
                        }
                    }

                    int newW = AnsiConsole.Profile.Width;
                    int newH = AnsiConsole.Profile.Height;
                    if (newW != lastW || newH != lastH)
                    {
                        h = newH;
                        lastW = newW;
                        lastH = newH;
                        baseViewer = BuildViewer(baseViewer.Markdown, settings);
                        dirty = true;
                    }

                    if (dirty)
                    {
                        currentBlock = Math.Min(currentBlock, Math.Max(0, viewer.BlockCount - 1));
                        viewer = baseViewer with { TopBlockIndex = currentBlock, ViewportHeight = h };
                        liveCtx.UpdateTarget(viewer);
                    }

                    await Task.Delay(16, cts.Token);
                }
            }
            catch (OperationCanceledException) { }
        });

        return 0;
    }

    // ─── Stream mode ──────────────────────────────────────────────────────────

    private async Task<int> RunStreamModeAsync(ViewSettings settings)
    {
        int h = AnsiConsole.Profile.Height;
        int currentBlock = 0;
        var renderCtx = new RenderContext(settings.ShowFrontmatter, settings.PlainLinks);
        var renderer = new SpectreMarkdownRenderer(renderCtx);
        var initialViewer = new MarkdownViewer(string.Empty) { ViewportHeight = h };
        MarkdownViewer viewer = initialViewer;

        using var cts = new CancellationTokenSource();

        var docStream = _streamInput.ExecuteAsync(cts.Token);
        var viewerStream = renderer.Stream(docStream, cts.Token);

        // Buffer incoming viewers so the poll loop can consume them
        var viewerChannel = System.Threading.Channels.Channel.CreateBounded<MarkdownViewer>(
            new System.Threading.Channels.BoundedChannelOptions(1)
            {
                FullMode = System.Threading.Channels.BoundedChannelFullMode.DropOldest,
            });

        var streamTask = Task.Run(async () =>
        {
            await foreach (var v in viewerStream)
            {
                viewerChannel.Writer.TryWrite(v);
            }

            viewerChannel.Writer.TryComplete();
        }, cts.Token);

        await AnsiConsole.Live(viewer).StartAsync(async liveCtx =>
        {
            liveCtx.UpdateTarget(viewer);
            int lastW = AnsiConsole.Profile.Width;
            int lastH = h;

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    bool dirty = false;

                    if (viewerChannel.Reader.TryRead(out var incoming))
                    {
                        var newBase = incoming;
                        currentBlock = Math.Max(0, newBase.BlockCount - 1);
                        viewer = newBase with { TopBlockIndex = currentBlock, ViewportHeight = h };
                        dirty = true;
                    }

                    if (System.Console.KeyAvailable)
                    {
                        var key = System.Console.ReadKey(intercept: true);
                        int maxBlock = Math.Max(0, viewer.BlockCount - 1);

                        switch (key.Key)
                        {
                            case ConsoleKey.DownArrow:
                            case ConsoleKey.J when key.Modifiers == 0:
                                currentBlock = Math.Min(currentBlock + 1, maxBlock);
                                dirty = true;
                                break;

                            case ConsoleKey.UpArrow:
                            case ConsoleKey.K when key.Modifiers == 0:
                                currentBlock = Math.Max(currentBlock - 1, 0);
                                dirty = true;
                                break;

                            case ConsoleKey.PageDown:
                            case ConsoleKey.D when key.Modifiers == ConsoleModifiers.Control:
                                currentBlock = Math.Min(currentBlock + Math.Max(1, h / 4), maxBlock);
                                dirty = true;
                                break;

                            case ConsoleKey.PageUp:
                            case ConsoleKey.U when key.Modifiers == ConsoleModifiers.Control:
                                currentBlock = Math.Max(currentBlock - Math.Max(1, h / 4), 0);
                                dirty = true;
                                break;

                            case ConsoleKey.G when key.Modifiers == ConsoleModifiers.Shift:
                                currentBlock = maxBlock;
                                dirty = true;
                                break;

                            case ConsoleKey.G when key.Modifiers == 0:
                                currentBlock = 0;
                                dirty = true;
                                break;

                            case ConsoleKey.Q:
                            case ConsoleKey.C when key.Modifiers == ConsoleModifiers.Control:
                                await cts.CancelAsync();
                                return;
                        }
                    }

                    int newW = AnsiConsole.Profile.Width;
                    int newH = AnsiConsole.Profile.Height;
                    if (newW != lastW || newH != lastH)
                    {
                        h = newH;
                        lastW = newW;
                        lastH = newH;
                        dirty = true;
                    }

                    if (dirty)
                    {
                        viewer = viewer with { TopBlockIndex = currentBlock, ViewportHeight = h };
                        liveCtx.UpdateTarget(viewer);
                    }

                    // Show any stream errors as a status line
                    if (renderer.Errors.TryRead(out var err))
                    {
                        AnsiConsole.MarkupLine($"[dim red]{Markup.Escape(FormatError(err))}[/]");
                    }

                    await Task.Delay(16, cts.Token);
                }
            }
            catch (OperationCanceledException) { }

            await streamTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        });

        return 0;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static MarkdownViewer BuildViewer(string source, ViewSettings settings) =>
        new(source)
        {
            ShowFrontmatter = settings.ShowFrontmatter,
            PlainLinks = settings.PlainLinks,
        };

    private static string FormatError(MkedError error) => error switch
    {
        MkedError.IoError e => $"{e.Path}: {e.Reason}",
        MkedError.ParseError e => $"Parse error at {e.Line}:{e.Column}: {e.Message}",
        MkedError.StreamError e => $"Stream error: {e.Reason}",
        _ => error.ToString() ?? "Unknown error",
    };
}
