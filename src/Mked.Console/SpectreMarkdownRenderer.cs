using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Spectre.Console.Rendering;

namespace Mked.Console;

/// <summary>
/// Adapts <see cref="IMarkdownRenderer{TOutput}"/> to Spectre.Console's <see cref="IRenderable"/>,
/// and provides a streaming variant that drives a live <see cref="MarkdownViewer"/>.
/// </summary>
public sealed class SpectreMarkdownRenderer(RenderContext context) : IMarkdownRenderer<IRenderable>
{
    private readonly Channel<MkedError> _errors = Channel.CreateUnbounded<MkedError>();

    /// <summary>Out-of-band errors surfaced during <see cref="Stream"/>.</summary>
    public ChannelReader<MkedError> Errors => _errors.Reader;

    /// <inheritdoc/>
    public IRenderable Render(MarkdownDocument document, RenderContext context) =>
        new MarkdownViewer(document.Source)
        {
            ShowFrontmatter = context.ShowFrontmatter,
            PlainLinks = context.PlainLinks,
        };

    /// <summary>
    /// Converts a stream of <see cref="StreamedDocument"/> results into <see cref="MarkdownViewer"/>
    /// instances. Errors are forwarded to <see cref="Errors"/> for out-of-band display.
    /// </summary>
    public async IAsyncEnumerable<MarkdownViewer> Stream(
        IAsyncEnumerable<Result<StreamedDocument, MkedError>> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var result in source.WithCancellation(cancellationToken))
        {
            if (result is Result<StreamedDocument, MkedError>.Ok(var doc))
            {
                yield return new MarkdownViewer(doc.Source)
                {
                    ShowFrontmatter = context.ShowFrontmatter,
                    PlainLinks = context.PlainLinks,
                };
            }
            else if (result is Result<StreamedDocument, MkedError>.Err(var error))
            {
                await _errors.Writer.WriteAsync(error, cancellationToken);
            }
        }

        _errors.Writer.TryComplete();
    }
}
