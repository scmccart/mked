# Epic 01 — Domain Core

The foundation of every other epic. Establish the primitive types, entities, value objects, and
interfaces that all other layers depend on. Nothing in Domain references Infrastructure, Application,
or Presentation — it is the innermost ring of the Clean Architecture.

## Features

### Feature: Result & Option Types

Provide the hand-rolled `Result<T,E>` and `Option<T>` primitives that replace exceptions for
all expected failure cases across the codebase.

- As a developer, I can create a success value with `Result.Ok(value)` and a failure with `Result.Err(error)`
- As a developer, I can transform a success value without touching the error using `Map`
- As a developer, I can chain two fallible operations using `Bind` so that a failure short-circuits
- As a developer, I can translate an error type using `MapError` without affecting the success path
- As a developer, I can consume a result exhaustively using `Match` with success and failure branches
- As a developer, I can use `BindAsync` and `MapAsync` to compose `Task<Result<T,E>>` pipelines
- As a developer, I can represent an optional value with `Option<T>` instead of `null`
- As a developer, I can bridge `Option<T>` to `Result<T,E>` using `OkOrErr`

### Feature: Error Types

Define the domain-specific error discriminated union that all layers use when reporting failures.

- As a developer, I can represent a file I/O failure as `MkedError.IoError` with path and reason
- As a developer, I can represent a Markdown parse failure as `MkedError.ParseError` with line, column, and message
- As a developer, I can represent an editor validation failure as `MkedError.ValidationError` with field and message
- As a developer, I can represent a broken stdin/stdout pipe as `MkedError.StreamError`
- As a developer, I can pattern-match exhaustively over all `MkedError` variants

### Feature: Markdown Document Model

Wrap Markdig's `MarkdownDocument` AST in a domain value object so the rest of the codebase never
takes a direct dependency on Markdig outside the parsing boundary.

- As a developer, I can obtain a `MarkdownDocument` by parsing a source string
- As a developer, I can query the document for its top-level block structure
- As a developer, I can extract optional frontmatter from a `MarkdownDocument`
- As a developer, I can determine whether a document is empty

### Feature: Editor State

Represent the full mutable state of an editing session as a domain entity.

- As a developer, I can create an `EditorState` from an initial string buffer
- As a developer, I can read and update the cursor position (`CursorPosition`)
- As a developer, I can query whether the buffer has unsaved changes (dirty flag)
- As a developer, I can query whether undo and redo are available
- As a developer, I can subscribe an `IEditorObserver` to receive buffer-change and cursor-move notifications

### Feature: Viewer State

Represent the read-only state of a viewing session as a domain entity.

- As a developer, I can create a `ViewerState` from a `MarkdownDocument`
- As a developer, I can query and update the `ViewportAnchor` for the current scroll position
- As a developer, I can toggle follow mode on and off

### Feature: Value Objects

Provide the small, immutable types used by `EditorState` and `ViewerState`.

- As a developer, I can represent a cursor location with `CursorPosition` (1-based line and column)
- As a developer, I can represent a selection as a `TextRange` (start and end `CursorPosition`)
- As a developer, I can represent a stable scroll anchor as a `ViewportAnchor` tied to an AST node

### Feature: Domain Interfaces

Declare the I/O abstractions that Infrastructure implements and Application depends on.

- As a developer, I can read a file's text content via `IFileReader` returning `Task<Result<string, MkedError>>`
- As a developer, I can write text to a file via `IFileWriter` returning `Task<Result<Unit, MkedError>>`
- As a developer, I can consume an async stream of text chunks via `IInputStream`
