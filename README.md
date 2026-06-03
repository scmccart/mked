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
| `-p`, `--plain` | Render link text only, omitting URLs. |

**Keyboard navigation**

| Key | Action |
|-----|--------|
| `j` / `↓` | Scroll down one line |
| `k` / `↑` | Scroll up one line |
| `Shift+j` / `Shift+↓` | Scroll down one block |
| `Shift+k` / `Shift+↑` | Scroll up one block |
| `PageDown` / `Ctrl+D` | Scroll down a quarter page |
| `PageUp` / `Ctrl+U` | Scroll up a quarter page |
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
