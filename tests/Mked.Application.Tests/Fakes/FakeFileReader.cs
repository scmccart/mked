namespace Mked.Application.Tests.Fakes;

internal sealed class FakeFileReader : IFileReader
{
    private readonly Dictionary<string, Result<string, MkedError>> _store = new();

    public void AddSuccess(string path, string content) =>
        _store[path] = Result.Ok<string, MkedError>(content);

    public void AddError(string path, MkedError error) =>
        _store[path] = Result.Err<string, MkedError>(error);

    public Task<Result<string, MkedError>> ReadAsync(string path) =>
        Task.FromResult(_store.TryGetValue(path, out var result)
            ? result
            : Result.Err<string, MkedError>(new MkedError.IoError(path, "File not found")));
}
