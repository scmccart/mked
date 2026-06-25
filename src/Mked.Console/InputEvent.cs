namespace Mked.Console;

/// <summary>
/// A discriminated union of interactive console input: a decoded keystroke or a mouse-wheel notch.
/// </summary>
public readonly struct InputEvent
{
    /// <summary>Discriminates the event kind.</summary>
    public InputEventKind Kind { get; }

    /// <summary>
    /// The decoded key. Valid only when <see cref="Kind"/> is <see cref="InputEventKind.Key"/>.
    /// </summary>
    public ConsoleKeyInfo Key { get; }

    /// <summary>
    /// Wheel direction and magnitude.
    /// <c>+1</c> = scroll toward end of document; <c>-1</c> = scroll toward start.
    /// Valid only when <see cref="Kind"/> is <see cref="InputEventKind.Wheel"/>.
    /// </summary>
    public int WheelDelta { get; }

    /// <summary>
    /// 0-based screen column of the click. Valid only when
    /// <see cref="Kind"/> is <see cref="InputEventKind.Click"/>.
    /// </summary>
    public int ClickX { get; }

    /// <summary>
    /// 0-based screen row of the click. Valid only when
    /// <see cref="Kind"/> is <see cref="InputEventKind.Click"/>.
    /// </summary>
    public int ClickY { get; }

    private InputEvent(ConsoleKeyInfo key)
    {
        Kind = InputEventKind.Key;
        Key = key;
    }

    private InputEvent(int wheelDelta)
    {
        Kind = InputEventKind.Wheel;
        WheelDelta = wheelDelta;
    }

    private InputEvent(int x, int y)
    {
        Kind = InputEventKind.Click;
        ClickX = x;
        ClickY = y;
    }

    /// <summary>Creates a key event wrapping <paramref name="key"/>.</summary>
    public static InputEvent OfKey(ConsoleKeyInfo key) => new(key);

    /// <summary>
    /// Creates a wheel event. Positive <paramref name="delta"/> scrolls toward end of document.
    /// </summary>
    public static InputEvent OfWheel(int delta) => new(delta);

    /// <summary>
    /// Creates a left-button click event at the given 0-based screen coordinates.
    /// </summary>
    public static InputEvent OfClick(int x, int y) => new(x, y);
}
