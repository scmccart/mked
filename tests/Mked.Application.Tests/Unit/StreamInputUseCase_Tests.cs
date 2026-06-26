using Mked.Application.Tests.Fakes;

namespace Mked.Application.Tests.UnitTests;

public sealed class StreamInputUseCase_Tests
{
    private static async Task<List<Result<StreamedDocument, MkedError>>> CollectAsync(
        IAsyncEnumerable<Result<StreamedDocument, MkedError>> source)
    {
        var results = new List<Result<StreamedDocument, MkedError>>();
        await foreach (var item in source)
            results.Add(item);
        return results;
    }

    [Fact]
    public async Task TwoChunks_YieldsTwoCumulativeDocuments()
    {
        // Arrange
        var reader = new FakeInputReader();
        reader.Add("# Heading");
        reader.Add("paragraph text");
        var sut = new StreamInputUseCase(reader);

        // Act
        var results = await CollectAsync(sut.ExecuteAsync());

        // Assert
        results.Should().HaveCount(2);
        results[0].Should().BeOfType<Result<StreamedDocument, MkedError>.Ok>();
        results[1].Should().BeOfType<Result<StreamedDocument, MkedError>.Ok>();

        var firstDoc = ((Result<StreamedDocument, MkedError>.Ok)results[0]).Value;
        var secondDoc = ((Result<StreamedDocument, MkedError>.Ok)results[1]).Value;
        firstDoc.Parsed.Blocks.Count.Should().BeGreaterThan(0);
        secondDoc.Parsed.Blocks.Count.Should().BeGreaterThan(firstDoc.Parsed.Blocks.Count);
    }

    [Fact]
    public async Task TwoChunks_SourceAccumulatesAcrossChunks()
    {
        // Arrange
        var reader = new FakeInputReader();
        reader.Add("# Heading");
        reader.Add("paragraph text");
        var sut = new StreamInputUseCase(reader);

        // Act
        var results = await CollectAsync(sut.ExecuteAsync());

        // Assert
        var firstOk = (Result<StreamedDocument, MkedError>.Ok)results[0];
        var secondOk = (Result<StreamedDocument, MkedError>.Ok)results[1];
        firstOk.Value.Source.Should().Contain("# Heading");
        secondOk.Value.Source.Should().Contain("# Heading");
        secondOk.Value.Source.Should().Contain("paragraph text");
    }

    [Fact]
    public async Task EmptyInput_CompletesImmediatelyWithNoItems()
    {
        // Arrange
        var reader = new FakeInputReader();
        var sut = new StreamInputUseCase(reader);

        // Act
        var results = await CollectAsync(sut.ExecuteAsync());

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task MidStreamError_SurfacesAsErrWithoutAffectingPriorOkItems()
    {
        // Arrange
        var reader = new FakeInputReader();
        reader.Add("# Heading");
        reader.AddError(new MkedError.StreamError("broken pipe"));
        reader.Add("more text");
        var sut = new StreamInputUseCase(reader);

        // Act
        var results = await CollectAsync(sut.ExecuteAsync());

        // Assert
        results.Should().HaveCount(3);
        results[0].Should().BeOfType<Result<StreamedDocument, MkedError>.Ok>();
        results[1].Should().BeOfType<Result<StreamedDocument, MkedError>.Err>();
        var err = (Result<StreamedDocument, MkedError>.Err)results[1];
        err.Error.Should().BeOfType<MkedError.StreamError>();
        results[2].Should().BeOfType<Result<StreamedDocument, MkedError>.Ok>();
    }

    [Fact]
    public async Task Cancellation_PropagatesOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var reader = new CancellableFakeInputReader(cts);
        var sut = new StreamInputUseCase(reader);

        // Act
        Func<Task> act = async () =>
        {
            await foreach (var _ in sut.ExecuteAsync(cts.Token)) { }
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private sealed class CancellableFakeInputReader(CancellationTokenSource cts) : IInputReader
    {
        public async IAsyncEnumerable<Result<string, MkedError>> ReadChunksAsync()
        {
            await cts.CancelAsync();
            await Task.Yield();
            yield return Result.Ok<string, MkedError>("should not be reached");
        }
    }
}
