namespace Mked.Domain.Tests;

public class Option_UnwrapOr_Tests
{
    [Fact]
    public void OnSome_ReturnsValue()
    {
        var value = Option.Some(11).UnwrapOr(0);

        value.Should().Be(11);
    }

    [Fact]
    public void OnNone_ReturnsFallback()
    {
        var value = Option.None<int>().UnwrapOr(99);

        value.Should().Be(99);
    }
}
