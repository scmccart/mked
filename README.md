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
| `-p`, `--plain` | Render in plain text |

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

# Render in plain text
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
| `Tab` | Insert a two-space indent |
| `←` `→` `↑` `↓` | Move cursor |
| `Ctrl+←` / `Ctrl+→` | Move cursor one word left / right |
| `Home` | Move to start of line |
| `End` | Move to end of line |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Ctrl+S` | Save (prompts for a path when editing a new document) |
| `Ctrl+O` | Open a different file |
| `Ctrl+N` | New empty document |
| `Ctrl+P` | Toggle the live preview pane |
| `Ctrl+Q` | Quit (prompts to save if there are unsaved changes) |
| `Shift+Tab` | Move focus between the editor and the preview pane (split mode) |

**When the preview pane is focused** (switch focus with `Shift+Tab`; split mode only)

| Key | Action |
|-----|--------|
| `↑` / `↓` | Scroll preview one line |
| `PageUp` / `PageDown` | Scroll preview half a viewport |
| `Home` | Jump to top |
| `End` | Jump to bottom |
| `Shift+Tab` | Move focus between the editor and the preview pane (split mode) |

**Examples**

```sh
# Edit an existing file
mked edit README.md

# Start a new document
mked edit

# Open with the preview pane visible from the start
mked edit notes.md --split
```

---

## Mked.Controls — embed Markdown in your own app

`Mked.Controls` is the library behind both commands, published as a standalone NuGet package.
Use it to add a Markdown pager (`MarkdownViewer`) or an interactive editor (`MarkdownEditor`) to
any [Spectre.Console](https://spectreconsole.net/) application.

### Install

`Mked.Controls` is on the GitHub Packages NuGet feed. Because its dependencies (Markdig,
Spectre.Console) come from nuget.org, add a `nuget.config` at your solution root that includes
both sources:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    <add key="mked-github" value="https://nuget.pkg.github.com/scmccart/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

Then add the package:

```sh
dotnet add package Mked.Controls
```

> **Authentication:** GitHub Packages requires a Personal Access Token (PAT) with `read:packages`
> scope. See [GitHub Docs — Authenticating to GitHub Packages](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry#authenticating-to-github-packages).

### Quick start

```csharp
// Viewer
var viewer = new MarkdownViewer(File.ReadAllText("notes.md")) { ViewportHeight = 30 };
AnsiConsole.Write(viewer);

// Editor (host-driven — you own the input loop)
var editor = new MarkdownEditor(initialBuffer: existingText);
editor.BufferChanged += text => UpdatePreview(text);
editor.HasFocus = true;

await AnsiConsole.Live(editor).StartAsync(async ctx =>
{
    ctx.Refresh();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Escape) break;
        editor.HandleKey(key);
        ctx.Refresh();
    }
});
string result = editor.Buffer;
```

Full API reference: [`docs/reference/controls-public-api.md`](docs/reference/controls-public-api.md)
