namespace Mked.Console;

/// <summary>Discriminates the kinds of interactive console input events.</summary>
public enum InputEventKind
{
    /// <summary>A decoded keystroke.</summary>
    Key,

    /// <summary>A mouse-wheel notch.</summary>
    Wheel,

    /// <summary>A left mouse-button press (click). Coordinates are 0-based screen cells.</summary>
    Click,

    /// <summary>
    /// A bracketed-paste event. The terminal has delivered a block of text from the
    /// system clipboard wrapped in paste-mode markers (<c>ESC [ 2 0 0 ~</c> … <c>ESC [ 2 0 1 ~</c>).
    /// </summary>
    Paste,
}
