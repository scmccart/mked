namespace Mked.Domain.Tests;

public class Maybe_Bind_Tests
{
    [Fact]
    public void OnSome_InvokesBinder()
    {
        var result = Maybe.Some(4).Bind(x => Maybe.Some(x + 10));

        result.Should().Be(Maybe.Some(14));
    }

    [Fact]
    public void OnSome_WhenBinderReturnsNone_ReturnsNone()
    {
        var result = Maybe.Some(4).Bind(_ => Maybe.None<int>());

        result.Should().Be(Maybe.None<int>());
    }

    [Fact]
    public void OnNone_ShortCircuits_DoesNotInvokeBinder()
    {
        var invoked = false;

        Maybe.None<int>().Bind(x =>
        {
            invoked = true;
            return Maybe.Some(x);
        });

        invoked.Should().BeFalse();
    }
}
