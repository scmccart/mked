namespace Mked.Application;

/// <summary>Loads a Markdown file from disk and returns it ready for viewing or editing.</summary>
public sealed class OpenFileUseCase(IFileReader reader)
{
    /// <summary>
    /// Reads the file at <paramref name="path"/>, parses it, and returns an
    /// <see cref="OpenedFile"/> on success or a <see cref="MkedError.IoError"/> on failure.
    /// </summary>
    public Task<Result<OpenedFile, MkedError>> ExecuteAsync(string path) =>
        reader.ReadAsync(path)
              .MapAsync(source => new OpenedFile(source, MarkdownDocument.Parse(source)));
}
