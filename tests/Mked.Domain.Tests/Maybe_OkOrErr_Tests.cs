namespace Mked.Domain.Tests;

public class Maybe_OkOrErr_Tests
{
    [Fact]
    public void OnSome_ReturnsOk()
    {
        var result = Maybe.Some(5).OkOrErr("missing");

        result.Should().Be(Result.Ok<int, string>(5));
    }

    [Fact]
    public void OnNone_ReturnsErr()
    {
        var result = Maybe.None<int>().OkOrErr("missing");

        result.Should().Be(Result.Err<int, string>("missing"));
    }
}
