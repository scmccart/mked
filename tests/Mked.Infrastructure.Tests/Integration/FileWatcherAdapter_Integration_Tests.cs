using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mked.Infrastructure;

namespace Mked.Infrastructure.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class FileWatcherAdapter_WatchAsync_Tests : IDisposable
{
    private readonly List<string> _tempFiles = new();
    private readonly List<FileWatcherAdapter> _adapters = new();

    private string CreateTempFile(string initialContent = "initial")
    {
        string path = Path.GetTempFileName();
        File.WriteAllText(path, initialContent);
        _tempFiles.Add(path);
        return path;
    }

    private FileWatcherAdapter CreateAdapter(string filePath)
    {
        var adapter = new FileWatcherAdapter(filePath);
        _adapters.Add(adapter);
        return adapter;
    }

    public void Dispose()
    {
        foreach (FileWatcherAdapter adapter in _adapters)
            adapter.Dispose();

        foreach (string path in _tempFiles)
        {
            try { File.Delete(path); } catch { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public async Task FileModified_YieldsNotification()
    {
        // Arrange
        string filePath = CreateTempFile();
        FileWatcherAdapter adapter = CreateAdapter(filePath);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        var enumerator = adapter.WatchAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        await Task.Delay(100, cts.Token);
        await File.WriteAllTextAsync(filePath, "updated content", cts.Token);

        bool moved = await enumerator.MoveNextAsync();

        // Assert
        moved.Should().BeTrue();
        enumerator.Current.Should().Be(filePath);

        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task RapidWrites_YieldsSingleNotification()
    {
        // Arrange
        string filePath = CreateTempFile();
        FileWatcherAdapter adapter = CreateAdapter(filePath);

        // Act — write 5 times rapidly before collecting
        for (int i = 0; i < 5; i++)
            await File.WriteAllTextAsync(filePath, $"write {i}");

        List<string> notifications = await CollectForAsync(
            adapter.WatchAsync(),
            TimeSpan.FromMilliseconds(500));

        // Assert
        notifications.Count.Should().Be(1);
    }

    [Fact]
    public async Task Dispose_StopsNotifications()
    {
        // Arrange
        string filePath = CreateTempFile();
        FileWatcherAdapter adapter = CreateAdapter(filePath);

        // Act
        adapter.Dispose();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        List<string> notifications = await CollectForAsync(adapter.WatchAsync(), TimeSpan.FromSeconds(2));

        // Assert — enumeration completes immediately (channel completed on dispose)
        notifications.Count.Should().Be(0);
    }

    private static async Task<List<string>> CollectForAsync(
        IAsyncEnumerable<string> source, TimeSpan duration)
    {
        using var cts = new CancellationTokenSource(duration);
        var items = new List<string>();
        try
        {
            await foreach (var item in source.WithCancellation(cts.Token))
                items.Add(item);
        }
        catch (OperationCanceledException) { }
        return items;
    }
}
