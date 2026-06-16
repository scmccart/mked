namespace Mked.Console;

/// <summary>
/// Maps a <see cref="MkedError"/> to a styled Spectre.Console panel and returns the
/// appropriate <see cref="ExitCode"/>. Single source of truth for error presentation.
/// </summary>
public static class ErrorPresenter
{
    /// <summary>
    /// Writes an error panel to <see cref="AnsiConsole"/> and returns the exit code.
    /// </summary>
    public static int Show(MkedError error)
    {
        var (header, body, code) = Describe(error);
        AnsiConsole.Write(
            new Panel(Markup.Escape(body))
                .Header($"[red bold] {Markup.Escape(header)} [/]")
                .BorderColor(Color.Red));
        return code;
    }

    private static (string header, string body, int code) Describe(MkedError error) =>
        error switch
        {
            MkedError.IoError { Kind: IoKind.ReadNotFound } e =>
                ("File not found", e.Path, ExitCode.Io),
            MkedError.IoError { Kind: IoKind.ReadAccessDenied } e =>
                ("Permission denied", $"Cannot read: {e.Path}", ExitCode.Io),
            MkedError.IoError { Kind: IoKind.WriteAccessDenied } e =>
                ("Permission denied", $"Cannot write: {e.Path}", ExitCode.Io),
            MkedError.IoError { Kind: IoKind.WriteGeneric } e =>
                ("Write error", $"{e.Path}: {e.Reason}", ExitCode.Io),
            MkedError.IoError { Kind: IoKind.ReadGeneric } e =>
                ("Read error", $"{e.Path}: {e.Reason}", ExitCode.Io),
            MkedError.ParseError e =>
                ("Parse error", $"Line {e.Line}, column {e.Column}: {e.Message}", ExitCode.Parse),
            MkedError.ValidationError e =>
                ("Invalid input", $"{e.Field}: {e.Message}", ExitCode.Usage),
            MkedError.StreamError e =>
                ("Stream error", e.Reason, ExitCode.Io),
            _ => ("Error", error.ToString() ?? "Unknown error", ExitCode.Usage),
        };
}
