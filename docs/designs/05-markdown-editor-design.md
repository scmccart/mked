# Epic 05 — Markdown Editor: Technical Design

> **Epic**: [`docs/epics/05-markdown-editor.md`](../../docs/epics/05-markdown-editor.md)
> **Status**: Draft
> **Date**: 2026-06-03

---

## Goals

1. Extend `EditorState` in `Mked.Domain` with character-level `Insert` and `Delete` operations
   backed by pure, stateless `BufferOperations` helper functions, so callers no longer
   reconstruct the full buffer string externally.
2. Add clamped cursor-movement as pure functions in `CursorNavigation` (Domain), used by
   `EditorState` wrapper methods that push to the undo stack only for buffer mutations — not
   cursor moves.
3. Define `IHighlightLayer`, `HighlightSpan`, and `HighlightKind` in `Mked.Domain`; implement
   five stateless highlight layer classes (heading, emphasis, link, frontmatter, code fence) as
   pure annotators.
4. Build `MarkdownEditorWidget` and `EditorStatusLine` in `Mked.Controls` — both are
   `IRenderable` types that accept pre-computed data and carry no Domain references.
5. Implement `EditCommand` and `EditSettings` in `Mked.Console` — the `mked edit` entry point.
   Separates key-to-action mapping (pure) from side-effecting application (imperative shell);
   uses `Result<T, MkedError>` (ROP) for all file I/O.
6. Add test coverage: `Mked.Domain.Tests` for new buffer and navigation functions; `Mked.Controls.Tests`
   for editor widget and status line rendering.

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
| Domain | `Mked.Domain` | New `BufferOperations`, `CursorNavigation`, `IHighlightLayer`, `HighlightSpan`, `HighlightKind`, five layer implementations; `EditorState` gains typed `Insert`/`Delete` and cursor-movement methods |
| Application | `Mked.Application` | Not touched — `OpenFileUseCase`, `SaveFileUseCase`, `NewDocumentUseCase` already cover file operations |
| Controls | `Mked.Controls` | New `StyledSpan`, `MarkdownEditorWidget`, `EditorStatusLine` |
| Presentation | `Mked.Console` | New `EditSettings`, `EditCommand`, `EditorAction`, `HighlightMapper`; `Program.cs` wired |

**Controls independence constraint** — `Mked.Controls` must not reference `Mked.Domain`.
`StyledSpan` is a Spectre.Console-native value type (int offsets + `Style`). `EditCommand` in
`Mked.Console` translates Domain's `HighlightSpan` (with `TextRange` + `HighlightKind`) to
`StyledSpan` via `HighlightMapper`. The widget's cursor position is expressed as `(int Line, int
Column)` — a plain value tuple — rather than `CursorPosition` from Domain.

**AOT/Trim** — All new types use BCL and Spectre.Console APIs. Markdig AST traversal in the
highlight layers is already trim-safe. No reflection, no `dynamic`, no `Activator`.

---

## Functional Core / Imperative Shell

This epic applies the functional core / imperative shell principle explicitly:

| Layer | Classification | Reason |
|-------|---------------|--------|
| `BufferOperations` | **Functional core** | Pure functions: `(string, CursorPosition, string) → string` — no state, fully testable |
| `CursorNavigation` | **Functional core** | Pure functions: `(string, CursorPosition) → CursorPosition` |
| `IHighlightLayer` implementations | **Functional core** | `(string, MarkdownDocument) → IEnumerable<HighlightSpan>` — stateless |
| `HighlightMapper` | **Functional core** | Pure span conversion |
| `MapKey` in `EditCommand` | **Functional core** | `ConsoleKeyInfo → EditorAction` — pure, switch-expression |
| `EditorState` | Mutable coordinator | Calls functional core, maintains undo stacks, fires observer events |
| `EditCommand` poll loop | **Imperative shell** | Reads keyboard, writes terminal, calls use cases |
| `SaveFileUseCase` / `OpenFileUseCase` | Railway (ROP) | Return `Result<T, MkedError>`; shell pattern-matches the result |

---

## Key Types and Interfaces

### New — `Mked.Domain`

| Type | Kind | Purpose |
|------|------|---------|
| `HighlightKind` | enum | `Heading`, `Bold`, `Italic`, `InlineCode`, `LinkText`, `LinkUrl`, `FrontmatterBlock`, `CodeFence` |
| `HighlightSpan` | readonly record struct | `(TextRange Range, HighlightKind Kind)` — source span + category |
| `IHighlightLayer` | interface | `IEnumerable<HighlightSpan> Annotate(string source, MarkdownDocument document)` |
| `HeadingHighlightLayer` | sealed class | Annotates `#` markers and heading text |
| `EmphasisHighlightLayer` | sealed class | Annotates `*`/`**`/`_`/`__` delimiters |
| `LinkHighlightLayer` | sealed class | Annotates `[text]` as `LinkText` and `(url)` as `LinkUrl` |
| `FrontMatterDimLayer` | sealed class | Annotates entire YAML frontmatter block as `FrontmatterBlock` |
| `CodeFenceLayer` | sealed class | Annotates fenced code block bodies as `CodeFence` (no inner highlighting) |
| `BufferOperations` | static class | Pure: `Insert`, `Delete`, `ToOffset`, `FromOffset` |
| `CursorNavigation` | static class | Pure: `MoveLeft`, `MoveRight`, `MoveUp`, `MoveDown`, `MoveWordLeft`, `MoveWordRight`, `MoveToLineStart`, `MoveToLineEnd`, `Clamp` |

### Modified — `Mked.Domain`

| Type | Change | Reason |
|------|--------|--------|
| `EditorState` | New `void Insert(CursorPosition, string)` and `void Delete(TextRange)` methods that delegate to `BufferOperations` and push to the undo stack | Atomic buffer mutations without external string reconstruction |
| `EditorState` | New `void MoveCursorLeft/Right/Up/Down/WordLeft/WordRight/ToLineStart/ToLineEnd()` methods that delegate to `CursorNavigation` and call `SetCursorInternal` — these do **not** push to the undo stack (cursor moves are not undoable) | Clamped cursor movement |
| `EditorState` | `UpdateBuffer(string)` is retained for compatibility but callers should prefer `Insert`/`Delete` | Non-breaking |

### New — `Mked.Controls`

| Type | Kind | Purpose |
|------|------|---------|
| `StyledSpan` | readonly record struct | `(int StartOffset, int Length, Style SpectreStyle)` — Spectre-native highlight span |
| `MarkdownEditorWidget` | sealed class, `IRenderable` | Raw text buffer with visible block cursor and `StyledSpan` overlays, clipped to viewport |
| `EditorStatusLine` | sealed class, `IRenderable` | One-line bar: `Ln {line}, Col {col}   ● (dirty)   {n} words` |

### New — `Mked.Console`

| Type | Kind | Purpose |
|------|------|---------|
| `EditSettings` | sealed class, `CommandSettings` | Optional `[path]` argument; `--split` flag |
| `EditCommand` | sealed class, `AsyncCommand<EditSettings>` | `mked edit` entry point; drives editor loop |
| `EditorAction` | abstract record | Discriminated union: `InsertChar`, `DeleteBackward`, `DeleteForward`, `MoveCursor`, `MoveWordCursor`, `MoveToLineStart`, `MoveToLineEnd`, `UndoAction`, `RedoAction`, `SaveFile`, `NewFile`, `OpenFile`, `TogglePreview`, `Quit`, `None` |
| `HighlightMapper` | static class | `IReadOnlyList<StyledSpan> Map(IEnumerable<HighlightSpan>, string buffer)` |

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

## `EditCommand` Architecture (Imperative Shell)

```
┌──────────────────────────────────────────────────────────────────┐
│  EditCommand.ExecuteAsync  (imperative shell)                     │
│                                                                   │
│  1. Load file or new doc via use case  →  Result<> ROP           │
│  2. Create EditorState, subscribe preview observer               │
│  3. Poll loop (LiveDisplay):                                      │
│     a. Console.ReadKey          (impure — terminal I/O)          │
│     b. MapKey(key)              (PURE  — returns EditorAction)   │
│     c. ApplyAction:                                               │
│        Insert/Delete/Move  →  EditorState methods                 │
│        Undo / Redo         →  EditorState.Undo / Redo            │
│        Save                →  SaveFileUseCase  →  Result<> ROP   │
│        Quit                →  dirty-check prompt, then exit      │
│     d. MarkdownDocument.Parse(state.Buffer)   (pure)             │
│     e. layers.SelectMany(l => l.Annotate(...)) (pure per layer)  │
│     f. HighlightMapper.Map(spans, buffer)      (pure)            │
│     g. liveCtx.UpdateTarget(BuildLayout(...))  (impure)          │
└──────────────────────────────────────────────────────────────────┘
```

### Key mapping (pure)

```csharp
private static EditorAction MapKey(ConsoleKeyInfo key) => key switch
{
    { Key: ConsoleKey.Z,         Modifiers: ConsoleModifiers.Control }  => new EditorAction.UndoAction(),
    { Key: ConsoleKey.Y,         Modifiers: ConsoleModifiers.Control }  => new EditorAction.RedoAction(),
    { Key: ConsoleKey.S,         Modifiers: ConsoleModifiers.Control }  => new EditorAction.SaveFile(),
    { Key: ConsoleKey.N,         Modifiers: ConsoleModifiers.Control }  => new EditorAction.NewFile(),
    { Key: ConsoleKey.O,         Modifiers: ConsoleModifiers.Control }  => new EditorAction.OpenFile(),
    { Key: ConsoleKey.P,         Modifiers: ConsoleModifiers.Control }  => new EditorAction.TogglePreview(),
    { Key: ConsoleKey.Q,         Modifiers: ConsoleModifiers.Control }  => new EditorAction.Quit(),
    { Key: ConsoleKey.LeftArrow, Modifiers: ConsoleModifiers.Control }  => new EditorAction.MoveWordCursor(Direction.Left),
    { Key: ConsoleKey.RightArrow,Modifiers: ConsoleModifiers.Control }  => new EditorAction.MoveWordCursor(Direction.Right),
    { Key: ConsoleKey.LeftArrow }                                        => new EditorAction.MoveCursor(Direction.Left),
    { Key: ConsoleKey.RightArrow }                                       => new EditorAction.MoveCursor(Direction.Right),
    { Key: ConsoleKey.UpArrow }                                          => new EditorAction.MoveCursor(Direction.Up),
    { Key: ConsoleKey.DownArrow }                                        => new EditorAction.MoveCursor(Direction.Down),
    { Key: ConsoleKey.Home }                                             => new EditorAction.MoveToLineStart(),
    { Key: ConsoleKey.End }                                              => new EditorAction.MoveToLineEnd(),
    { Key: ConsoleKey.Backspace }                                        => new EditorAction.DeleteBackward(),
    { Key: ConsoleKey.Delete }                                           => new EditorAction.DeleteForward(),
    { KeyChar: char c } when !char.IsControl(c)                          => new EditorAction.InsertChar(c),
    _                                                                    => new EditorAction.None(),
};
```

### File operations (ROP)

```csharp
// Save: fallible path → ROP
private async Task<Result<Unit, MkedError>> SaveAsync(
    ref string? filePath, string content)
{
    if (filePath is null)
        filePath = AnsiConsole.Ask<string>("Save as: ");  // impure — only at the boundary

    return await _saveFile.ExecuteAsync(filePath, content);
}

// Callers pattern-match the result; errors surface as a status-line message:
var saveResult = await SaveAsync(ref _filePath, state.Buffer);
if (saveResult is Result<Unit, MkedError>.Err(var err))
    _statusMessage = FormatError(err);  // displayed on next render frame
```

### Split-pane layout

When `settings.Split` is `true` (or toggled on via `Ctrl+P`), `EditCommand` builds a
Spectre.Console `Layout` with two panes:

```
┌──────────────────────┬──────────────────────┐
│  MarkdownEditorWidget│  MarkdownViewer       │
│  (editor pane)       │  (preview pane)       │
└──────────────────────┴──────────────────────┘
│  EditorStatusLine (full width)               │
└──────────────────────────────────────────────┘
```

The preview pane is a `MarkdownViewer` reconstructed from `state.Buffer` each time
`OnBufferChanged` fires. `MarkdownViewer` is an existing Epic 04 type — no new Controls
types are needed for the preview. The status line spans the full terminal width beneath both
panes.

### Observer wiring

`EditCommand` subscribes two observers to `EditorState`:

1. **`ReparseObserver`** (inline closure) — calls `MarkdownDocument.Parse(newBuffer)` on each
   `OnBufferChanged`; stores the result for the next highlight pass.
2. **`WordCountObserver`** (inline closure) — recomputes word count from the new buffer on
   `OnBufferChanged`; stores for status line.

Cursor changes (`OnCursorMoved`) are consumed directly in the render step from `state.Cursor`.

---

## Data Flow

### Startup

1. `EditSettings.Path` is supplied → `OpenFileUseCase.ExecuteAsync(path)`:
   - `Ok(file)` → `EditorState state = new(file.Source); _filePath = path;`
   - `Err(e)` → `AnsiConsole.MarkupLine("[red bold]Error:[/] …")`, return exit code 1.
2. `EditSettings.Path` is `null` → `EditorState state = NewDocumentUseCase.Execute(); _filePath = null;`
3. Highlight pipeline instantiated once: `IHighlightLayer[] layers = [new HeadingHighlightLayer(), ...]`.
4. `AnsiConsole.Live(initialLayout).StartAsync(...)` enters the poll loop.

### Per-keypress (inside `LiveDisplay`)

1. `ConsoleKeyInfo key = Console.ReadKey(intercept: true)` — impure.
2. `EditorAction action = MapKey(key)` — pure.
3. `ApplyAction(action, state)` — imperative, may call use cases with ROP.
4. `var doc = MarkdownDocument.Parse(state.Buffer)` — pure, incremental re-parse.
5. `var spans = layers.SelectMany(l => l.Annotate(state.Buffer, doc)).ToList()` — pure.
6. `var styled = HighlightMapper.Map(spans, state.Buffer)` — pure.
7. `liveCtx.UpdateTarget(BuildLayout(state, styled, settings))` — impure.

### On terminal resize

`EditCommand` polls terminal dimensions on each loop tick. When width or height changes:
- Rebuild `MarkdownEditorWidget` with new `ViewportHeight`; `topLineIndex` is preserved.
- Rebuild `MarkdownViewer` (in split mode) — clears its width-dependent render cache.

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
| `Mked.Domain.Tests` | `BufferOperations.Insert` / `Delete` (insert at start, EOL, multi-line; delete across lines); `CursorNavigation` (all directions, word-boundary jumps, clamp-to-empty-buffer); each `IHighlightLayer` implementation (known input → correct `TextRange` set); `EditorState.Insert` and `Delete` fire `OnBufferChanged`; cursor-movement methods do **not** push to undo stack |
| `Mked.Controls.Tests` | `MarkdownEditorWidget` renders visible lines, applies `StyledSpan` colour, cursor renders as invert block, viewport clips correctly; `EditorStatusLine` shows correct dirty/clean indicator, word count, and line/col; ArchUnitNet: `Mked.Controls` has no reference to `Mked.Domain` |

---

## Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | **Undo granularity** — current `EditorState.UpdateCursor` pushes cursor state to the undo stack. The new cursor-movement methods do **not**. Should `UpdateCursor` be deprecated for editor use, or left as-is for existing callers? | Resolved: leave `UpdateCursor` unchanged for backward compatibility; the new movement methods skip the undo stack. Existing tests remain green. |
| 2 | **Word count** — whitespace-split from raw buffer string (v1), or AST-derived (excludes frontmatter/code)? | Resolved: whitespace-split in v1 for simplicity; noted in `EditorStatusLine` docs as a known approximation. |
| 3 | **`LiveDisplay` cursor flicker** — `LiveDisplay` re-renders the full widget per frame. For fast typists this may flicker. Is a frame-rate cap (e.g., 60 Hz) needed in the poll loop? | TBD — evaluate during Task 6 implementation; add `await Task.Delay(16, ct)` cap if flicker is observed. |
| 4 | **Path prompt for new files on save** — `AnsiConsole.Ask<string>` inside a `LiveDisplay` may not behave correctly. Use a full-screen prompt or pause the live display before prompting? | TBD — resolve in Task 6; if necessary, cancel `LiveDisplay`, prompt, then restart. |
