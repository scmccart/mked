# Epic 05 — Markdown Editor: Implementation Plan

> **Epic**: [`docs/epics/05-markdown-editor.md`](../../docs/epics/05-markdown-editor.md)
> **Design**: [`docs/designs/05-markdown-editor-design.md`](../../docs/designs/05-markdown-editor-design.md)
> **Status**: Approved
> **Date**: 2026-06-03

---

## Task 1 — `BufferOperations` and `CursorNavigation` pure functions

Add `BufferOperations` and `CursorNavigation` as static classes in `Mked.Domain`. `BufferOperations`
exposes `Insert(string, CursorPosition, string)`, `Delete(string, TextRange)`, `ToOffset(string,
CursorPosition)`, and `FromOffset(string, int)` — each returns a new value and mutates nothing.
`CursorNavigation` exposes `MoveLeft`, `MoveRight`, `MoveUp`, `MoveDown`, `MoveWordLeft`,
`MoveWordRight`, `MoveToLineStart`, `MoveToLineEnd`, and `Clamp` — each takes `(string buffer,
CursorPosition current)` and returns a clamped `CursorPosition`. Add tests in `Mked.Domain.Tests`
covering edge cases: insert at start, mid-line, end-of-line, and across lines; delete spanning
line boundaries; all navigation directions including word-jump boundaries; clamp on empty buffer.
Done when all tests pass and both classes build with zero warnings.

---

## Task 2 — Extend `EditorState` with `Insert`, `Delete`, and cursor-movement methods

Add `void Insert(CursorPosition, string)` and `void Delete(TextRange)` to `EditorState`. Each
delegates to `BufferOperations`, pushes the prior buffer string onto `_undoStack`, clears
`_redoStack`, calls `SetBufferInternal`, and fires `OnBufferChanged` on all observers — mirroring
the existing `UpdateBuffer` pattern. Add eight cursor-movement methods (`MoveCursorLeft`,
`MoveCursorRight`, `MoveCursorUp`, `MoveCursorDown`, `MoveCursorWordLeft`, `MoveCursorWordRight`,
`MoveCursorToLineStart`, `MoveCursorToLineEnd`) that delegate to `CursorNavigation`, call
`SetCursorInternal`, and fire `OnCursorMoved` — without touching `_undoStack` or `_redoStack`.
Add tests asserting: `Insert` and `Delete` fire `OnBufferChanged` and push to the undo stack;
cursor-movement methods fire `OnCursorMoved` and leave `_undoStack.Count` unchanged. All existing
`EditorState` tests must continue to pass.

Depends on: Task 1

---

## Task 3 — Highlight pipeline: `IHighlightLayer`, value types, and five layer implementations

Define `HighlightKind` (enum), `HighlightSpan` (readonly record struct — `TextRange Range,
HighlightKind Kind`), and `IHighlightLayer` (interface — `IEnumerable<HighlightSpan>
Annotate(string source, MarkdownDocument document)`) in `Mked.Domain`. Implement five sealed,
stateless layer classes: `HeadingHighlightLayer` (marks `#` prefixes and heading text),
`EmphasisHighlightLayer` (marks `*`/`**`/`_`/`__` delimiters), `LinkHighlightLayer` (marks
`[text]` as `LinkText` and `(url)` as `LinkUrl`), `FrontMatterDimLayer` (marks the entire YAML
frontmatter block as `FrontmatterBlock`), and `CodeFenceLayer` (marks fenced code block bodies as
`CodeFence`). Add tests in `Mked.Domain.Tests` for each layer: feed a known Markdown string,
assert the returned `TextRange` set matches the expected source spans. Done when all five layers
handle their token types correctly with no `NotImplementedException` paths.

---

## Task 4 — `MarkdownEditorWidget` and `EditorStatusLine` in `Mked.Controls`

Add `StyledSpan` (readonly record struct — `int StartOffset, int Length, Style SpectreStyle`) to
`Mked.Controls`. Implement `MarkdownEditorWidget(string buffer, (int Line, int Column) cursor,
IReadOnlyList<StyledSpan> highlights, int topLineIndex, int? viewportHeight) : IRenderable` — splits
the buffer into lines, clips to the viewport window, applies `StyledSpan` colouring per run, and
renders the cursor character with `Decoration.Invert` (a space with invert at end-of-line).
Implement `EditorStatusLine((int Line, int Column) cursor, bool isDirty, int wordCount) :
IRenderable` — renders `Ln {line}, Col {col}  ● {n} words` where `●` is yellow when dirty, dim
when clean. Add tests in `Mked.Controls.Tests` covering: editor widget renders visible lines only,
applies highlight colours, cursor block appears at correct position, viewport clips top and bottom;
status line shows dirty/clean indicator, correct word count, and line/col. Add an ArchUnitNet rule
asserting `Mked.Controls` holds no reference to `Mked.Domain`. All tests must pass.

---

## Task 5 — `EditCommand`: core editing loop, file operations, and keyboard shortcuts

Add `EditSettings` (`CommandSettings` with optional `[path]` argument and `--split` flag) and
`EditCommand` (`AsyncCommand<EditSettings>`) to `Mked.Console`. On startup, call
`OpenFileUseCase.ExecuteAsync(path)` when a path is given (ROP: `Err` → log and exit 1) or
`NewDocumentUseCase.Execute()` when not; create `EditorState`. Implement the `LiveDisplay` poll
loop using a pure `MapKey(ConsoleKeyInfo) → EditorAction` switch expression; per tick: read a
key, map to `EditorAction`, apply to `EditorState` (Insert, Delete, cursor movement, Undo/Redo),
re-parse the buffer via `MarkdownDocument.Parse`, run all five highlight layers, call
`HighlightMapper.Map(spans, buffer)` to produce `StyledSpan[]`, and update the live target with a
new `MarkdownEditorWidget` and `EditorStatusLine`. Implement file operations as ROP pipelines:
`Ctrl+S` → `SaveFileUseCase.ExecuteAsync(...)` (prompt for path if none; on `Err` display in
status area); `Ctrl+Q` → dirty-check prompt then exit; `Ctrl+N` / `Ctrl+O` → dirty-check, then
new document or `OpenFileUseCase`. Register `EditCommand` in `Program.cs`. Done when
`mked edit file.md` opens a file, accepts typed characters, moves the cursor with arrow keys and
word-jump shortcuts, applies visible syntax highlighting, undoes and redoes, saves and exits
cleanly.

Depends on: Task 2, Task 3, Task 4

---

## Task 6 — Split-pane layout and status line wiring

Extend `EditCommand` with the split-pane layout. When `settings.Split` is `true` on startup, or
when the user toggles `Ctrl+P`, build a Spectre.Console `Layout` with a left pane
(`MarkdownEditorWidget`) and a right pane (`MarkdownViewer` constructed from `state.Buffer`);
`EditorStatusLine` spans the full terminal width beneath both panes. On each `OnBufferChanged`
notification, reconstruct the `MarkdownViewer` from the updated buffer and mark the layout dirty.
On terminal resize, rebuild both widgets with new dimensions. When split is off, the layout is
a single-pane `MarkdownEditorWidget` above `EditorStatusLine`. Done when `mked edit --split
file.md` shows both panes side-by-side, the preview updates in real time as the user types, and
`Ctrl+P` toggles the preview pane without restarting the editor or losing buffer state.

Depends on: Task 5
