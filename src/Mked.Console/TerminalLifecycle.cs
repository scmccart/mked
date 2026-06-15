using System.Runtime.InteropServices;

namespace Mked.Console;

/// <summary>
/// Registers OS-level cancellation signals (Ctrl+C / SIGTERM) and guarantees cursor
/// restoration on abnormal exit. Dispose to unregister handlers and restore the cursor.
/// </summary>
public sealed class TerminalLifecycle : IDisposable
{
    private readonly CancellationTokenSource _cts;
    private readonly PosixSignalRegistration? _sigterm;
    private bool _disposed;

    /// <summary>
    /// Hides the cursor and registers signal handlers that cancel <paramref name="cts"/>.
    /// </summary>
    public TerminalLifecycle(CancellationTokenSource cts)
    {
        _cts = cts;
        System.Console.CursorVisible = false;
        System.Console.CancelKeyPress += OnCancelKeyPress;

        // PosixSignalRegistration is available on .NET 6+ including Windows.
        try
        {
            _sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnSignal);
        }
        catch (PlatformNotSupportedException) { }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        System.Console.CancelKeyPress -= OnCancelKeyPress;
        _sigterm?.Dispose();
        System.Console.CursorVisible = true;
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true; // prevent default process kill; we perform clean shutdown
        _cts.Cancel();
    }

    private void OnSignal(PosixSignalContext ctx)
    {
        ctx.Cancel = true;
        _cts.Cancel();
    }
}
