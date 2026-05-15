# Markdig

## What It Is

[Markdig](https://github.com/xoofx/markdig) is a fast, extensible, CommonMark-compliant Markdown parser for .NET. It parses Markdown source text into an Abstract Syntax Tree (AST) and supports a rich extension model for custom syntax.

## Why mked Uses It

mked needs to both *render* Markdown (viewer) and *highlight* Markdown syntax while editing (editor). Markdig provides:

- A reliable, spec-compliant parse of Markdown source into a typed AST.
- An extension pipeline that lets mked add mked-specific parse rules if needed.
- A well-tested foundation that avoids reimplementing a Markdown parser.

## Pipeline Architecture

Markdig builds a `MarkdownPipeline` from a set of extensions:

```csharp
var pipeline = new MarkdownPipelineBuilder()
    .UseAdvancedExtensions()   // tables, footnotes, etc.
    .UseFrontMatter()           // YAML frontmatter extraction
    .Build();
```

Parsing returns a `MarkdownDocument`:

```csharp
MarkdownDocument document = Markdown.Parse(source, pipeline);
```

## The AST

The `MarkdownDocument` is a tree of `MarkdownObject` nodes:

| Node type | Represents |
|---|---|
| `HeadingBlock` | `# Heading` |
| `ParagraphBlock` | Body text |
| `FencedCodeBlock` | ` ```lang ... ``` ` |
| `ListBlock` / `ListItemBlock` | Ordered and unordered lists |
| `QuoteBlock` | `> blockquote` |
| `EmphasisInline` | `*italic*` / `**bold**` |
| `CodeInline` | `` `code` `` |
| `LinkInline` | `[text](url)` |

## How mked Uses Markdig

### Viewer

The viewer walks the `MarkdownDocument` AST and maps each node type to a Spectre.Console `IRenderable`. Frontmatter (`YamlFrontMatterBlock`) is extracted and hidden by default. Code fences (`FencedCodeBlock`) are rendered as plain text (no syntax highlighting inside fences, per spec).

### Editor (Syntax Highlighting)

The editor calls `Markdown.Parse` incrementally as the user types. It uses the resulting AST to determine which spans of the source text map to which Markdown element types, then applies Spectre.Console markup tags to colour those spans. Because code fences are excluded from highlighting, `FencedCodeBlock` spans are identified and rendered verbatim.

### Frontmatter

mked uses Markdig's `UseFrontMatter()` extension. The `YamlFrontMatterBlock`, if present, is silently stripped from viewer output. The editor renders it dimmed/greyed to signal it is metadata rather than content.

## Extension Points

If mked needs custom syntax (e.g., wiki-style `[[links]]`), it can implement `IMarkdownExtension` and register it on the pipeline without forking Markdig.
