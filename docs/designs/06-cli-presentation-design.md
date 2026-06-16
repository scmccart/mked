# Epic 06 — CLI & Presentation: Technical Design

> **Epic**: [`docs/epics/06-cli-presentation.md`](../epics/06-cli-presentation.md)
> **Status**: Draft
> **Date**: 2026-06-15

---

## Goals

1. Replace all inline `new` dependency construction in `ViewCommand`/`EditCommand` with a
   real `Microsoft.Extensions.DependencyInjection` composition root, so `CommandApp` is
   the sole integration point between Application and Infrastructure.
2. Repurpose `--plain` (`-p`) to select a **plain-text (no-pager) output mode** and
   auto-fall-back to it when stdout is redirected, removing the old "link text without
   URLs" meaning entirely.
3. Make `mked view` (no argument, piped stdin) read stdin automatically without requiring
   `--stream`; keep `--stream` as an explicit override.
4. Establish a single `ErrorPresenter` that maps every `MkedError` variant to a styled
   Spectre panel and a canonical exit code (`0` success / `1` usage / `2` IO / `3` parse).
5. Register `Console.CancelKeyPress` and POSIX signals (SIGTERM) through a
   `TerminalLifecycle` helper that cancels the shared token and restores the cursor,
   covering cases where the user sends a signal during a blocking prompt.
6. Introduce a `Mked.Console.Tests` project that verifies command dispatch, exit codes,
   the error presenter, and the renderer selector via Spectre's `CommandAppTester`.
7. Confirm the NativeAOT publish is free of ILLink / AOT warnings after DI is introduced.

## Non-Goals

- Any new viewer or editor features (scrolling, syntax highlighting, split pane) — those
  belong to Epics 04/05.
- NuGet packaging (`Mked.Controls`) — Epic 07.
- Distribution / WinGet — Epic 08.
- Alternate-screen buffer (`DECSET ?1049h`) — deferred.
- `--plain-links` or any new replacement for the old link-URL-omission behavior (dropped
  without replacement; plain mode inherently omits hyperlink decorations).

---

## Architecture Overview

This epic touches only the presentation layer directly. Infrastructure and Domain receive
minor enrichments to enable semantic error distinctions.

| Layer | Project | Changes |
|-------|---------|---------|
| Domain | `Mked.Domain` | `MkedError.IoError` gains an `IoKind` discriminant (`ReadNotFound`, `ReadAccessDenied`, `WriteAccessDenied`, `WriteGeneric`); no new top-level cases |
| Application | `Mked.Application` | None — use cases are correct; they receive injected ports already |
| Infrastructure | `Mked.Infrastructure` | `FileSystemReader` and `FileSystemWriter` produce the richer `IoKind` values in their exception handlers |
| Presentation | `Mked.Console` | All structural additions live here: composition root, DI bridge, error presenter, exit codes, renderer selector, plain-text renderer, terminal lifecycle |

**AOT / trim:** `Microsoft.Extensions.DependencyInjection` is `IsAotCompatible="true"` as
of .NET 8 and is used in the official NativeAOT console template. Commands are registered
via explicit factory lambdas, not `Activator.CreateInstance`. Settings classes retain their
`[DynamicallyAccessedMembers(All)]` attribute (required by Spectre.Console.Cli's internal
settings binding). Publish must be smoke-tested with `-p:PublishAot=true`.

---

## Key Types and Interfaces

### New Types — `Mked.Domain`

| Type | Kind | Purpose |
|------|------|---------|
| `IoKind` | top-level enum | Differentiates `ReadNotFound`, `ReadAccessDenied`, `ReadGeneric`, `WriteAccessDenied`, `WriteGeneric` within a single `IoError` |

### New Types — `Mked.Console`

| Type | Kind | Project | Purpose |
|------|------|---------|---------|
| `TypeRegistrar` | sealed class, `ITypeRegistrar` | `Mked.Console` | Wraps `IServiceCollection`; Spectre.Console.Cli DI bridge |
| `TypeResolver` | sealed class, `ITypeResolver`, `IDisposable` | `Mked.Console` | Wraps `IServiceProvider` built from the registrar; resolves command instances |
| `ExitCode` | static class | `Mked.Console` | Named constants: `Success = 0`, `Usage = 1`, `Io = 2`, `Parse = 3` |
| `ErrorPresenter` | static class | `Mked.Console` | `Show(MkedError) : int` — prints a styled Spectre panel to `AnsiConsole` and returns the exit code |
| `TerminalLifecycle` | sealed class, `IDisposable` | `Mked.Console` | Registers `Console.CancelKeyPress` + `PosixSignalRegistration`; disposes to unregister; restores cursor via `finally` in the command scope |
| `RendererSelector` | static class | `Mked.Console` | `IsPlainMode(ViewSettings) : bool` — returns `true` when `--plain` is passed or `Console.IsOutputRedirected` is `true` |
| `PlainTextRenderer` | static class | `Mked.Console` | `RenderAsync(string source, bool showFrontmatter, TextWriter out) : Task` — writes the Markdown source text (or source minus YAML frontmatter when `showFrontmatter` is `false`) to `out` once, with no ANSI codes |

### Modified Types

| Type | Change | Reason |
|------|--------|--------|
| `MkedError.IoError` | Add `IoKind Kind` property | Enable exit-code split (IO=2) and user-friendly panel messages (not-found vs permission) |
| `ViewSettings` | Rename `PlainLinks` → `Plain`; update XML doc and `[Description]` | `--plain` now means plain-text output mode |
| `ViewCommand` | Constructor injection of `OpenFileUseCase`, `StreamInputUseCase`; dispatch order update for stdin auto-detect; replace both `FormatError` calls with `ErrorPresenter.Show`; wrap interactive entry in `TerminalLifecycle` | DI, UX parity, standardized errors |
| `EditCommand` | Constructor injection of `OpenFileUseCase`, `SaveFileUseCase`; replace `FormatError` with `ErrorPresenter.Show`; wrap in `TerminalLifecycle` | DI, standardized errors, clean shutdown |
| `Program.cs` | Build `ServiceCollection`, pass `TypeRegistrar` to `CommandApp`; configure Spectre exception handler for usage errors → exit 1 | Composition root |
| `FileSystemReader` | Produce `IoKind.ReadNotFound` / `IoKind.ReadAccessDenied` | Semantic error distinction |
| `FileSystemWriter` | Distinguish `UnauthorizedAccessException` → `IoKind.WriteAccessDenied`; other `IOException` → `IoKind.WriteGeneric` | Semantic error distinction |

---

## Composition Root Contract

```csharp
// Program.cs (top-level statements)
var services = new ServiceCollection();

// Infrastructure → Domain port bindings
services.AddSingleton<IFileReader, FileSystemReader>();
services.AddSingleton<IFileWriter, FileSystemWriter>();
services.AddSingleton<IInputStream, StdinInputReader>();

// Application use cases (transient — stateless)
services.AddTransient<OpenFileUseCase>();
services.AddTransient<SaveFileUseCase>();
services.AddTransient<StreamInputUseCase>();
services.AddTransient<RenderDocumentUseCase>();

// Presentation commands
services.AddTransient<ViewCommand>();
services.AddTransient<EditCommand>();

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);
app.Configure(config =>
{
    config.SetApplicationName("mked");
    config.AddCommand<ViewCommand>("view")
          .WithDescription("View a Markdown file in a scrollable pager.");
    config.AddCommand<EditCommand>("edit")
          .WithDescription("Edit a Markdown file in an interactive editor.");
    config.SetExceptionHandler((ex, _) =>
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
        return ExitCode.Usage;
    });
});
return await app.RunAsync(args);
```

`TypeRegistrar` and `TypeResolver` are the canonical Spectre.Console.Cli DI bridge pattern:

```csharp
namespace Mked.Console;

/// <summary>Bridges <see cref="IServiceCollection"/> with Spectre.Console.Cli's DI hook.</summary>
internal sealed class TypeRegistrar(IServiceCollection services) : ITypeRegistrar
{
    public ITypeResolver Build() => new TypeResolver(services.BuildServiceProvider());
    public void Register(Type service, Type implementation) =>
        services.AddSingleton(service, implementation);
    public void RegisterInstance(Type service, object implementation) =>
        services.AddSingleton(service, implementation);
    public void RegisterLazy(Type service, Func<object> factory) =>
        services.AddSingleton(service, _ => factory());
}

/// <summary>Resolves types from the built <see cref="IServiceProvider"/>.</summary>
internal sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable
{
    public object? Resolve(Type? type) =>
        type is null ? null : provider.GetService(type);
    public void Dispose() => (provider as IDisposable)?.Dispose();
}
```

---

## `ExitCode` and `ErrorPresenter` Contract

```csharp
namespace Mked.Console;

/// <summary>Canonical exit codes for <c>mked</c>.</summary>
public static class ExitCode
{
    public const int Success = 0;
    /// <summary>Bad usage: missing argument, unknown option, conflicting flags.</summary>
    public const int Usage   = 1;
    /// <summary>File or I/O error: file not found, access denied, write failure.</summary>
    public const int Io      = 2;
    /// <summary>Markdown parse or validation error.</summary>
    public const int Parse   = 3;
}
```

```csharp
namespace Mked.Console;

/// <summary>
/// Renders a <see cref="MkedError"/> as a styled Spectre.Console panel and returns the
/// appropriate <see cref="ExitCode"/>. Single source of truth for error presentation.
/// </summary>
public static class ErrorPresenter
{
    /// <summary>
    /// Writes an error panel to <see cref="AnsiConsole"/> and returns the exit code.
    /// </summary>
    public static int Show(MkedError error)
    {
        var (header, body, code) = Describe(error);
        AnsiConsole.Write(new Panel(body)
            .Header($"[red bold]{header}[/]")
            .BorderColor(Color.Red));
        return code;
    }

    private static (string header, string body, int code) Describe(MkedError error) =>
        error switch
        {
            MkedError.IoError { Kind: IoKind.ReadNotFound } e =>
                ("File not found", e.Path, ExitCode.Io),
            MkedError.IoError { Kind: IoKind.ReadAccessDenied } e =>
                ("Permission denied", $"Cannot read: {e.Path}", ExitCode.Io),
            MkedError.IoError { Kind: IoKind.WriteAccessDenied } e =>
                ("Permission denied", $"Cannot write: {e.Path}", ExitCode.Io),
            MkedError.IoError { Kind: IoKind.WriteGeneric } e =>
                ("Write error", $"{e.Path}: {e.Reason}", ExitCode.Io),
            MkedError.ParseError e =>
                ("Parse error", $"Line {e.Line}, column {e.Column}: {e.Message}", ExitCode.Parse),
            MkedError.ValidationError e =>
                ("Invalid input", $"{e.Field}: {e.Message}", ExitCode.Usage),
            MkedError.StreamError e =>
                ("Stream error", e.Reason, ExitCode.Io),
            _ => ("Error", error.ToString() ?? "Unknown error", ExitCode.Usage),
        };
}
```

---

## `RendererSelector` and `PlainTextRenderer` Contract

```csharp
namespace Mked.Console;

/// <summary>Determines whether plain-text (non-pager) output mode is active.</summary>
public static class RendererSelector
{
    /// <summary>
    /// Returns <see langword="true"/> when <c>--plain</c> is set or stdout is redirected.
    /// </summary>
    public static bool IsPlainMode(ViewSettings settings) =>
        settings.Plain || System.Console.IsOutputRedirected;
}
```

```csharp
namespace Mked.Console;

/// <summary>
/// Writes a Markdown document to <paramref name="output"/> as plain text — no pager, no ANSI.
/// Used when <see cref="RendererSelector.IsPlainMode"/> returns <see langword="true"/>.
/// </summary>
public static class PlainTextRenderer
{
    /// <summary>
    /// Writes <paramref name="source"/> to <paramref name="output"/>.
    /// YAML frontmatter (everything before the second <c>---</c> line) is omitted unless
    /// <paramref name="showFrontmatter"/> is <see langword="true"/>.
    /// </summary>
    public static async Task RenderAsync(
        string source,
        bool showFrontmatter,
        TextWriter output)
    {
        var text = showFrontmatter ? source : StripFrontmatter(source);
        await output.WriteAsync(text);
    }

    [GeneratedRegex(@"^---\r?\n.*?\r?\n---\r?\n", RegexOptions.Singleline)]
    private static partial Regex FrontmatterPattern();

    private static string StripFrontmatter(string source) =>
        FrontmatterPattern().Replace(source, string.Empty);
}
```

Plain mode outputs the raw Markdown source (minus frontmatter when `--show-frontmatter` is
absent). This is maximally compatible with downstream tools (`grep`, `sed`, `wc`). ANSI
formatting codes are not present because `PlainTextRenderer` writes to `Console.Out` directly,
bypassing Spectre entirely.

---

## `TerminalLifecycle` Contract

```csharp
namespace Mked.Console;

/// <summary>
/// Registers OS-level cancellation signals (Ctrl+C / SIGTERM) and guarantees cursor
/// restoration on abnormal exit. Wrap the interactive command entry in <c>using</c>.
/// </summary>
public sealed class TerminalLifecycle : IDisposable
{
    private readonly CancellationTokenSource _cts;
    private readonly PosixSignalRegistration? _sigterm;
    private bool _disposed;

    public TerminalLifecycle(CancellationTokenSource cts)
    {
        _cts = cts;

        // CancelKeyPress fires for Ctrl+C (SIGINT) on all platforms.
        System.Console.CancelKeyPress += OnCancelKeyPress;

        // SIGTERM is available on .NET 6+ on Linux, macOS, and Windows.
        _sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnSignal);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        System.Console.CancelKeyPress -= OnCancelKeyPress;
        _sigterm?.Dispose();
        System.Console.CursorVisible = true;
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true; // prevent default process kill; we do clean shutdown
        _cts.Cancel();
    }

    private void OnSignal(PosixSignalContext ctx)
    {
        ctx.Cancel = true;
        _cts.Cancel();
    }
}
```

Commands use it as:

```csharp
using var cts = new CancellationTokenSource();
using var lifecycle = new TerminalLifecycle(cts);
System.Console.CursorVisible = false;
try
{
    // ... interactive live loop ...
}
finally
{
    System.Console.CursorVisible = true;
}
```

The existing polled-keystroke Ctrl+C handling inside the `AnsiConsole.Live` loops is
**kept** — the signal handler supplements it, covering gaps during blocking prompts (e.g.
`AnsiConsole.Ask`, `SelectionPrompt`).

---

## Data Flow / Sequence

### Use Case: `mked view file.md` (interactive, stdout is a TTY)

1. `Program.cs` builds the DI container; `CommandApp` resolves `ViewCommand` via
   `TypeResolver`; `ViewSettings.Path = "file.md"`, `Plain = false`.
2. `RendererSelector.IsPlainMode(settings)` → `false`.
3. `TerminalLifecycle` constructed; cursor hidden.
4. `_openFile.ExecuteAsync("file.md")` → `Ok(file)`.
5. Interactive `AnsiConsole.Live` pager loop starts (unchanged from Epic 04).
6. User presses `q` or `Ctrl+C` → `cts.Cancel()` → live loop exits.
7. `TerminalLifecycle.Dispose()` → cursor restored.
8. Return `ExitCode.Success`.

### Use Case: `mked view` (no argument, stdin is piped)

1. `ViewSettings.Path = null`, `Stream = false`.
2. `RendererSelector.IsPlainMode(settings)` checks `Console.IsOutputRedirected` (true if
   stdout is also piped; false if stdout is still a TTY).
3. `Console.IsInputRedirected` is `true` → dispatch to stdin path (same `RunStreamModeAsync`
   as `--stream`).
4. Renderer mode (interactive vs. plain) is applied within that path.

### Use Case: `mked view --plain file.md` or `mked view file.md | cat`

1. `RendererSelector.IsPlainMode(settings)` → `true` (`--plain` or `Console.IsOutputRedirected`).
2. File is opened via `OpenFileUseCase`.
3. `PlainTextRenderer.RenderAsync(file.Source, settings.ShowFrontmatter, Console.Out)` called.
4. Returns `ExitCode.Success` immediately — no pager, no key loop.

### Use Case: `mked view missing.md` (file not found)

1. `OpenFileUseCase` → `Err(IoError { Kind = ReadNotFound, Path = "missing.md" })`.
2. `ErrorPresenter.Show(error)` prints `┌ File not found ┐` panel; returns `ExitCode.Io` (2).
3. Command returns `2`. Shell sees exit code 2.

### Use Case: `mked edit file.md` then `Ctrl+C` during save prompt

1. `EditCommand.ExecuteAsync` opens file, enters outer loop.
2. User presses `Ctrl+S` → host sets `PendingAction = Save`; inner CTS cancelled; Live stops.
3. `AnsiConsole.Ask<string>("Save as: ")` begins (blocking prompt).
4. User sends `Ctrl+C` → `TerminalLifecycle.OnCancelKeyPress` fires → outer CTS cancelled.
5. `Ask<string>` throws `OperationCanceledException` (or returns early) → caught by outer
   `try/finally` → `Console.CursorVisible = true` → return `ExitCode.Success`.

---

## Error Handling Strategy

- **New `MkedError` variant**: none. Enrichment is additive: `MkedError.IoError` gains a
  top-level `IoKind` enum with five values; no new top-level DU case is introduced.
- **Error production boundaries**: `FileSystemReader`/`FileSystemWriter` produce enriched
  `IoError`; `StreamInputUseCase` produces `StreamError`; all other error production sites
  (parse errors) remain unchanged.
- **User-visible failures**: all errors flow through `ErrorPresenter.Show`. Exit codes map:
  `IoError` → 2, `ParseError` → 3, `ValidationError`/`StreamError` → 1.
- **Spectre usage errors** (unrecognized option, missing required arg): caught by the
  `SetExceptionHandler` in `Program.cs` → styled message, return 1.
- **Stream errors in plain mode**: stream mode reads stdin to completion; any `StreamError`
  is shown inline via `ErrorPresenter.Show` without exiting (same as interactive stream mode).

---

## Testing Approach

- **Unit tests — `Mked.Console.Tests`** (new project, xUnit + AwesomeAssertions):
  - `ErrorPresenter_Tests`: each `MkedError` variant → expected panel text + exit code.
  - `RendererSelector_Tests`: `IsPlainMode` returns correct value for (plain flag, redirect
    state) combinations via a testable overload that accepts the redirect flag directly.
  - `PlainTextRenderer_Tests`: with and without frontmatter; verify output text.
  - `ExitCode_Tests`: constant values match spec (trivial; guards against accidental renaming).
  - `ViewCommand_ExitCode_Tests` via `CommandAppTester`: returns 0 on success, 2 on
    not-found, 1 on no-arg with no stdin redirect.
  - `EditCommand_ExitCode_Tests` via `CommandAppTester`: file-not-found returns 2.
- **Architecture tests — `Mked.Console.Tests`**:
  - `Console_CompositionRoot_ReferencesInfrastructure`: only `Program.cs`'s assembly (or a
    dedicated composition-root namespace) may reference `Mked.Infrastructure` types.
- **Integration tests**: AOT publish smoke-test (manual, then CI gate via publish step).
- **Existing test projects** — no changes required; all infrastructure/domain/application
  tests continue to pass unmodified.

A note on `CommandAppTester` and AOT: `CommandAppTester` is a test-only Spectre class; it
runs in the JIT test runner and does not need to be AOT-safe. The published exe is what
needs the AOT publish verification.

---

## Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | **AOT × MS.DI**: `Microsoft.Extensions.DependencyInjection` declares `IsAotCompatible` since .NET 8; Spectre.Console.Cli 0.55.x uses reflection for settings binding. Are there additional `[DynamicallyAccessedMembers]` annotations needed beyond the existing `All` on `ViewSettings`/`EditSettings`? | Resolve during Task 9 (AOT publish) |
| 2 | **POSIX signals on Windows**: `PosixSignalRegistration.Create(SIGTERM, …)` is available on .NET 6+ including Windows (via a Windows-compatible signal path). Confirm no exception is thrown at startup on Windows when registered. | Resolve during manual verification |
| 3 | **Stream mode + plain + no-TTY stdout**: when stdin is piped AND stdout is redirected, the stream path renders each incoming document chunk to `Console.Out`. Is each chunk written as a replacement (overwriting prior output) or appended? Appended is correct for piped output (no ANSI cursor movement). | Resolved: plain stream mode appends chunks; interactive stream mode uses `AnsiConsole.Live` and requires a TTY |
| 4 | **`RendererSelector` + `CommandAppTester` testability**: `Console.IsOutputRedirected` is a static property that cannot be mocked. A thin testable wrapper (`IOutputRedirectDetector`) may be needed, or the overload approach (accept a `bool isRedirected` parameter in tests). | Resolve during Task 8 — prefer overload to avoid interface proliferation |
