# Design Patterns

This document describes the key design patterns applied in mked and explains their specific role in the codebase.

---

## Observer — Live Preview Updates

### Intent
Define a one-to-many dependency so that when the editor's buffer changes, all interested parties (live preview pane, status line word count) are notified automatically.

### Application in mked

The `EditorState` is the subject. Observers implement `IEditorObserver`:

```csharp
public interface IEditorObserver
{
    void OnBufferChanged(string newBuffer);
    void OnCursorMoved(CursorPosition position);
}
```

The live preview widget subscribes to `OnBufferChanged` and re-renders the `MarkdownDocument` via Markdig. The status line subscribes to `OnCursorMoved` to update the line/column display.

`EditorState` selectively fires each callback: buffer mutations only call `OnBufferChanged`; cursor-only moves only call `OnCursorMoved`. This prevents spurious re-parses on cursor navigation.

This decouples the editing logic from the rendering logic — `EditorState` never imports Spectre.Console.

---

## Strategy — Rendering Backends

### Intent
Define a family of algorithms (rendering strategies) and make them interchangeable. The viewer and editor can switch rendering strategy without changing the host code.

### Application in mked

`IMarkdownRenderer<TOutput>` is the strategy interface (defined in `Mked.Application`):

```csharp
public interface IMarkdownRenderer<TOutput>
{
    TOutput Render(MarkdownDocument document, RenderContext context);
}
```

The generic `TOutput` parameter keeps `Mked.Application` free of Spectre.Console references. Callers in the Presentation layer close the type parameter over a concrete output type at the composition root.

Concrete strategy:

| Class | `TOutput` | Used when |
|---|---|---|
| `SpectreMarkdownRenderer` | `IRenderable` | Stream mode (`--stream`) — renders incremental stdin input as a live `MarkdownViewer` |

`SpectreMarkdownRenderer` is instantiated directly by `ViewCommand.RunStreamModeAsync` with a `RenderContext` capturing the current `--show-frontmatter` and `--plain` settings. For the non-streaming paths (`RunFileModeAsync`, `RunFollowModeAsync`) the command constructs a `MarkdownViewer` directly via `BuildViewer()`, bypassing the renderer abstraction.

`SpectreMarkdownRenderer` also exposes a `Stream` method and an `Errors` channel reader:

```csharp
// Converts an async stream of Result<StreamedDocument, MkedError> into MarkdownViewer instances.
// Out-of-band errors are forwarded to Errors for display without interrupting the stream.
public async IAsyncEnumerable<MarkdownViewer> Stream(
    IAsyncEnumerable<Result<StreamedDocument, MkedError>> source,
    CancellationToken cancellationToken = default);

public ChannelReader<MkedError> Errors { get; }
```

---

## Decorator — Syntax Highlighting Layers

### Intent
Attach additional responsibilities (highlighting) to an object dynamically, without subclassing.

### Application in mked

The editor's raw text is processed through a pipeline of `IHighlightLayer` decorators, each of which annotates spans of the source:

```csharp
public interface IHighlightLayer
{
    IEnumerable<HighlightSpan> Annotate(string source, MarkdownDocument ast);
}
```

Layers (applied in order):

1. `HeadingHighlightLayer` — colours `#` markers and heading text.
2. `EmphasisHighlightLayer` — italics / bold markers.
3. `LinkHighlightLayer` — `[text]` and `(url)` components.
4. `FrontMatterDimLayer` — dims the YAML frontmatter block.
5. `CodeFenceLayer` — marks fenced code block content as verbatim (no inner colouring).

Each layer only handles its own token type. Composing them is trivial:

```csharp
IHighlightLayer[] layers =
[
    new HeadingHighlightLayer(),
    new EmphasisHighlightLayer(),
    new LinkHighlightLayer(),
    new FrontMatterDimLayer(),
    new CodeFenceLayer(),
];
```

---

## Command — Editor Operations (Undo/Redo)

### Intent
Encapsulate a request as an object, enabling parameterisation, queuing, logging, and reversible operations.

### Application in mked

Every mutation that `EditorState` applies is recorded as a private `IEditorCommand` object. The interface has three responsibilities:

```csharp
// (private to EditorState)
interface IEditorCommand
{
    void Apply(EditorState state);

    // Captures the current state as the inverse, ready to push onto the opposite stack.
    IEditorCommand CaptureInverse(EditorState state);

    // Fires only the observer callbacks relevant to this command type.
    void Notify(EditorState state);
}
```

There are two concrete command types:

| Class | Triggered by | Fires |
|---|---|---|
| `BufferCommand` | `Insert`, `Delete`, `UpdateBuffer` | `IEditorObserver.OnBufferChanged` |
| `CursorCommand` | `UpdateCursor` | `IEditorObserver.OnCursorMoved` |

Both types capture the *before* state at construction time. `CaptureInverse()` creates the mirror command (a `BufferCommand` capturing the current buffer becomes another `BufferCommand` capturing the post-apply buffer). This round-trip design keeps `BufferCommand`↔`BufferCommand` and `CursorCommand`↔`CursorCommand` symmetric, so corrupted redo for cursor-only operations cannot occur.

The undo/redo stacks live directly in `EditorState`:

```csharp
public void Undo()
{
    var cmd = _undoStack.Pop();
    _redoStack.Push(cmd.CaptureInverse(this));
    cmd.Apply(this);
    cmd.Notify(this);
}
```

Cursor-navigation methods (`MoveCursorLeft`, `MoveCursorUp`, etc.) do **not** push to the undo stack. Only buffer mutations and explicit cursor repositioning via `UpdateCursor` are undoable.

The Save, New, and Open actions are *not* commands — they are use cases in the Application layer and do not participate in undo/redo.
