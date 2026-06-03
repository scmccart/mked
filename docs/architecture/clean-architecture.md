# Clean Architecture

## What It Is

Clean Architecture (Robert C. Martin) organises code into concentric layers where inner layers know nothing about outer layers. Dependencies always point *inward*. The result is a codebase where business logic is independent of frameworks, UI, and infrastructure.

## Layer Map for mked

```
┌─────────────────────────────────────────┐
│  Presentation (Mked.Console)            │  ← Spectre.Console.Cli commands,
│  Entry point, CLI wiring, DI container  │    widget rendering
├─────────────────────────────────────────┤
│  Application (Mked.Application)         │  ← Use cases: OpenFile, SaveFile,
│  Orchestration, use-case services       │    StreamInput, RenderDocument
├─────────────────────────────────────────┤
│  Infrastructure (Mked.Infrastructure)   │  ← File system, stdin/stdout,
│  I/O adapters, OS integrations          │    file-watcher adapter
├─────────────────────────────────────────┤
│  Domain (Mked.Domain)                   │  ← MarkdownDocument, EditorState,
│  Entities, value objects, interfaces    │    Result<T,E>, interfaces
└─────────────────────────────────────────┘
```

## Layer Responsibilities

### Domain (`Mked.Domain`)

The innermost layer. Contains:

- **Entities** — `EditorState`, `ViewerState`, `MarkdownDocument` wrapper.
- **Value objects** — `CursorPosition`, `TextRange`, `ViewportAnchor`.
- **Interfaces** — `IFileReader`, `IFileWriter`, `IInputReader` — defined here, implemented in Infrastructure.
- **Result types** — `Result<T,E>` and `Maybe<T>` (see [`result-types.md`](result-types.md)).

No dependencies on NuGet packages except the .NET BCL. AOT-safe by construction.

### Application (`Mked.Application`)

Orchestrates use cases by composing Domain objects with injected interfaces:

- `OpenFileUseCase` — reads a file via `IFileReader`, parses via Markdig, returns a `MarkdownDocument`.
- `SaveFileUseCase` — writes editor buffer to disk via `IFileWriter`.
- `StreamInputUseCase` — reads from `IInputReader`, emits incremental `MarkdownDocument` updates.

Returns `Result<T,E>` for all operations that can fail. No direct I/O; no Spectre.Console references.

### Infrastructure (`Mked.Infrastructure`)

Implements Domain interfaces against the real OS:

- `FileSystemReader` / `FileSystemWriter` — wraps `System.IO`.
- `StdinInputReader` — wraps `Console.In` / `Console.OpenStandardInput()`.
- `FileWatcherAdapter` — wraps `FileSystemWatcher` for `--follow` mode.

AOT compatibility: avoid reflection-based serialisation; use `System.IO` directly.

### Presentation (`Mked.Console`)

The executable entry point. Responsibilities:

- Build the DI container (manual or `Microsoft.Extensions.DependencyInjection`).
- Register Spectre.Console.Cli commands (`ViewCommand`, `EditCommand`).
- Wire Application use cases to Spectre.Console widget rendering.
- Handle terminal lifecycle (resize events, signal handling).

## Dependency Rule in Practice

```
Mked.Console → Mked.Application → Mked.Domain
Mked.Infrastructure → Mked.Domain
Mked.Console → Mked.Infrastructure  (wiring only, at composition root)
```

Application and Domain never reference Infrastructure or Console projects.

## Testing Implications

Because Application depends only on Domain interfaces (not Infrastructure), use cases are testable with simple in-memory fakes — no file system or terminal required.
