namespace Mked.Console;

/// <summary>Canonical exit codes returned by <c>mked</c> commands.</summary>
public static class ExitCode
{
    /// <summary>The command completed successfully.</summary>
    public const int Success = 0;

    /// <summary>Bad usage: missing argument, unknown option, or conflicting flags.</summary>
    public const int Usage = 1;

    /// <summary>File or I/O error: file not found, access denied, or write failure.</summary>
    public const int Io = 2;

    /// <summary>Markdown parse or validation error.</summary>
    public const int Parse = 3;
}
