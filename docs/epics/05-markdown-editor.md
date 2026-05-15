# Epic 05 — Markdown Editor

Deliver the `mked edit` experience: a keyboard-driven, syntax-highlighted in-terminal Markdown
editor. Live syntax highlighting updates as the user types; undo/redo provides a full editing
history; the split-pane layout optionally shows a rendered preview alongside the raw source.

## Features

- `EditorBuffer` — holds the mutable text buffer; tracks cursor position and selection range
- `MarkdownEditorWidget` — full-screen editor pane built on Spectre.Console raw key input
- Live syntax highlighting via `IHighlightLayer` decorator pipeline:
  - `HeadingHighlightLayer` — colours `#` markers and heading text
  - `EmphasisHighlightLayer` — bold and italic markers
  - `LinkHighlightLayer` — `[text]` and `(url)` components
  - `FrontMatterDimLayer` — dims YAML frontmatter block
  - Code fences excluded from all layers (rendered verbatim)
- `IEditorCommand` / `CommandHistory` for undo (`Ctrl+Z`) and redo (`Ctrl+Y` / `Ctrl+Shift+Z`)
- Built-in commands: `InsertTextCommand`, `DeleteBackwardCommand`, `DeleteForwardCommand`, `PasteCommand`, `CutCommand`
- `--split` layout: side-by-side editor pane and live preview pane using Spectre.Console `Layout`
- Status line: current line/column, word count, dirty indicator
- Save: `Ctrl+S`; quit: `Ctrl+Q` (prompts on unsaved changes); new: `Ctrl+N`
- Observer pattern: `EditorState` notifies preview pane (`OnBufferChanged`) and status line (`OnCursorMoved`)
