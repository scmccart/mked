namespace Mked.Console;

/// <summary>
/// Keyboard-only input source backed by <see cref="System.Console.ReadKey(bool)"/>.
/// Used when input is redirected or when the platform input source fails to initialise.
/// Mouse-wheel events are never produced.
/// </summary>
internal sealed class NullMouseInputSource : IConsoleInputSource  // keyboard-only fallback
{
    /// <inheritdoc/>
    public bool TryRead(out InputEvent ev)
    {
        if (!System.Console.KeyAvailable)
        {
            ev = default;
            return false;
        }

        ev = InputEvent.OfKey(System.Console.ReadKey(intercept: true));
        return true;
    }

    /// <inheritdoc/>
    public void Dispose() { }
}
