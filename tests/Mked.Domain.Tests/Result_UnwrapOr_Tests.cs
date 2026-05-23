namespace Mked.Domain.Tests;

public class Result_UnwrapOr_Tests
{
    [Fact]
    public void OnOk_ReturnsValue()
    {
        var value = Result.Ok<int, string>(7).UnwrapOr(0);

        value.Should().Be(7);
    }

    [Fact]
    public void OnErr_ReturnsFallback()
    {
        var value = Result.Err<int, string>("nope").UnwrapOr(-1);

        value.Should().Be(-1);
    }
}
