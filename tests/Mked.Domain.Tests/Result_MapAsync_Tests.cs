namespace Mked.Domain.Tests;

public class Result_MapAsync_Tests
{
    [Fact]
    public async Task OnOk_TransformsValue()
    {
        var result = await Task.FromResult(Result.Ok<int, string>(5)).MapAsync(x => x * 10);

        result.Should().Be(Result.Ok<int, string>(50));
    }

    [Fact]
    public async Task OnErr_PassesThroughUnchanged()
    {
        var result = await Task.FromResult(Result.Err<int, string>("e")).MapAsync(x => x * 10);

        result.Should().Be(Result.Err<int, string>("e"));
    }
}
