namespace Mked.Domain.Tests;

public class Option_Factory_Tests
{
    [Fact]
    public void Some_CreatesPresentOption()
    {
        var option = Option.Some(42);

        option.IsSome.Should().BeTrue();
        option.IsNone.Should().BeFalse();
    }

    [Fact]
    public void None_CreatesAbsentOption()
    {
        var option = Option.None<int>();

        option.IsSome.Should().BeFalse();
        option.IsNone.Should().BeTrue();
    }

    [Fact]
    public void Some_StructurallyEqualsSomeWithSameValue()
    {
        Option.Some(5).Should().Be(Option.Some(5));
    }

    [Fact]
    public void None_StructurallyEqualsNone()
    {
        Option.None<int>().Should().Be(Option.None<int>());
    }
}
