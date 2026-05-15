# Epic 03 — Application Use Cases

Orchestrate the domain model and infrastructure interfaces into named, testable use cases. Each use
case returns `Result<T,E>` and has no direct I/O — it receives its dependencies via constructor
injection, enabling in-memory fakes in tests.

## Features

- `OpenFileUseCase` — reads a file via `IFileReader`, parses with Markdig, returns `Result<MarkdownDocument, MkedError>`
- `SaveFileUseCase` — validates the editor buffer, writes via `IFileWriter`, returns `Result<Unit, MkedError>`
- `StreamInputUseCase` — reads incremental chunks from `IInputStream`, reparses and emits `MarkdownDocument` updates
- `RenderDocumentUseCase` — accepts a `MarkdownDocument` and a render context, returns an `IRenderable` for display
- `NewDocumentUseCase` — creates a blank `EditorState` with an empty buffer
- Pipeline composition: use cases chain via `BindAsync` / `MapAsync` — no try/catch inside use-case bodies
- In-memory fakes for `IFileReader`, `IFileWriter`, `IInputStream` used in unit tests (no file system required)
