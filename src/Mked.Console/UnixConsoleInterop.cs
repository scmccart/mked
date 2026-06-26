using System.Runtime.InteropServices;

namespace Mked.Console;

/// <summary>
/// Native libc bindings for Unix terminal control. Used only on non-Windows platforms.
/// <see cref="Termios"/> is an opaque 128-byte buffer large enough to hold the
/// <c>termios</c> struct on both Linux (≈36 bytes) and macOS (≈68 bytes).
/// </summary>
internal static partial class UnixConsoleInterop
{
    internal const int TCSANOW = 0;
    internal const int STDIN_FD = 0;
    internal const short POLLIN = 0x0001;

    [LibraryImport("libc", EntryPoint = "tcgetattr")]
    internal static partial int TcGetAttr(int fd, ref Termios termios);

    [LibraryImport("libc", EntryPoint = "tcsetattr")]
    internal static partial int TcSetAttr(int fd, int optionalActions, ref Termios termios);

    [LibraryImport("libc", EntryPoint = "cfmakeraw")]
    internal static partial void CfMakeRaw(ref Termios termios);

    [LibraryImport("libc", EntryPoint = "poll")]
    internal static partial int Poll(ref Pollfd fds, uint nfds, int timeout);

    // ─── Structures ───────────────────────────────────────────────────────────

    /// <summary>
    /// Opaque buffer for a <c>termios</c> struct. 128 bytes is sufficient for both Linux (≈36)
    /// and macOS (≈68). We never access fields directly — <c>cfmakeraw</c> handles all flag
    /// manipulation so we do not need to know the platform-specific layout.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 128)]
    internal struct Termios { }

    /// <summary>Minimal <c>struct pollfd</c> for <c>poll()</c>.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct Pollfd
    {
        public int fd;
        public short events;
        public short revents;
    }
}
