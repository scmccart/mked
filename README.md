# mked

A terminal-native Markdown viewer and editor.

## Installation

```
dotnet tool install -g mked
```

## Usage

### `mked view` — Markdown pager

View a Markdown file in an interactive scrollable pager:

```
mked view <path> [options]
```

**Options**

| Flag | Description |
|------|-------------|
| `-f`, `--follow` | Re-read and redisplay the file each time it changes on disk. |
| `-s`, `--stream` | Read Markdown from stdin and update the viewer as data arrives. |
| `--show-frontmatter` | Display the YAML front matter block above the document body. |
| `-p`, `--plain` | Write plain text to stdout with no pager or ANSI codes. Auto-enabled when stdout is redirected. |

**Keyboard navigation**

| Key | Action |
|-----|--------|
| `j` / `↓` | Scroll down one line |
| `k` / `↑` | Scroll up one line |
| `Shift+j` / `Shift+↓` | Scroll down one block |
| `Shift+k` / `Shift+↑` | Scroll up one block |
| `PageDown` / `Ctrl+D` | Scroll down half a page |
| `PageUp` / `Ctrl+U` | Scroll up half a page |
| `g` | Jump to top |
| `G` | Jump to bottom |
| `q` / `Ctrl+C` | Quit |

**Examples**

```sh
# View a file
mked view README.md

# Watch a file for live updates (e.g. while editing)
mked view README.md --follow

# Pipe output from another command into the viewer
curl -s https://raw.githubusercontent.com/.../README.md | mked view --stream

# Hide URLs when reading documentation
mked view docs/guide.md --plain
```

---

### `mked edit` — Markdown editor

Open a Markdown file in a keyboard-driven terminal editor with live syntax highlighting and unlimited undo:

```
mked edit [path] [options]
```

Omit `path` to start with a blank document.

**Options**

| Flag | Description |
|------|-------------|
| `--split` | Open with a live preview pane alongside the editor on startup. |

**Keyboard shortcuts**

| Key | Action |
|-----|--------|
| _Any printable character_ | Insert at cursor |
| `Enter` | Insert new line |
| `Backspace` | Delete character before cursor |
| `Delete` | Delete character after cursor |
| `←` `→` `↑` `↓` | Move cursor |
| `Ctrl+←` / `Ctrl+→` | Move cursor one word left / right |
| `Home` | Move to start of line |
| `End` | Move to end of line |
| `Tab` | Insert a two-space indent |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Ctrl+S` | Save (prompts for a path when editing a new document) |
| `Ctrl+O` | Open a different file |
| `Ctrl+N` | New empty document |
| `Ctrl+P` | Toggle the live preview pane |
| `Ctrl+Q` | Quit (prompts to save if there are unsaved changes) |

**Examples**

```sh
# Edit an existing file
mked edit README.md

# Start a new document
mked edit

# Open with the preview pane visible from the start
mked edit notes.md --split
```
