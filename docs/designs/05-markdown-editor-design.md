# Epic 05 — Markdown Editor: Technical Design

> **Epic**: [`docs/epics/05-markdown-editor.md`](../../docs/epics/05-markdown-editor.md)
> **Status**: Implemented (see refactoring note below)
> **Date**: 2026-06-03

---

## Goals

1. Build pure, stateless `BufferOperations` helper functions (Insert, Delete, ToOffset,
   FromOffset) so callers no longer reconstruct the full buffer string externally.
2. Add clamped cursor-movement as pure functions in `CursorNavigation`, used by `EditorState`
   wrapper methods that push to the undo stack only for buffer mutations — not cursor moves.
3. Define `IHighlightLayer`, `HighlightSpan`, and `HighlightKind`; implement five stateless
   highlight layer classes (heading, emphasis, link, frontmatter, code fence) as pure annotators.
4. Build `MarkdownEditorWidget` and `EditorStatusLine` in `Mked.Controls` — both are `IRenderable`
   types that accept pre-computed data.
5. Build `MarkdownEditor` in `Mked.Controls` — a self-contained, embeddable interactive control
   that owns editing state, the highlight pipeline, undo/redo, scroll, and input dispatch.
6. Implement `EditCommand` and `EditSettings` in `Mked.Console` — the `mked edit` entry point
   using `AnsiConsole.Live + Layout`, routing input through `MarkdownEditor.HandleKey`.
7. Add test coverage: `Mked.Controls.Tests` for buffer and navigation functions, editor state,
   highlight layers, widget and status line rendering, and the ArchUnitNET rule.

> **Refactoring note (feat/epic-5-refactor):** The editor machinery — `EditorState`,
> `IEditorObserver`, `CursorPosition`, `TextRange`, `BufferOperations`, `CursorNavigation`,
> `IHighlightLayer`, `HighlightSpan`, `HighlightKind`, the five layer implementations, and
> `HighlightMapper` — was initially placed in `Mked.Domain` but was later moved to `Mked.Controls`
> and the `NewDocumentUseCase` use case was deleted. These are editor-control implementation
> details, not business domain types. The move keeps `Mked.Controls` fully self-contained and
> preserves the "Controls does not reference Domain" ArchUnitNET rule.

## Non-Goals

- True gap buffer or rope structure (line-array string operations suffice for v1 Markdown
  files; the internal representation is hidden behind `EditorState` and can be swapped without
  callers changing).
- `MarkdownEditor : IPrompt<string>` standalone library widget — that is the Epic 07 Controls
  NuGet API; this epic builds the `mked edit` command experience only.
- Clipboard integration (Ctrl+C / X / V).
- Search and replace within the editor.
- Full DI container wiring (Epic 06).
- NuGet packaging (Epic 07).
- `AlternateScreen` buffer rendering — `LiveDisplay` is used here; `AlternateScreen` is deferred.

---

## Architecture Overview

| Layer | Project | Changes |
|-------|---------|---------|
| Domain | `Mked.Domain` | Unchanged — only genuine domain types remain (`MkedError`, `Result`, `Maybe`, `ViewerState`, file ports, `MarkdownDocument`) |
| Application | `Mked.Application` | `NewDocumentUseCase` removed (host inlines `new MarkdownEditor("")`); `OpenFileUseCase` and `SaveFileUseCase` unchanged |
| Controls | `Mked.Controls` | All editor machinery lives here: `CursorPosition`, `TextRange`, `BufferOperations`, `CursorNavigation`, `EditorState`, `IEditorObserver`, `IHighlightLayer`, `HighlightSpan`, `HighlightKind`, five layer implementations, `HighlightMapper`, `StyledSpan`, `MarkdownEditorWidget`, `EditorStatusLine`; new embeddable `MarkdownEditor` control |
| Presentation | `Mked.Console` | `EditSettings`, `EditCommand` (rewritten to `AnsiConsole.Live + Layout` using `MarkdownEditor`); `Program.cs` wired |

**Controls independence constraint** — `Mked.Controls` must not reference `Mked.Domain`.
The editor machinery types (`EditorState`, cursor/buffer/navigation helpers, highlight layers)
are control-implementation details, not business domain. They live in `Mked.Controls` and are
fully self-contained there. The ArchUnitNET rule `Controls_DoesNotReferenceDomain` remains
green because no `Mked.Domain.*` type is referenced from `Mked.Controls`.

**AOT/Trim** — All new types use BCL and Spectre.Console APIs. Markdig AST traversal in the
highlight layers is already trim-safe. No reflection, no `dynamic`, no `Activator`.

---

## Functional Core / Imperative Shell

This epic applies the functional core / imperative shell principle explicitly:

| Layer | Classification | Reason |
|-------|---------------|--------|
| `BufferOperations` | **Functional core** | Pure functions: `(string, CursorPosition, string) → string` — no state, fully testable |
| `CursorNavigation` | **Functional core** | Pure functions: `(string, CursorPosition) → CursorPosition` |
| `IHighlightLayer` implementations | **Functional core** | `(string, Markdig.Syntax.MarkdownDocument) → IEnumerable<HighlightSpan>` — stateless |
| `HighlightMapper` | **Functional core** | Pure span conversion |
| `MarkdownEditor.HandleKey` | **Functional core** | Translates `ConsoleKeyInfo` to state mutation; returns `bool` (dirty) |
| `EditorState` | Mutable coordinator | Calls functional core, maintains undo stacks, fires observer events |
| `MarkdownEditor` | Control façade | Owns `EditorState`, highlight cache, scroll; exposes `IRenderable` and `HandleKey` |
| `EditCommand` poll loop | **Imperative shell** | Reads keyboard, routes to `editor.HandleKey`, calls use cases, drives `liveCtx.UpdateTarget` |
| `SaveFileUseCase` / `OpenFileUseCase` | Railway (ROP) | Return `Result<T, MkedError>`; shell pattern-matches the result |

---

## Key Types and Interfaces

### `Mked.Domain` — unchanged by this epic

Domain retains only genuine business-domain types: `MkedError`, `Result<T,E>`, `Maybe<T>`,
`Unit`, `ViewerState`, `ViewportAnchor`, `MarkdownDocument`, and the file/input port interfaces.
The editor machinery described below lives in `Mked.Controls`.

### New — `Mked.Controls` (editor machinery + controls)

| Type | Kind | Purpose |
|------|------|---------|
| `CursorPosition` | readonly record struct | 1-based `(int Line, int Column)` cursor coordinate |
| `TextRange` | readonly record struct | `(CursorPosition Start, CursorPosition End)` text span |
| `BufferOperations` | static class | Pure: `Insert`, `Delete`, `ToOffset`, `FromOffset` |
| `CursorNavigation` | static class | Pure: `MoveLeft`, `MoveRight`, `MoveUp`, `MoveDown`, `MoveWordLeft`, `MoveWordRight`, `MoveToLineStart`, `MoveToLineEnd`, `Clamp` |
| `IEditorObserver` | interface | `OnBufferChanged(string)` / `OnCursorMoved(CursorPosition)` |
| `EditorState` | sealed class | Mutable text buffer + cursor + undo/redo stacks; notifies observers |
| `HighlightKind` | enum | `Heading`, `Bold`, `Italic`, `InlineCode`, `LinkText`, `LinkUrl`, `FrontmatterBlock`, `CodeFence` |
| `HighlightSpan` | readonly record struct | `(TextRange Range, HighlightKind Kind)` — source span + category |
| `IHighlightLayer` | interface | `IEnumerable<HighlightSpan> Annotate(string source, Markdig.Syntax.MarkdownDocument document)` |
| `HeadingHighlightLayer` | sealed class | Annotates `#` markers and heading text |
| `EmphasisHighlightLayer` | sealed class | Annotates `*`/`**`/`_`/`__` delimiters |
| `LinkHighlightLayer` | sealed class | Annotates `[text]` as `LinkText` and `(url)` as `LinkUrl` |
| `FrontMatterDimLayer` | sealed class | Annotates entire YAML frontmatter block as `FrontmatterBlock` |
| `CodeFenceLayer` | sealed class | Annotates fenced code block bodies as `CodeFence` |
| `HighlightMapper` | static class | `IReadOnlyList<StyledSpan> Map(IEnumerable<HighlightSpan>, string buffer)` |
| `StyledSpan` | readonly record struct | `(int StartOffset, int Length, Style SpectreStyle)` — Spectre-native highlight span |
| `MarkdownEditorWidget` | sealed class, `IRenderable` | Passive renderer: raw text buffer with block cursor and `StyledSpan` overlays, clipped to viewport |
| `EditorStatusLine` | sealed class, `IRenderable` | One-line bar: `Ln {line}, Col {col}   ● (dirty)   {n} words` |
| **`MarkdownEditor`** | **sealed class, `IRenderable`** | **Embeddable interactive control: owns `EditorState`, highlight pipeline, scroll, `HandleKey`, `BufferChanged` event** |

### New — `Mked.Console`

| Type | Kind | Purpose |
|------|------|---------|
| `EditSettings` | sealed class, `CommandSettings` | Optional `[path]` argument; `--split` flag |
| `EditCommand` | sealed class, `AsyncCommand<EditSettings>` | `mked edit` entry point; `AnsiConsole.Live + Layout` loop routing keys through `MarkdownEditor.HandleKey` |

---

## `BufferOperations` Contract

```csharp
namespace Mked.Domain;

/// <summary>
/// Pure buffer-manipulation functions. Every method returns a new string; no state is mutated.
/// Positions are 1-based (Line, Column) matching <see cref="CursorPosition"/>.
/// </summary>
public static class BufferOperations
{
    /// <summary>
    /// Returns a new buffer string with <paramref name="text"/> spliced in at
    /// <paramref name="position"/>. Column past end-of-line clamps to EOL.
    /// </summary>
    public static string Insert(string buffer, CursorPosition position, string text);

    /// <summary>
    /// Returns a new buffer string with the content of <paramref name="range"/> removed.
    /// </summary>
    public static string Delete(string buffer, TextRange range);

    /// <summary>
    /// Converts a 1-based <see cref="CursorPosition"/> to a 0-based linear character offset.
    /// </summary>
    public static int ToOffset(string buffer, CursorPosition position);

    /// <summary>
    /// Converts a 0-based linear character offset to a 1-based <see cref="CursorPosition"/>.
    /// </summary>
    public static CursorPosition FromOffset(string buffer, int offset);
}
```

---

## `CursorNavigation` Contract

```csharp
namespace Mked.Domain;

/// <summary>
/// Pure cursor-navigation functions. All methods clamp to valid positions within
/// <paramref name="buffer"/>. None mutate state.
/// </summary>
public static class CursorNavigation
{
    public static CursorPosition MoveLeft(string buffer, CursorPosition current);
    public static CursorPosition MoveRight(string buffer, CursorPosition current);
    public static CursorPosition MoveUp(string buffer, CursorPosition current);
    public static CursorPosition MoveDown(string buffer, CursorPosition current);

    /// <summary>Moves to the start of the previous word (whitespace-delimited).</summary>
    public static CursorPosition MoveWordLeft(string buffer, CursorPosition current);

    /// <summary>Moves to the start of the next word.</summary>
    public static CursorPosition MoveWordRight(string buffer, CursorPosition current);

    public static CursorPosition MoveToLineStart(string buffer, CursorPosition current);
    public static CursorPosition MoveToLineEnd(string buffer, CursorPosition current);

    /// <summary>Clamps <paramref name="position"/> to a valid location in <paramref name="buffer"/>.</summary>
    public static CursorPosition Clamp(string buffer, CursorPosition position);
}
```

---

## `EditorState` Changes

### `Insert` and `Delete` (buffer mutations — undo-tracked)

```csharp
/// <summary>Inserts <paramref name="text"/> at <paramref name="position"/> and notifies observers.</summary>
public void Insert(CursorPosition position, string text)
{
    ArgumentNullException.ThrowIfNull(text);
    _undoStack.Push(new BufferCommand(Buffer));
    _redoStack.Clear();
    SetBufferInternal(BufferOperations.Insert(Buffer, position, text));
    foreach (var o in _observers) o.OnBufferChanged(Buffer);
}

/// <summary>Deletes the text within <paramref name="range"/> and notifies observers.</summary>
public void Delete(TextRange range)
{
    _undoStack.Push(new BufferCommand(Buffer));
    _redoStack.Clear();
    SetBufferInternal(BufferOperations.Delete(Buffer, range));
    foreach (var o in _observers) o.OnBufferChanged(Buffer);
}
```

### Cursor movement (not undo-tracked)

Cursor moves call `SetCursorInternal` directly and notify observers, but they do **not** push to
`_undoStack` or clear `_redoStack`. Undo restores buffer content only; cursor position after
undo is recalculated by the caller.

```csharp
public void MoveCursorLeft()
{
    var next = CursorNavigation.MoveLeft(Buffer, Cursor);
    if (next == Cursor) return;
    SetCursorInternal(next);
    foreach (var o in _observers) o.OnCursorMoved(Cursor);
}
// ... same pattern for all eight directions
```

---

## `IHighlightLayer` Interface and Implementations

```csharp
namespace Mked.Domain;

/// <summary>
/// Pure, stateless Markdown syntax annotator. Implementors must not retain mutable state.
/// </summary>
public interface IHighlightLayer
{
    /// <summary>
    /// Returns source spans annotated with their highlight category.
    /// Results from multiple layers may overlap; callers merge by priority.
    /// </summary>
    IEnumerable<HighlightSpan> Annotate(string source, MarkdownDocument document);
}
```

Each implementation walks the Markdig AST to locate relevant spans in `source`:

| Layer | Strategy |
|-------|---------|
| `HeadingHighlightLayer` | Uses `HeadingBlock.Line`, `HeadingBlock.Column`, and `HeadingBlock.Level` to mark `#` characters and heading text |
| `EmphasisHighlightLayer` | Uses `EmphasisInline.DelimiterCount` and span offsets to mark `*`/`**` delimiters and enclosed text |
| `LinkHighlightLayer` | Uses `LinkInline` spans to split `[text]` (`LinkText`) from `(url)` (`LinkUrl`) |
| `FrontMatterDimLayer` | Marks the full `YamlFrontMatterBlock` span as `FrontmatterBlock` |
| `CodeFenceLayer` | Marks each `FencedCodeBlock` body span as `CodeFence`; outer delimiters are included |

All implementations are `sealed class` with no constructor parameters.

---

## `MarkdownEditorWidget` Contract

```csharp
namespace Mked.Controls;

/// <summary>Renders a raw text buffer with a block cursor and syntax-highlight overlays.</summary>
public sealed class MarkdownEditorWidget : IRenderable
{
    public MarkdownEditorWidget(
        string buffer,
        (int Line, int Column) cursor,          // 1-based; plain value tuple — no Domain dependency
        IReadOnlyList<StyledSpan> highlights,
        int topLineIndex = 0,
        int? viewportHeight = null);

    public Measurement Measure(RenderOptions options, int maxWidth);
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth);
}
```

**Rendering algorithm:**
1. Split `buffer` into lines; clip to `[topLineIndex, topLineIndex + viewportHeight)`.
2. For each visible line, scan through `StyledSpan` entries intersecting that line; emit
   `Segment` objects with the appropriate `Style` for each run, plain `Style.Plain` for
   unhighlighted runs.
3. For the cursor line and column: the character at the cursor is emitted with
   `Decoration.Invert` (block cursor). If the cursor is at end-of-line, a space with
   `Decoration.Invert` is appended.

---

## `EditorStatusLine` Contract

```csharp
namespace Mked.Controls;

/// <summary>Single-line status bar for the editor.</summary>
public sealed class EditorStatusLine : IRenderable
{
    public EditorStatusLine(
        (int Line, int Column) cursor,
        bool isDirty,
        int wordCount);

    public Measurement Measure(RenderOptions options, int maxWidth);
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth);
}
```

Renders: `  Ln {line}, Col {col}   ● {n} words  ` where `●` is `[yellow]●[/]` when dirty, dim
when clean. Word count is computed from the raw buffer string (whitespace-split; YAML
frontmatter is not excluded in v1).

---

## `MarkdownEditor` Control API

`MarkdownEditor` is the embeddable interactive control that lives in `Mked.Controls`. It owns
the full editing lifecycle and exposes a clean host API:

```csharp
public sealed class MarkdownEditor : IRenderable, IEditorObserver
{
    public MarkdownEditor(string initialBuffer = "");

    // Input dispatch — returns true if anything changed (host should redraw).
    // Returns false for unhandled host-level keys (Save, Quit, Open, New, TogglePreview).
    public bool HandleKey(ConsoleKeyInfo key);

    // Buffer and cursor access
    public string Buffer { get; }
    public (int Line, int Column) Cursor { get; }
    public bool IsDirty { get; }
    public int WordCount { get; }
    public bool CanUndo { get; }
    public bool CanRedo { get; }

    // Document lifecycle
    public void LoadDocument(string buffer);   // reset buffer, undo/redo, cursor (1,1), clean baseline, scroll
    public void MarkClean();                   // call after a successful Save

    // Layout integration
    public bool HasFocus { get; set; }          // gates the block cursor; Ctrl+Tab flips focus in split-view
    public int? ViewportHeight { get; set; }    // set by host before each UpdateTarget call
    public event Action<string>? BufferChanged; // drives the preview pane
    public IRenderable StatusLine();            // bundled EditorStatusLine snapshot

    public Measurement Measure(RenderOptions options, int maxWidth);
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth);
}
```

`HandleKey` absorbs all editing/navigation/undo-redo keys internally. Host-level keys (Ctrl+S,
Ctrl+Q, Ctrl+N, Ctrl+O, Ctrl+P) return `false` so the host can handle them.

`Render` uses `ViewportHeight` to keep the cursor row visible (scroll clamp), then delegates
to `MarkdownEditorWidget`. The highlight pipeline runs only when `Buffer` changes by reference
(ReferenceEquals cache) via the `IEditorObserver.OnBufferChanged` callback.

---

## `EditCommand` Architecture (Imperative Shell)

```
┌──────────────────────────────────────────────────────────────────┐
│  EditCommand.ExecuteAsync  (imperative shell)                     │
│                                                                   │
│  1. Load file or new doc via use case  →  Result<> ROP           │
│  2. Create MarkdownEditor; wire BufferChanged → previewSource    │
│  3. Outer loop (runs after each host-action prompt):              │
│     a. AnsiConsole.Live(BuildLayout(...)).StartAsync(...)         │
│     b. Inside: Console.ReadKey  (impure — terminal I/O)          │
│        → editor.HandleKey(key)                                    │
│            true  → dirty = true                                   │
│            false → host switch (Ctrl+S/Q/N/O/P):                 │
│               S/Q/N/O → set pendingAction, cancel inner CTS      │
│               P       → toggle splitEnabled, dirty = true        │
│     c. On resize → editor.ViewportHeight = newH − 1; dirty       │
│     d. dirty → liveCtx.UpdateTarget(BuildLayout(...))  (impure)  │
│  4. After live loop: handle pendingAction (prompts + use cases)   │
│     SaveAsync / HandleQuitAsync / HandleNewAsync / HandleOpenAsync│
│  5. Repeat until session.Cancelled                                │
└──────────────────────────────────────────────────────────────────┘
```

### Key routing

The host dispatches each key to `editor.HandleKey(key)`. Unhandled keys are pattern-matched
by the host in a `switch (key)` block for file-operation bindings.

### Split-pane layout

`BuildLayout` returns a Spectre.Console `Layout` for both split and non-split modes:

```
// Split mode
Layout("Root") → SplitRows(
  Layout("Main") → SplitColumns(
    Layout("Editor")   ← editor (IRenderable)
    Layout("Preview")  ← MarkdownViewer(previewSource)
  ),
  Layout("Status").Size(1) ← editor.StatusLine()
)

// Non-split mode
Layout("Root") → SplitRows(
  Layout("Editor"),
  Layout("Status").Size(1)
)
```

`editor.ViewportHeight = AnsiConsole.Profile.Height - 1` is set before each outer loop
iteration; the preview's `ViewportHeight` is read from `editor.ViewportHeight`.

### Preview wiring

```csharp
string previewSource = editor.Buffer;
editor.BufferChanged += md => previewSource = md;
// BuildLayout creates new MarkdownViewer(previewSource) on each dirty frame.
```

### File operations (prompt-outside-live)

Host-level file operations run **after** `StartAsync` returns, with the live display stopped.
This ensures `AnsiConsole.Ask` / `SelectionPrompt` render correctly without conflicting with
the live display — resolving Open Question #4.

```csharp
// After StartAsync returns, pendingAction drives the operation:
case HostAction.Save:
    if (session.FilePath is null)
        session.FilePath = AnsiConsole.Ask<string>("Save as: ");
    var result = await new SaveFileUseCase(...).ExecuteAsync(session.FilePath, editor.Buffer);
    if (result is Result<Unit, MkedError>.Err(var err)) AnsiConsole.MarkupLine(...);
    else editor.MarkClean();
    break;
```

---

## Data Flow

### Startup

1. `EditSettings.Path` is supplied → `OpenFileUseCase.ExecuteAsync(path)`:
   - `Ok(file)` → `editor.LoadDocument(file.Source); session.FilePath = path;`
   - `Err(e)` → `AnsiConsole.MarkupLine("[red bold]Error:[/] …")`, return exit code 1.
2. `EditSettings.Path` is `null` → `new MarkdownEditor()` with empty buffer; `session.FilePath = null`.
3. `editor.BufferChanged += md => previewSource = md;` wired once.
4. `AnsiConsole.Live(BuildLayout(...)).StartAsync(...)` enters the poll loop.

### Per-keypress (inside `LiveDisplay`)

1. `ConsoleKeyInfo key = Console.ReadKey(intercept: true)` — impure.
2. `editor.HandleKey(key)` — pure editor dispatch (returns `false` for host keys).
3. If `HandleKey` returns `false`, host `switch (key)` sets `session.PendingAction` and cancels CTS.
4. `liveCtx.UpdateTarget(BuildLayout(editor, previewSource, session))` — impure; `MarkdownEditor.Render` runs the highlight pipeline internally and delegates to `MarkdownEditorWidget`.

### On terminal resize

`EditCommand` polls `AnsiConsole.Profile.Height` on each outer loop iteration.
When height changes, `editor.ViewportHeight` is updated before calling `StartAsync` again.
The `MarkdownEditor` derives scroll position from `ViewportHeight` inside `Render`; the
preview's `ViewportHeight` tracks `editor.ViewportHeight` automatically.

---

## Error Handling

| Source | Error | Handling in shell |
|--------|-------|------------------|
| `OpenFileUseCase` → `Err` | `MkedError.IoError` | Log via `AnsiConsole.MarkupLine`, exit code 1 (before editor loop) |
| `SaveFileUseCase` → `Err` | `MkedError.IoError` or `ValidationError` | Display message in status area `[red]…[/]`; do not exit |
| `Ctrl+Q` with unsaved buffer | (not a domain error) | Prompt: "Unsaved changes. Save? [y/n/cancel]"; `y` → `SaveAsync`, then exit; `n` → exit; `c` → continue |
| `Ctrl+N` / `Ctrl+O` with unsaved buffer | (not a domain error) | Same prompt as `Ctrl+Q`; proceed if confirmed |

No new `MkedError` variants are introduced.

---

## Testing

| Test project | What is covered |
|-------------|----------------|
| `Mked.Controls.Tests` | `CursorPosition` value semantics; `TextRange` boundary checks; `BufferOperations.Insert` / `Delete` (start, EOL, multi-line, across lines); `CursorNavigation` (all directions, word-boundary jumps, clamp-to-empty-buffer); each `IHighlightLayer` (known input → correct `HighlightSpan` set); `EditorState` construction / insert / delete / cursor movement / undo-redo / observer notifications; `MarkdownEditorWidget` renders visible lines, applies `StyledSpan` colour, cursor renders as invert block; `EditorStatusLine` shows dirty/clean indicator, word count, line/col; ArchUnitNet: `Mked.Controls` has no reference to `Mked.Domain` |
| `Mked.Domain.Tests` | Domain types only (no editor machinery has remained in Domain since the refactor) |
| `Mked.Application.Tests` | `OpenFileUseCase`, `SaveFileUseCase`, `StreamInputUseCase` — use cases that operate on strings / `OpenedFile` |

---

## Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | **Undo granularity** — current `EditorState.UpdateCursor` pushes cursor state to the undo stack. The new cursor-movement methods do **not**. Should `UpdateCursor` be deprecated for editor use, or left as-is for existing callers? | Resolved: leave `UpdateCursor` unchanged for backward compatibility; the new movement methods skip the undo stack. Existing tests remain green. |
| 2 | **Word count** — whitespace-split from raw buffer string (v1), or AST-derived (excludes frontmatter/code)? | Resolved: whitespace-split in v1 for simplicity; noted in `EditorStatusLine` docs as a known approximation. |
| 3 | **`LiveDisplay` cursor flicker** — `LiveDisplay` re-renders the full widget per frame. For fast typists this may flicker. Is a frame-rate cap (e.g., 60 Hz) needed in the poll loop? | Resolved: `await Task.Delay(16, ct)` is added after each `ReadKey` in the inner loop, capping redraws to ~60 fps. If synchronized-output artifacts appear on a specific terminal, wrap the `UpdateTarget` call in `\x1B[?2026h` / `\x1B[?2026l` markers at the host boundary. |
| 4 | **Path prompt for new files on save** — `AnsiConsole.Ask<string>` inside a `LiveDisplay` may not behave correctly. Use a full-screen prompt or pause the live display before prompting? | Resolved: host-level keys (Save/New/Open/Quit) cancel the inner `CancellationTokenSource`, which causes `StartAsync` to return. All `AnsiConsole.Ask` / `SelectionPrompt` calls run **after** `StartAsync` returns, with the live display fully stopped. The outer loop then re-enters `StartAsync` if no quit was requested. |
