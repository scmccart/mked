namespace Mked.Console;

/// <summary>
/// Windows-native console input source. Reads from <c>CONIN$</c> via
/// <c>ReadConsoleInputW</c> so that both keystrokes and mouse-wheel events are delivered.
/// On construction the input mode is adjusted to enable mouse input and disable Quick-Edit
/// mode (which would otherwise swallow wheel events for text selection).
/// <c>ENABLE_PROCESSED_INPUT</c> is <b>cleared</b> so that Ctrl+C and Ctrl+X arrive as raw
/// <c>KEY_EVENT</c>s that the editor can intercept for copy/cut and dirty-aware quit.
/// As a consequence <see cref="System.Console.CancelKeyPress"/> does <b>not</b> fire on Windows
/// while this source is active — the editor handles Ctrl+C directly. SIGTERM is still
/// delivered via <see cref="TerminalLifecycle"/>'s POSIX signal registration.
/// </summary>
internal sealed class WindowsConsoleInputSource : IConsoleInputSource
{
    private readonly nint _hIn;
    private readonly uint _savedMode;
    private bool _disposed;

    // Pending repeat keypresses from a single ReadConsoleInputW with wRepeatCount > 1.
    private WindowsConsoleInterop.InputRecord _pendingRecord;
    private int _pendingRepeats; // remaining count after the first delivery

    internal WindowsConsoleInputSource()
    {
        _hIn = WindowsConsoleInterop.GetStdHandle(WindowsConsoleInterop.STD_INPUT_HANDLE);

        WindowsConsoleInterop.GetConsoleMode(_hIn, out _savedMode);

        // Enable extended flags + mouse input; clear Quick-Edit mode (would swallow wheel events)
        // and clear ENABLE_PROCESSED_INPUT so Ctrl+C/Ctrl+X arrive as raw KEY_EVENTs that
        // the editor can intercept for copy/cut. Ctrl+Q remains the quit shortcut.
        uint newMode = (_savedMode | WindowsConsoleInterop.ENABLE_EXTENDED_FLAGS
                                   | WindowsConsoleInterop.ENABLE_MOUSE_INPUT)
                     & ~WindowsConsoleInterop.ENABLE_QUICK_EDIT_MODE
                     & ~WindowsConsoleInterop.ENABLE_PROCESSED_INPUT;

        WindowsConsoleInterop.SetConsoleMode(_hIn, newMode);
    }

    /// <inheritdoc/>
    public bool TryRead(out InputEvent ev)
    {
        // Drain a pending repeated key first.
        if (_pendingRepeats > 0)
        {
            var translated = WindowsInputTranslator.Translate(in _pendingRecord);
            _pendingRepeats--;
            if (translated.HasValue)
            {
                ev = translated.Value;
                return true;
            }
        }

        // Check if any records are waiting.
        if (!WindowsConsoleInterop.GetNumberOfConsoleInputEvents(_hIn, out uint available)
            || available == 0)
        {
            ev = default;
            return false;
        }

        if (!WindowsConsoleInterop.ReadConsoleInputW(_hIn, out var record, 1, out uint read)
            || read == 0)
        {
            ev = default;
            return false;
        }

        // For key events with repeat count > 1, stash the record and enqueue repeats.
        if (record.EventType == WindowsConsoleInterop.KEY_EVENT
            && record.KeyEvent.bKeyDown != 0
            && record.KeyEvent.wRepeatCount > 1)
        {
            _pendingRecord = record;
            _pendingRepeats = record.KeyEvent.wRepeatCount - 1;
        }

        var result = WindowsInputTranslator.Translate(in record);
        if (result.HasValue)
        {
            ev = result.Value;
            return true;
        }

        // Discard unhandled records (mouse move, focus events, etc.) without blocking.
        ev = default;
        return false;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        WindowsConsoleInterop.SetConsoleMode(_hIn, _savedMode);
    }
}
