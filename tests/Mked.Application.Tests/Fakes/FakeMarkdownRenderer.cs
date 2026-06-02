namespace Mked.Application.Tests.Fakes;

internal sealed class FakeMarkdownRenderer<TOutput>(TOutput returnValue) : IMarkdownRenderer<TOutput>
{
    public List<(MarkdownDocument Document, RenderContext Context)> Calls { get; } = [];

    public TOutput Render(MarkdownDocument document, RenderContext context)
    {
        Calls.Add((document, context));
        return returnValue;
    }
}
