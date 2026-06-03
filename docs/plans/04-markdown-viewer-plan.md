# Epic 04 — Markdown Viewer: Implementation Plan

> **Epic**: [`docs/epics/04-markdown-viewer.md`](../../docs/epics/04-markdown-viewer.md)
> **Design**: [`docs/designs/04-markdown-viewer-design.md`](../../docs/designs/04-markdown-viewer-design.md)
> **Status**: Approved
> **Date**: 2026-06-03

---

## Task 1 — Add `Source` to `MarkdownDocument`

Add `public string Source { get; }` to `MarkdownDocument` in `Mked.Domain`. The private
constructor stores the string passed to `Parse` and returns it unchanged. Add one new test to
`Mked.Domain.Tests` asserting `MarkdownDocument.Parse("# Hello").Source == "# Hello"`. All
existing `MarkdownDocument` tests continue to pass.

---

## Task 2 — Add `StreamedDocument` and update `StreamInputUseCase`

Add `public sealed record StreamedDocument(string Source, MarkdownDocument Parsed)` to
`Mked.Application`. Change `StreamInputUseCase.ExecuteAsync` to return
`IAsyncEnumerable<Result<StreamedDocument, MkedError>>`; the internal `StringBuilder`
accumulator already holds the source text, so each `Ok` item wraps the current accumulator
string and the freshly parsed document. Update all `Mked.Application.Tests` tests that cover
`StreamInputUseCase` to assert against `StreamedDocument` payloads.

---

## Task 3 — Scaffold `Mked.Controls` and implement `MarkdownBlockRenderer`

Create `src/Mked.Controls/Mked.Controls.csproj` referencing Spectre.Console and Markdig with
no reference to any other `Mked.*` project. Implement the internal `MarkdownBlockRenderer`
sealed class that walks a Markdig `MarkdownDocument` AST and produces a `List<SegmentLine>`,
tracking the first-line offset of each top-level block. All block and inline types in the
design's rendering map must be handled with no `NotImplementedException` paths remaining;
`Markup.Escape` is applied to all user-supplied text before embedding in tag strings. Done
when the project builds cleanly with zero warnings.

---

## Task 4 — Implement `MarkdownViewer`

Implement `MarkdownViewer(string Markdown) : IRenderable` as a `sealed record class` in
`Mked.Controls` with `init` properties `ShowFrontmatter`, `PlainLinks`, `TopLineIndex`, and
`ViewportHeight`. On first `Measure` or `Render` call (or when the cache key changes), delegate
to `MarkdownBlockRenderer`, store the result and `MarkdownViewerScrollInfo` in a
`RenderStateHolder` keyed on `(maxWidth, ShowFrontmatter, PlainLinks)`, and clip the emitted
`Segment` sequence to the `ViewportHeight` window starting at `TopLineIndex`. Because
`MarkdownViewer` is a record, `with`-expression copies share the same `RenderStateHolder` when
only scroll properties change. `BlockCount` and `ScrollInfo` are derived from the cached data.
Done when a `MarkdownViewer` instance can be written to a `TestConsole` and its segment
sequence queried in isolation.

Depends on: Task 3

---

## Task 5 — `Mked.Controls.Tests`

Create `tests/Mked.Controls.Tests/Mked.Controls.Tests.csproj` (xUnit, AwesomeAssertions,
ArchUnitNet). Implement unit tests covering every scenario in the design's testing approach:
all block types (heading colour levels, bold/italic, inline code, fenced code block indentation,
blockquote `│ ` prefix, ordered and unordered list markers, link text-only and URL-bracket
variants, table borders, full-width HR), frontmatter hidden by default and shown with
`ShowFrontmatter = true`, scroll clipping (`TopBlockIndex` / `ViewportHeight`), `with`-copy
shared `Lazy<>` reference, `ScrollInfo.BlockStartLines` accuracy, and `BlockCount` correctness.
Add an ArchUnitNet test asserting `Mked.Controls` holds no references to `Mked.Domain`,
`Mked.Application`, `Mked.Infrastructure`, or `Mked.Console`. All tests must pass.

Depends on: Task 4

---

## Task 6 — Scaffold `Mked.Console` and implement `SpectreMarkdownRenderer`

Create `src/Mked.Console/Mked.Console.csproj` as a NativeAOT-targeted executable referencing
`Mked.Application`, `Mked.Infrastructure`, `Mked.Controls`, and `Spectre.Console.Cli`.
Implement `SpectreMarkdownRenderer` with two members: the single-shot
`IMarkdownRenderer<IRenderable>.Render(document, context)` (returns `new MarkdownViewer(document.Source)
{ ShowFrontmatter = …, PlainLinks = … }`) and the reactive
`async IAsyncEnumerable<MarkdownViewer> Stream(IAsyncEnumerable<Result<StreamedDocument, MkedError>>,
CancellationToken)` (yields a `MarkdownViewer` for each `Ok` item; skips `Err` items and writes
them to a `Channel<MkedError>` for out-of-band handling). Done when both members compile and a
simple inline test with a fake `IAsyncEnumerable` confirms `Stream` yields the expected viewers.

Depends on: Task 1, Task 2, Task 4

---

## Task 7 — `ViewSettings`, `ViewCommand`, and interactive file viewing

Implement `ViewSettings` (`CommandSettings` with `[path]` argument and `--stream`, `--follow`,
`--show-frontmatter`, `--plain` options; annotate with `[DynamicallyAccessedMembers(All)]` for
AOT safety). Implement `ViewCommand.ExecuteAsync` for the interactive file-viewing mode: call
`OpenFileUseCase.ExecuteAsync`, on error write `[red bold]Error:[/] [red]{message}[/]` and
return exit code 1; on success enter a `LiveDisplay` poll loop handling:
- ↓/j — scroll down one line; ↑/k — scroll up one line
- Shift+↓/Shift+J — jump to next block; Shift+↑/Shift+K — jump to previous block
- Page Down/Ctrl+D — scroll down half a screen; Page Up/Ctrl+U — scroll up half a screen
- g — jump to top; G — jump to bottom; q/Ctrl+C — exit

On resize, rebuild the base viewer (clears width-dependent cache); `currentLine` is preserved
and `Render` clamps it to the new document bounds. Add a minimal `Program.cs` that registers
`ViewCommand` in a `CommandApp`. Done when `mked view file.md` opens a file, navigates
correctly with all key bindings, preserves scroll position on terminal resize, and exits cleanly.

Depends on: Task 6

---

## Task 8 — Follow mode (`--follow`)

Add the `--follow` branch to `ViewCommand`. Implement a local `WatchDocuments` async
enumerable that yields the initial file load as a `Result<StreamedDocument, MkedError>`, then
yields a reload result for each notification from `FileWatcherAdapter.WatchAsync`. Pipe the
sequence through `renderer.Stream(...)` to obtain a reactive `MarkdownViewer` stream; on each
reload apply the saved `currentBlock` anchor with `baseViewer with { TopBlockIndex = currentBlock,
ViewportHeight = H }` and recompute `topLine` from the new `ScrollInfo.BlockStartLines`. Link
the watcher's lifetime to the keyboard loop's `CancellationTokenSource` so either task can
terminate the session. Done when `mked view --follow file.md` re-renders on file change while
preserving scroll position.

Depends on: Task 7

---

## Task 9 — Stream mode (`--stream`)

Add the `--stream` branch to `ViewCommand`. Pipe `StreamInputUseCase.ExecuteAsync(ct)` through
`renderer.Stream(...)` to obtain a `MarkdownViewer` stream; on each update auto-advance
`currentBlock` to the last block (tail-follow behaviour) and call `ctx.UpdateTarget`. Surface
mid-stream `Err(MkedError.StreamError)` items received from the renderer's error channel as a
dim status line at the bottom of the live region. Done when `echo "# Hello" | mked view --stream`
renders the heading and updates in place as stdin data arrives, and q/Ctrl+C exits cleanly.

Depends on: Task 7
