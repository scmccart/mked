# Epic 03 — Application Use Cases

Orchestrate the domain model and infrastructure interfaces into named, testable use cases. Each use
case returns `Result<T,E>` and has no direct I/O — it receives its dependencies via constructor
injection, enabling in-memory fakes in tests.

## Features

### Feature: Open File

Load a Markdown file from disk and return a parsed domain document ready for display or editing.

- As a user, when I run `mked view file.md`, the file is opened and rendered
- As a user, I see a clear error message if the file does not exist or I lack read permission
- As a developer, `OpenFileUseCase` reads via `IFileReader`, parses with Markdig, returns `Result<MarkdownDocument, MkedError>`
- As a developer, the use case is unit-testable with an in-memory `IFileReader` fake — no file system required

### Feature: Save File

Persist the current editor buffer to disk, validating the content before writing.

- As a user, pressing `Ctrl+S` saves my changes without leaving the editor
- As a user, I see a clear error if the file cannot be written (permissions, disk full, etc.)
- As a developer, `SaveFileUseCase` validates the buffer, writes via `IFileWriter`, returns `Result<Unit, MkedError>`
- As a developer, validation errors surface as `MkedError.ValidationError` before any I/O is attempted

### Feature: Stream Input

Read Markdown incrementally from stdin and emit a live-updating document suitable for the viewer's
tail mode.

- As a user, piped content renders progressively as it arrives rather than waiting for EOF
- As a developer, `StreamInputUseCase` reads chunks from `IInputStream` and reparses incrementally
- As a developer, each chunk yields a fresh `MarkdownDocument` via `IAsyncEnumerable<Result<MarkdownDocument, MkedError>>`
- As a developer, a clean EOF terminates the enumerable normally; a broken pipe yields an `Err`

### Feature: New Document

Create a blank editing session without requiring a file path.

- As a user, running `mked edit` with no file argument opens an empty editor
- As a developer, `NewDocumentUseCase` returns a default `EditorState` with an empty buffer
- As a developer, saving a new document for the first time prompts for a file path

### Feature: Render Document

Produce a Spectre.Console `IRenderable` from a `MarkdownDocument` using the configured renderer
strategy.

- As a developer, `RenderDocumentUseCase` accepts a `MarkdownDocument` and `RenderContext` and returns an `IRenderable`
- As a developer, the renderer strategy (`IMarkdownRenderer`) is injected; the use case never references Spectre.Console directly
- As a developer, the use case is unit-testable with a stub renderer that records calls
