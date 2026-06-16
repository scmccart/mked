namespace Mked.Console.Tests.Unit;

public sealed class RendererSelector_Tests
{
    private static ViewSettings Settings(bool plain) =>
        new() { Plain = plain };

    [Fact]
    public void PlainFlag_True_OutputNotRedirected_IsPlainMode()
    {
        RendererSelector.IsPlainMode(Settings(plain: true), isOutputRedirected: false)
            .Should().BeTrue();
    }

    [Fact]
    public void PlainFlag_False_OutputRedirected_IsPlainMode()
    {
        RendererSelector.IsPlainMode(Settings(plain: false), isOutputRedirected: true)
            .Should().BeTrue();
    }

    [Fact]
    public void PlainFlag_True_OutputRedirected_IsPlainMode()
    {
        RendererSelector.IsPlainMode(Settings(plain: true), isOutputRedirected: true)
            .Should().BeTrue();
    }

    [Fact]
    public void PlainFlag_False_OutputNotRedirected_IsNotPlainMode()
    {
        RendererSelector.IsPlainMode(Settings(plain: false), isOutputRedirected: false)
            .Should().BeFalse();
    }
}
