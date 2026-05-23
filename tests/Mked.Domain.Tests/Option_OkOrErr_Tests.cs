namespace Mked.Domain.Tests;

public class Option_OkOrErr_Tests
{
    [Fact]
    public void OnSome_ReturnsOk()
    {
        var result = Option.Some(5).OkOrErr("missing");

        result.Should().Be(Result.Ok<int, string>(5));
    }

    [Fact]
    public void OnNone_ReturnsErr()
    {
        var result = Option.None<int>().OkOrErr("missing");

        result.Should().Be(Result.Err<int, string>("missing"));
    }
}
