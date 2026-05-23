using System.IO;
using Mked.Infrastructure;

namespace Mked.Infrastructure.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class FileSystemReader_ReadAsync_Tests : IDisposable
{
    private readonly FileSystemReader _reader = new();
    private readonly List<string> _pathsToClean = [];

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private string UniqueTempFile()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _pathsToClean.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (string path in _pathsToClean)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // best-effort cleanup — test isolation should not fail the suite
            }
        }
    }

    // -----------------------------------------------------------------------
    // Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ExistingFile_ReturnsOkWithContent()
    {
        // Arrange
        string path = UniqueTempFile();
        const string expected = "# Hello\n\nThis is test content.";
        await File.WriteAllTextAsync(path, expected);

        // Act
        Result<string, MkedError> result = await _reader.ReadAsync(path);

        // Assert
        result.IsOk.Should().BeTrue();
        string actual = ((Result<string, MkedError>.Ok)result).Value;
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task Utf8NonAsciiFile_RoundTripsCorrectly()
    {
        // Arrange
        string path = UniqueTempFile();
        const string expected = "Héllo wörld";
        await File.WriteAllTextAsync(path, expected, System.Text.Encoding.UTF8);

        // Act
        Result<string, MkedError> result = await _reader.ReadAsync(path);

        // Assert
        result.IsOk.Should().BeTrue();
        string actual = ((Result<string, MkedError>.Ok)result).Value;
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task NonExistentPath_ReturnsErrWithIoError()
    {
        // Arrange
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        // Act
        Result<string, MkedError> result = await _reader.ReadAsync(path);

        // Assert
        result.IsErr.Should().BeTrue();
        MkedError error = ((Result<string, MkedError>.Err)result).Error;
        MkedError.IoError ioError = error.Should().BeOfType<MkedError.IoError>().Subject;
        ioError.Path.Should().Be(path);
    }

    [Fact(Skip = "Requires non-admin process")]
    public async Task RestrictedFile_ReturnsErrWithIoError()
    {
        // Skipped: programmatically setting deny ACLs on Windows requires elevated
        // privileges or platform-specific P/Invoke that is out of scope for this
        // test environment. Run in a non-admin process with icacls-restricted file
        // to exercise the UnauthorizedAccessException path.
        await Task.CompletedTask;
    }
}
