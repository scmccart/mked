namespace Mked.Controls.Tests;

/// <summary>
/// Shared test double for <see cref="IEditorObserver"/>. Records the most recent call
/// for each notification method and counts total invocations.
/// </summary>
internal sealed class SpyObserver : IEditorObserver
{
    /// <summary>Buffer text from the most recent <see cref="OnBufferChanged"/> call, or <see langword="null"/> if never called.</summary>
    public string? LastBuffer { get; private set; }

    /// <summary>Cursor position from the most recent <see cref="OnCursorMoved"/> call, or <see langword="null"/> if never called.</summary>
    public CursorPosition? LastCursor { get; private set; }

    /// <summary>Total number of <see cref="OnBufferChanged"/> calls received.</summary>
    public int BufferCallCount { get; private set; }

    /// <summary>Total number of <see cref="OnCursorMoved"/> calls received.</summary>
    public int CursorCallCount { get; private set; }

    /// <inheritdoc/>
    public void OnBufferChanged(string newBuffer)
    {
        LastBuffer = newBuffer;
        BufferCallCount++;
    }

    /// <inheritdoc/>
    public void OnCursorMoved(CursorPosition position)
    {
        LastCursor = position;
        CursorCallCount++;
    }
}
