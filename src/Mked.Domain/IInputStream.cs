namespace Mked.Domain;

/// <summary>Provides an asynchronous stream of text chunks from standard input.</summary>
public interface IInputStream
{
    /// <summary>
    /// Yields text chunks as they arrive. The enumeration completes normally on EOF.
    /// A broken-pipe or unexpected closure yields <c>Err</c> with a
    /// <see cref="MkedError.StreamError"/>.
    /// </summary>
    IAsyncEnumerable<Result<string, MkedError>> ReadChunksAsync();
}
