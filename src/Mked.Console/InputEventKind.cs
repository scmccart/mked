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
}
