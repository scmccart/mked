namespace Mked.Domain.Tests;

public class Result_Unwrap_Tests
{
    [Fact]
    public void OnOk_ReturnsValue()
    {
        var value = Result.Ok<int, string>(99).Unwrap();

        value.Should().Be(99);
    }

    [Fact]
    public void OnErr_ThrowsInvalidOperationException()
    {
        Action act = () => Result.Err<int, string>("error").Unwrap();

        act.Should().Throw<InvalidOperationException>();
    }
}
