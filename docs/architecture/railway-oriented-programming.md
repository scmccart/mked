# Railway-Oriented Programming

## What It Is

Railway-Oriented Programming (ROP) is a functional error-handling style popularised by Scott Wlaschin. Instead of throwing exceptions for expected failure cases, operations return a value that is *either* a success or a failure. These values compose like railway tracks: the "happy path" runs straight ahead; errors divert to a parallel track and bypass subsequent steps automatically.

## Why mked Uses It

mked performs many operations that can fail in predictable, non-exceptional ways: file not found, permission denied, parse error, stream closed. Using ROP:

- Makes failure modes explicit in function signatures.
- Eliminates try/catch noise in use-case code.
- Composes naturally with `async`/`await`.
- Is AOT-safe — no reflection, no dynamic code.

## The Result Type

mked uses hand-rolled `Result<T,E>` and `Maybe<T>` types (see [`result-types.md`](result-types.md)) rather than a library dependency.

```csharp
Result<MarkdownDocument, MkedError> result = await openFile.ExecuteAsync(path);

result.Match(
    onOk: doc => viewer.Render(doc),
    onErr: err => console.WriteError(err.Message)
);
```

## Core Operations

### Map (transform success value)

```csharp
Result<string, MkedError> content = await ReadFile(path);
Result<MarkdownDocument, MkedError> doc = content.Map(text => parser.Parse(text));
```

### Bind (chain fallible operations)

```csharp
Result<MarkdownDocument, MkedError> result =
    await ReadFile(path)
        .BindAsync(text => ParseMarkdown(text))
        .BindAsync(doc => ValidateFrontMatter(doc));
```

### MapError (transform error type)

```csharp
Result<MarkdownDocument, DisplayError> display =
    result.MapError(e => new DisplayError(e.Message, ErrorSeverity.Warning));
```

## Application in mked

### File I/O

```
ReadFile → Result<string, IoError>
   ↓ Bind
ParseMarkdown → Result<MarkdownDocument, ParseError>
   ↓ Map
ExtractFrontMatter → (MarkdownDocument, FrontMatter?)
```

If `ReadFile` returns a failure, `ParseMarkdown` and `ExtractFrontMatter` are skipped entirely. The failure propagates unchanged to the caller.

### Stream Input (tail mode)

Each chunk from stdin is:

```
ReadChunk → Result<string, StreamError>
   ↓ Bind
AppendAndReparse → Result<MarkdownDocument, ParseError>
   ↓ Map
EmitUpdate → ViewerUpdateEvent
```

A stream close is a success (EOF), not an error. A broken pipe is a failure.

### Editor Save

```
ValidateBuffer → Result<string, ValidationError>
   ↓ Bind
WriteFile → Result<Unit, IoError>
   ↓ Map
UpdateEditorState → EditorState
```

## Async Conventions

All I/O-bound operations return `Task<Result<T,E>>`. Extension methods `BindAsync` and `MapAsync` handle unwrapping:

```csharp
Task<Result<B, E>> BindAsync<A, B, E>(
    this Task<Result<A, E>> resultTask,
    Func<A, Task<Result<B, E>>> binder);
```

## When NOT to Use ROP

ROP is for *expected* failure cases — file not found, parse error, user cancelled. Use regular exceptions for:

- Programming errors (null reference, index out of range) — let them crash and surface during development.
- Truly exceptional conditions that indicate a bug, not a runtime condition.
