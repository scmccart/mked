# Epic 01 — Domain Core

The foundation of every other epic. Establish the primitive types, entities, value objects, and
interfaces that all other layers depend on. Nothing in Domain references Infrastructure, Application,
or Presentation — it is the innermost ring of the Clean Architecture.

## Features

- `Result<T,E>` and `Option<T>` types with factory methods (`Result.Ok`, `Result.Err`)
- `ResultExtensions`: `Map`, `Bind`, `MapError`, `Match`, `BindAsync`, `MapAsync`
- `OptionExtensions`: `Map`, `Bind`, `Match`, `UnwrapOr`, `OkOrErr`
- `MkedError` discriminated union: `IoError`, `ParseError`, `ValidationError`, `StreamError`
- `MarkdownDocument` value-object wrapping a Markdig `MarkdownDocument` AST
- `EditorState` entity: buffer text, cursor position, dirty flag, undo/redo capability signal
- `ViewerState` entity: loaded document, viewport anchor, follow mode flag
- `CursorPosition` value object: line and column (1-based)
- `TextRange` value object: start/end `CursorPosition`
- `ViewportAnchor` value object: anchors the visible region to a recognisable AST node
- `IFileReader` interface: read a file path to a string result
- `IFileWriter` interface: write a string to a file path result
- `IInputStream` interface: async enumerable of string chunks
