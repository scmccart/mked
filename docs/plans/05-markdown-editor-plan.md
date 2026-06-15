# Epic 05 — Markdown Editor: Implementation Plan

> **Epic**: [`docs/epics/05-markdown-editor.md`](../../docs/epics/05-markdown-editor.md)
> **Design**: [`docs/designs/05-markdown-editor-design.md`](../../docs/designs/05-markdown-editor-design.md)
> **Status**: Completed
> **Date**: 2026-06-03
> **Completed**: 2026-06-10

> **Note (feat/epic-5-refactor):** After the initial implementation landed, all editor-machinery
> types (`CursorPosition`, `TextRange`, `BufferOperations`, `CursorNavigation`, `EditorState`,
> `IEditorObserver`, the five highlight layers, and `HighlightMapper`) were relocated from
> `Mked.Domain` / `Mked.Console` into `Mked.Controls`, preserving the "Controls does not reference
> Domain" ArchUnit rule. A new `MarkdownEditor : IRenderable` control wraps the editor state and
> highlight pipeline; `EditCommand` was rewritten to a thin host loop following the `ViewCommand`
> `AnsiConsole.Live + Layout` idiom. The task descriptions below reflect the original plan; for
> the as-built architecture see `docs/designs/05-markdown-editor-design.md`.

---

## Task 1 — `BufferOperations` and `CursorNavigation` pure functions ✓

`BufferOperations` and `CursorNavigation` are static classes in `Mked.Controls` (moved from the
initial `Mked.Domain` landing). `BufferOperations` exposes `Insert`, `Delete`, `ToOffset`, and
`FromOffset`. `CursorNavigation` exposes `MoveLeft`, `MoveRight`, `MoveUp`, `MoveDown`,
`MoveWordLeft`, `MoveWordRight`, `MoveToLineStart`, `MoveToLineEnd`, and `Clamp`. Tests live in
`Mked.Controls.Tests` (`BufferOperations_Tests.cs`, `CursorNavigation_Tests.cs`).

---

## Task 2 — Extend `EditorState` with `Insert`, `Delete`, and cursor-movement methods ✓

`EditorState` lives in `Mked.Controls`. `Insert` and `Delete` delegate to `BufferOperations`,
push to the undo stack, and fire `OnBufferChanged`. Eight cursor-movement methods delegate to
`CursorNavigation`, fire `OnCursorMoved`, and do not touch the undo stack. Tests cover observer
notifications and undo-stack invariants (`EditorState_Insert_Tests.cs`,
`EditorState_Delete_Tests.cs`, `EditorState_CursorMovement_Tests.cs`,
`EditorState_Observer_Tests.cs` — all in `Mked.Controls.Tests`).

Depends on: Task 1

---

## Task 3 — Highlight pipeline: `IHighlightLayer`, value types, and five layer implementations ✓

`HighlightKind`, `HighlightSpan`, `IHighlightLayer`, and the five layer classes live in
`Mked.Controls`. `IHighlightLayer.Annotate` accepts the raw `Markdig.Syntax.MarkdownDocument`
(not the Domain wrapper) — `MarkdownEditor` owns an internal parse helper that mirrors the
pipeline config (`UseAdvancedExtensions().UseYamlFrontMatter()`). `HighlightMapper` (moved from
`Mked.Console`) also lives in `Mked.Controls`. Tests are in `Mked.Controls.Tests`
(`HighlightLayer_Tests.cs`).

---

## Task 4 — `MarkdownEditorWidget`, `EditorStatusLine`, and `MarkdownEditor` in `Mked.Controls` ✓

`StyledSpan`, `MarkdownEditorWidget`, and `EditorStatusLine` are implemented as described.
`MarkdownEditorWidget` gains an additive `bool showCursor = true` parameter so the split-view
preview pane can share the widget without displaying a cursor. A new `MarkdownEditor : IRenderable,
IEditorObserver` control wraps all editor state, the highlight pipeline (with ReferenceEquals
cache), scroll math, and key dispatch. `MarkdownEditor` exposes `HandleKey`, `Buffer`, `Cursor`,
`IsDirty`, `WordCount`, `CanUndo`, `CanRedo`, `HasFocus`, `LoadDocument`, `MarkClean`,
`BufferChanged`, `ViewportHeight`, and `StatusLine()`. The ArchUnitNet "Controls does not
reference Domain" rule passes.

---

## Task 5 — `EditCommand`: core editing loop, file operations, and keyboard shortcuts ✓

`EditSettings` has `[path]` and `--split`. `EditCommand` follows the `AnsiConsole.Live + Layout`
idiom (matching `ViewCommand`). `editor.HandleKey(key)` dispatches all editing keys; host keys
(Save/Quit/New/Open/TogglePreview) cancel an inner `CancellationTokenSource` and set a
`HostAction` enum value. File operations (`SaveAsync`, `HandleQuitAsync`, `HandleNewAsync`,
`HandleOpenAsync`) run **after** `StartAsync` returns, outside the live display — resolving the
prompt-inside-live issue. `EditAction.cs`, `Direction.cs`, and `HighlightMapper.cs` have been
deleted from `Mked.Console`; `NewDocumentUseCase` was deleted from `Mked.Application`.

Depends on: Task 2, Task 3, Task 4

---

## Task 6 — Split-pane layout and status line wiring ✓

`BuildLayout` returns a Spectre `Layout`. In split mode it nests `Editor` and `Preview` columns
inside a `Main` row, with a 1-row `Status` beneath. `editor.BufferChanged += md => previewSource
= md` wires the preview. `editor.HasFocus` gates the block cursor in the focused pane;
`Ctrl+Tab` flips focus between editor and preview (bare `Tab` inserts a two-space indent). When
the preview is focused, `↑/↓/PageUp/PageDown/Home/End` scroll it and other edit keys are not
forwarded. `Ctrl+P` toggles `session.SplitEnabled` without restarting the outer loop; toggling
the split off always restores `editor.HasFocus = true`. Resize is handled by updating
`editor.ViewportHeight = AnsiConsole.Profile.Height - 1` each outer iteration.
`editor.StatusLine()` is inserted into the Status layout cell.

Depends on: Task 5
