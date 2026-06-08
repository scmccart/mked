# Keyboard bindings

## Editor mode

> **Not yet implemented.** `mked edit` is planned for [Epic 5](../epics/05-markdown-editor.md). The bindings below are documented for reference and will take effect in that release.

| Key | Action |
|---|---|
| Ctrl+S | Save file |
| Ctrl+Q | Quit (prompts if there are unsaved changes) |
| Ctrl+Z | Undo |
| Ctrl+Y | Redo |
| Ctrl+X | Cut selection |
| Ctrl+C | Copy selection |
| Ctrl+V | Paste |
| Ctrl+A | Select all |
| Arrow keys | Move cursor one character / line |
| Home | Move to start of line |
| End | Move to end of line |
| Ctrl+Home | Move to start of document |
| Ctrl+End | Move to end of document |
| PageUp | Scroll preview pane up one page |
| PageDown | Scroll preview pane down one page |
| Enter | Insert new line |
| Backspace | Delete character before cursor |
| Delete | Delete character after cursor |
| Ctrl+Backspace | Delete word before cursor |
| Tab | Indent (insert spaces) |
| Shift+Tab | Decrease indent |

### Note on Ctrl+C

On Unix-like systems, Ctrl+C sends `SIGINT` to the process. The editor intercepts this signal and treats it as a quit request, prompting to save if there are unsaved changes rather than exiting immediately.

## Viewer mode

| Key | Action |
|---|---|
| `q` / `Ctrl+C` | Quit |
| `↑` / `k` | Scroll up one line |
| `↓` / `j` | Scroll down one line |
| `Shift+↑` / `Shift+K` | Jump to previous block boundary |
| `Shift+↓` / `Shift+J` | Jump to next block boundary |
| `PageUp` / `Ctrl+U` | Scroll up half a screen |
| `PageDown` / `Ctrl+D` | Scroll down half a screen |
| `g` | Jump to top of document |
| `G` | Jump to bottom of document |

The viewer offers both arrow-key / page-key bindings and vim-style single-key bindings, so it feels natural whether the user is accustomed to a terminal pager like `less` or a text editor like Vim.

Scrolling is **line-based**: one terminal line at a time for fine-grained navigation, or half-screen jumps for faster traversal. The Shift+J / Shift+K bindings snap to the nearest block boundary, making it easy to jump between headings, code blocks, and other top-level elements.
