namespace Mked.Domain.Tests;

public class Maybe_UnwrapOr_Tests
{
    [Fact]
    public void OnSome_ReturnsValue()
    {
        var value = Maybe.Some(11).UnwrapOr(0);

        value.Should().Be(11);
    }

    [Fact]
    public void OnNone_ReturnsFallback()
    {
        var value = Maybe.None<int>().UnwrapOr(99);

        value.Should().Be(99);
    }
}
