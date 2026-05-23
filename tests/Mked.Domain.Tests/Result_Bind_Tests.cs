namespace Mked.Domain.Tests;

public class Result_Bind_Tests
{
    [Fact]
    public void OnOk_InvokesBinder()
    {
        var result = Result.Ok<int, string>(4).Bind(x => Result.Ok<int, string>(x + 1));

        result.Should().Be(Result.Ok<int, string>(5));
    }

    [Fact]
    public void OnOk_WhenBinderReturnsErr_PropagatesErr()
    {
        var result = Result.Ok<int, string>(4).Bind(_ => Result.Err<int, string>("bound error"));

        result.Should().Be(Result.Err<int, string>("bound error"));
    }

    [Fact]
    public void OnErr_ShortCircuits_DoesNotInvokeBinder()
    {
        var invoked = false;

        Result.Err<int, string>("original").Bind(x =>
        {
            invoked = true;
            return Result.Ok<int, string>(x);
        });

        invoked.Should().BeFalse();
    }

    [Fact]
    public void OnErr_PassesThroughOriginalError()
    {
        var result = Result.Err<int, string>("original").Bind(x => Result.Ok<int, string>(x));

        result.Should().Be(Result.Err<int, string>("original"));
    }
}
