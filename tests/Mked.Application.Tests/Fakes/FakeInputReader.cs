namespace Mked.Application.Tests.Fakes;

internal sealed class FakeInputReader : IInputReader
{
    private readonly List<Result<string, MkedError>> _chunks = [];

    public void Add(string chunk) =>
        _chunks.Add(Result.Ok<string, MkedError>(chunk));

    public void AddError(MkedError error) =>
        _chunks.Add(Result.Err<string, MkedError>(error));

    public async IAsyncEnumerable<Result<string, MkedError>> ReadChunksAsync()
    {
        foreach (var chunk in _chunks)
        {
            await Task.Yield();
            yield return chunk;
        }
    }
}
