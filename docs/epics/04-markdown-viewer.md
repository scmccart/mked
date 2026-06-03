# Epic 04 — Markdown Viewer

Deliver the `mked view` experience: a rich, read-only terminal rendering of Markdown documents.
Supports both static file display and live streaming/tail mode. Viewport stability is a first-class
concern — redraws never disorient the user.

## Features

### Feature: Static Document Rendering

Render a fully-loaded `MarkdownDocument` to the terminal with rich styling.

- As a user, headings are visually prominent and colour-coded by level
- As a user, bold and italic emphasis are rendered using terminal font attributes
- As a user, inline code and fenced code blocks are rendered in a monospace style distinct from prose
- As a user, blockquotes are visually indented with a vertical bar
- As a user, ordered and unordered lists render with correct indentation and markers
- As a user, links display the link text (not the URL) unless `--plain` is passed
- As a user, tables render with aligned columns and border lines
- As a user, horizontal rules render as full-width dividers

### Feature: Frontmatter Handling

Control whether YAML frontmatter is shown or silently stripped.

- As a user, frontmatter is hidden by default so I only see the document content
- As a user, passing `--show-frontmatter` displays the raw YAML block above the document
- As a developer, `YamlFrontMatterBlock` is detected and toggled without re-parsing

### Feature: Rendering Strategies

Style output for the terminal environment. `mked view` is a TTY-only command; piped output is not supported.

- As a user, content is styled with colour and emphasis via `SpectreMarkdownRenderer`; colour depth degrades automatically on limited terminals
- As a developer, `IMarkdownRenderer` is injected; switching strategy requires no change to the viewer widget

### Feature: Scrolling & Navigation

Let the user navigate long documents without a mouse.

- As a user, I can scroll down with `↓` or `j` and up with `↑` or `k`
- As a user, `Page Down` / `Page Up` scroll by a full screen height
- As a user, `g` jumps to the top of the document; `G` jumps to the bottom
- As a user, pressing `q` or `Ctrl+C` exits the viewer cleanly

### Feature: Viewport Stability

Ensure redraws (on resize, file-follow, or stream update) keep the user oriented.

- As a user, when the terminal is resized, the content I was reading stays visible
- As a user, when a watched file reloads, the visible region is preserved by `ViewportAnchor`
- As a developer, `ViewportAnchor` identifies the nearest heading or paragraph block above the top of the viewport and re-seeks to it after a redraw

### Feature: Streaming & Tail Mode

Support live-updating display for piped input and file-follow mode.

- As a user, `mked view --stream` renders stdin content as it arrives, updating in place
- As a user, `mked view --follow file.md` re-renders automatically when the file changes on disk
- As a developer, `LiveDisplay` drives incremental redraws; viewport anchor is re-applied on each update
