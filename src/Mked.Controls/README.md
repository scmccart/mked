# Mked.Controls

Spectre.Console widgets for viewing and editing Markdown in the terminal.

`Mked.Controls` lets you embed a full-featured **Markdown pager** (`MarkdownViewer`) and an
**interactive editor** (`MarkdownEditor`) directly in your own Spectre.Console application —
no dependency on the `mked` tool required.

## Install

```shell
dotnet add package Mked.Controls --source https://nuget.pkg.github.com/scmccart/index.json
```

> **Note:** This package is currently published to the GitHub Packages feed. Consuming it
> requires a `nuget.config` pointing at `https://nuget.pkg.github.com/scmccart/index.json`
> and a GitHub Personal Access Token (PAT) with `read:packages` scope.
> See [GitHub Docs — Authenticating to GitHub Packages](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry#authenticating-to-github-packages).

## MarkdownViewer

`MarkdownViewer` is a Spectre.Console `IRenderable` that renders a Markdown document as styled
terminal output. Drop it into any Spectre layout or live context.

```csharp
using Mked.Controls;
using Spectre.Console;

string markdown = File.ReadAllText("notes.md");

var viewer = new MarkdownViewer(markdown)
{
    ViewportHeight = 30,
    TopLineIndex   = 0,
    ShowFrontmatter = false,
};

await AnsiConsole.Live(viewer).StartAsync(async ctx =>
{
    ctx.Refresh();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Q) break;

        // update TopLineIndex to scroll, then refresh
        viewer = viewer with { TopLineIndex = viewer.TopLineIndex + 1 };
        ctx.UpdateTarget(viewer);
        ctx.Refresh();
    }
});

// Use ScrollInfo to drive a scroll indicator:
var info = viewer.ScrollInfo;
Console.WriteLine($"Line 1 of {info.TotalLineCount}");
```

## MarkdownEditor

`MarkdownEditor` is a host-driven editor widget: your code owns the input loop and feeds
keystrokes via `HandleKey`. React to changes in real time through the `BufferChanged` event.

```csharp
using Mked.Controls;
using Spectre.Console;

// Pre-seed with existing content (optional)
var editor = new MarkdownEditor(initialBuffer: existingContent);
editor.HasFocus    = true;
editor.ViewportHeight = 40;

// Subscribe for real-time updates (e.g. live preview in a split pane)
editor.BufferChanged += text => UpdatePreview(text);

bool done = false;
var layout = new Layout("Root")
    .SplitColumns(
        new Layout("Editor"),
        new Layout("Preview"));

await AnsiConsole.Live(layout).StartAsync(async ctx =>
{
    layout["Editor"].Update(editor);
    ctx.Refresh();

    while (!done)
    {
        var key = Console.ReadKey(intercept: true);

        if (key.Key == ConsoleKey.Escape)
        {
            done = true;
            break;
        }

        editor.HandleKey(key);
        ctx.Refresh();
    }
});

// Retrieve the final buffer content
string result = editor.Buffer;
```

### Key editor members

| Member | Description |
|--------|-------------|
| `MarkdownEditor(string initialBuffer = "")` | Constructor; pre-seeds the buffer. |
| `event Action<string>? BufferChanged` | Raised after each mutation with the new buffer text. |
| `void LoadDocument(string buffer)` | Replace buffer; resets undo history, cursor, and dirty flag. |
| `bool HandleKey(ConsoleKeyInfo key)` | Process a keystroke. Returns `true` if handled. |
| `string Buffer` | Current buffer text. |
| `(int Line, int Column) Cursor` | Current 1-based cursor position. |
| `bool IsDirty` | `true` if the buffer has unsaved changes. |
| `void MarkClean()` | Clear the dirty flag without modifying the buffer. |
| `IRenderable StatusLine()` | Returns a status-bar renderable (word count, cursor, dirty indicator). |
| `bool CanUndo / CanRedo` | Whether undo/redo history is available. |
| `bool HasFocus` | Controls cursor visibility. Set to `false` when focus moves elsewhere. |
| `int? ViewportHeight` | Maximum lines to render; `null` for full height. |

## Worked example

The `mked` CLI tool (`src/Mked.Console`) in the [mked repository](https://github.com/scmccart/mked)
is a complete worked example of both widgets: `ViewCommand.cs` drives `MarkdownViewer` with a
live scrolling pager, and `EditCommand.cs` drives `MarkdownEditor` in a split-pane layout.

## Requirements

- .NET 10 or later
- [Spectre.Console](https://spectreconsole.net/) 0.55.x (pulled in automatically)
- [Markdig](https://github.com/xoofx/markdig) 1.x (pulled in automatically)

## License

MIT — © 2026 Sean McCarthy. See [LICENSE](https://github.com/scmccart/mked/blob/main/LICENSE).
