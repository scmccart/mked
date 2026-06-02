# Epic 03 — Application Use Cases: Implementation Plan

> **Epic**: [`docs/epics/03-application-use-cases.md`](../../docs/epics/03-application-use-cases.md)
> **Design**: [`docs/designs/03-application-use-cases-design.md`](../../docs/designs/03-application-use-cases-design.md)
> **Status**: Complete

---

## Overview

The work is sequenced as a thin foundational scaffold followed by one feature-slice per use
case. Task 1 stands up `Mked.Application` and `Mked.Application.Tests` together with the
ArchUnitNet rules — this gives every subsequent task an executable guardrail against accidental
layer leaks. Tasks 2–6 each deliver one use case as a vertical slice: the use case type, any
new supporting types it owns, a hand-rolled fake, and unit tests. Use cases are mutually
independent once the project exists, so the order from Task 2 onward is driven by what the
rest of the program will lean on earliest (Open → Save → New → Stream → Render).

---

## Task List

- [x] **Task 1: Project scaffolding and architecture tests**
  Create `src/Mked.Application/Mked.Application.csproj` referencing only `Mked.Domain`, and
  `tests/Mked.Application.Tests/Mked.Application.Tests.csproj` referencing the new
  application project plus the standard test stack (xUnit, AwesomeAssertions, Moq,
  ArchUnitNet). Register both in `mked.slnx`. Add `Architecture/ApplicationLayer_DependencyRules_Tests.cs`
  enforcing: no reference to `Mked.Infrastructure` / `Mked.Console`, no reference to
  `Spectre.Console.*`, no reference to `System.IO.*` types, and a positive reference to
  `Mked.Domain`. Done when the solution builds clean and the empty test project runs the
  four architecture assertions green.

- [x] **Task 2: OpenFileUseCase and OpenedFile**
  Add `OpenedFile.cs` (sealed record carrying `Source` and `Parsed`) and `OpenFileUseCase.cs`
  in `src/Mked.Application/`. Implement `ExecuteAsync(string path)` as
  `IFileReader.ReadAsync(path).MapAsync(source => new OpenedFile(source, MarkdownDocument.Parse(source)))`.
  Add `tests/Mked.Application.Tests/Fakes/FakeFileReader.cs` (in-memory map of
  `path → Result<string, MkedError>`). Add `Unit/OpenFileUseCase_*.cs` covering the three
  scenarios from the design: heading content, empty content, and `IoError` passthrough.
  Done when all tests pass and the architecture rules still hold.
  Depends on: Task 1

- [x] **Task 3: SaveFileUseCase**
  Add `SaveFileUseCase.cs` with `ExecuteAsync(string path, string content)`. Validate path
  via `string.IsNullOrWhiteSpace` → `ValidationError("path", "Path cannot be empty.")`;
  otherwise delegate to `IFileWriter.WriteAsync`. Add
  `tests/Mked.Application.Tests/Fakes/FakeFileWriter.cs` recording writes and returning a
  configurable result. Add `Unit/SaveFileUseCase_*.cs` covering: empty/whitespace path
  rejected without invoking the writer (verified via Moq `Times.Never`), happy-path write
  with exact path/content, and `IoError` passthrough. Done when tests pass.
  Depends on: Task 1

- [x] **Task 4: NewDocumentUseCase**
  Add `NewDocumentUseCase.cs` with infallible synchronous `Execute()` returning
  `new EditorState("")`. Add `Unit/NewDocumentUseCase_*.cs` asserting the returned state
  has empty buffer, `IsDirty == false`, cursor at `(1,1)`, `CanUndo == false`, and that two
  successive `Execute()` calls return distinct instances. Done when tests pass.
  Depends on: Task 1

- [x] **Task 5: StreamInputUseCase**
  Add `StreamInputUseCase.cs` with `ExecuteAsync(CancellationToken)` returning
  `IAsyncEnumerable<Result<MarkdownDocument, MkedError>>`. Maintain an internal
  `StringBuilder`; for each `Ok(chunk)` append the chunk plus `\n` and yield
  `Ok(MarkdownDocument.Parse(accumulator))`; for `Err` yield through unchanged. Add
  `tests/Mked.Application.Tests/Fakes/FakeInputReader.cs` yielding a configured sequence.
  Add `Unit/StreamInputUseCase_*.cs` covering: two `Ok` chunks produce two cumulative
  documents, empty input completes immediately, mid-stream `Err` surfaces as `Err` without
  losing earlier `Ok` items, and cancellation propagates. Done when tests pass.
  Depends on: Task 1

- [x] **Task 6: Renderer abstraction and RenderDocumentUseCase**
  Add `RenderContext.cs` (sealed record with `ShowFrontmatter` and `PlainLinks`),
  `IMarkdownRenderer.cs` (generic `IMarkdownRenderer<TOutput>` with
  `Render(MarkdownDocument, RenderContext)`), and `RenderDocumentUseCase.cs` (generic
  `RenderDocumentUseCase<TOutput>` that forwards to the injected renderer). Add
  `tests/Mked.Application.Tests/Fakes/FakeMarkdownRenderer.cs` (generic, records calls,
  returns a configured value). Add `Unit/RenderDocumentUseCase_*.cs` verifying: the
  configured `TOutput` is returned, the fake records exactly one invocation, and the supplied
  `RenderContext` is forwarded unchanged. Done when tests pass and the architecture rule
  prohibiting Spectre.Console references still holds (no `Spectre.Console.*` type is named
  anywhere in `Mked.Application`).
  Depends on: Task 1

---

## Notes

- **Task ordering after Task 1 is flexible.** Tasks 2–6 are mutually independent vertical
  slices; the listed order (Open → Save → New → Stream → Render) is the order downstream
  epics will lean on them, not a hard dependency chain. Doing them in this order means each
  intermediate commit closes out a feature visible in the epic description.
- **Fakes belong with the test project, not the production assembly.** Each task adds its
  fake under `tests/Mked.Application.Tests/Fakes/`. The architecture tests will catch any
  drift if a fake accidentally moves into `src/`.
- **No `Mked.Console.Tests` analogue.** Per the testing conventions, integration tests of
  these use cases (against the real file system or real stdin) belong in
  `Mked.Infrastructure.Tests` or eventual end-to-end tests in Epic 06 — not here.
- **AOT verification is deferred.** `Mked.Application` is a class library with no executable
  output; AOT warnings only surface when `Mked.Console` is published. Epic 08 owns the
  publish-time enforcement; this epic ensures the *code* is AOT-safe by construction (no
  reflection, no `dynamic`, no `Regex`, no `JsonSerializer`).
