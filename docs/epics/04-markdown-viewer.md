# Epic 04 — Markdown Viewer

Deliver the `mked view` experience: a rich, read-only terminal rendering of Markdown documents.
Supports both static file display and live streaming/tail mode. Viewport stability is a first-class
concern — redraws never disorient the user.

## Features

- `MarkdownViewerWidget` — implements `IRenderable`; accepts a `MarkdownDocument` and renders it as styled Spectre.Console markup
- AST-to-Spectre mapping: headings, paragraphs, emphasis, code spans, fenced code blocks, blockquotes, lists, links, horizontal rules, tables
- Frontmatter (`YamlFrontMatterBlock`) hidden by default; optionally shown with `--show-frontmatter`
- `SpectreMarkdownRenderer` — default renderer strategy
- `PlainTextRenderer` — plain text output for piped/non-interactive use
- `AnsiMarkdownRenderer` — raw ANSI fallback for limited terminals
- `IMarkdownRenderer` strategy interface injected into viewer; switchable at startup
- `LiveDisplay`-based incremental redraw for streaming input (`--stream`) and file-follow mode (`--follow`)
- `ViewportAnchor` preserves visible region across redraws (anchors to nearest heading or paragraph block)
- Scrolling: up/down arrow, Page Up/Down, `g`/`G` for top/bottom
- Quit: `q` or `Ctrl+C`
