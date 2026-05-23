namespace Mked.Domain;

/// <summary>Reads the text content of a file from the file system.</summary>
public interface IFileReader
{
    /// <summary>
    /// Reads all text from <paramref name="path"/>, returning <c>Ok</c> with the content
    /// or <c>Err</c> with an <see cref="MkedError.IoError"/> on failure.
    /// </summary>
    public Task<Result<string, MkedError>> ReadAsync(string path);
}
