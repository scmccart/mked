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
| Image | `![alt](url)` — displayed as `[image: alt]` in the viewer |

## Not supported (v1)

| Feature | Notes |
|---|---|
| Raw HTML tags | Stripped from output; not rendered |
| HTML blocks | Treated as literal fenced text |
| Inline HTML entities | Common entities (`&amp;`, `&lt;`, `&gt;`, `&quot;`) are decoded; others passed through literally |
| Math / LaTeX | Not planned for v1 |
| Footnotes | Not planned for v1 |
| Definition lists | Not planned for v1 |

HTML stripping is enforced by omitting the `HtmlBlocks` and `HtmlInline` extensions from the Markdig pipeline.

## Markdig extensions enabled

| Extension | Purpose |
|---|---|
| `Tables` | GFM pipe tables |
| `TaskLists` | `- [ ]` / `- [x]` |
| `Strikethrough` | `~~text~~` |
| `AutoLinks` | Bare URLs converted to clickable links |
| `Yaml` | YAML front matter block — parsed separately and not rendered inline |

## Terminal rendering notes

- **Images** render as `[image: alt text]` since most terminals cannot display raster images.
- **Link URLs** may be shown inline or as a numbered footnote list depending on terminal width.
- **Code blocks** receive syntax highlighting via ANSI colours when the language tag is recognised.
- **Tables** are rendered using Spectre.Console's table layout; columns reflow to fit terminal width.
