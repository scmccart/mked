namespace Mked.Console;

/// <summary>
/// Factory for <see cref="IConsoleInputSource"/>. Selects the platform-appropriate backend that
/// surfaces both keystrokes and mouse-wheel events.
/// </summary>
public static class ConsoleInputSource
{
    /// <summary>Number of document lines scrolled per wheel notch.</summary>
    public const int LinesPerNotch = 3;

    /// <summary>
    /// Creates the best available input source for the current platform.
    /// Falls back to <see cref="NullMouseInputSource"/> when input is redirected or when the
    /// platform-specific setup fails.
    /// </summary>
    public static IConsoleInputSource Create()
    {
        if (System.Console.IsInputRedirected)
            return new NullMouseInputSource();

        try
        {
            if (OperatingSystem.IsWindows())
                return new WindowsConsoleInputSource();

            return new UnixConsoleInputSource();
        }
        catch
        {
            return new NullMouseInputSource();
        }
    }
}
