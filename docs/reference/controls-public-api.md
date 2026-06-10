# Mked.Controls public API

## Overview

`Mked.Controls` extends Spectre.Console with terminal-native Markdown widgets:

- `MarkdownViewer` — read-only, scrollable rendering of a Markdown string, implemented as an `IRenderable`
- `MarkdownEditorWidget` — raw text buffer with a block cursor and syntax-highlight overlays, implemented as an `IRenderable`
- `EditorStatusLine` — single-line status bar showing cursor position, dirty state, and word count, implemented as an `IRenderable`
- `StyledSpan` — character-offset span with a Spectre.Console `Style`, used to pass highlight data into `MarkdownEditorWidget`

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

---

## MarkdownEditorWidget

Renders a raw text buffer with a block cursor and syntax-highlight overlays. Implements `IRenderable`.

```csharp
using Mked.Controls;

var widget = new MarkdownEditorWidget(
    buffer: state.Buffer,
    cursor: (state.Cursor.Line, state.Cursor.Column),
    highlights: styledSpans,
    topLineIndex: 0,
    viewportHeight: AnsiConsole.Profile.Height - 1);

AnsiConsole.Write(widget);
```

### Constructor

```csharp
public MarkdownEditorWidget(
    string buffer,
    (int Line, int Column) cursor,
    IReadOnlyList<StyledSpan> highlights,
    int topLineIndex = 0,
    int? viewportHeight = null);
```

| Parameter | Type | Description |
|---|---|---|
| `buffer` | `string` | Raw newline-delimited text to render. |
| `cursor` | `(int Line, int Column)` | 1-based cursor position. The character at this position is rendered with a block-cursor highlight. |
| `highlights` | `IReadOnlyList<StyledSpan>` | Syntax-highlight overlays expressed as character-offset spans with `Style` values. |
| `topLineIndex` | `int` | 0-based index of the first line to display. Defaults to `0`. |
| `viewportHeight` | `int?` | Maximum number of terminal rows to render. `null` renders all lines. |

### Rendering notes

- The widget never emits a trailing `Segment.LineBreak`, so it composes safely with `VerticalLayout` and Spectre.Console's `Layout` without causing height overshoot.
- Highlight spans are applied at the character level within each visible line. Overlapping spans are rendered left-to-right with the last overlapping span winning.
- The block cursor is rendered as an inverted-style character at the current cursor position. An empty column at line-end is rendered as a space with the cursor style.

---

## EditorStatusLine

Single-line status bar showing cursor position, dirty indicator, and word count. Implements `IRenderable`.

```csharp
using Mked.Controls;

var status = new EditorStatusLine(
    cursor: (state.Cursor.Line, state.Cursor.Column),
    isDirty: state.IsDirty,
    wordCount: wordCount);

AnsiConsole.Write(status);
```

### Constructor

```csharp
public EditorStatusLine(
    (int Line, int Column) cursor,
    bool isDirty,
    int wordCount);
```

| Parameter | Type | Description |
|---|---|---|
| `cursor` | `(int Line, int Column)` | 1-based cursor position displayed as `Ln N, Col N`. |
| `isDirty` | `bool` | When `true`, the dirty dot (`●`) is rendered in yellow; when `false`, it is grey. |
| `wordCount` | `int` | Word count displayed at the right of the status line. |

---

## StyledSpan

A character-offset span with a Spectre.Console `Style`. Used to pass syntax-highlight regions into `MarkdownEditorWidget`.

```csharp
public readonly record struct StyledSpan(int StartOffset, int Length, Style SpectreStyle);
```

| Member | Type | Description |
|---|---|---|
| `StartOffset` | `int` | 0-based character offset within the raw buffer string where the highlighted region begins. |
| `Length` | `int` | Number of characters covered by this span. |
| `SpectreStyle` | `Style` | Spectre.Console `Style` applied to all characters in the span. |

`StyledSpan` uses character offsets rather than `(line, column)` coordinates so it integrates directly with Spectre.Console's `Segment`-based rendering pipeline. Use `HighlightMapper.Map()` (in `Mked.Console`) to convert Domain `HighlightSpan` values (which carry `TextRange` coordinates) into `StyledSpan` values for use with this widget.
