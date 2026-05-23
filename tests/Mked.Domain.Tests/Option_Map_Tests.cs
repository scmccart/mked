namespace Mked.Domain.Tests;

public class Option_Map_Tests
{
    [Fact]
    public void OnSome_TransformsValue()
    {
        var result = Option.Some(3).Map(x => x * 2);

        result.Should().Be(Option.Some(6));
    }

    [Fact]
    public void OnSome_CanChangeType()
    {
        var result = Option.Some(7).Map(x => x.ToString());

        result.Should().Be(Option.Some("7"));
    }

    [Fact]
    public void OnNone_ReturnsNone()
    {
        var result = Option.None<int>().Map(x => x * 2);

        result.Should().Be(Option.None<int>());
    }
}
