namespace Mked.Domain.Tests;

public class Result_Factory_Tests
{
    [Fact]
    public void Ok_CreatesSuccessResult()
    {
        var result = Result.Ok<int, string>(42);

        result.IsOk.Should().BeTrue();
        result.IsErr.Should().BeFalse();
    }

    [Fact]
    public void Err_CreatesFailedResult()
    {
        var result = Result.Err<int, string>("oops");

        result.IsOk.Should().BeFalse();
        result.IsErr.Should().BeTrue();
    }

    [Fact]
    public void Ok_StructurallyEqualsOkWithSameValue()
    {
        Result.Ok<int, string>(1).Should().Be(Result.Ok<int, string>(1));
    }

    [Fact]
    public void Err_StructurallyEqualsErrWithSameError()
    {
        Result.Err<int, string>("e").Should().Be(Result.Err<int, string>("e"));
    }
}
