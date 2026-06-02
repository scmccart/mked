namespace Mked.Application;

/// <summary>Validates and persists the editor buffer to disk.</summary>
public sealed class SaveFileUseCase(IFileWriter writer)
{
    /// <summary>
    /// Validates <paramref name="path"/>, then writes <paramref name="content"/> via the
    /// injected writer. Returns <see cref="MkedError.ValidationError"/> if the path is empty
    /// or whitespace — no I/O is attempted in that case.
    /// </summary>
    public Task<Result<Unit, MkedError>> ExecuteAsync(string path, string content)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult(Result.Err<Unit, MkedError>(
                new MkedError.ValidationError("path", "Path cannot be empty.")));
        }

        return writer.WriteAsync(path, content);
    }
}
