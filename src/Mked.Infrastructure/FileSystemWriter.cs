using System.IO;
using System.Text;
using Mked.Domain;

namespace Mked.Infrastructure;

/// <summary>
/// Writes text content to the file system using an atomic write strategy:
/// content is written to a hidden temporary file in the same directory and then
/// renamed over the destination, minimising the window of partial writes.
/// </summary>
public sealed class FileSystemWriter : IFileWriter
{
    /// <summary>
    /// Atomically writes <paramref name="content"/> to <paramref name="path"/>.
    /// </summary>
    /// <remarks>
    /// <list type="number">
    ///   <item>Ensures the parent directory exists (creating it if necessary).</item>
    ///   <item>Writes to a hidden <c>.&lt;guid&gt;.tmp</c> file in the same directory.</item>
    ///   <item>Renames the temp file over the destination in a single atomic operation.</item>
    /// </list>
    /// On failure the temporary file is deleted on a best-effort basis and an
    /// <see cref="MkedError.IoError"/> is returned.
    /// </remarks>
    /// <param name="path">Absolute or relative destination path.</param>
    /// <param name="content">UTF-8 text content to write.</param>
    /// <returns>
    /// <c>Ok(Unit.Value)</c> on success; <c>Err(IoError)</c> on any
    /// <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/>.
    /// </returns>
    public async Task<Result<Unit, MkedError>> WriteAsync(string path, string content)
    {
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));
        ArgumentNullException.ThrowIfNull(content);
        string? rawDirectory = Path.GetDirectoryName(path);
        string directory = string.IsNullOrEmpty(rawDirectory) ? "." : rawDirectory;
        string tempPath = Path.Combine(directory, $".{Guid.NewGuid():N}.tmp");

        try
        {
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(tempPath, content, Encoding.UTF8);
            File.Move(tempPath, path, overwrite: true);
            return Result.Ok<Unit, MkedError>(Unit.Value);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            try { File.Delete(tempPath); } catch { /* best-effort cleanup */ }
            return Result.Err<Unit, MkedError>(new MkedError.IoError(path, ex.Message));
        }
    }
}
