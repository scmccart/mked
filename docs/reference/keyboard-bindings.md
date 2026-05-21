# Keyboard bindings

## Editor mode

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
| q or Ctrl+Q | Quit |
| ↑ or k | Scroll up one line |
| ↓ or j | Scroll down one line |
| PageUp or b | Scroll up one page |
| PageDown or Space | Scroll down one page |
| g or Home | Jump to top of document |
| G or End | Jump to bottom of document |

The viewer offers both arrow-key / page-key bindings and vim-style single-key bindings, so it feels natural whether the user is accustomed to a terminal pager like `less` or a GUI editor.
