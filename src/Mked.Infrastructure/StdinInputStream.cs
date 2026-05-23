using Mked.Domain;

namespace Mked.Infrastructure;

/// <summary>
/// Reads standard input line-by-line and yields each line as a <see cref="Result{T,E}"/>.
/// When standard input is not redirected (i.e. an interactive terminal), the enumeration
/// completes immediately with no items.
/// </summary>
public sealed class StdinInputStream : IInputStream
{
    private readonly TextReader _reader;
    private readonly bool _isRedirected;

    /// <summary>
    /// Initialises a new <see cref="StdinInputStream"/>.
    /// </summary>
    /// <param name="reader">
    /// The underlying <see cref="TextReader"/> to read from.
    /// Defaults to <see cref="Console.In"/> when <see langword="null"/>.
    /// </param>
    /// <param name="isRedirected">
    /// Whether stdin is redirected from a pipe or file.
    /// Defaults to <see cref="Console.IsInputRedirected"/> when <see langword="null"/>.
    /// </param>
    public StdinInputStream(TextReader? reader = null, bool? isRedirected = null)
    {
        _reader = reader ?? Console.In;
        _isRedirected = isRedirected ?? Console.IsInputRedirected;
    }

    /// <summary>
    /// Asynchronously yields each line of standard input as an <c>Ok</c> result.
    /// Completes normally on EOF or when stdin is not redirected.
    /// Yields a single <c>Err</c> with <see cref="MkedError.StreamError"/> if an
    /// <see cref="IOException"/> occurs, then completes.
    /// </summary>
    public async IAsyncEnumerable<Result<string, MkedError>> ReadChunksAsync()
    {
        if (!_isRedirected)
            yield break;

        while (true)
        {
            string? line;
            MkedError? error = null;
            try
            {
                line = await _reader.ReadLineAsync();
            }
            catch (IOException ex)
            {
                line = null;
                error = new MkedError.StreamError(ex.Message);
            }

            if (error is not null)
            {
                yield return Result.Err<string, MkedError>(error);
                yield break;
            }

            if (line is null)
                yield break;

            yield return Result.Ok<string, MkedError>(line);
        }
    }
}
