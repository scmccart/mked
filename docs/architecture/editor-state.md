# Editor State & Highlight Pipeline

This document covers the domain types that back the `mked edit` command: the mutable `EditorState`
entity, the pure functional helpers (`BufferOperations`, `CursorNavigation`), and the composable
syntax-highlighting pipeline.

## EditorState

`EditorState` (in `Mked.Domain`) is the single mutable entity for an active editing session. It
owns:

- **Buffer** — the raw `string` content of the document.
- **Cursor** — a `CursorPosition` (1-based line and column) within that buffer.
- **Dirty flag** — whether the buffer differs from the last saved (clean) baseline.
- **Undo / redo stacks** — `Stack<IEditorCommand>` pairs that record every reversible change.
- **Observer list** — zero or more `IEditorObserver` subscribers notified on change.

### Public API summary

| Member | Description |
|---|---|
| `Buffer` | Current text content (read-only). |
| `Cursor` | Current cursor position (read-only). |
| `IsDirty` | `true` when buffer differs from the clean baseline. |
| `CanUndo` / `CanRedo` | Whether the respective stack is non-empty. |
| `Subscribe(observer)` | Register a change observer. |
| `UpdateBuffer(newBuffer)` | Replace the buffer; pushes a `BufferCommand` undo entry. |
| `UpdateCursor(position)` | Reposition cursor; pushes a `CursorCommand` undo entry. |
| `Insert(position, text)` | Insert `text` at `position`; pushes a `BufferCommand` undo entry. |
| `Delete(range)` | Delete `range`; moves cursor to `range.Start`; pushes a `BufferCommand` undo entry. |
| `MoveCursorLeft/Right/Up/Down()` | Navigate without touching the undo stack. |
| `MoveCursorWordLeft/Right()` | Word-boundary navigation (no undo entry). |
| `MoveCursorToLineStart/End()` | Line navigation (no undo entry). |
| `Undo()` | Pop undo stack; push inverse onto redo stack; apply; notify. |
| `Redo()` | Pop redo stack; push inverse onto undo stack; apply; notify. |
| `MarkClean()` | Reset the dirty baseline to the current buffer (call after save / open / new). |

### Dirty tracking

`MarkClean()` stores `Buffer` as the new `_cleanBuffer`. Subsequent calls to `SetBufferInternal`
compare by reference (`buffer != _cleanBuffer`) to avoid an O(n) string comparison on every
keystroke.

### Undo / redo mechanics

Every buffer mutation or explicit cursor update is recorded as a private `IEditorCommand`. There
are two concrete types:

- **`BufferCommand(string before)`** — captures the buffer snapshot before a change. On `Apply`,
  it calls `SetBufferInternal(before)`, restoring the buffer. On `Notify`, it fires
  `OnBufferChanged` on all observers.
- **`CursorCommand(CursorPosition before)`** — captures the cursor before a move. On `Apply`, it
  calls `SetCursorInternal(before)`. On `Notify`, it fires `OnCursorMoved`.

`CaptureInverse()` creates the mirror command *from the current state*, ready to push onto the
opposite stack:

```
Undo:
  cmd = _undoStack.Pop()
  _redoStack.Push(cmd.CaptureInverse(this))   // snapshot of state AFTER applying
  cmd.Apply(this)                              // restore to BEFORE state
  cmd.Notify(this)                             // fire selective callbacks
```

This round-trip symmetry keeps `BufferCommand↔BufferCommand` and `CursorCommand↔CursorCommand`,
preventing corrupted redo when cursor-only operations are interleaved with buffer mutations.

Cursor-navigation methods (`MoveCursorLeft`, `MoveCursorUp`, etc.) do **not** push to the undo
stack. Only `UpdateCursor` (explicit repositioning, e.g. after opening a file) is undoable.

### Cursor clamping

`SetBufferInternal` always calls `CursorNavigation.Clamp(buffer, Cursor)` after updating the
buffer. This guarantees that Undo, Redo, and `UpdateBuffer` can never leave the cursor outside
the valid range for the restored buffer.

### Observer pattern

```csharp
public interface IEditorObserver
{
    void OnBufferChanged(string newBuffer);
    void OnCursorMoved(CursorPosition position);
}
```

`EditorState.Subscribe(observer)` appends to `_observers`. Each mutation method fires only the
appropriate callback — `Insert`/`Delete`/`UpdateBuffer` fire `OnBufferChanged`; `MoveCursor*`
methods fire `OnCursorMoved` — preventing unnecessary Markdig re-parses on cursor-only navigation.

---

## BufferOperations

`BufferOperations` (in `Mked.Domain`) is a static class of pure functions. It treats the buffer
as a newline-delimited string and never modifies state.

| Method | Signature | Description |
|---|---|---|
| `Insert` | `(string buffer, CursorPosition pos, string text) → string` | Inserts `text` at `pos`. Returns the new buffer. |
| `Delete` | `(string buffer, TextRange range) → string` | Removes the characters in `range`. Returns the new buffer. |
| `ToOffset` | `(string buffer, CursorPosition pos) → int` | Converts a 1-based `(line, column)` position to a 0-based character offset. |
| `FromOffset` | `(string buffer, int offset) → CursorPosition` | Converts a 0-based offset back to a 1-based `CursorPosition`. |

`ToOffset` and `FromOffset` are the bridge between `CursorPosition` (line/column) and the raw
string index required by `string.Substring` / slice operations.

---

## CursorNavigation

`CursorNavigation` (in `Mked.Domain`) is a static class of pure cursor-movement functions. Each
function takes `(string buffer, CursorPosition current)` and returns the new `CursorPosition`
without mutating anything.

| Method | Behaviour |
|---|---|
| `MoveLeft` | One character left; wraps to end of previous line at column 1; no-op at buffer start. |
| `MoveRight` | One character right; wraps to start of next line at line end; no-op at buffer end. |
| `MoveUp` | One line up; clamps column to new line length; no-op on first line. |
| `MoveDown` | One line down; clamps column to new line length; no-op on last line. |
| `MoveWordLeft` | Skips whitespace then a word boundary to the left. |
| `MoveWordRight` | Skips a word then whitespace to the right. |
| `MoveToLineStart` | Returns `(line, 1)`. |
| `MoveToLineEnd` | Returns `(line, lineLength + 1)` (one past last char). |
| `Clamp` | Clamps `(line, column)` to the valid range for the buffer; returns `(1, 1)` for an empty buffer. |

Column values are 1-based throughout. The "past end of line" sentinel is `lineLength + 1`, which
is the position used for `End` key and for inserting at the end of a line.

---

## Highlight pipeline

Syntax highlighting runs as a composable pipeline of stateless layers, each implementing
`IHighlightLayer`:

```csharp
public interface IHighlightLayer
{
    IEnumerable<HighlightSpan> Annotate(string source, MarkdownDocument document);
}
```

Each layer receives the raw source string and the pre-parsed Markdig `MarkdownDocument` AST, and
returns zero or more `HighlightSpan` values.

### HighlightSpan and HighlightKind

```csharp
public readonly record struct HighlightSpan(TextRange Range, HighlightKind Kind);
```

`TextRange` is a `(CursorPosition Start, CursorPosition End)` pair in buffer coordinates.
`HighlightKind` is an enum:

| Value | What it marks |
|---|---|
| `Heading` | ATX heading marker (`#`) and heading text |
| `Bold` | Bold emphasis (`**...**`) |
| `Italic` | Italic emphasis (`*...*`) |
| `InlineCode` | Inline code span (`` `...` ``) |
| `LinkText` | Link display text (`[text]`) |
| `LinkUrl` | Link URL (`(url)`) |
| `FrontmatterBlock` | YAML front-matter block |
| `CodeFence` | Fenced code block content |

### Built-in layers

| Class | Highlights |
|---|---|
| `HeadingHighlightLayer` | ATX headings via Markdig's `HeadingBlock` AST nodes. |
| `EmphasisHighlightLayer` | `EmphasisInline` nodes; distinguishes bold, italic, and inline code spans. |
| `LinkHighlightLayer` | `LinkInline` nodes; emits separate spans for `[text]` and `(url)`. |
| `FrontMatterDimLayer` | `YamlFrontMatterBlock`; emits a single `FrontmatterBlock` span over the whole block. |
| `CodeFenceLayer` | `FencedCodeBlock`; emits a `CodeFence` span so the code content is rendered verbatim. |

Layers are stateless and may be called concurrently. The highlight pipeline runs only when the
buffer changes (cursor-only redraws reuse the cached spans).

### Translating to Spectre.Console

`IHighlightLayer` operates in Domain-layer types (`TextRange`, `HighlightKind`). Before passing
spans to `MarkdownEditorWidget` in `Mked.Controls`, the `HighlightMapper` class (in `Mked.Console`)
converts them to `StyledSpan` values (character offsets + Spectre.Console `Style`):

```
IHighlightLayer.Annotate()
    → IEnumerable<HighlightSpan>          (Domain types, TextRange coordinates)
    → HighlightMapper.Map(spans, buffer)
    → IReadOnlyList<StyledSpan>           (Mked.Controls types, character offsets)
    → MarkdownEditorWidget(highlights)
```

This translation layer keeps `Mked.Controls` free of any `Mked.Domain` dependency, allowing it
to be published as a standalone NuGet package.
