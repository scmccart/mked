namespace Mked.Console.Tests.Unit;

public sealed class ErrorPresenter_Tests
{
    [Fact]
    public void IoError_ReadNotFound_ReturnsExitCodeIo()
    {
        var error = new MkedError.IoError("/missing.md", "File not found", IoKind.ReadNotFound);
        ErrorPresenter.Show(error).Should().Be(ExitCode.Io);
    }

    [Fact]
    public void IoError_ReadAccessDenied_ReturnsExitCodeIo()
    {
        var error = new MkedError.IoError("/secret.md", "Access denied", IoKind.ReadAccessDenied);
        ErrorPresenter.Show(error).Should().Be(ExitCode.Io);
    }

    [Fact]
    public void IoError_WriteAccessDenied_ReturnsExitCodeIo()
    {
        var error = new MkedError.IoError("/out.md", "Access denied", IoKind.WriteAccessDenied);
        ErrorPresenter.Show(error).Should().Be(ExitCode.Io);
    }

    [Fact]
    public void IoError_WriteGeneric_ReturnsExitCodeIo()
    {
        var error = new MkedError.IoError("/out.md", "Disk full", IoKind.WriteGeneric);
        ErrorPresenter.Show(error).Should().Be(ExitCode.Io);
    }

    [Fact]
    public void IoError_ReadGeneric_ReturnsExitCodeIo()
    {
        var error = new MkedError.IoError("/data.md", "Sharing violation", IoKind.ReadGeneric);
        ErrorPresenter.Show(error).Should().Be(ExitCode.Io);
    }

    [Fact]
    public void ParseError_ReturnsExitCodeParse()
    {
        var error = new MkedError.ParseError(3, 7, "unexpected token");
        ErrorPresenter.Show(error).Should().Be(ExitCode.Parse);
    }

    [Fact]
    public void ValidationError_ReturnsExitCodeUsage()
    {
        var error = new MkedError.ValidationError("path", "Path cannot be empty.");
        ErrorPresenter.Show(error).Should().Be(ExitCode.Usage);
    }

    [Fact]
    public void StreamError_ReturnsExitCodeIo()
    {
        var error = new MkedError.StreamError("Pipe closed unexpectedly");
        ErrorPresenter.Show(error).Should().Be(ExitCode.Io);
    }
}
