namespace Mked.Domain.Tests;

public class Result_Match_Tests
{
    [Fact]
    public void OnOk_CallsOnOk_ReturnsItsValue()
    {
        var output = Result.Ok<int, string>(9).Match(onOk: x => x * 2, onErr: _ => -1);

        output.Should().Be(18);
    }

    [Fact]
    public void OnErr_CallsOnErr_ReturnsItsValue()
    {
        var output = Result.Err<int, string>("oops").Match(onOk: x => x * 2, onErr: e => e.Length);

        output.Should().Be(4);
    }
}
