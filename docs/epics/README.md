# Epics

Feature delivery epics for mked, ordered by build dependency (innermost layers first).

| # | Epic | Summary |
|---|---|---|
| 01 | [Domain Core](01-domain-core.md) | `Result<T,E>`, `Option<T>`, domain entities, value objects, and interfaces — the innermost layer everything else depends on |
| 02 | [Infrastructure Adapters](02-infrastructure-adapters.md) | File system reader/writer, stdin stream, and file-watcher adapter — implements domain interfaces against the real OS |
| 03 | [Application Use Cases](03-application-use-cases.md) | `OpenFileUseCase`, `SaveFileUseCase`, `StreamInputUseCase`, and friends — orchestration with no direct I/O |
| 04 | [Markdown Viewer](04-markdown-viewer.md) | `MarkdownViewerWidget`, AST-to-Spectre rendering, streaming tail mode, and viewport stability |
| 05 | [Markdown Editor](05-markdown-editor.md) | `MarkdownEditorWidget`, live syntax highlighting, undo/redo command history, and split-pane layout |
| 06 | [CLI & Presentation](06-cli-presentation.md) | `CommandApp` wiring, `ViewCommand`, `EditCommand`, DI composition root, and terminal lifecycle |
| 07 | [Controls Library (NuGet)](07-controls-library.md) | `Mked.Controls` NuGet package — public API for embedding viewer and editor in third-party apps |
| 08 | [Distribution & AOT](08-distribution-aot.md) | NativeAOT single-file binaries, dotnet tool packaging, GitHub Releases, and WinGet manifest |
