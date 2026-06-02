using System.Runtime.CompilerServices;
using System.Text;

namespace Mked.Application;

/// <summary>
/// Reads Markdown chunks from stdin and emits a freshly re-parsed document after each chunk.
/// </summary>
public sealed class StreamInputUseCase(IInputReader reader)
{
    /// <summary>
    /// Accumulates chunks from <see cref="IInputReader"/> and yields a new
    /// <see cref="MarkdownDocument"/> for each successful chunk. Reader errors are yielded
    /// as-is. The enumeration completes on clean EOF.
    /// </summary>
    public async IAsyncEnumerable<Result<MarkdownDocument, MkedError>> ExecuteAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var buffer = new StringBuilder();

        await foreach (var chunk in reader.ReadChunksAsync().WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (chunk is Result<string, MkedError>.Ok(var text))
            {
                buffer.AppendLine(text);
                yield return Result.Ok<MarkdownDocument, MkedError>(
                    MarkdownDocument.Parse(buffer.ToString()));
            }
            else if (chunk is Result<string, MkedError>.Err(var error))
            {
                yield return Result.Err<MarkdownDocument, MkedError>(error);
            }
        }
    }
}
