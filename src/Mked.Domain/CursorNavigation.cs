namespace Mked.Domain;

/// <summary>Stateless cursor movement operations over a newline-delimited text buffer.</summary>
public static class CursorNavigation
{
    /// <summary>
    /// Moves the cursor one character to the left. If already at column 1, wraps to the
    /// end of the previous line. If at the very start of the buffer, returns unchanged.
    /// </summary>
    public static CursorPosition MoveLeft(string buffer, CursorPosition current)
    {
        if (buffer.Length == 0)
            return new CursorPosition(1, 1);

        string[] lines = buffer.Split('\n');
        int lineIndex = Math.Clamp(current.Line - 1, 0, lines.Length - 1);

        if (current.Column > 1)
            return new CursorPosition(lineIndex + 1, current.Column - 1);

        if (lineIndex == 0)
            return new CursorPosition(1, 1);

        int prevLine = lineIndex - 1;
        return new CursorPosition(prevLine + 1, lines[prevLine].Length + 1);
    }

    /// <summary>
    /// Moves the cursor one character to the right. If at the end of a line, wraps to
    /// column 1 of the next line. If at the very end of the buffer, returns unchanged.
    /// </summary>
    public static CursorPosition MoveRight(string buffer, CursorPosition current)
    {
        if (buffer.Length == 0)
            return new CursorPosition(1, 1);

        string[] lines = buffer.Split('\n');
        int lineIndex = Math.Clamp(current.Line - 1, 0, lines.Length - 1);
        string line = lines[lineIndex];

        if (current.Column <= line.Length)
            return new CursorPosition(lineIndex + 1, current.Column + 1);

        if (lineIndex == lines.Length - 1)
            return new CursorPosition(lineIndex + 1, current.Column);

        return new CursorPosition(lineIndex + 2, 1);
    }

    /// <summary>
    /// Moves the cursor up one line, clamping the column to the new line's length.
    /// If already on the first line, returns unchanged.
    /// </summary>
    public static CursorPosition MoveUp(string buffer, CursorPosition current)
    {
        if (buffer.Length == 0)
            return new CursorPosition(1, 1);

        string[] lines = buffer.Split('\n');
        int lineIndex = Math.Clamp(current.Line - 1, 0, lines.Length - 1);

        if (lineIndex == 0)
            return new CursorPosition(1, Math.Max(1, Math.Min(current.Column, lines[0].Length + 1)));

        int newLineIndex = lineIndex - 1;
        int col = Math.Clamp(current.Column, 1, lines[newLineIndex].Length + 1);
        return new CursorPosition(newLineIndex + 1, col);
    }

    /// <summary>
    /// Moves the cursor down one line, clamping the column to the new line's length.
    /// If already on the last line, returns unchanged.
    /// </summary>
    public static CursorPosition MoveDown(string buffer, CursorPosition current)
    {
        if (buffer.Length == 0)
            return new CursorPosition(1, 1);

        string[] lines = buffer.Split('\n');
        int lineIndex = Math.Clamp(current.Line - 1, 0, lines.Length - 1);

        if (lineIndex == lines.Length - 1)
            return new CursorPosition(lineIndex + 1, Math.Clamp(current.Column, 1, lines[lineIndex].Length + 1));

        int newLineIndex = lineIndex + 1;
        int col = Math.Clamp(current.Column, 1, lines[newLineIndex].Length + 1);
        return new CursorPosition(newLineIndex + 1, col);
    }

    /// <summary>
    /// Moves the cursor left past whitespace and then past a word (non-whitespace run).
    /// </summary>
    public static CursorPosition MoveWordLeft(string buffer, CursorPosition current)
    {
        if (buffer.Length == 0)
            return new CursorPosition(1, 1);

        int offset = BufferOperations.ToOffset(buffer, current);

        // Move left past any whitespace
        while (offset > 0 && char.IsWhiteSpace(buffer[offset - 1]))
            offset--;

        // Move left past non-whitespace
        while (offset > 0 && !char.IsWhiteSpace(buffer[offset - 1]))
            offset--;

        return BufferOperations.FromOffset(buffer, offset);
    }

    /// <summary>
    /// Moves the cursor right past a word (non-whitespace run) and then past whitespace.
    /// </summary>
    public static CursorPosition MoveWordRight(string buffer, CursorPosition current)
    {
        if (buffer.Length == 0)
            return new CursorPosition(1, 1);

        int offset = BufferOperations.ToOffset(buffer, current);

        // Move right past non-whitespace
        while (offset < buffer.Length && !char.IsWhiteSpace(buffer[offset]))
            offset++;

        // Move right past whitespace
        while (offset < buffer.Length && char.IsWhiteSpace(buffer[offset]))
            offset++;

        return BufferOperations.FromOffset(buffer, offset);
    }

    /// <summary>Moves the cursor to column 1 of the current line.</summary>
    public static CursorPosition MoveToLineStart(string buffer, CursorPosition current)
    {
        if (buffer.Length == 0)
            return new CursorPosition(1, 1);

        string[] lines = buffer.Split('\n');
        int lineIndex = Math.Clamp(current.Line - 1, 0, lines.Length - 1);
        return new CursorPosition(lineIndex + 1, 1);
    }

    /// <summary>
    /// Moves the cursor to one past the last character on the current line
    /// (consistent with most editor conventions).
    /// </summary>
    public static CursorPosition MoveToLineEnd(string buffer, CursorPosition current)
    {
        if (buffer.Length == 0)
            return new CursorPosition(1, 1);

        string[] lines = buffer.Split('\n');
        int lineIndex = Math.Clamp(current.Line - 1, 0, lines.Length - 1);
        return new CursorPosition(lineIndex + 1, lines[lineIndex].Length + 1);
    }

    /// <summary>
    /// Returns a position clamped so that line is within [1, lineCount] and column
    /// is within [1, lineLength+1]. Returns <c>(1,1)</c> for an empty buffer.
    /// </summary>
    public static CursorPosition Clamp(string buffer, CursorPosition position)
    {
        if (buffer.Length == 0)
            return new CursorPosition(1, 1);

        string[] lines = buffer.Split('\n');
        int lineIndex = Math.Clamp(position.Line - 1, 0, lines.Length - 1);
        int col = Math.Clamp(position.Column, 1, lines[lineIndex].Length + 1);
        return new CursorPosition(lineIndex + 1, col);
    }
}
