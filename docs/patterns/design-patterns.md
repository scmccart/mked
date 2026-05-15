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
    void OnBufferChanged(BufferChangedEvent e);
    void OnCursorMoved(CursorMovedEvent e);
}
```

The live preview widget subscribes to `OnBufferChanged` and re-renders the `MarkdownDocument` via Markdig. The status line subscribes to `OnCursorMoved` to update the line/column display.

This decouples the editing logic from the rendering logic — `EditorState` never imports Spectre.Console.

---

## Strategy — Rendering Backends

### Intent
Define a family of algorithms (rendering strategies) and make them interchangeable. The viewer and editor can switch rendering strategy without changing the host code.

### Application in mked

`IMarkdownRenderer` is the strategy interface:

```csharp
public interface IMarkdownRenderer
{
    IRenderable Render(MarkdownDocument document, RenderContext context);
}
```

Concrete strategies:

| Class | Used when |
|---|---|
| `SpectreMarkdownRenderer` | Normal viewer/editor output |
| `PlainTextRenderer` | Piped/non-interactive output (`--plain` flag) |
| `AnsiMarkdownRenderer` | Raw ANSI fallback for terminals with limited VT support |

The `ViewCommand` receives an `IMarkdownRenderer` via constructor injection and never knows which concrete strategy it is using.

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

Each layer only handles its own token type. Composing them is trivial:

```csharp
var layers = new IHighlightLayer[]
{
    new HeadingHighlightLayer(),
    new EmphasisHighlightLayer(),
    new LinkHighlightLayer(),
    new FrontMatterDimLayer()
};
```

Code fences (`FencedCodeBlock`) are explicitly excluded from all layers — their content is rendered verbatim.

---

## Command — Editor Operations (Undo/Redo)

### Intent
Encapsulate a request as an object, enabling parameterisation, queuing, logging, and reversible operations.

### Application in mked

Every user action that mutates the editor buffer is an `IEditorCommand`:

```csharp
public interface IEditorCommand
{
    void Execute(EditorBuffer buffer);
    void Undo(EditorBuffer buffer);
}
```

Concrete commands:

| Class | Triggered by |
|---|---|
| `InsertTextCommand` | Normal key input |
| `DeleteBackwardCommand` | Backspace |
| `DeleteForwardCommand` | Delete |
| `PasteCommand` | Ctrl+V / Cmd+V |
| `CutCommand` | Ctrl+X / Cmd+X |

The `CommandHistory` maintains an undo stack (`Stack<IEditorCommand>`) and a redo stack. Undo pops from the undo stack and calls `Undo(buffer)`, then pushes onto the redo stack.

```csharp
public sealed class CommandHistory
{
    public void Execute(IEditorCommand command, EditorBuffer buffer);
    public void Undo(EditorBuffer buffer);
    public void Redo(EditorBuffer buffer);
}
```

The toolbar's Save, New, and Open actions are *not* `IEditorCommand` instances — they are use cases in the Application layer. Only buffer-mutation operations participate in undo/redo.
