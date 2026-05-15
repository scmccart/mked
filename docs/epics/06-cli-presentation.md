# Epic 06 — CLI & Presentation

Wire the application together into a runnable executable. Register Spectre.Console.Cli commands,
build the DI container, handle terminal lifecycle events, and ensure the presentation layer never
leaks infrastructure concerns into Application or Domain.

## Features

- `CommandApp` registration: `ViewCommand` (`mked view`) and `EditCommand` (`mked edit`)
- `ViewSettings`: `[file]` argument, `--follow`/`-f`, `--stream`, `--plain`, `--show-frontmatter`
- `EditSettings`: `<file>` argument, `--split`
- Manual DI composition root: register use cases, infrastructure adapters, and renderer strategies
- `ViewCommand.Execute` — resolves file or stdin, invokes `OpenFileUseCase` / `StreamInputUseCase`, hands result to `MarkdownViewerWidget`
- `EditCommand.Execute` — invokes `OpenFileUseCase` (or `NewDocumentUseCase`), hands result to `MarkdownEditorWidget`
- Terminal lifecycle: handle `Console.CancelKeyPress` (Ctrl+C) and `SIGTERM` for clean shutdown
- Terminal resize event handling: reflow layout on `Console.WindowWidth` / `Console.WindowHeight` changes
- Error rendering: `MkedError` variants mapped to styled Spectre.Console error panels
- Exit codes: `0` success, `1` usage error, `2` file/IO error, `3` parse error
