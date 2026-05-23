using Mked.Infrastructure;

namespace Mked.Infrastructure.Tests.UnitTests;

public sealed class StdinInputReader_ReadChunksAsync_Tests
{
    private static async Task<List<Result<string, MkedError>>> CollectAsync(
        IAsyncEnumerable<Result<string, MkedError>> source)
    {
        var results = new List<Result<string, MkedError>>();
        await foreach (var item in source)
            results.Add(item);
        return results;
    }

    [Fact]
    public async Task TwoLines_YieldsTwoOkResults()
    {
        // Arrange
        var reader = new StringReader("line1\nline2");
        var sut = new StdinInputReader(reader, isRedirected: true);

        // Act
        var results = await CollectAsync(sut.ReadChunksAsync());

        // Assert
        results.Should().HaveCount(2);

        var first = results[0] as Result<string, MkedError>.Ok;
        first.Should().NotBeNull();
        first!.Value.Should().Be("line1");

        var second = results[1] as Result<string, MkedError>.Ok;
        second.Should().NotBeNull();
        second!.Value.Should().Be("line2");
    }

    [Fact]
    public async Task EmptyReader_CompletesWithNoItems()
    {
        // Arrange
        var reader = new StringReader("");
        var sut = new StdinInputReader(reader, isRedirected: true);

        // Act
        var results = await CollectAsync(sut.ReadChunksAsync());

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task NotRedirected_CompletesWithNoItems()
    {
        // Arrange
        var reader = new StringReader("should not be read");
        var sut = new StdinInputReader(reader, isRedirected: false);

        // Act
        var results = await CollectAsync(sut.ReadChunksAsync());

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task IoExceptionOnRead_YieldsErrThenCompletes()
    {
        // Arrange
        var reader = new ThrowingTextReader();
        var sut = new StdinInputReader(reader, isRedirected: true);

        // Act
        var results = await CollectAsync(sut.ReadChunksAsync());

        // Assert
        results.Should().HaveCount(1);

        var err = results[0] as Result<string, MkedError>.Err;
        err.Should().NotBeNull();
        err!.Error.Should().BeOfType<MkedError.StreamError>();
    }

    private sealed class ThrowingTextReader : TextReader
    {
        public override Task<string?> ReadLineAsync() =>
            throw new IOException("broken pipe");
    }
}
