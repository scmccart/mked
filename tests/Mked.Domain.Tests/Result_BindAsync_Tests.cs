namespace Mked.Domain.Tests;

public class Result_BindAsync_Tests
{
    [Fact]
    public async Task OnOk_ChainsAsyncBinder()
    {
        var result = await Task.FromResult(Result.Ok<int, string>(3))
            .BindAsync(x => Task.FromResult(Result.Ok<int, string>(x * 4)));

        result.Should().Be(Result.Ok<int, string>(12));
    }

    [Fact]
    public async Task OnErr_ShortCircuits_DoesNotInvokeBinder()
    {
        var invoked = false;

        await Task.FromResult(Result.Err<int, string>("stop"))
            .BindAsync(x =>
            {
                invoked = true;
                return Task.FromResult(Result.Ok<int, string>(x));
            });

        invoked.Should().BeFalse();
    }

    [Fact]
    public async Task OnOk_WhenBinderReturnsErr_PropagatesErr()
    {
        var result = await Task.FromResult(Result.Ok<int, string>(1))
            .BindAsync(_ => Task.FromResult(Result.Err<int, string>("async fail")));

        result.Should().Be(Result.Err<int, string>("async fail"));
    }
}
