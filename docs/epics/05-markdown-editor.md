# Epic 05 — Markdown Editor

Deliver the `mked edit` experience: a keyboard-driven, syntax-highlighted in-terminal Markdown
editor. Live syntax highlighting updates as the user types; undo/redo provides a full editing
history; the split-pane layout optionally shows a rendered preview alongside the raw source.

## Features

### Feature: Editor Buffer & Cursor

Maintain the mutable text buffer and cursor position that back all editing operations.

- As a user, characters I type appear at the cursor position
- As a user, I can move the cursor with arrow keys, `Home`, `End`, `Ctrl+←`/`Ctrl+→`
- As a developer, `EditorBuffer` holds the text as a gap buffer or rope structure for efficient insert/delete
- As a developer, cursor movement is clamped to valid buffer positions at all times
- As a developer, the buffer exposes `Insert(position, text)` and `Delete(range)` as atomic operations

### Feature: Syntax Highlighting

Apply live, incremental syntax colouring to the source text as the user types.

- As a user, heading markers (`#`) and heading text are coloured distinctly
- As a user, bold (`**`) and italic (`*`) markers are rendered with the corresponding font attribute
- As a user, link syntax (`[text]` and `(url)`) is colour-coded
- As a user, YAML frontmatter is visibly dimmed to distinguish it from document content
- As a user, fenced code block content is rendered verbatim (no colouring inside fences)
- As a developer, each token type is handled by a separate `IHighlightLayer` in a composable pipeline
- As a developer, highlighting runs after each keypress using the incremental Markdig parse result

### Feature: Undo & Redo

Give the user a full, unlimited undo history for the current editing session.

- As a user, `Ctrl+Z` undoes the last edit operation
- As a user, `Ctrl+Y` (or `Ctrl+Shift+Z`) redoes a previously undone operation
- As a user, the undo history is unlimited within a session
- As a developer, every buffer mutation is an `IEditorCommand` pushed onto `CommandHistory`
- As a developer, `CommandHistory` maintains separate undo and redo stacks
- As a developer, Save, Open, and New are not `IEditorCommand` instances — they do not participate in undo/redo

### Feature: Keyboard Shortcuts & File Operations

Provide essential file management and session control from the keyboard.

- As a user, `Ctrl+S` saves the current file; new files prompt for a path
- As a user, `Ctrl+Q` quits the editor; if there are unsaved changes I am asked to confirm
- As a user, `Ctrl+N` opens a new empty document (prompts to save if dirty)
- As a user, `Ctrl+O` opens a file picker to load a different file

### Feature: Split-Pane Layout

Optionally show a rendered live preview alongside the raw Markdown source.

- As a user, passing `--split` at launch divides the screen between the editor and a preview pane
- As a user, the preview pane updates in real time as I type
- As a user, I can toggle the preview pane on and off with `Ctrl+P` without restarting
- As a developer, `Layout` (Spectre.Console) splits the screen; the preview pane hosts a `MarkdownViewerWidget`
- As a developer, the `IEditorObserver` pattern connects the editor buffer to the preview pane update

### Feature: Status Line

Display contextual information about the current editing state at the bottom of the screen.

- As a user, I can see my current line and column number at all times
- As a user, a dirty indicator (e.g. `●`) shows when there are unsaved changes
- As a user, a word count is displayed and updates as I type
- As a developer, the status line subscribes to `OnCursorMoved` and `OnBufferChanged` via `IEditorObserver`
