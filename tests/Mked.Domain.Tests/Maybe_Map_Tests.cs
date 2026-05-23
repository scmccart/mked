namespace Mked.Domain.Tests;

public class Maybe_Map_Tests
{
    [Fact]
    public void OnSome_TransformsValue()
    {
        var result = Maybe.Some(3).Map(x => x * 2);

        result.Should().Be(Maybe.Some(6));
    }

    [Fact]
    public void OnSome_CanChangeType()
    {
        var result = Maybe.Some(7).Map(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture));

        result.Should().Be(Maybe.Some("7"));
    }

    [Fact]
    public void OnNone_ReturnsNone()
    {
        var result = Maybe.None<int>().Map(x => x * 2);

        result.Should().Be(Maybe.None<int>());
    }
}
