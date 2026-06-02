using Mked.Application.Tests.Fakes;

namespace Mked.Application.Tests.UnitTests;

public sealed class RenderDocumentUseCase_Tests
{
    [Fact]
    public void Execute_ReturnsValueFromRenderer()
    {
        // Arrange
        var renderer = new FakeMarkdownRenderer<string>("rendered output");
        var sut = new RenderDocumentUseCase<string>(renderer);
        var document = MarkdownDocument.Parse("# Hello");
        var context = new RenderContext(ShowFrontmatter: false, PlainLinks: false);

        // Act
        var result = sut.Execute(document, context);

        // Assert
        result.Should().Be("rendered output");
    }

    [Fact]
    public void Execute_CallsRendererExactlyOnce()
    {
        // Arrange
        var renderer = new FakeMarkdownRenderer<string>("output");
        var sut = new RenderDocumentUseCase<string>(renderer);
        var document = MarkdownDocument.Parse("# Hello");
        var context = new RenderContext(ShowFrontmatter: false, PlainLinks: false);

        // Act
        sut.Execute(document, context);

        // Assert
        renderer.Calls.Should().ContainSingle();
    }

    [Fact]
    public void Execute_ForwardsDocumentAndContextUnchanged()
    {
        // Arrange
        var renderer = new FakeMarkdownRenderer<string>("output");
        var sut = new RenderDocumentUseCase<string>(renderer);
        var document = MarkdownDocument.Parse("# Hello");
        var context = new RenderContext(ShowFrontmatter: true, PlainLinks: true);

        // Act
        sut.Execute(document, context);

        // Assert
        var (capturedDoc, capturedCtx) = renderer.Calls[0];
        capturedDoc.Should().BeSameAs(document);
        capturedCtx.Should().Be(context);
    }
}
