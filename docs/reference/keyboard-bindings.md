# Keyboard bindings

## Editor mode

| Key | Action |
|---|---|
| _Any printable character_ | Insert at cursor |
| `Enter` | Insert new line |
| `Backspace` | Delete character before cursor |
| `Delete` | Delete character after cursor |
| `←` `→` `↑` `↓` | Move cursor one character / line |
| `Ctrl+←` | Move cursor one word left |
| `Ctrl+→` | Move cursor one word right |
| `Home` | Move to start of line |
| `End` | Move to end of line |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Ctrl+S` | Save file (prompts for path on new documents) |
| `Ctrl+N` | New empty document (prompts to save if dirty) |
| `Ctrl+O` | Open a different file (prompts to save if dirty) |
| `Tab` | Insert a two-space indent |
| `Shift+Tab` | Switch focus to the preview pane (split mode) |
| `Ctrl+P` | Toggle the live preview pane |
| `Ctrl+Q` | Quit (prompts to save if there are unsaved changes) |

## Preview pane (split mode, preview focused)

When `Shift+Tab` has moved focus to the preview pane in split view, these keys scroll it:

| Key | Action |
|---|---|
| `↑` | Scroll up one line |
| `↓` | Scroll down one line |
| `PageUp` | Scroll up half a screen |
| `PageDown` | Scroll down half a screen |
| `Home` | Jump to top of preview |
| `End` | Jump to bottom of preview |
| `Shift+Tab` | Return focus to the editor |

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
