# Epic 07 — Controls Library (NuGet)

Package `MarkdownEditorWidget` and `MarkdownViewerWidget` as a standalone NuGet library
(`Mked.Controls`) so third-party Spectre.Console applications can embed Markdown viewing and
editing without taking a dependency on the full `mked` tool.

## Features

### Feature: Viewer Widget Public API

Expose `MarkdownViewerWidget` as a first-class, documented public API.

- As a library consumer, I can create a `MarkdownViewerWidget` with a `MarkdownDocument` and add it to any Spectre.Console layout
- As a library consumer, I can choose a rendering strategy by passing an `IMarkdownRenderer` implementation
- As a library consumer, all public members have XML doc comments and appear correctly in IDE tooltips
- As a developer, the public API surface is intentionally minimal — implementation details are `internal`

### Feature: Editor Widget Public API

Expose `MarkdownEditorWidget` as a first-class, documented public API.

- As a library consumer, I can call `await editor.RunAsync()` to present a full-screen editor and receive the final buffer text on exit
- As a library consumer, I can pre-seed the editor with initial content
- As a library consumer, I can subscribe to buffer-change events for real-time integration
- As a developer, the widget's internal command history and highlight layers are not part of the public API

### Feature: NuGet Packaging

Publish `Mked.Controls` to nuget.org with complete package metadata.

- As a library consumer, I can install the package with `dotnet add package Mked.Controls`
- As a library consumer, the package page shows a description, tags, license (MIT), and a link to the repository
- As a developer, the package is pushed to nuget.org automatically by the GitHub Actions release workflow on a version tag
- As a developer, the package includes a README.md surfaced on the nuget.org package page

### Feature: Sample Project

Provide a runnable sample demonstrating both widgets for new library consumers.

- As a library consumer, I can clone the repository and run `dotnet run --project samples/Mked.Sample` to see both widgets in action
- As a library consumer, the sample source is concise enough to copy-paste as a starting point
- As a developer, the sample project is excluded from AOT publish and from the NuGet package itself
