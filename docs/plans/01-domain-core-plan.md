# Epic 01 — Domain Core: Implementation Plan

> **Epic**: [`docs/epics/01-domain-core.md`](../../docs/epics/01-domain-core.md)
> **Design**: [`docs/designs/01-domain-core-design.md`](../../docs/designs/01-domain-core-design.md)
> **Status**: Complete

---

## Overview

Tasks are sequenced foundation-first. The project scaffold lands first so every subsequent task
has a compilable project to extend. The ROP primitives (`Result<T,E>`, `Option<T>`, `Unit`) come
next because the domain interfaces depend on them. `MkedError` and the value objects follow in
parallel-safe order. Domain interfaces, `MarkdownDocument`, `EditorState`, and `ViewerState`
layer on top once their dependencies are in place. Architecture tests close the epic by asserting
the dependency constraints hold across everything built.

---

## Task List

- [x] **Task 1: Project scaffold**
  Create the solution with `dotnet new slnx -n mked` (`.slnx` format). Create a top-level
  `Directory.Build.props` that sets shared MSBuild properties for all projects (target framework,
  nullable, implicit usings, warnings as errors, documentation file generation). Create a
  `Directory.Packages.props` with `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`
  and pinned versions for every NuGet package used across the solution (Markdig; xUnit,
  AwesomeAssertions, ArchUnitNet for tests). Use `dotnet new classlib` to create
  `src/Mked.Domain/Mked.Domain.csproj` and `dotnet new xunit` to create
  `tests/Mked.Domain.Tests/Mked.Domain.Tests.csproj`; `<PackageReference>` elements in both
  project files include name only — no `Version` attribute, as versions are sourced centrally
  from `Directory.Packages.props`. Register both projects with `dotnet sln mked.slnx add`.
  `dotnet build` and `dotnet test` (zero tests) both pass cleanly.

- [x] **Task 2: Result\<T,E\>, Option\<T\>, and Unit**
  Implement `Result<T,E>` as an `abstract record` with sealed nested `Ok(T Value)` and
  `Err(E Error)` record cases, the static `Result` factory class, and all `ResultExtensions`:
  `Map`, `Bind`, `MapError`, `Match`, `Unwrap`, `UnwrapOr`, `BindAsync`, `MapAsync`. Implement
  `Option<T>` as an `abstract record` with `Some(T Value)` and `None` cases and all
  `OptionExtensions`: `Map`, `Bind`, `Match`, `UnwrapOr`, `OkOrErr`. Implement `Unit` as a
  `readonly record struct`. Every combinator is covered by unit tests, including async variants
  exercised via `Task.FromResult`.
  Depends on: Task 1

- [x] **Task 3: MkedError discriminated union**
  Implement `MkedError` as an `abstract record` with four sealed nested record cases:
  `IoError(string Path, string Reason)`, `ParseError(int Line, int Column, string Message)`,
  `ValidationError(string Field, string Message)`, and `StreamError(string Reason)`. Unit tests
  verify construction and exhaustive pattern matching via C# switch expressions for all four
  cases.
  Depends on: Task 1

- [x] **Task 4: Value objects**
  Implement `CursorPosition(int Line, int Column)`, `TextRange(CursorPosition Start, CursorPosition End)`,
  and `ViewportAnchor(int BlockIndex)` as `readonly record struct` types. Unit tests verify value
  equality, positional deconstruction, and that `CursorPosition` preserves 1-based line and
  column values as supplied.
  Depends on: Task 1

- [x] **Task 5: Domain interfaces**
  Declare `IFileReader` (`Task<Result<string, MkedError>> ReadAsync(string path)`), `IFileWriter`
  (`Task<Result<Unit, MkedError>> WriteAsync(string path, string content)`), and `IInputStream`
  (`IAsyncEnumerable<Result<string, MkedError>> ReadChunksAsync()`). These are interface
  declarations only — no implementations. XML doc comments on all members. The project builds
  without warnings.
  Depends on: Task 2, Task 3

- [x] **Task 6: MarkdownDocument**
  Implement `MarkdownDocument` as a `sealed class` wrapping `Markdig.Syntax.MarkdownDocument`.
  The static `Parse(string source)` method builds a Markdig pipeline with standard extensions
  and the YAML front matter extension, throws `ArgumentNullException` for null input, and returns
  a `MarkdownDocument`. Expose `IsEmpty` (bool), `Blocks` (`IReadOnlyList<Markdig.Syntax.Block>`),
  and `Frontmatter` (`Option<string>`). Unit tests cover: empty string → `IsEmpty` true; heading
  input → correct block count; YAML front matter present → `Frontmatter` is `Some`; absent →
  `None`; null input → `ArgumentNullException`.
  Depends on: Task 2

- [x] **Task 7: EditorState and IEditorObserver**
  Declare `IEditorObserver` with `void OnBufferChanged(string newBuffer)` and
  `void OnCursorMoved(CursorPosition position)`. Implement `EditorState` with: a constructor
  accepting an initial `string` buffer; `Buffer`, `Cursor`, `IsDirty`, `CanUndo`, and `CanRedo`
  properties; `UpdateBuffer(string)` and `UpdateCursor(CursorPosition)` mutation methods;
  `Subscribe(IEditorObserver)` for observer registration; and an internal command-object undo
  stack (the redo stack is cleared on any new mutation). Unit tests cover initial-state
  invariants, dirty flag after mutation, `CanUndo`/`CanRedo` transitions, and observer
  notification for both mutation types using a hand-rolled spy `IEditorObserver`.
  Depends on: Task 4

- [x] **Task 8: ViewerState**
  Implement `ViewerState` with: a constructor accepting a `MarkdownDocument`; an `Anchor`
  property (`ViewportAnchor`) defaulting to block index 0; `SetAnchor(ViewportAnchor)` that
  validates the index is within the document's block count and throws `ArgumentOutOfRangeException`
  otherwise; and `SetFollowMode(bool)` toggling `IsFollowing`. Unit tests cover construction
  defaults, valid anchor updates, out-of-range rejection, and follow-mode toggle.
  Depends on: Task 4, Task 6

- [x] **Task 9: Architecture tests**
  Add ArchUnitNet tests in `Mked.Domain.Tests` asserting: (1) no type in `Mked.Domain` depends
  on `Mked.Application`, `Mked.Infrastructure`, or `Mked.Console`; (2) no type in `Mked.Domain`
  references `System.IO` or `System.Console` namespaces directly. All tests pass under
  `dotnet test`.
  Depends on: Task 1, Task 2, Task 3, Task 4, Task 5, Task 6, Task 7, Task 8

---

## Notes

Tasks 2, 3, and 4 share only Task 1 as a prerequisite and can be implemented in any order.
Task 5 is the first that requires both the ROP primitives and the error types to be present.
Task 9 is a gating check that should run last so it catches any stray outward dependencies
introduced during the preceding tasks.

The internal `IEditorCommand` interface (Task 7) is not part of the public API — it is a
private nested type within `EditorState`. The public `Undo()` and `Redo()` methods are
deferred to Epic 05.
