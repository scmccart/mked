namespace Mked.Domain.Tests;

public class Option_Match_Tests
{
    [Fact]
    public void OnSome_CallsOnSome()
    {
        var output = Option.Some(8).Match(onSome: x => x * 3, onNone: () => -1);

        output.Should().Be(24);
    }

    [Fact]
    public void OnNone_CallsOnNone()
    {
        var output = Option.None<int>().Match(onSome: x => x * 3, onNone: () => -1);

        output.Should().Be(-1);
    }
}
