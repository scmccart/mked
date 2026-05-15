# Epic 07 — Controls Library (NuGet)

Package `MarkdownEditorWidget` and `MarkdownViewerWidget` as a standalone NuGet library
(`Mked.Controls`) so third-party Spectre.Console applications can embed Markdown viewing and
editing without taking a dependency on the full `mked` tool.

## Features

- `Mked.Controls` project targeting `netstandard2.0` + `net10.0` (or `net8.0`+ TFM as appropriate)
- Public `MarkdownViewerWidget : IRenderable` — accepts `MarkdownDocument`, renders to Spectre.Console markup
- Public `MarkdownEditorWidget` — full-screen editing widget; exposes `Task<string> RunAsync()` returning final buffer text
- `IMarkdownRenderer` extension point exposed publicly so consumers can customise rendering
- XML doc comments on all public API members
- NuGet packaging metadata: description, tags, icon, license, project URL, repository URL
- nuget.org publish via GitHub Actions on tag push
- Sample project demonstrating viewer and editor widget usage
- AOT/trim compatibility annotations (`[DynamicDependency]`, `[RequiresUnreferencedCode]` where unavoidable)
