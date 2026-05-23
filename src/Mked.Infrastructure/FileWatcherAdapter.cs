using System.IO;
using System.Threading;
using System.Threading.Channels;
using Mked.Domain;

namespace Mked.Infrastructure;

/// <summary>
/// Watches a single file for changes and surfaces notifications as an
/// <see cref="IAsyncEnumerable{T}"/> of file paths via a bounded channel,
/// automatically debouncing rapid successive writes.
/// </summary>
public sealed class FileWatcherAdapter : IFileWatcher
{
    private readonly FileSystemWatcher _watcher;
    private readonly Channel<string> _channel;
    private readonly string _filePath;

    /// <summary>
    /// Initialises a new <see cref="FileWatcherAdapter"/> that watches
    /// <paramref name="filePath"/> for last-write and rename events.
    /// </summary>
    /// <param name="filePath">Absolute path to the file to watch.</param>
    public FileWatcherAdapter(string filePath)
    {
        _filePath = filePath;

        string directory = Path.GetDirectoryName(filePath) ?? ".";
        string filter = Path.GetFileName(filePath);

        _channel = Channel.CreateBounded<string>(
            new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

        _watcher = new FileSystemWatcher(directory, filter)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };

        _watcher.Changed += OnEvent;
        _watcher.Created += OnEvent;
        _watcher.Renamed += OnEvent;
        _watcher.Error += OnError;

        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Yields the path of the watched file each time it changes on disk. The enumeration
    /// completes when the watcher is disposed or <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    public IAsyncEnumerable<string> WatchAsync(CancellationToken cancellationToken = default)
        => _channel.Reader.ReadAllAsync(cancellationToken);

    /// <inheritdoc/>
    public void Dispose()
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
        _channel.Writer.TryComplete();
    }

    private void OnEvent(object sender, FileSystemEventArgs e)
        => _channel.Writer.TryWrite(_filePath);

    private void OnError(object sender, ErrorEventArgs e)
        => System.Diagnostics.Trace.TraceWarning(
            $"FileWatcherAdapter: watcher error for {_filePath}: {e.GetException().Message}");
}
