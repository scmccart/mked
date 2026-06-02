# Epic 03 — Application Use Cases: Technical Design

> **Epic**: [`docs/epics/03-application-use-cases.md`](../../docs/epics/03-application-use-cases.md)
> **Status**: Draft
> **Date**: 2026-05-25

---

## Goals

1. Create `Mked.Application` as a new class library that references only `Mked.Domain` — no
   I/O, no Spectre.Console, no NuGet dependencies beyond the BCL.
2. Implement `OpenFileUseCase` — reads a file via `IFileReader`, parses with
   `MarkdownDocument.Parse`, returns both the raw source and the parsed document so the result
   serves both viewing and editing.
3. Implement `SaveFileUseCase` — validates the path, then writes via `IFileWriter`; validation
   failures surface as `MkedError.ValidationError` before any I/O is attempted.
4. Implement `StreamInputUseCase` — reads chunks from `IInputReader` and emits an
   `IAsyncEnumerable<Result<MarkdownDocument, MkedError>>` where each item is a freshly
   re-parsed accumulation of all chunks received so far.
5. Implement `NewDocumentUseCase` — returns a fresh `EditorState` with an empty buffer for
   the `mked edit` no-argument flow.
6. Define `IMarkdownRenderer<TOutput>` and `RenderContext` in `Mked.Application`; implement
   `RenderDocumentUseCase<TOutput>` so the Application layer can drive rendering without
   referencing Spectre.Console.
7. Create `Mked.Application.Tests` with full unit coverage using in-memory fakes of the
   domain interfaces and an ArchUnitNet rule that enforces the layer's inward-only dependency.

## Non-Goals

- Concrete `IMarkdownRenderer<IRenderable>` implementations — Epic 04 (`SpectreMarkdownRenderer`,
  `PlainTextRenderer`, `AnsiMarkdownRenderer`).
- Spectre.Console widget integration or `LiveDisplay` orchestration — Epic 04 / Epic 05.
- CLI parsing, command wiring, or DI container setup — Epic 06.
- Editor command execution (undo/redo, keybindings) — Epic 05.
- File-watch integration into a use case — the `mked view --follow` flow drives `IFileWatcher`
  from Presentation; no use case is needed.
- Buffer content validation rules beyond a non-empty target path — semantic content rules
  (e.g., frontmatter linting) are deferred until the editor needs them.

---

## Architecture Overview

`Mked.Application` is created from scratch. It depends only on `Mked.Domain`. No other src
project is touched. A mirror test project `Mked.Application.Tests` is added under `tests/`.

| Layer | Project | Role |
|-------|---------|------|
| Domain | `Mked.Domain` | Not touched |
| Application | `Mked.Application` | New project — hosts the five use cases, `IMarkdownRenderer<TOutput>`, `RenderContext`, and `OpenedFile` |
| Infrastructure | `Mked.Infrastructure` | Not touched |
| Presentation | `Mked.Console` | Not touched |

**AOT/Trim**: `Mked.Application` uses only BCL types and types from `Mked.Domain`. No
reflection, no `dynamic`, no `Activator.CreateInstance`, no `JsonSerializer`, no `Regex`. No
new NuGet dependencies are required. `IAsyncEnumerable<T>` and `Task<T>` are AOT-safe; the
generic `IMarkdownRenderer<TOutput>` is closed-over at the Presentation composition root with
a concrete `TOutput`, so there is no late binding.

---

## Key Types and Interfaces

### New Types

| Type | Kind | Project | Purpose |
|------|------|---------|---------|
| `OpenedFile` | sealed record | `Mked.Application` | Result payload of `OpenFileUseCase`; carries `Source` (raw text) and `Parsed` (`MarkdownDocument`) so callers can either render or edit |
| `OpenFileUseCase` | sealed class | `Mked.Application` | `Task<Result<OpenedFile, MkedError>> ExecuteAsync(string path)` |
| `SaveFileUseCase` | sealed class | `Mked.Application` | `Task<Result<Unit, MkedError>> ExecuteAsync(string path, string content)` |
| `NewDocumentUseCase` | sealed class | `Mked.Application` | `EditorState Execute()` — returns `new EditorState("")` |
| `StreamInputUseCase` | sealed class | `Mked.Application` | `IAsyncEnumerable<Result<MarkdownDocument, MkedError>> ExecuteAsync(CancellationToken)` |
| `RenderContext` | sealed record | `Mked.Application` | `ShowFrontmatter: bool`, `PlainLinks: bool` — display options shared across renderer strategies |
| `IMarkdownRenderer<TOutput>` | interface | `Mked.Application` | `TOutput Render(MarkdownDocument document, RenderContext context)` — generic over output so Application stays free of Spectre.Console |
| `RenderDocumentUseCase<TOutput>` | sealed class | `Mked.Application` | `TOutput Execute(MarkdownDocument document, RenderContext context)` — thin wrapper around the injected `IMarkdownRenderer<TOutput>` |

### Modified Types

None — Epic 03 creates `Mked.Application` from scratch and does not modify Domain or
Infrastructure.

---

## Data Flow / Sequence

### Use Case: OpenFileUseCase.ExecuteAsync

1. Caller invokes `ExecuteAsync(path)`.
2. The use case calls `IFileReader.ReadAsync(path)`.
3. On `Ok(source)` → call `MarkdownDocument.Parse(source)` and return
   `Result.Ok(new OpenedFile(source, parsed))`.
4. On `Err(MkedError.IoError)` → pass through unchanged.
5. `MarkdownDocument.Parse` does not throw for malformed Markdown (Markdig is lenient); the
   only documented throw is `ArgumentNullException` on `null` source, which `IFileReader`
   never produces on `Ok`.

```
IFileReader.ReadAsync(path)        → Result<string, MkedError>
  .MapAsync(source =>
      new OpenedFile(source,
          MarkdownDocument.Parse(source)))  → Result<OpenedFile, MkedError>
```

### Use Case: SaveFileUseCase.ExecuteAsync

1. Caller invokes `ExecuteAsync(path, content)`.
2. Validate `path`: if `string.IsNullOrWhiteSpace(path)` → return
   `Result.Err(new MkedError.ValidationError("path", "Path cannot be empty."))` — no I/O is
   attempted.
3. Validate `content`: any non-null string is accepted. (Content-level rules can be added
   later by inserting a `Bind` step before the writer call without touching the public
   signature.)
4. Call `IFileWriter.WriteAsync(path, content)` and return its result directly.

```
Validate(path, content)            → Result<(string path, string content), MkedError>
  .BindAsync(args =>
      IFileWriter.WriteAsync(args.path, args.content))  → Result<Unit, MkedError>
```

### Use Case: NewDocumentUseCase.Execute

1. Caller invokes `Execute()`.
2. Return `new EditorState("")`.

Synchronous and infallible — no `Result<T,E>` wrapper.

### Use Case: StreamInputUseCase.ExecuteAsync

1. Caller invokes `ExecuteAsync(cancellationToken)` and starts enumerating.
2. The use case maintains an internal `StringBuilder` accumulator (initially empty).
3. For each item yielded by `IInputReader.ReadChunksAsync()`:
   - **`Ok(chunk)`** → append `chunk` to the accumulator, append a newline separator,
     re-parse the full accumulated text via `MarkdownDocument.Parse`, and yield
     `Result.Ok(parsed)`.
   - **`Err(MkedError.StreamError)`** → yield the error unchanged. The enumeration may
     continue if more chunks follow (the inner enumerator decides whether to terminate).
4. When the inner enumerator completes (clean EOF), the outer enumeration completes
   normally.
5. `CancellationToken` cancellation propagates from the inner async enumerable.

```
foreach chunk in IInputReader.ReadChunksAsync():
    match chunk:
      Ok(text)  → buffer.AppendLine(text); yield Ok(MarkdownDocument.Parse(buffer.ToString()))
      Err(e)    → yield Err(e)
```

Re-parsing on every chunk is acceptable for streamed Markdown; performance optimisations
(diff-based incremental parsing) are deferred until profiling shows a need.

### Use Case: RenderDocumentUseCase<TOutput>.Execute

1. Caller invokes `Execute(document, context)`.
2. The use case calls `IMarkdownRenderer<TOutput>.Render(document, context)` and returns the
   result directly.

The use case is a thin wrapper that exists to (a) give Application a named seam for rendering
and (b) keep concrete renderer choice as a DI-time decision rather than a hard-coded call.
At the Presentation composition root, `TOutput = Spectre.Console.Rendering.IRenderable`; the
use case itself never names that type.

---

## Error Handling Strategy

- **New `MkedError` variants**: None — all four variants exist in Domain (Epic 01).
- **Error production boundaries**:
  - `OpenFileUseCase` — produces no new errors; passes `MkedError.IoError` through from
    `IFileReader`.
  - `SaveFileUseCase` — *produces* `MkedError.ValidationError` when the path is empty or
    whitespace; passes `MkedError.IoError` through from `IFileWriter`.
  - `StreamInputUseCase` — produces no new errors; passes `MkedError.StreamError` through
    from `IInputReader`.
  - `NewDocumentUseCase` and `RenderDocumentUseCase<TOutput>` — infallible; no `Result`
    wrapper.
- **User-visible failures**: None directly. Presentation (Epic 06) maps `MkedError` cases to
  terminal output.

---

## Testing Approach

All tests live in `Mked.Application.Tests` (xUnit, AwesomeAssertions). Use cases are
exercised through hand-rolled in-memory fakes of the domain interfaces; Moq is only used when
a test must verify an interaction (e.g., "the renderer was called exactly once with this
context").

```
tests/Mked.Application.Tests/
├── Unit/
│   ├── OpenFileUseCase_*.cs
│   ├── SaveFileUseCase_*.cs
│   ├── NewDocumentUseCase_*.cs
│   ├── StreamInputUseCase_*.cs
│   └── RenderDocumentUseCase_*.cs
└── Architecture/
    └── ApplicationLayer_DependencyRules_Tests.cs
```

**Fakes** (under `tests/Mked.Application.Tests/Fakes/`):

- `FakeFileReader` — in-memory map of `path → Result<string, MkedError>`. Tests stage either
  successful content or an `IoError`.
- `FakeFileWriter` — captures writes (`path`, `content`) and can be configured to return
  `Ok(Unit.Value)` or `Err(IoError)`.
- `FakeInputReader` — yields a configured sequence of `Result<string, MkedError>` items.
- `FakeMarkdownRenderer<TOutput>` — records the calls received and returns a configured
  `TOutput` value (typically `string` in tests).

**Unit tests** — no I/O, no terminal:

- **`OpenFileUseCase`**:
  - Reader returns `Ok` with a heading → result is `Ok(OpenedFile)` with `Source` matching
    the reader content and `Parsed.IsEmpty` false.
  - Reader returns `Ok` with an empty string → `Parsed.IsEmpty` true; `Source` equals empty
    string.
  - Reader returns `Err(IoError)` → result is `Err` with the same error; parser is not
    invoked (no observable parse side-effects — verified by absence of failure).

- **`SaveFileUseCase`**:
  - Empty / whitespace path → result is `Err(ValidationError("path", ...))`; writer is not
    called (verified via Moq `Verify(..., Times.Never)`).
  - Valid path with non-null content → writer is called with the exact path and content;
    result is `Ok(Unit.Value)`.
  - Writer returns `Err(IoError)` → result is `Err` with the same error.

- **`NewDocumentUseCase`**:
  - `Execute()` → `EditorState` with `Buffer == ""`, `IsDirty == false`, `Cursor == (1,1)`,
    `CanUndo == false`.
  - Two successive calls return distinct instances (independent editor sessions).

- **`StreamInputUseCase`**:
  - Two-chunk input (`"# A"`, `"text"`) → enumeration yields two `Ok(MarkdownDocument)`
    items; the second document parses both lines together.
  - Empty input → enumeration completes immediately with no items.
  - Mid-stream `Err(StreamError)` → that item surfaces as `Err`; preceding `Ok` items are
    unaffected.
  - Cancellation via `CancellationToken` → enumeration terminates with
    `OperationCanceledException` propagated from the underlying reader.

- **`RenderDocumentUseCase<TOutput>`**:
  - With a `FakeMarkdownRenderer<string>` configured to return `"rendered"` → `Execute`
    returns `"rendered"` and the fake records exactly one call with the supplied document
    and context.
  - `RenderContext` defaults — verify the use case forwards the context unchanged (i.e.,
    no mutation, no shadow defaults).

**Architecture tests** (ArchUnitNet, no trait):

- `Mked.Application` types must not depend on `Mked.Infrastructure` or `Mked.Console`.
- `Mked.Application` must not reference any `Spectre.Console.*` type.
- `Mked.Application` must not reference `System.IO.*` types (file system, streams) outside
  of the type parameter for `IAsyncEnumerable` etc.
- `Mked.Application` must reference `Mked.Domain` (positive assertion).

---

## Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | **`OpenFileUseCase` return shape**: `MarkdownDocument` alone or a record carrying both raw source and parsed document? **Resolution: `OpenedFile(Source, Parsed)`** record — viewer ignores `Source`; editor uses it. Keeps a single use case serving both modes without modifying Domain. | Resolved |
| 2 | **`RenderDocumentUseCase` output type**: how do we keep `Mked.Application` free of Spectre.Console when the renderer produces `IRenderable`? **Resolution: `IMarkdownRenderer<TOutput>` generic interface and `RenderDocumentUseCase<TOutput>` generic use case.** Application never names `IRenderable`; Presentation closes the generic over `IRenderable` at the composition root. | Resolved |
| 3 | **`SaveFileUseCase` validation surface**: what content rules trigger `ValidationError`? **Resolution: only an empty/whitespace path** for now. Buffer content is accepted as-is. A `Bind` step is the future insertion point for content rules. | Resolved |
| 4 | **`StreamInputUseCase` chunk separator**: should chunks be concatenated directly or joined with newlines? **Resolution: append a newline between chunks.** `IInputReader` is line-oriented (`StdinInputReader` calls `ReadLineAsync` and strips the terminator); rejoining with `\n` reconstructs the document's original line structure. | Resolved |
| 5 | **`StreamInputUseCase` reparse cost**: re-parsing the entire accumulator on every chunk is O(N²) in chunk count. Is this acceptable for Epic 03? **Resolution: yes — defer incremental parsing.** Markdig parsing is fast in absolute terms; the streaming surface area is small (CLI piping is the primary scenario). Revisit if profiling in Epic 04 shows a problem. | Resolved |
| 6 | **`RenderContext` field set**: should viewport width / streaming flags live in `RenderContext` now? **Resolution: no — keep it lean** (`ShowFrontmatter`, `PlainLinks`). Viewport concerns belong to Epic 04's renderer implementations; the record is open to extension. | Resolved |
