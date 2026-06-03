# Mked.Controls public API

## Overview

`Mked.Controls` extends Spectre.Console with a scrollable Markdown rendering widget:

- `MarkdownViewer` — read-only, scrollable rendering of a Markdown string, implemented as an `IRenderable`

`MarkdownEditor` and theming support are planned in later epics and are not yet implemented.

## MarkdownViewer

Implements Spectre.Console's `IRenderable` — can be passed to `AnsiConsole.Write`, embedded in a `Panel`, or used inside a `Live` display.

```csharp
using Mked.Controls;

var viewer = new MarkdownViewer(markdownText)
{
    TopLineIndex   = 0,
    ViewportHeight = AnsiConsole.Profile.Height,
    ShowFrontmatter = false,
    PlainLinks = false,
};

AnsiConsole.Write(viewer);
```

### Constructor

```csharp
public MarkdownViewer(string Markdown);
```

`MarkdownViewer` is a `record class`. The underlying Markdig AST is parsed once at construction time and shared across `with`-copies, so scrolling (changing `TopLineIndex`) does not re-parse the document.

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `ShowFrontmatter` | `bool` | `false` | Show the raw YAML front-matter block above the document body. |
| `PlainLinks` | `bool` | `false` | Render link text only, omitting the URL. |
| `TopLineIndex` | `int` | `0` | 0-based index of the first terminal line to display. Clamped to `[0, TotalLineCount − ViewportHeight]` during render. |
| `ViewportHeight` | `int?` | `null` | Maximum number of terminal rows to render. `null` emits the entire document. |

### Read-only members

| Member | Type | Description |
|---|---|---|
| `BlockCount` | `int` | Number of top-level Markdown blocks (blank lines and link-definition groups excluded; front matter excluded unless `ShowFrontmatter` is `true`). |
| `ScrollInfo` | `MarkdownViewerScrollInfo` | Scroll metadata populated on the first `Render` or `Measure` call. Returns `MarkdownViewerScrollInfo.Empty` until then. |

### Scrolling pattern

`MarkdownViewer` is immutable; scroll by creating a `with`-copy:

```csharp
// Scroll down one line
viewer = viewer with { TopLineIndex = viewer.TopLineIndex + 1 };
liveCtx.UpdateTarget(viewer);
```

The render cache (line segments) is stored in a shared `RenderStateHolder` and is keyed on `(width, ShowFrontmatter, PlainLinks)`, so `with`-copies that only change `TopLineIndex` do not re-render the document.

---

## MarkdownViewerScrollInfo

Scroll metadata returned by `MarkdownViewer.ScrollInfo` after the first render.

```csharp
public sealed record MarkdownViewerScrollInfo(
    int TotalLineCount,
    IReadOnlyList<int> BlockStartLines);
```

| Member | Type | Description |
|---|---|---|
| `TotalLineCount` | `int` | Total number of rendered terminal lines in the document. |
| `BlockStartLines` | `IReadOnlyList<int>` | First terminal-line index for each top-level block, in document order. Use this list to implement block-boundary navigation. |

`MarkdownViewerScrollInfo.Empty` is a sentinel with `TotalLineCount = 0` and an empty list.
