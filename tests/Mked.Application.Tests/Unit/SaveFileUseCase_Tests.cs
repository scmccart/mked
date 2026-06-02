using Mked.Application.Tests.Fakes;

namespace Mked.Application.Tests.UnitTests;

public sealed class SaveFileUseCase_Tests
{
    private readonly FakeFileWriter _writer = new();
    private readonly SaveFileUseCase _sut;

    public SaveFileUseCase_Tests() => _sut = new SaveFileUseCase(_writer);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task EmptyOrWhitespacePath_ReturnsValidationError_WithoutCallingWriter(string path)
    {
        // Act
        var result = await _sut.ExecuteAsync(path, "content");

        // Assert
        var err = result as Result<Unit, MkedError>.Err;
        err.Should().NotBeNull();
        err!.Error.Should().BeOfType<MkedError.ValidationError>();
        var ve = (MkedError.ValidationError)err.Error;
        ve.Field.Should().Be("path");
        _writer.Writes.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidPath_CallsWriterWithExactArguments()
    {
        // Act
        var result = await _sut.ExecuteAsync("/tmp/out.md", "# Title");

        // Assert
        result.IsOk.Should().BeTrue();
        _writer.Writes.Should().ContainSingle()
            .Which.Should().Be(("/tmp/out.md", "# Title"));
    }

    [Fact]
    public async Task WriterReturnsIoError_PassesThroughAsErr()
    {
        // Arrange
        var error = new MkedError.IoError("/tmp/out.md", "Permission denied");
        _writer.SetError(error);

        // Act
        var result = await _sut.ExecuteAsync("/tmp/out.md", "content");

        // Assert
        var err = result as Result<Unit, MkedError>.Err;
        err.Should().NotBeNull();
        err!.Error.Should().Be(error);
    }
}
