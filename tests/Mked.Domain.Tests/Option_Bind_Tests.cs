namespace Mked.Domain.Tests;

public class Option_Bind_Tests
{
    [Fact]
    public void OnSome_InvokesBinder()
    {
        var result = Option.Some(4).Bind(x => Option.Some(x + 10));

        result.Should().Be(Option.Some(14));
    }

    [Fact]
    public void OnSome_WhenBinderReturnsNone_ReturnsNone()
    {
        var result = Option.Some(4).Bind(_ => Option.None<int>());

        result.Should().Be(Option.None<int>());
    }

    [Fact]
    public void OnNone_ShortCircuits_DoesNotInvokeBinder()
    {
        var invoked = false;

        Option.None<int>().Bind(x =>
        {
            invoked = true;
            return Option.Some(x);
        });

        invoked.Should().BeFalse();
    }
}
