namespace Mked.Domain;

/// <summary>
/// Discriminated union of all expected failure cases in mked. Layers produce errors by
/// constructing the appropriate case; they never throw for these conditions.
/// </summary>
public abstract record MkedError
{
    /// <summary>A file could not be read or written.</summary>
    public sealed record IoError(string Path, string Reason) : MkedError;

    /// <summary>A Markdown source string failed strict-mode parsing.</summary>
    public sealed record ParseError(int Line, int Column, string Message) : MkedError;

    /// <summary>An editor buffer field failed validation.</summary>
    public sealed record ValidationError(string Field, string Message) : MkedError;

    /// <summary>The stdin/stdout pipe was closed unexpectedly.</summary>
    public sealed record StreamError(string Reason) : MkedError;
}
