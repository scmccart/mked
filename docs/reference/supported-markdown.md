# Supported Markdown syntax

mked targets [CommonMark](https://spec.commonmark.org/) as its baseline plus a small set of widely-used extensions. Raw HTML pass-through is explicitly excluded.

## Supported elements

### Block elements

| Element | Syntax |
|---|---|
| ATX heading | `# H1` through `###### H6` |
| Setext heading | Underlined with `=` (H1) or `-` (H2) |
| Paragraph | Plain text, hard-wrapped or soft-wrapped |
| Fenced code block | ` ``` ` or `~~~` with optional language tag |
| Indented code block | 4-space indent |
| Blockquote | `> ` prefix, nestable |
| Unordered list | `-`, `*`, or `+` bullets |
| Ordered list | `1.`, `2.`, … |
| Task list | `- [ ]` (open) / `- [x]` (checked) |
| Table | GFM pipe tables — `\| col \| col \|` |
| Horizontal rule | `---`, `***`, or `___` on its own line |

### Inline elements

| Element | Syntax |
|---|---|
| Bold | `**text**` or `__text__` |
| Italic | `*text*` or `_text_` |
| Bold + italic | `***text***` |
| Strikethrough | `~~text~~` |
| Inline code | `` `code` `` |
| Link | `[text](url)` or `[text][ref]` with `[ref]: url` |
| Autolink | `<https://example.com>` |
| Image | `![alt](url)` — rendered as alt text followed by the URL in parentheses (same as a regular link) |
| HTML entities | `&amp;` `&lt;` `&gt;` `&quot;` — decoded; other named/numeric entities passed through literally |

## Not supported (v1)

| Feature | Notes |
|---|---|
| Raw HTML tags | Rendered as literal escaped text (the raw tag string is shown) |
| HTML blocks | Rendered as literal plain text, one line per source line |
| Math / LaTeX | Not planned for v1 |
| Footnotes | Not planned for v1 |
| Definition lists | Not planned for v1 |

HTML is not interpreted or stripped — `HtmlBlock` nodes are emitted line-by-line as plain text, and `HtmlInline` nodes (e.g. `<br>`, `<em>`) are escaped and shown verbatim. HTML parsing in Markdig is part of the core pipeline and cannot be disabled; the terminal renderer simply passes the raw text through.

## Markdig extensions enabled

The pipeline is built with `UseAdvancedExtensions()` (which bundles a broad set of Markdig extensions) plus `UseYamlFrontMatter()`. The following extensions have dedicated terminal rendering support:

| Extension | Purpose |
|---|---|
| `Tables` | GFM pipe tables — rendered via Spectre.Console's table layout |
| `TaskLists` | `- [ ]` / `- [x]` — list items with checkbox markers |
| `Strikethrough` | `~~text~~` |
| `AutoLinks` | Bare URLs converted to inline links |
| `Yaml` | YAML front matter block — hidden by default; shown with `--show-frontmatter` |

Other extensions bundled by `UseAdvancedExtensions()` (e.g. footnotes, definition lists, math) are parsed but have no dedicated renderer; their nodes fall through to plain-text output.

## Terminal rendering notes

- **Images** (`![alt](url)`) — rendered as their alt text followed by the URL in parentheses (same as a regular link). Most terminals cannot display raster images inline.
- **Link URLs** — shown inline after the link text in parentheses, e.g. `link text (https://…)`. Pass `--plain` / `-p` to show only the link text and omit the URL.
- **Code blocks** — rendered with dim styling. Language tags are parsed but no syntax-colour highlighting is applied in v1.
- **Tables** — rendered using Spectre.Console's table layout with rounded borders; columns reflow to fit terminal width.
