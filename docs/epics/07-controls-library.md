# Epic 07 — Controls Library (NuGet)

Package `MarkdownEditor` and `MarkdownViewer` as a standalone NuGet library (`Mked.Controls`)
so third-party Spectre.Console applications can embed Markdown viewing and editing without
taking a dependency on the full `mked` tool.

## Features

### Feature: Viewer Widget Public API

Expose `MarkdownViewer` as a first-class, documented public API.

- As a library consumer, I can create a `MarkdownViewer` with a markdown string and add it to any Spectre.Console layout
- As a library consumer, all public members have XML doc comments and appear correctly in IDE tooltips
- As a developer, the public API surface is intentionally minimal — implementation details are `internal`

### Feature: Editor Widget Public API

Expose `MarkdownEditor` as a first-class, documented public API using a host-driven model.

- As a library consumer, I can create a `MarkdownEditor` and drive it from my own input loop by calling `editor.HandleKey(key)` and re-rendering after each keystroke
- As a library consumer, I can pre-seed the editor with initial content via the constructor (`new MarkdownEditor(initialBuffer)`) or `LoadDocument(buffer)` at any time
- As a library consumer, I can subscribe to `editor.BufferChanged` to react to edits in real time (e.g. update a live preview pane)
- As a library consumer, I can read `editor.Buffer` on exit to retrieve the final content
- As a developer, the widget's internal command history, cursor navigation, and highlight layers are not part of the public API

### Feature: NuGet Packaging

Publish `Mked.Controls` to the GitHub Packages feed (`nuget.pkg.github.com/scmccart`) with
complete package metadata. Publishing to nuget.org is deferred to Epic 9 (v1 readiness).

- As a library consumer, I can add the package from the GitHub Packages feed with `dotnet add package Mked.Controls --source https://nuget.pkg.github.com/scmccart/index.json`
- As a library consumer, the package page shows a description, tags, license (MIT), and a link to the repository
- As a developer, the package is pushed to the GitHub Packages feed automatically by the GitHub Actions release workflow (`.github/workflows/release.yml`) on a `v*` version tag
- As a developer, the package includes a `README.md` surfaced on the package page
