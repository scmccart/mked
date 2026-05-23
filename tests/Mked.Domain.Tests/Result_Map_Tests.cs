namespace Mked.Domain.Tests;

public class Result_Map_Tests
{
    [Fact]
    public void OnOk_TransformsValue()
    {
        var result = Result.Ok<int, string>(10).Map(x => x * 3);

        result.Should().Be(Result.Ok<int, string>(30));
    }

    [Fact]
    public void OnOk_CanChangeType()
    {
        var result = Result.Ok<int, string>(5).Map(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture));

        result.Should().Be(Result.Ok<string, string>("5"));
    }

    [Fact]
    public void OnErr_PassesThroughUnchanged()
    {
        var result = Result.Err<int, string>("fail").Map(x => x * 3);

        result.Should().Be(Result.Err<int, string>("fail"));
    }
}
