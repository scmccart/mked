# Mked.Controls public API (draft)

> **Note:** The implementation does not exist yet. This is a design sketch to guide Epic 07 work. The final API will differ as implementation progresses — update this document as the design evolves.

## Overview

`Mked.Controls` extends Spectre.Console with two widgets:

- `MarkdownViewer` — read-only, scrollable rendering of a Markdown string
- `MarkdownEditor` — interactive, full-screen Markdown editor that returns the edited text

Both are designed for use by any Spectre.Console application, not just the `mked` tool.

## MarkdownViewer

Implements `IRenderable` — can be passed to `AnsiConsole.Write`, embedded in a `Panel`, or used inside a `Live` display.

```csharp
using Mked.Controls;

var viewer = new MarkdownViewer(markdownText)
{
    Theme = MarkdownTheme.Default,
    ShowScrollbar = true,
};

AnsiConsole.Write(viewer);
```

### Constructor

```csharp
public MarkdownViewer(string markdownText);
```

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Theme` | `MarkdownTheme` | `MarkdownTheme.Default` | Colour and style theme |
| `ShowScrollbar` | `bool` | `true` | Whether to render a scrollbar gutter |
| `MaxHeight` | `int?` | `null` | Cap the rendered height in rows; `null` fills available space |

### Extension method

```csharp
AnsiConsole.ViewMarkdown(string markdownText);
```

---

## MarkdownEditor

Implements `IPrompt<string>` — takes over the terminal, handles all keyboard input, and returns the final text when the user saves or quits.

```csharp
using Mked.Controls;

string result = AnsiConsole.EditMarkdown(initialText: "# Hello\n");
```

### Constructor

```csharp
public MarkdownEditor(string initialText = "");
```

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Theme` | `MarkdownTheme` | `MarkdownTheme.Default` | Colour and style theme |
| `ShowPreview` | `bool` | `true` | Whether to show a live side-by-side preview pane |
| `ShowStatusLine` | `bool` | `true` | Whether to show the status bar (line/col, dirty flag, save hint) |

### Extension method

```csharp
string AnsiConsole.EditMarkdown(string initialText = "");
```

---

## MarkdownTheme

An immutable value object describing the ANSI styles applied to each Markdown element.

```csharp
public sealed class MarkdownTheme
{
    public static MarkdownTheme Default { get; }

    public Style Heading1 { get; init; }
    public Style Heading2 { get; init; }
    public Style Heading3 { get; init; }
    public Style Bold { get; init; }
    public Style Italic { get; init; }
    public Style InlineCode { get; init; }
    public Style CodeBlock { get; init; }
    public Style Blockquote { get; init; }
    public Style Link { get; init; }
}
```

Custom themes are created with C# `with` expressions:

```csharp
var myTheme = MarkdownTheme.Default with
{
    Heading1 = new Style(Color.Gold1, decoration: Decoration.Bold),
    InlineCode = new Style(Color.Grey, background: Color.Grey11),
};
```

---

## Extension method registration

Both extension methods hang off `IAnsiConsole`. They are defined in `Mked.Controls.AnsiConsoleExtensions` and available to any project that references `Mked.Controls`.
