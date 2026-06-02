using Mked.Application.Tests.Fakes;

namespace Mked.Application.Tests.UnitTests;

public sealed class OpenFileUseCase_Tests
{
    private readonly FakeFileReader _reader = new();
    private readonly OpenFileUseCase _sut;

    public OpenFileUseCase_Tests() => _sut = new OpenFileUseCase(_reader);

    [Fact]
    public async Task HeadingContent_ReturnsOkWithParsedDocument()
    {
        // Arrange
        _reader.AddSuccess("/tmp/file.md", "# Hello");

        // Act
        var result = await _sut.ExecuteAsync("/tmp/file.md");

        // Assert
        var ok = result as Result<OpenedFile, MkedError>.Ok;
        ok.Should().NotBeNull();
        ok!.Value.Source.Should().Be("# Hello");
        ok!.Value.Parsed.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyContent_ReturnsOkWithEmptyDocument()
    {
        // Arrange
        _reader.AddSuccess("/tmp/empty.md", "");

        // Act
        var result = await _sut.ExecuteAsync("/tmp/empty.md");

        // Assert
        var ok = result as Result<OpenedFile, MkedError>.Ok;
        ok.Should().NotBeNull();
        ok!.Value.Source.Should().BeEmpty();
        ok!.Value.Parsed.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task ReaderReturnsIoError_PassesThroughAsErr()
    {
        // Arrange
        var error = new MkedError.IoError("/tmp/missing.md", "File not found");
        _reader.AddError("/tmp/missing.md", error);

        // Act
        var result = await _sut.ExecuteAsync("/tmp/missing.md");

        // Assert
        var err = result as Result<OpenedFile, MkedError>.Err;
        err.Should().NotBeNull();
        err!.Error.Should().Be(error);
    }
}
