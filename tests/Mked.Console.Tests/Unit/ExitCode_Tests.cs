namespace Mked.Console.Tests.Unit;

public sealed class ExitCode_Tests
{
    [Fact]
    public void Success_IsZero() => ExitCode.Success.Should().Be(0);

    [Fact]
    public void Usage_IsOne() => ExitCode.Usage.Should().Be(1);

    [Fact]
    public void Io_IsTwo() => ExitCode.Io.Should().Be(2);

    [Fact]
    public void Parse_IsThree() => ExitCode.Parse.Should().Be(3);
}
