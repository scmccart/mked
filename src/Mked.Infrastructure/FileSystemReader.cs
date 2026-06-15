using System.IO;
using System.Text;
using Mked.Domain;

namespace Mked.Infrastructure;

/// <summary>
/// Reads text content from the file system, mapping I/O exceptions to
/// <see cref="MkedError.IoError"/> values rather than propagating them as exceptions.
/// </summary>
public sealed class FileSystemReader : IFileReader
{
    /// <summary>
    /// Reads all text from <paramref name="path"/> using UTF-8 encoding.
    /// </summary>
    /// <param name="path">Absolute or relative path to the file to read.</param>
    /// <returns>
    /// <c>Ok(content)</c> on success; <c>Err(<see cref="MkedError.IoError"/>)</c> when the
    /// file is not found, access is denied, or any other I/O error occurs.
    /// </returns>
    public async Task<Result<string, MkedError>> ReadAsync(string path)
    {
        try
        {
            string content = await File.ReadAllTextAsync(path, Encoding.UTF8);
            return Result.Ok<string, MkedError>(content);
        }
        catch (FileNotFoundException)
        {
            return Result.Err<string, MkedError>(new MkedError.IoError(path, "File not found", IoKind.ReadNotFound));
        }
        catch (UnauthorizedAccessException)
        {
            return Result.Err<string, MkedError>(new MkedError.IoError(path, "Access denied", IoKind.ReadAccessDenied));
        }
        catch (IOException ex)
        {
            return Result.Err<string, MkedError>(new MkedError.IoError(path, ex.Message, IoKind.ReadNotFound));
        }
    }
}
