using System.Diagnostics.CodeAnalysis;

namespace Mked.Console;

/// <summary>
/// The <c>mked view</c> command. Renders a Markdown file in an interactive scrollable pager.
/// Supports plain file view, <c>--follow</c> (live file reload), and <c>--stream</c> (stdin).
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class ViewCommand(OpenFileUseCase openFile, StreamInputUseCase streamInput)
    : AsyncCommand<ViewSettings>
{
    private readonly OpenFileUseCase _openFile = openFile;
    private readonly StreamInputUseCase _streamInput = streamInput;

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, ViewSettings settings, CancellationToken cancellationToken)
    {
        bool useStdin = settings.Stream || (settings.Path is null && System.Console.IsInputRedirected);

        if (useStdin)
        {
            if (RendererSelector.IsPlainMode(settings))
                return await RunPlainStreamModeAsync(settings, cancellationToken);
            return await RunStreamModeAsync(settings);
        }

        if (settings.Path is null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] A file path is required.");
            return ExitCode.Usage;
        }

        if (settings.Follow)
            return await RunFollowModeAsync(settings);

        if (RendererSelector.IsPlainMode(settings))
            return await RunPlainFileModeAsync(settings);

        return await RunFileModeAsync(settings);
    }

    // ─── Plain text output (no pager) ────────────────────────────────────────

    private async Task<int> RunPlainFileModeAsync(ViewSettings settings)
    {
        var result = await _openFile.ExecuteAsync(settings.Path!);
        if (result is not Result<OpenedFile, MkedError>.Ok(var file))
            return ErrorPresenter.Show(((Result<OpenedFile, MkedError>.Err)result).Error);

        await PlainTextRenderer.RenderAsync(file.Source, settings.ShowFrontmatter, System.Console.Out);
        return ExitCode.Success;
    }

    private async Task<int> RunPlainStreamModeAsync(ViewSettings settings, CancellationToken cancellationToken)
    {
        var docStream = _streamInput.ExecuteAsync(cancellationToken);
        await foreach (var chunk in docStream.WithCancellation(cancellationToken))
        {
            if (chunk is Result<StreamedDocument, MkedError>.Ok(var doc))
                await PlainTextRenderer.RenderAsync(doc.Source, settings.ShowFrontmatter, System.Console.Out);
            else if (chunk is Result<StreamedDocument, MkedError>.Err(var err))
                ErrorPresenter.Show(err);
        }
        return ExitCode.Success;
    }

    // ─── Plain file mode ──────────────────────────────────────────────────────

    private async Task<int> RunFileModeAsync(ViewSettings settings)
    {
        var result = await _openFile.ExecuteAsync(settings.Path!);
        if (result is not Result<OpenedFile, MkedError>.Ok(var file))
            return ErrorPresenter.Show(((Result<OpenedFile, MkedError>.Err)result).Error);

        int h = AnsiConsole.Profile.Height;
        int currentLine = 0;
        var baseViewer = BuildViewer(file.Source, settings);
        var viewer = baseViewer with { TopLineIndex = 0, ViewportHeight = h };

        using var cts = new CancellationTokenSource();
        using var lifecycle = new TerminalLifecycle(cts);
        using var input = ConsoleInputSource.Create();

        await AnsiConsole.Live(viewer).StartAsync(async liveCtx =>
        {
            liveCtx.UpdateTarget(viewer);
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
                        if (ViewerInput.Apply(ev, ref currentLine, viewer.ScrollInfo, h, out bool quit))
                            dirty = true;
                        if (quit) { await cts.CancelAsync(); return; }
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
                        viewer = baseViewer with { TopLineIndex = currentLine, ViewportHeight = h };
                        liveCtx.UpdateTarget(viewer);
                    }

                    await timer.WaitForNextTickAsync(cts.Token);
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
            return ErrorPresenter.Show(((Result<OpenedFile, MkedError>.Err)result).Error);

        int h = AnsiConsole.Profile.Height;
        int currentLine = 0;
        var baseViewer = BuildViewer(file.Source, settings);
        var viewer = baseViewer with { TopLineIndex = 0, ViewportHeight = h };

        using var cts = new CancellationTokenSource();
        using var lifecycle = new TerminalLifecycle(cts);
        using var input = ConsoleInputSource.Create();
        using var watcher = new FileWatcherAdapter(settings.Path!);

        // Feed file-change notifications into a channel so we can consume them in the poll loop
        var reloadChannel = System.Threading.Channels.Channel.CreateBounded<bool>(
            new System.Threading.Channels.BoundedChannelOptions(1)
            {
                FullMode = System.Threading.Channels.BoundedChannelFullMode.DropWrite,
            });

        Exception? watcherFault = null;
        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var _ in watcher.WatchAsync(cts.Token))
                {
                    reloadChannel.Writer.TryWrite(true);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                watcherFault = ex;
                cts.Cancel();
            }
            finally
            {
                reloadChannel.Writer.TryComplete();
            }
        }, cts.Token);

        await AnsiConsole.Live(viewer).StartAsync(async liveCtx =>
        {
            liveCtx.UpdateTarget(viewer);
            int lastW = AnsiConsole.Profile.Width;
            int lastH = h;

            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(16));
                while (!cts.Token.IsCancellationRequested)
                {
                    bool dirty = false;

                    // Reload if file changed
                    if (reloadChannel.Reader.TryRead(out _))
                    {
                        var reloaded = await _openFile.ExecuteAsync(settings.Path!);
                        if (reloaded is Result<OpenedFile, MkedError>.Ok(var newFile))
                        {
                            var newViewer = BuildViewer(newFile.Source, settings);
                            // Populate the render cache so LineHashes are available for
                            // anchor remapping before the new viewer is displayed.
                            newViewer.Measure(
                                RenderOptions.Create(AnsiConsole.Console),
                                AnsiConsole.Profile.Width);
                            currentLine = ScrollAnchor.RemapTopLine(
                                viewer.ScrollInfo.LineHashes,
                                newViewer.ScrollInfo.LineHashes,
                                currentLine,
                                h);
                            baseViewer = newViewer;
                            dirty = true;
                        }
                    }

                    while (input.TryRead(out var ev))
                    {
                        if (ViewerInput.Apply(ev, ref currentLine, viewer.ScrollInfo, h, out bool quit))
                            dirty = true;
                        if (quit) { await cts.CancelAsync(); return; }
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
                        viewer = baseViewer with { TopLineIndex = currentLine, ViewportHeight = h };
                        liveCtx.UpdateTarget(viewer);
                    }

                    await timer.WaitForNextTickAsync(cts.Token);
                }
            }
            catch (OperationCanceledException) { }
        });

        if (watcherFault is not null)
            return ErrorPresenter.Show(new MkedError.StreamError(watcherFault.Message));

        return 0;
    }

    // ─── Stream mode ──────────────────────────────────────────────────────────

    private async Task<int> RunStreamModeAsync(ViewSettings settings)
    {
        int h = AnsiConsole.Profile.Height;
        int currentLine = 0;
        var renderCtx = new RenderContext(settings.ShowFrontmatter, PlainLinks: false);
        var renderer = new SpectreMarkdownRenderer(renderCtx);
        var initialViewer = new MarkdownViewer(string.Empty) { ViewportHeight = h };
        MarkdownViewer viewer = initialViewer;

        using var cts = new CancellationTokenSource();
        using var lifecycle = new TerminalLifecycle(cts);
        // Stream mode reads its content from stdin, so keyboard is handled separately via
        // the null-mouse source (Console.KeyAvailable / ReadKey) to avoid conflicts.
        using var input = new NullMouseInputSource();

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
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(16));
                while (!cts.Token.IsCancellationRequested)
                {
                    bool dirty = false;

                    if (viewerChannel.Reader.TryRead(out var incoming))
                    {
                        viewer = incoming;
                        currentLine = int.MaxValue; // tail-follow: Render clamps to last visible line
                        dirty = true;
                    }

                    while (input.TryRead(out var ev))
                    {
                        if (ViewerInput.Apply(ev, ref currentLine, viewer.ScrollInfo, h, out bool quit))
                            dirty = true;
                        if (quit) { await cts.CancelAsync(); return; }
                    }

                    int newW = AnsiConsole.Profile.Width;
                    int newH = AnsiConsole.Profile.Height;
                    if (newW != lastW || newH != lastH)
                    {
                        bool widthChanged = newW != lastW;
                        h = newH;
                        lastW = newW;
                        lastH = newH;
                        if (widthChanged)
                            viewer = BuildViewer(viewer.Markdown, settings);
                        dirty = true;
                    }

                    if (dirty)
                    {
                        viewer = viewer with { TopLineIndex = currentLine, ViewportHeight = h };
                        liveCtx.UpdateTarget(viewer);
                    }

                    // Show any stream errors as a status line
                    if (renderer.Errors.TryRead(out var err))
                        ErrorPresenter.Show(err);

                    await timer.WaitForNextTickAsync(cts.Token);
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
        };

}
