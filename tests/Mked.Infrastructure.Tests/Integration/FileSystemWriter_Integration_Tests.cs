using System.IO;
using Mked.Infrastructure;
using Unit = Mked.Domain.Unit;

namespace Mked.Infrastructure.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class FileSystemWriter_WriteAsync_Tests : IDisposable
{
    private readonly FileSystemWriter _writer = new();
    private readonly List<string> _pathsToClean = [];

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private string TrackPath(string path)
    {
        _pathsToClean.Add(path);
        return path;
    }

    private string UniqueTempFile(string extension = ".md")
    {
        return TrackPath(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}"));
    }

    public void Dispose()
    {
        foreach (string path in _pathsToClean)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);

                // Remove any stray .tmp siblings left by failed writes
                string? dir = Path.GetDirectoryName(path);
                if (dir is not null && Directory.Exists(dir))
                {
                    foreach (string tmp in Directory.GetFiles(dir, ".*.tmp"))
                        File.Delete(tmp);

                    // Remove the directory itself only when it looks like one we created
                    if (!dir.Equals(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase)
                        && Directory.GetFileSystemEntries(dir).Length == 0)
                    {
                        Directory.Delete(dir, recursive: true);
                    }
                }
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
    public async Task NewFileInExistingDirectory_CreatesFileWithCorrectContent()
    {
        // Arrange
        string path = UniqueTempFile();
        const string expected = "# Hello\n\nThis is content.";

        // Act
        Result<Mked.Domain.Unit, MkedError> result = await _writer.WriteAsync(path, expected);

        // Assert
        result.IsOk.Should().BeTrue();
        File.Exists(path).Should().BeTrue();
        string actual = File.ReadAllText(path);
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task NewFileInMissingDirectory_CreatesDirectoryAndFile()
    {
        // Arrange
        string path = TrackPath(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "sub", "file.md"));
        const string expected = "# Nested\n\nContent here.";

        // Act
        Result<Mked.Domain.Unit, MkedError> result = await _writer.WriteAsync(path, expected);

        // Assert
        result.IsOk.Should().BeTrue();
        Directory.Exists(Path.GetDirectoryName(path)!).Should().BeTrue();
        File.Exists(path).Should().BeTrue();
        string actual = File.ReadAllText(path);
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task ExistingFile_OverwritesContent()
    {
        // Arrange
        string path = UniqueTempFile();
        await _writer.WriteAsync(path, "original content");
        const string expected = "# Updated\n\nReplaced content.";

        // Act
        Result<Mked.Domain.Unit, MkedError> result = await _writer.WriteAsync(path, expected);

        // Assert
        result.IsOk.Should().BeTrue();
        string actual = File.ReadAllText(path);
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task NoTempFileLeftAfterSuccess()
    {
        // Arrange
        string path = UniqueTempFile();
        string directory = Path.GetDirectoryName(path)!;

        // Act
        Result<Mked.Domain.Unit, MkedError> result = await _writer.WriteAsync(path, "content");

        // Assert
        result.IsOk.Should().BeTrue();
        string[] tmpFiles = Directory.GetFiles(directory, ".*.tmp");
        tmpFiles.Should().BeEmpty("no .tmp file should remain after a successful atomic write");
    }

    [Fact(Skip = "Platform-specific ACL setup required")]
    public async Task NoTempFileLeftAfterFailure()
    {
        // This test would verify that when WriteAllTextAsync or File.Move fails due to
        // insufficient permissions (e.g., writing into a directory whose ACL denies writes),
        // the FileSystemWriter still removes the .tmp file it created.
        //
        // To implement: create a directory, revoke write permission via icacls/ACL APIs,
        // call WriteAsync, then restore permissions and assert no .tmp files remain.
        //
        // Skipped because programmatically toggling ACLs in a reliable cross-account
        // manner requires elevated privileges or platform-specific P/Invoke that is out
        // of scope for the current test environment.
        await Task.CompletedTask;
    }
}
