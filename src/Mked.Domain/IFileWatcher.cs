namespace Mked.Domain;

/// <summary>Provides asynchronous file-change notifications for a single watched file.</summary>
public interface IFileWatcher : IDisposable
{
    /// <summary>
    /// Yields the path of the watched file each time it changes on disk. The enumeration
    /// completes when the watcher is disposed or <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    public IAsyncEnumerable<string> WatchAsync(CancellationToken cancellationToken = default);
}
