# Epic 01 — Domain Core: Technical Design

> **Epic**: [`docs/epics/01-domain-core.md`](../../docs/epics/01-domain-core.md)
> **Status**: Complete
> **Date**: 2026-05-23

---

## Goals

1. Define `Result<T,E>` and `Maybe<T>` with all extension methods so every layer can express
   success/failure without exceptions.
2. Define the `MkedError` discriminated union covering all expected failure modes.
3. Define the `MarkdownDocument` wrapper type that isolates Markdig's AST from upper layers.
4. Define domain entities (`EditorState`, `ViewerState`) and value objects (`CursorPosition`,
   `TextRange`, `ViewportAnchor`, `IEditorObserver`).
5. Declare domain interfaces (`IFileReader`, `IFileWriter`, `IInputReader`) that Infrastructure
   will implement in Epic 02.
6. Establish `Mked.Domain.Tests` with full coverage of all pure domain logic, including an
   ArchUnitNet rule that enforces the domain's zero-outward-dependency constraint.

## Non-Goals

- Infrastructure implementations of `IFileReader`, `IFileWriter`, `IInputReader` (Epic 02).
- Application use-case orchestration (Epic 03).
- Spectre.Console rendering or CLI parsing (Epics 04–06).
- Full undo/redo stack operations — only the query surface (`CanUndo`, `CanRedo`) is in scope;
  `Undo()` and `Redo()` are deferred to Epic 05 (editor).
- YAML frontmatter parsing — `MarkdownDocument` exposes raw frontmatter text as `Maybe<string>`;
  structured YAML deserialization is a later concern.

---

## Architecture Overview

Only `Mked.Domain` and its mirror test project `Mked.Domain.Tests` are touched. All other
projects are either not yet created or not yet wired together.

| Layer | Project | Role |
|-------|---------|------|
| Domain | `Mked.Domain` | All new types live here; the only src project this epic creates |
| Application | `Mked.Application` | Not touched |
| Infrastructure | `Mked.Infrastructure` | Not touched |
| Presentation | `Mked.Console` | Not touched |

**AOT/Trim**: `Mked.Domain` takes one NuGet dependency — Markdig. Markdig uses explicit
pipeline construction and carries no reflection-based serialization; it is trim-compatible.
The domain layer itself must use no `dynamic`, no `Activator.CreateInstance`, and no
`Type.GetMethod` on unannotated types. All types are AOT-safe by construction.

---

## Record Types and Pattern Matching

### Discriminated Unions as Abstract Records

`Result<T,E>`, `Maybe<T>`, and `MkedError` are modelled as `abstract record` types with sealed
nested record cases. This provides structural equality and `ToString()` for free, enables
positional deconstruction, and allows C# switch expressions to exhaustively match every case.

```csharp
abstract record Result<T, E>
{
    public sealed record Ok(T Value) : Result<T, E>;
    public sealed record Err(E Error) : Result<T, E>;
}

abstract record MkedError
{
    public sealed record IoError(string Path, string Reason) : MkedError;
    public sealed record ParseError(int Line, int Column, string Message) : MkedError;
    public sealed record ValidationError(string Field, string Message) : MkedError;
    public sealed record StreamError(string Reason) : MkedError;
}
```

### Switch Expressions for Exhaustive Matching

C# switch expressions are the primary consumption mechanism at call sites. They are concise,
exhaustive, and compile-time verified:

```csharp
string userMessage = error switch
{
    MkedError.IoError(var path, var reason)          => $"Cannot read '{path}': {reason}",
    MkedError.ParseError(var line, var col, var msg) => $"Parse error at {line}:{col} — {msg}",
    MkedError.ValidationError(var field, var msg)    => $"{field}: {msg}",
    MkedError.StreamError(var reason)                => $"Stream closed unexpectedly: {reason}",
};
```

Consuming a `Result<T,E>` inline:

```csharp
var output = result switch
{
    Result<MarkdownDocument, MkedError>.Ok(var doc)  => renderer.Render(doc),
    Result<MarkdownDocument, MkedError>.Err(var err) => console.WriteError(err),
};
```

The `Match` extension method is still provided for **pipeline chaining** where the matched
value feeds forward rather than branching at the call site:

```csharp
return result
    .Map(doc => doc.Blocks.Count)
    .Match(onOk: count => $"{count} blocks", onErr: err => err.ToString());
```

### Value Objects as Readonly Record Structs

Small immutable value objects use `readonly record struct` — stack-allocated, zero GC pressure,
built-in value equality, and positional deconstruction:

```csharp
readonly record struct CursorPosition(int Line, int Column);
readonly record struct TextRange(CursorPosition Start, CursorPosition End);
readonly record struct ViewportAnchor(int BlockIndex);
readonly record struct Unit { public static readonly Unit Value = new(); }
```

---

## Key Types and Interfaces

### New Types

| Type | Kind | Project | Purpose |
|------|------|---------|---------|
| `Result<T,E>` | abstract record | `Mked.Domain` | ROP success/failure container; sealed `Ok(T Value)` and `Err(E Error)` nested records |
| `Result` | static class | `Mked.Domain` | `Ok<T,E>(value)` and `Err<T,E>(error)` factory methods |
| `ResultExtensions` | static class | `Mked.Domain` | `Map`, `Bind`, `MapError`, `Match`, `Unwrap`, `UnwrapOr`, `BindAsync`, `MapAsync` |
| `Maybe<T>` | abstract record | `Mked.Domain` | Optional value; sealed `Some(T Value)` and `None` nested records |
| `Maybe` | static class | `Mked.Domain` | `Some<T>(value)` and `None<T>()` factory methods |
| `MaybeExtensions` | static class | `Mked.Domain` | `Map`, `Bind`, `Match`, `UnwrapOr`, `OkOrErr` |
| `Unit` | readonly record struct | `Mked.Domain` | Void-equivalent for ROP pipelines that return no value on success |
| `MkedError` | abstract record | `Mked.Domain` | Discriminated union: `IoError`, `ParseError`, `ValidationError`, `StreamError` nested records |
| `MarkdownDocument` | sealed class | `Mked.Domain` | Wraps `Markdig.Syntax.MarkdownDocument`; exposes `IsEmpty`, `Blocks`, `Frontmatter` |
| `EditorState` | class | `Mked.Domain` | Mutable entity representing an active editing session |
| `ViewerState` | class | `Mked.Domain` | Entity representing an active viewing session |
| `CursorPosition` | readonly record struct | `Mked.Domain` | 1-based line and column coordinates |
| `TextRange` | readonly record struct | `Mked.Domain` | Selection expressed as `Start` and `End` `CursorPosition` |
| `ViewportAnchor` | readonly record struct | `Mked.Domain` | Scroll position as a 0-based top-level block index |
| `IEditorObserver` | interface | `Mked.Domain` | Observer contract: `OnBufferChanged(string)` and `OnCursorMoved(CursorPosition)` |
| `IFileReader` | interface | `Mked.Domain` | `Task<Result<string, MkedError>> ReadAsync(string path)` |
| `IFileWriter` | interface | `Mked.Domain` | `Task<Result<Unit, MkedError>> WriteAsync(string path, string content)` |
| `IInputReader` | interface | `Mked.Domain` | `IAsyncEnumerable<Result<string, MkedError>> ReadChunksAsync()` |

### Modified Types

None — Epic 01 creates `Mked.Domain` from scratch.

---

## Data Flow / Sequence

### Use Case: Result Composition Pipeline

1. A caller invokes a fallible operation and receives `Result<A, MkedError>`.
2. `.Map(fn)` transforms the success value to `Result<B, MkedError>`; an `Err` passes through
   unchanged.
3. `.Bind(fn)` chains a second fallible step; if the first result was `Err`, `fn` is skipped.
4. `.Match(onOk, onErr)` exhaustively consumes the result at the call site.
5. Async pipelines chain via `.BindAsync` / `.MapAsync` on `Task<Result<T,E>>`, avoiding
   `await` inside a `Bind` lambda.

```
Result.Ok(value)
  .Map(transform)              → Result<B, MkedError>
  .Bind(fallibleStep)          → Result<C, MkedError>  (short-circuits on Err)
  .Match(onOk, onErr)          → TOut
```

### Use Case: Maybe Bridging

1. A query that may produce nothing returns `Maybe<T>` rather than `T?`.
2. `.Map(fn)` transforms the inner value when present; `None` passes through.
3. `.OkOrErr(error)` converts `None` into `Result<T,E>.Err(error)`, enabling the value to enter
   an ROP chain.

### Use Case: MarkdownDocument Lifecycle

1. Caller passes a raw `string` to `MarkdownDocument.Parse(source)`.
2. Internally, a Markdig pipeline with standard extensions and the YAML frontmatter extension
   parses the string. Markdig never throws on malformed input — it degrades gracefully.
3. The resulting `MarkdownDocument` exposes:
   - `IsEmpty` — `true` when the top-level block list contains no blocks.
   - `Blocks` — `IReadOnlyList<Markdig.Syntax.Block>` of top-level AST blocks.
   - `Frontmatter` — `Maybe<string>` containing raw YAML text when a `YamlFrontMatterBlock`
     is the first block; `None` otherwise.

### Use Case: EditorState Mutations and Observation

1. Caller constructs `new EditorState(initialBuffer)` with the starting string.
2. Mutations — buffer replacements, cursor moves — update internal state and push an internal
   `IEditorCommand` (capturing the inverse operation) onto the undo stack; the redo stack is
   cleared on any new mutation.
3. After each mutation, `EditorState` iterates registered `IEditorObserver` instances and calls
   the appropriate callback (`OnBufferChanged` or `OnCursorMoved`).
4. `CanUndo` and `CanRedo` reflect whether there are entries in the undo/redo stacks.
5. `IsDirty` is `true` when the current buffer differs from the buffer at construction (or last
   explicit clear of the dirty flag).

### Use Case: ViewerState Scroll

1. Caller constructs `new ViewerState(document)` from a `MarkdownDocument`.
2. `Anchor` defaults to `new ViewportAnchor(0)` — the first top-level block.
3. Caller assigns a new `ViewportAnchor` to scroll; `ViewerState` validates the index is within
   the document's block count.
4. `SetFollowMode(bool)` toggles whether the viewer auto-advances the anchor as new blocks
   arrive (used in piped / `--follow` mode).

---

## Error Handling Strategy

- **New `MkedError` variants**: All four variants are introduced in this epic:
  - `IoError(string Path, string Reason)` — file not found, permission denied, etc.
  - `ParseError(int Line, int Column, string Message)` — strict-mode parse failure (reserved for
    future use; Markdig itself is lenient).
  - `ValidationError(string Field, string Message)` — editor buffer validation failure.
  - `StreamError(string Reason)` — broken pipe or unexpected stdin closure.
- **Error production boundaries**: Domain only *defines* error types; it never produces them at
  runtime (no I/O). Infrastructure will produce `IoError` and `StreamError` (Epic 02). Application
  will produce `ParseError` and `ValidationError` (Epic 03).
- **User-visible failures**: None in this epic. Error rendering is handled by Presentation (Epic 06).

---

## Testing Approach

All tests live in `Mked.Domain.Tests` (xUnit, AwesomeAssertions, no Moq needed — all domain
types are pure).

- **`Result<T,E>` and `ResultExtensions`**: `Map`, `Bind`, `MapError`, `Match` for both `Ok`
  and `Err` inputs; `Unwrap` happy and throw paths; `UnwrapOr`; async variants via
  `Task.FromResult`.
- **`Maybe<T>` and `MaybeExtensions`**: `Map`, `Bind`, `Match`, `UnwrapOr`, `OkOrErr` for
  both `Some` and `None` inputs.
- **`MkedError`**: Construction and pattern matching for each of the four variants.
- **`MarkdownDocument`**: Parse a heading → `IsEmpty` false, `Blocks` count; parse empty string
  → `IsEmpty` true; parse document with YAML front matter → `Frontmatter` is `Some`; without →
  `Frontmatter` is `None`.
- **`EditorState`**: Construction → buffer matches initial, `IsDirty` false; simulated buffer
  replacement → `IsDirty` true, `CanUndo` true; cursor update → observer notified; subscribe
  multiple observers → all notified.
- **`ViewerState`**: Construction → anchor at block 0; anchor update → reflects new index;
  follow-mode toggle → `IsFollowing` reflects value.
- **Value objects**: `CursorPosition` equality; `TextRange` equality; `ViewportAnchor` equality.
- **Architecture tests** (ArchUnitNet): `Mked.Domain` must not reference `Mked.Application`,
  `Mked.Infrastructure`, or `Mked.Console`. `Mked.Domain` must not directly reference
  `System.IO` or `System.Console` outside of the `IInputReader` / `IFileReader` / `IFileWriter`
  interface declarations.

---

## Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | `MarkdownDocument.Parse` with `null` input — **throws `ArgumentNullException`**. Null is a programming error, not a runtime condition. | Resolved |
| 2 | `ViewportAnchor` representation — **int block index**. Avoids coupling the value object to Markdig types; sufficient for all scroll-position needs. | Resolved |
| 3 | `EditorState` undo storage — **command objects** (`IEditorCommand`, internal). Each mutation captures its inverse operation. Migrating from snapshots to commands later would require significant rework; commands are the right foundation even though `Undo()`/`Redo()` are deferred to Epic 05. | Resolved |
| 4 | `IInputReader.ReadChunksAsync` EOF signalling — **enumeration completing naturally**. `StreamError` is returned only for broken-pipe or abnormal closure. | Resolved |
