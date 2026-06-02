namespace Mked.Application;

/// <summary>
/// Strategy for converting a <see cref="MarkdownDocument"/> to a rendered output value.
/// The generic parameter keeps <c>Mked.Application</c> free of Spectre.Console references;
/// callers close it over a concrete type (e.g., <c>IRenderable</c>) at the composition root.
/// </summary>
public interface IMarkdownRenderer<TOutput>
{
    /// <summary>Renders <paramref name="document"/> using the provided <paramref name="context"/>.</summary>
    public TOutput Render(MarkdownDocument document, RenderContext context);
}
