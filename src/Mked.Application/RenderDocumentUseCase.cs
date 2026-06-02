namespace Mked.Application;

/// <summary>Produces rendered output from a <see cref="MarkdownDocument"/> via an injected strategy.</summary>
public sealed class RenderDocumentUseCase<TOutput>(IMarkdownRenderer<TOutput> renderer)
{
    /// <summary>
    /// Renders <paramref name="document"/> using <paramref name="context"/> and returns the
    /// strategy's output.
    /// </summary>
    public TOutput Execute(MarkdownDocument document, RenderContext context) =>
        renderer.Render(document, context);
}
