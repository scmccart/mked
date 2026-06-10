namespace Mked.Console;

/// <summary>Discriminated union of all actions that the editor command loop can dispatch.</summary>
public abstract record EditorAction
{
    /// <summary>Insert a single printable character at the cursor.</summary>
    public sealed record InsertChar(char Character) : EditorAction;

    /// <summary>Delete the character immediately before the cursor (Backspace).</summary>
    public sealed record DeleteBackward : EditorAction;

    /// <summary>Delete the character at the cursor (Delete).</summary>
    public sealed record DeleteForward : EditorAction;

    /// <summary>Move the cursor one character in the given direction.</summary>
    public sealed record MoveCursor(Direction Dir) : EditorAction;

    /// <summary>Move the cursor one word in the given direction.</summary>
    public sealed record MoveWordCursor(Direction Dir) : EditorAction;

    /// <summary>Move the cursor to the start of the current line.</summary>
    public sealed record MoveToLineStart : EditorAction;

    /// <summary>Move the cursor to the end of the current line.</summary>
    public sealed record MoveToLineEnd : EditorAction;

    /// <summary>Undo the last buffer operation.</summary>
    public sealed record UndoAction : EditorAction;

    /// <summary>Redo the last undone buffer operation.</summary>
    public sealed record RedoAction : EditorAction;

    /// <summary>Save the current buffer to disk.</summary>
    public sealed record SaveFile : EditorAction;

    /// <summary>Discard the current document and start a new empty one.</summary>
    public sealed record NewFile : EditorAction;

    /// <summary>Open a file from disk, replacing the current document.</summary>
    public sealed record OpenFile : EditorAction;

    /// <summary>Toggle the split preview pane.</summary>
    public sealed record TogglePreview : EditorAction;

    /// <summary>Exit the editor.</summary>
    public sealed record Quit : EditorAction;

    /// <summary>No operation; the key press is not mapped to an editor action.</summary>
    public sealed record None : EditorAction;
}
