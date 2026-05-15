# Spectre.Console

## What It Is

[Spectre.Console](https://spectreconsole.net/) is a .NET library for building rich console applications. It provides a composable layout system, markup-based text styling, live-updating displays, progress reporting, and interactive prompts — all built on top of the ANSI/VT escape code standards supported by modern terminals.

## Why mked Uses It

mked's entire rendering surface is terminal output. Spectre.Console provides the primitives we need to build both the `MarkdownViewer` and `MarkdownEditor` controls without reimplementing ANSI handling, colour theming, or layout composition.

## Key APIs

### Markup and Styling

`Markup` applies inline styling using a BBCode-inspired syntax (`[bold red]text[/]`). mked uses it to render styled Markdown elements (headings, emphasis, code spans) in the viewer.

### Layout

`Layout` composes panels in rows and columns. The editor uses this to split the screen between the text area and the live preview pane.

### Live

`LiveDisplay` enables incremental, in-place updates to console output. The viewer's streaming-input tail mode and the editor's live preview both rely on `LiveDisplay` to redraw without clearing the entire screen.

### Panels and Rules

`Panel` adds borders and titles around content blocks. `Rule` renders horizontal dividers. Both are used for the editor's toolbar and status line framing.

### Prompts and Input

Spectre.Console provides `TextPrompt` and `SelectionPrompt` for simple interactive input. mked extends this model with a custom `MarkdownEditor` control that handles raw key events for full editing semantics.

## How mked Extends It

mked ships a `Mked.Controls` library that registers custom Spectre.Console widgets:

- **`MarkdownEditorWidget`** — a multi-line text area that intercepts raw key input, maintains cursor state, applies syntax highlighting inline, and renders its content via Spectre.Console markup.
- **`MarkdownViewerWidget`** — a scrollable panel that accepts a Markdown AST (from Markdig) and renders it as styled Spectre.Console markup, hiding frontmatter by default.

These controls follow Spectre.Console's `IRenderable` protocol so they compose naturally with existing layout primitives.
