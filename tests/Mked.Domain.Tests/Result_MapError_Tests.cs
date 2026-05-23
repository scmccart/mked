namespace Mked.Domain.Tests;

public class Result_MapError_Tests
{
    [Fact]
    public void OnErr_TransformsError()
    {
        var result = Result.Err<int, string>("msg").MapError(e => e.Length);

        result.Should().Be(Result.Err<int, int>(3));
    }

    [Fact]
    public void OnOk_PassesThroughUnchanged()
    {
        var result = Result.Ok<int, string>(7).MapError(e => e.Length);

        result.Should().Be(Result.Ok<int, int>(7));
    }
}
