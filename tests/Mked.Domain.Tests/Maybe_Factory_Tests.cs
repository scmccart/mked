namespace Mked.Domain.Tests;

public class Maybe_Factory_Tests
{
    [Fact]
    public void Some_CreatesPresentMaybe()
    {
        var maybe = Maybe.Some(42);

        maybe.IsSome.Should().BeTrue();
        maybe.IsNone.Should().BeFalse();
    }

    [Fact]
    public void None_CreatesAbsentMaybe()
    {
        var maybe = Maybe.None<int>();

        maybe.IsSome.Should().BeFalse();
        maybe.IsNone.Should().BeTrue();
    }

    [Fact]
    public void Some_StructurallyEqualsSomeWithSameValue()
    {
        Maybe.Some(5).Should().Be(Maybe.Some(5));
    }

    [Fact]
    public void None_StructurallyEqualsNone()
    {
        Maybe.None<int>().Should().Be(Maybe.None<int>());
    }
}
