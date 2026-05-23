namespace Mked.Domain;

/// <summary>Writes text content to a file on the file system.</summary>
public interface IFileWriter
{
    /// <summary>
    /// Writes <paramref name="content"/> to <paramref name="path"/>, returning <c>Ok</c>
    /// on success or <c>Err</c> with an <see cref="MkedError.IoError"/> on failure.
    /// </summary>
    public Task<Result<Unit, MkedError>> WriteAsync(string path, string content);
}
