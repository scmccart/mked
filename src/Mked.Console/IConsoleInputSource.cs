namespace Mked.Console;

/// <summary>
/// Non-blocking source of <see cref="InputEvent"/> values — keystrokes and mouse-wheel notches.
/// Implementations enable any required terminal modes on construction and restore them on
/// <see cref="IDisposable.Dispose"/>.
/// </summary>
public interface IConsoleInputSource : IDisposable
{
    /// <summary>
    /// Attempts to read the next available input event without blocking.
    /// Returns <see langword="true"/> and populates <paramref name="ev"/> when an event is ready;
    /// returns <see langword="false"/> when the input queue is empty.
    /// </summary>
    public bool TryRead(out InputEvent ev);
}
