namespace Mked.Controls;

/// <summary>Pure string operations on a newline-delimited text buffer.</summary>
public static class BufferOperations
{
    /// <summary>
    /// Inserts <paramref name="text"/> into <paramref name="buffer"/> at the given
    /// <paramref name="position"/> and returns the resulting string.
    /// </summary>
    public static string Insert(string buffer, CursorPosition position, string text)
    {
        if (buffer.Length == 0)
            return text;

        int offset = ToOffset(buffer, position);
        return buffer[..offset] + text + buffer[offset..];
    }

    /// <summary>
    /// Removes the characters within <paramref name="range"/> from <paramref name="buffer"/>
    /// and returns the resulting string.
    /// </summary>
    public static string Delete(string buffer, TextRange range)
    {
        if (buffer.Length == 0)
            return buffer;

        int start = ToOffset(buffer, range.Start);
        int end = ToOffset(buffer, range.End);

        if (start >= end)
            return buffer;

        end = Math.Min(end, buffer.Length);
        return buffer[..start] + buffer[end..];
    }

    /// <summary>
    /// Converts a 1-based <see cref="CursorPosition"/> to a 0-based character offset
    /// within <paramref name="buffer"/>.
    /// </summary>
    public static int ToOffset(string buffer, CursorPosition position)
    {
        if (buffer.Length == 0)
            return 0;

        string[] lines = buffer.Split('\n');
        int lineIndex = Math.Clamp(position.Line - 1, 0, lines.Length - 1);

        int offset = 0;
        for (int i = 0; i < lineIndex; i++)
            offset += lines[i].Length + 1; // +1 for the '\n'

        string currentLine = lines[lineIndex];
        int col = Math.Clamp(position.Column - 1, 0, currentLine.Length);
        return offset + col;
    }

    /// <summary>
    /// Converts a 0-based character <paramref name="offset"/> within <paramref name="buffer"/>
    /// to a 1-based <see cref="CursorPosition"/>.
    /// </summary>
    public static CursorPosition FromOffset(string buffer, int offset)
    {
        if (buffer.Length == 0)
            return new CursorPosition(1, 1);

        offset = Math.Clamp(offset, 0, buffer.Length);

        string[] lines = buffer.Split('\n');
        int remaining = offset;

        for (int i = 0; i < lines.Length; i++)
        {
            int lineLen = lines[i].Length + 1; // +1 for '\n'
            if (remaining < lineLen || i == lines.Length - 1)
                return new CursorPosition(i + 1, remaining + 1);
            remaining -= lineLen;
        }

        return new CursorPosition(1, 1);
    }
}
