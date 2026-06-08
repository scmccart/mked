# Examples

Sample Markdown files for exploring `mked view`. Run any of them from the repository root:

```sh
mked view examples/dolphins.md
mked view examples/project-status-report.md
mked view examples/toothpaste-transcript.md
```

## Files

| File | What it demonstrates |
|---|---|
| `dolphins.md` | Headings, paragraphs, blockquotes, lists, inline code, and a table — a broad survey of common Markdown elements |
| `project-status-report.md` | A realistic work-artefact: nested lists, task lists (`- [x]`), tables, and fenced code blocks |
| `toothpaste-transcript.md` | A dialogue transcript that exercises blockquotes, bold/italic emphasis, and strikethrough |

## Trying viewer features

```sh
# Live-reload while you edit the file in another terminal
mked view examples/dolphins.md --follow

# Hide URLs for cleaner reading
mked view examples/dolphins.md --plain

# Show the YAML front-matter block (if present)
mked view examples/project-status-report.md --show-frontmatter

# Pipe output from curl into the viewer
curl -s https://raw.githubusercontent.com/scmccart/mked/main/README.md | mked view --stream
```

See [keyboard bindings](docs/reference/keyboard-bindings.md#viewer-mode) for navigation shortcuts while the viewer is open.
