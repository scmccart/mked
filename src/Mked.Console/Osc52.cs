using System.Text;

namespace Mked.Console;

/// <summary>
/// Generates OSC 52 terminal escape sequences for clipboard write operations.
/// OSC 52 is supported by most modern terminals (including Windows Terminal, kitty,
/// iTerm2, and tmux) and works over SSH.
/// </summary>
internal static class Osc52
{
    /// <summary>
    /// Returns the OSC 52 escape sequence that writes <paramref name="text"/> to the
    /// system clipboard when written to the terminal's stdout.
    /// </summary>
    public static string EncodeCopy(string text)
    {
        string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        return $"\x1b]52;c;{b64}\x07";
    }
}
