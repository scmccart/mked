namespace Mked.Console;

/// <summary>
/// Unix terminal input source that enables SGR 1006 mouse tracking and reads raw bytes from
/// stdin. Enters raw terminal mode (<c>cfmakeraw</c>) on construction so that escape sequences
/// — including mouse wheel reports — arrive immediately without line-buffering or echo.
/// Restores all settings on <see cref="Dispose"/>.
/// </summary>
internal sealed class UnixConsoleInputSource : IConsoleInputSource
{
    private UnixConsoleInterop.Termios _savedTermios;
    private readonly Stream _stdin;
    private readonly byte[] _readBuf = new byte[64];
    private readonly List<byte> _parseBuf = new(128);
    private readonly TerminalInputParser _parser = new();
    private bool _disposed;

    internal UnixConsoleInputSource()
    {
        // Save current termios and switch to raw mode.
        UnixConsoleInterop.TcGetAttr(UnixConsoleInterop.STDIN_FD, ref _savedTermios);
        var rawTermios = _savedTermios;
        UnixConsoleInterop.CfMakeRaw(ref rawTermios);
        UnixConsoleInterop.TcSetAttr(UnixConsoleInterop.STDIN_FD, UnixConsoleInterop.TCSANOW, ref rawTermios);

        // Enable SGR (1006) mouse reporting: button+motion events and extended coordinates.
        System.Console.Write("\x1b[?1000h\x1b[?1006h");

        _stdin = System.Console.OpenStandardInput();
    }

    /// <inheritdoc/>
    public bool TryRead(out InputEvent ev)
    {
        // Drain any bytes the parser has already buffered from a previous read.
        if (_parseBuf.Count > 0 && _parser.TryParse(_parseBuf, out ev))
            return true;

        // Check if stdin has data ready (non-blocking, timeout=0).
        var pfd = new UnixConsoleInterop.Pollfd
        {
            fd = UnixConsoleInterop.STDIN_FD,
            events = UnixConsoleInterop.POLLIN,
        };

        if (UnixConsoleInterop.Poll(ref pfd, 1, 0) <= 0
            || (pfd.revents & UnixConsoleInterop.POLLIN) == 0)
        {
            ev = default;
            return false;
        }

        // Read available bytes (won't block; poll confirmed data is ready).
        int n = _stdin.Read(_readBuf, 0, _readBuf.Length);
        if (n <= 0) { ev = default; return false; }

        _parseBuf.AddRange(new ArraySegment<byte>(_readBuf, 0, n));

        return _parser.TryParse(_parseBuf, out ev);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Disable SGR mouse tracking.
        System.Console.Write("\x1b[?1000l\x1b[?1006l");

        // Restore termios.
        UnixConsoleInterop.TcSetAttr(
            UnixConsoleInterop.STDIN_FD,
            UnixConsoleInterop.TCSANOW,
            ref _savedTermios);
    }
}
