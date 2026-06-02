namespace Mked.Application.Tests.Fakes;

internal sealed class FakeFileWriter : IFileWriter
{
    private Result<Unit, MkedError> _result = Result.Ok<Unit, MkedError>(Unit.Value);

    public List<(string Path, string Content)> Writes { get; } = [];

    public void SetError(MkedError error) =>
        _result = Result.Err<Unit, MkedError>(error);

    public Task<Result<Unit, MkedError>> WriteAsync(string path, string content)
    {
        Writes.Add((path, content));
        return Task.FromResult(_result);
    }
}
