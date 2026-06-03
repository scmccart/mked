# Epic 04 — Markdown Viewer: Technical Design

> **Epic**: [`docs/epics/04-markdown-viewer.md`](../../docs/epics/04-markdown-viewer.md)
> **Status**: Draft
> **Date**: 2026-06-02

---

## Goals

1. Add a `Source` property to `MarkdownDocument` in `Mked.Domain` so renderer implementations
   can obtain the original source text without re-parsing.
2. Add `StreamedDocument` to `Mked.Application` and extend `StreamInputUseCase` to yield
   `Result<StreamedDocument, MkedError>` items carrying both accumulated source text and the
   parsed document.
3. Build `MarkdownViewer : IRenderable` in `Mked.Controls` — a standalone, scrollable Markdown
   widget that maps all Markdig block and inline types to styled `Segment` output, clips to a
   viewport height, and exposes scroll metadata for line-precise navigation. Designed as a
   `sealed record class` so `with`-expression copies share cached rendered lines while
   differing in scroll position.
4. Implement `SpectreMarkdownRenderer : IMarkdownRenderer<IRenderable>` in `Mked.Console` — the
   rich-terminal renderer that wraps `MarkdownViewer`. Uses Spectre.Console `Markup` for inline
   styling; Spectre's rendering pipeline degrades colour automatically based on terminal
   capability, eliminating the need for a separate limited-terminal renderer. Gains a reactive
   `Stream` method that transforms `IAsyncEnumerable<Result<StreamedDocument, MkedError>>` into
   `IAsyncEnumerable<MarkdownViewer>`, unifying stream and follow modes as reactive pipelines.
5. Build `ViewCommand` in `Mked.Console` supporting three operating modes: interactive file
   viewing (scrollable `LiveDisplay` with keyboard navigation), file-follow mode (`--follow`,
   auto-reload via `FileWatcherAdapter`), and stream mode (`--stream`, stdin tail display).
   Both follow and stream modes consume `renderer.Stream(...)` as a reactive pipeline.
   Piped / non-interactive output is explicitly out of scope — `mked view` always expects a TTY.
7. Implement keyboard navigation: ↓/j (scroll down), ↑/k (scroll up), Page Down / Page Up
   (scroll one screen), g (top), G (bottom), q / Ctrl+C (quit).
8. Implement viewport stability: `ViewportAnchor` records the topmost visible block; re-applied
   after terminal resize (detected via a background 1 Hz polling task), file reload, or stream
   update so the user stays oriented.
9. Create `Mked.Controls.Tests` with widget coverage for all rendered block types, scroll
   clipping, frontmatter visibility toggle, and architecture rules.

## Non-Goals

- `MarkdownEditor` widget, edit command, undo/redo stack (Epic 05).
- Full `CommandApp` registration and DI container wiring for the complete CLI (Epic 06 — this
  epic provisions a minimal working `ViewCommand` but defers final CLI wiring).
- NuGet packaging and release of `Mked.Controls` as a standalone library (Epic 07).
- Syntax highlighting within fenced code blocks (monospace style only; colour coding deferred).
- Search and filtering within the viewer.
- Mouse / pointer input.

---

## Architecture Overview

| Layer | Project | Role |
|-------|---------|------|
| Domain | `Mked.Domain` | Gains `Source` property on `MarkdownDocument`; no other changes |
| Application | `Mked.Application` | Gains `StreamedDocument` record; `StreamInputUseCase` return type updated |
| Infrastructure | `Mked.Infrastructure` | Not touched |
| Controls | `Mked.Controls` | New project; `MarkdownViewer` widget and `MarkdownBlockRenderer` |
| Presentation | `Mked.Console` | New project; renderer implementations, `ViewCommand`, `ViewSettings` |

**`Mked.Controls` independence constraint**: `Mked.Controls` must not reference `Mked.Domain`,
`Mked.Application`, or `Mked.Infrastructure`. It depends only on Spectre.Console and Markdig.
This preserves its status as a standalone NuGet-shippable library.

**Renderer bridging**: `IMarkdownRenderer<TOutput>` is defined in `Mked.Application` and
`IRenderable` lives in Spectre.Console. Renderer implementations that implement the interface
cannot live in `Mked.Controls` (which doesn't reference Application). They live in `Mked.Console`,
the composition root that references all layers.

**Single renderer strategy**: `SpectreMarkdownRenderer` is the only renderer. It produces
`Markup`-based `IRenderable` output; Spectre.Console's rendering pipeline degrades colour depth
automatically based on `console.Profile.Capabilities.ColorSystem`. `mked view` always expects a
TTY — piped output is not a supported mode and no plain-text renderer is provided.

**AOT/Trim**: `Mked.Controls` uses Spectre.Console rendering APIs (`IRenderable`, `Segment`,
`Style`, `Measurement`) and Markdig AST traversal — all trim-safe. `Mked.Console` introduces
Spectre.Console.Cli for `ViewCommand`; the existing AOT caveat around settings binding applies
(annotate `ViewSettings` with `[DynamicDependency]` until upstream source-gen support lands).

---

## Key Types and Interfaces

### New Types

| Type | Kind | Project | Purpose |
|------|------|---------|---------|
| `StreamedDocument` | sealed record | `Mked.Application` | `Source: string`, `Parsed: MarkdownDocument` — return payload of `StreamInputUseCase`; carries the accumulated source text so `MarkdownViewer` can be constructed without re-parsing |
| `MarkdownViewer` | sealed record class, `IRenderable` | `Mked.Controls` | Standalone scrollable Markdown widget; parses `string markdown`, renders styled `Segment` sequences clipped to `ViewportHeight`; record class enables `with`-expression copies that share the cached rendered-lines state |
| `MarkdownBlockRenderer` | sealed class (internal) | `Mked.Controls` | Walks Markdig AST; maps each block and inline type to a `List<SegmentLine>`; used by `MarkdownViewer` |
| `MarkdownViewerScrollInfo` | sealed record | `Mked.Controls` | `TotalLineCount: int`, `BlockStartLines: IReadOnlyList<int>` — scroll metadata computed once per `MarkdownViewer` instance |
| `SpectreMarkdownRenderer` | sealed class, `IMarkdownRenderer<IRenderable>` | `Mked.Console` | Wraps `MarkdownViewer` using `document.Source`; implements rich-terminal rendering for the Application layer; gains a reactive `Stream` method for follow/stream modes |
| `ViewSettings` | sealed class, `CommandSettings` | `Mked.Console` | Spectre.Console.Cli settings: `[path]` argument, `--stream`, `--follow`, `--show-frontmatter`, `--plain` (show link URLs) options |
| `ViewCommand` | sealed class, `IAsyncCommand<ViewSettings>` | `Mked.Console` | Entry point for `mked view`; dispatches to interactive, follow, or stream sub-flows |

### Modified Types

| Type | Change | Reason |
|------|--------|--------|
| `MarkdownDocument` | New `public string Source { get; }` property; source string stored at parse time and returned unchanged | `SpectreMarkdownRenderer` needs the original source to construct `MarkdownViewer(string)` without re-parsing the document |
| `StreamInputUseCase` | Return type changed from `IAsyncEnumerable<Result<MarkdownDocument, MkedError>>` to `IAsyncEnumerable<Result<StreamedDocument, MkedError>>`; internal `StringBuilder` accumulator now also surfaces as `StreamedDocument.Source` | Stream-mode viewer needs source text to construct `MarkdownViewer`; the use case already accumulates it internally |

---

## `MarkdownViewer` Contract

```csharp
namespace Mked.Controls;

/// <summary>Standalone scrollable Markdown rendering widget.</summary>
public sealed record class MarkdownViewer(string Markdown) : IRenderable
{
    /// <summary>Show the raw YAML front matter block above the document body.</summary>
    public bool ShowFrontmatter { get; init; }

    /// <summary>Render link text only, omitting URLs.</summary>
    public bool PlainLinks { get; init; }

    /// <summary>
    /// 0-based index of the first terminal line to display.
    /// Defaults to 0 (document top). Clamped to [0, TotalLineCount - ViewportHeight].
    /// </summary>
    public int TopLineIndex { get; init; }

    /// <summary>
    /// Maximum number of terminal rows to render.
    /// <see langword="null"/> emits the entire document.
    /// </summary>
    public int? ViewportHeight { get; init; }

    /// <summary>Total top-level block count.</summary>
    public int BlockCount { get; }

    /// <summary>
    /// Scroll metadata: total rendered line count and the first line index of each block.
    /// Recomputed whenever the cache key (maxWidth, ShowFrontmatter, PlainLinks) changes.
    /// </summary>
    public MarkdownViewerScrollInfo ScrollInfo { get; }

    // IRenderable
    public Measurement Measure(RenderOptions options, int maxWidth);
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth);
}
```

`MarkdownViewer` parses `Markdown` with Markdig (using `UseAdvancedExtensions().UseYamlFrontMatter()`)
and caches the full `List<SegmentLine>` and `MarkdownViewerScrollInfo` in a `RenderStateHolder`
keyed on `(maxWidth, ShowFrontmatter, PlainLinks)`. Because `MarkdownViewer` is a record,
`with`-expression copies share the same `RenderStateHolder` reference when only `TopLineIndex`
or `ViewportHeight` changes — the cached lines are reused, and `Render` simply emits the
appropriate slice starting at `TopLineIndex`.

---

## `SpectreMarkdownRenderer` Contract

```csharp
namespace Mked.Console;

public sealed class SpectreMarkdownRenderer(RenderContext context)
    : IMarkdownRenderer<IRenderable>
{
    // Single-shot render (IMarkdownRenderer<IRenderable> implementation).
    // Used for non-interactive output (piped TTY, or a quick static write).
    public IRenderable Render(MarkdownDocument document, RenderContext context) =>
        new MarkdownViewer(document.Source)
        {
            ShowFrontmatter = context.ShowFrontmatter,
            PlainLinks = context.PlainLinks
        };

    // Reactive streaming: maps each StreamedDocument to a MarkdownViewer.
    // Used by ViewCommand for both stream mode and follow mode.
    public async IAsyncEnumerable<MarkdownViewer> Stream(
        IAsyncEnumerable<Result<StreamedDocument, MkedError>> source,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var result in source.WithCancellation(ct))
        {
            if (result is Result<StreamedDocument, MkedError>.Ok ok)
                yield return new MarkdownViewer(ok.Value.Source)
                {
                    ShowFrontmatter = context.ShowFrontmatter,
                    PlainLinks = context.PlainLinks
                };
            // Err items: ViewCommand handles errors via an out-of-band channel
            // (writes a dim status line); the stream continues.
        }
    }
}
```

`ViewCommand` applies the current scroll position and viewport height before passing each yielded
viewer to `ctx.UpdateTarget`:

```csharp
await foreach (var baseViewer in renderer.Stream(docStream, ct))
{
    var viewer = baseViewer with { TopLineIndex = currentLine, ViewportHeight = H };
    ctx.UpdateTarget(viewer);
}
```

---

## Block Rendering Map

`MarkdownBlockRenderer` maps every Markdig block type that appears in standard Markdown.
Inline styling uses Spectre.Console `Markup` tag strings; container blocks build
`Spectre.Console.Table` and `Rule` renderables as needed.

| Markdig block type | Rendered output |
|--------------------|-----------------|
| `HeadingBlock` (H1–H6) | `[bold blue]…[/]` H1, `[bold green]…[/]` H2, `[bold yellow]…[/]` H3, `[bold grey]…[/]` H4–H6 |
| `ParagraphBlock` | Inline content (see inline table below); single blank line after |
| `FencedCodeBlock`, `CodeBlock` | `[dim]…[/]`; left-indented 2 spaces |
| `QuoteBlock` | Each line prefixed `[dim]│ [/]`; child blocks rendered recursively |
| `ListBlock` + `ListItemBlock` | Ordered: `1. `, `2. `…; unordered: `• `; nested items indented 2 spaces per level, marker `◦ ` |
| `HtmlBlock` | Emitted as plain text (no HTML interpretation) |
| `ThematicBreakBlock` | Full-width `Spectre.Console.Rule` in dim style |
| `Table` | `Spectre.Console.Table` with box borders; column headers `[bold]…[/]` |
| `YamlFrontMatterBlock` | Skipped unless `ShowFrontmatter = true`; shown as `[dim]…[/]` monospace block |

| Markdig inline type | Rendered output |
|---------------------|-----------------|
| `LiteralInline` | `[/]` (plain) |
| `EmphasisInline` (1 star/underscore) | `[italic]…[/]` |
| `EmphasisInline` (2 stars/underscores) | `[bold]…[/]` |
| `CodeInline` | `[dim]…[/]` |
| `LinkInline` | Link text only (`PlainLinks = true`); or link text + `[dim] (url)[/]` |
| `AutolinkInline` | URL as plain text |
| `LineBreakInline` | `Segment.LineBreak` |

All `Markup` tag strings use `Markup.Escape(text)` on user-supplied content before embedding.
Spectre.Console degrades colour tags automatically when the terminal reports limited `ColorSystem`.

---

## Data Flow / Sequence

### Use Case: Interactive File Viewing (`mked view file.md`)

1. `ViewCommand` detects that stdout is a TTY and `settings.Stream` and `settings.Follow` are
   both `false`.
2. Calls `OpenFileUseCase.ExecuteAsync(settings.Path)`.
   - On `Err` → `AnsiConsole.MarkupLine("[red bold]Error:[/] [red]{msg}[/]")`, exit 1.
3. Creates `new ViewerState(openedFile.Parsed)` and initialises `topLine = 0`,
   `H = console.Profile.Height`.
4. Starts `AnsiConsole.Live(initialViewer).StartAsync(async ctx => { ... })`:

```
// Poll loop inside the LiveDisplay callback:

loop:
  if key available:
    match key:
      ↓ / j (no modifier)       → currentLine += 1
      ↑ / k (no modifier)       → currentLine -= 1
      Shift+↓ / Shift+J         → currentLine = next BlockStartLines entry > currentLine
      Shift+↑ / Shift+K         → currentLine = last BlockStartLines entry < currentLine
      PageDown / Ctrl+D         → currentLine += H/2
      PageUp   / Ctrl+U         → currentLine -= H/2
      g                         → currentLine = 0
      G                         → currentLine = max(0, TotalLineCount - H)
      q / Ctrl+C                → cancel and break
    clamp currentLine to [0, max(0, TotalLineCount - H)]

  if resize detected:
    H = new height
    rebuild baseViewer (new RenderStateHolder, busts width cache)

  if dirty:
    viewer = baseViewer with { TopLineIndex = currentLine, ViewportHeight = H }
    ctx.UpdateTarget(viewer)
```

### Use Case: Follow Mode (`mked view --follow file.md`)

Follow mode models file-watch events as `IAsyncEnumerable<Result<StreamedDocument, MkedError>>`
and feeds them through `renderer.Stream(...)`:

```csharp
// Build a watch sequence: initial load + reload on each file-change notification
async IAsyncEnumerable<Result<StreamedDocument, MkedError>> WatchDocuments(
    string path, CancellationToken ct)
{
    var initial = await openFileUseCase.ExecuteAsync(path, ct);
    yield return initial.Map(f => new StreamedDocument(f.Source, f.Parsed));

    await foreach (var _ in fileWatcher.WatchAsync(ct))
    {
        var reloaded = await openFileUseCase.ExecuteAsync(path, ct);
        yield return reloaded.Map(f => new StreamedDocument(f.Source, f.Parsed));
    }
}

// ViewCommand pipes this through the reactive renderer:
var docStream   = WatchDocuments(settings.Path, ct);
var viewerStream = renderer.Stream(docStream, ct);

await AnsiConsole.Live(initialViewer).StartAsync(async ctx =>
{
    var reloadTask = Task.Run(async () =>
    {
        await foreach (var baseViewer in viewerStream)
        {
            // Preserve currentLine across reload; Render clamps to new document bounds
            viewer = baseViewer with { TopLineIndex = currentLine, ViewportHeight = H };
            ctx.UpdateTarget(viewer);
        }
    }, ct);

    await Task.WhenAny(keyTask, resizeTask, reloadTask);
    linkedCts.Cancel();
});
```

On reload, `currentLine` is preserved; `MarkdownViewer.Render` clamps it to the new document's
valid range so the user stays as close to their previous position as possible.

### Use Case: Stream Mode (`mked view --stream`)

Stream mode is structurally identical to follow mode but the source is `StreamInputUseCase`:

```csharp
var docStream    = streamInputUseCase.ExecuteAsync(ct);   // IAsyncEnumerable<Result<StreamedDocument, MkedError>>
var viewerStream = renderer.Stream(docStream, ct);         // IAsyncEnumerable<MarkdownViewer>

// On each new chunk, tail-follow to the bottom by setting currentLine = int.MaxValue;
// Render clamps this to max(0, TotalLineCount - H) automatically.
await AnsiConsole.Live(initialViewer).StartAsync(async ctx =>
{
    var streamTask = Task.Run(async () =>
    {
        await foreach (var incoming in viewerStream)
        {
            currentLine = int.MaxValue; // Render clamps to last visible line
            viewer = incoming with { TopLineIndex = currentLine, ViewportHeight = H };
            ctx.UpdateTarget(viewer);
        }
    }, ct);

    await Task.WhenAny(keyTask, resizeTask, streamTask);
    linkedCts.Cancel();
});
```

Errors from `StreamInputUseCase` (`Err(MkedError.StreamError)`) are not yielded by `renderer.Stream`;
`ViewCommand` receives them via a secondary error-notification channel (a
`Channel<MkedError>` written to inside `WatchDocuments` / equivalent) and renders a dim status
line at the bottom of the viewport.

---

## Scroll Metadata and Viewport Stability

```csharp
public sealed record MarkdownViewerScrollInfo(
    int TotalLineCount,
    IReadOnlyList<int> BlockStartLines);   // index i → first rendered line of block i
```

`MarkdownViewer` builds this on the first `Render` / `Measure` call, tracking the `SegmentLine`
offset at which each top-level block starts. The metadata is keyed on `(maxWidth, ShowFrontmatter,
PlainLinks)` and is recomputed automatically when any of those change. On terminal resize,
`ViewCommand` rebuilds the base viewer (clearing the cache); `currentLine` is preserved and
`Render` clamps it to the new document's valid range.

---

## Error Handling Strategy

- **New `MkedError` variants**: None.
- **Error production boundaries**: All errors originate upstream (Epics 02 and 03).
  `ViewCommand` is a consumer-only layer.
- **`ViewCommand` error handling** (TTY only; piped output is not a supported mode):
  - `OpenFileUseCase` returns `Err` → `[red bold]Error:[/] [red]{message}[/]` via
    `AnsiConsole.MarkupLine`, exit code 1.
  - `Err(StreamError)` mid-stream → write a dim status line at the bottom of the live region
    via the secondary error channel; enumeration continues.
  - `FileWatcherAdapter` internal error event (OS buffer overflow) → log at trace level,
    suppress from UI; the watch sequence continues.
- **User-visible error format**: `[red bold]Error:[/] [red]{message}[/]` — provisional; Epic 06
  will standardise this across all commands.

---

## Testing Approach

All widget tests live in `Mked.Controls.Tests` (xUnit, AwesomeAssertions; Moq not required for
pure rendering). `ViewCommand` integration is verified manually in this epic; command-level unit
coverage lands in Epic 06.

```
tests/Mked.Controls.Tests/
├── Unit/
│   ├── MarkdownViewer_Render_Tests.cs
│   ├── MarkdownViewer_Scroll_Tests.cs
│   └── MarkdownViewer_Frontmatter_Tests.cs
└── Architecture/
    └── ControlsLayer_DependencyRules_Tests.cs
```

**Rendering tests** — use `TestConsole` to capture output text and segment-level assertions
for style properties:

```csharp
// Output text assertion
var console = new TestConsole().Width(80);
console.Write(new MarkdownViewer("# Hello"));
console.Output.Should().Contain("Hello");

// Style assertion via segments
var options  = RenderOptions.Create(new TestConsole());
var segments = new MarkdownViewer("**bold**").Render(options, 80).ToList();
segments.Should().Contain(s => s.Style.Decoration.HasFlag(Decoration.Bold));
```

Specific scenarios:

- **Heading**: H1 renders with blue foreground + bold; H2 green; H3 yellow; H4–H6 grey.
- **Bold / italic**: `**text**` → `Decoration.Bold`; `*text*` → `Decoration.Italic`.
- **Inline code**: `` `code` `` → dim foreground, no background colour.
- **Fenced code block**: all segments dim; content indented 2 spaces.
- **Blockquote**: each line prefixed with `│ `; prefix segment is dim.
- **Unordered list**: top-level items prefixed `• `; nested items indented + `◦ `.
- **Ordered list**: items prefixed `1.`, `2.`, etc.
- **Link (default)**: link text plain; URL rendered dim in brackets.
- **Link (`PlainLinks = true`)**: link text only; URL absent from output.
- **Table**: rendered via Spectre.Console `Table`; column headers bold; borders present.
- **Horizontal rule**: full-width rule in dim style.
- **Frontmatter (default)**: YAML block absent from output.
- **Frontmatter (`ShowFrontmatter = true`)**: raw YAML present as dim monospace block.
- **Scroll / clip**: `TopLineIndex = BlockStartLines[1], ViewportHeight = 5` on a 3-block document
  — first block's text absent from output; blocks 1 and 2 present.
- **`with`-expression scroll**: `viewer with { TopLineIndex = 1 }` shares cached segments;
  `ScrollInfo` reference is identical to the original instance's.
- **`ScrollInfo` accuracy**: `BlockStartLines[0] == 0`; `BlockStartLines[i] ==
  BlockStartLines[i-1] + lineCountOfBlock(i-1)` for all `i`.
- **`BlockCount`**: equals top-level Markdig document block count.

**Architecture tests** (ArchUnitNet, no trait):

- `Mked.Controls` must not reference `Mked.Domain`, `Mked.Application`, `Mked.Infrastructure`,
  or `Mked.Console`.
- `Mked.Controls` must reference `Spectre.Console` and `Markdig`.

---

## Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | **`StreamInputUseCase` source text**: should the use case return source text alongside the parsed document? **Resolution: yes — `StreamInputUseCase` now returns `IAsyncEnumerable<Result<StreamedDocument, MkedError>>`** where `StreamedDocument(Source, Parsed)` carries the accumulated source. The internal `StringBuilder` already held this text; surfacing it requires no new accumulation logic. | Resolved |
| 2 | **Resize detection**: should `ViewCommand` poll terminal dimensions on every key event, or via a background task? **Resolution: background `PeriodicTimer` at ~1 Hz.** Polling on key events misses resizes when no key is pressed (e.g., in follow/stream mode). A 1 Hz `PeriodicTimer` task running concurrently inside the `LiveDisplay` callback detects resize promptly without burning CPU. | Resolved |
| 3 | **`AnsiMarkdownRenderer` implementation**: rendering via `TestConsole` in production was identified as inappropriate. **Resolution: `AnsiMarkdownRenderer` is removed entirely.** `SpectreMarkdownRenderer` uses Spectre.Console `Markup` for all inline styling; Spectre's rendering pipeline degrades colour automatically based on `console.Profile.Capabilities.ColorSystem`. Only piped output (no ANSI at all) requires a separate strategy (`PlainTextRenderer`). | Resolved |
| 4 | **`SpectreMarkdownRenderer` coupling**: should the renderer be reactive, accepting a document stream and emitting viewers as new content arrives? **Resolution: yes — `SpectreMarkdownRenderer` gains an `async IAsyncEnumerable<MarkdownViewer> Stream(IAsyncEnumerable<Result<StreamedDocument, MkedError>>, CancellationToken)` method.** Both stream mode and follow mode model their input as `IAsyncEnumerable<Result<StreamedDocument, MkedError>>` and pipe it through `renderer.Stream(...)`. `ViewCommand` applies the current scroll anchor via `baseViewer with { TopBlockIndex = currentBlock }` before handing each viewer to `ctx.UpdateTarget`. | Resolved |
