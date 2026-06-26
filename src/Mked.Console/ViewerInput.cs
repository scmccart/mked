namespace Mked.Console;

/// <summary>
/// Shared input dispatcher for all viewer modes (file, follow, stream).
/// Applies an <see cref="InputEvent"/> to a viewer scroll position using the common
/// key map and wheel support, avoiding the 3× copy-paste in <see cref="ViewCommand"/>.
/// </summary>
internal static class ViewerInput
{
    /// <summary>
    /// Applies <paramref name="ev"/> to <paramref name="currentLine"/>.
    /// Returns <see langword="true"/> when the scroll position changed and a redraw is needed.
    /// Sets <paramref name="quit"/> when the user pressed a quit key (q / Ctrl+C).
    /// </summary>
    internal static bool Apply(
        InputEvent ev,
        ref int currentLine,
        MarkdownViewerScrollInfo scrollInfo,
        int viewportHeight,
        out bool quit)
    {
        quit = false;

        // The viewer has no cursor; clicks and pastes have nothing to act on.
        if (ev.Kind == InputEventKind.Click) return false;
        if (ev.Kind == InputEventKind.Paste) return false;

        int maxLine = Math.Max(0, scrollInfo.TotalLineCount - viewportHeight);

        if (ev.Kind == InputEventKind.Wheel)
        {
            int target = Math.Clamp(currentLine + ev.WheelDelta * ConsoleInputSource.LinesPerNotch, 0, maxLine);
            if (target == currentLine) return false;
            currentLine = target;
            return true;
        }

        var key = ev.Key;
        switch (key.Key)
        {
            case ConsoleKey.DownArrow when key.Modifiers == 0:
            case ConsoleKey.J when key.Modifiers == 0:
                currentLine = Math.Min(currentLine + 1, maxLine);
                return true;

            case ConsoleKey.UpArrow when key.Modifiers == 0:
            case ConsoleKey.K when key.Modifiers == 0:
                currentLine = Math.Max(currentLine - 1, 0);
                return true;

            case ConsoleKey.DownArrow when key.Modifiers == ConsoleModifiers.Shift:
            case ConsoleKey.J when key.Modifiers == ConsoleModifiers.Shift:
            {
                int line = currentLine;
                var next = scrollInfo.BlockStartLines.FirstOrDefault(s => s > line, line);
                currentLine = Math.Min(next, maxLine);
                return true;
            }

            case ConsoleKey.UpArrow when key.Modifiers == ConsoleModifiers.Shift:
            case ConsoleKey.K when key.Modifiers == ConsoleModifiers.Shift:
            {
                int line = currentLine;
                var prev = scrollInfo.BlockStartLines.LastOrDefault(s => s < line, 0);
                currentLine = prev;
                return true;
            }

            case ConsoleKey.PageDown:
            case ConsoleKey.D when key.Modifiers == ConsoleModifiers.Control:
                currentLine = Math.Min(currentLine + Math.Max(1, viewportHeight / 2), maxLine);
                return true;

            case ConsoleKey.PageUp:
            case ConsoleKey.U when key.Modifiers == ConsoleModifiers.Control:
                currentLine = Math.Max(currentLine - Math.Max(1, viewportHeight / 2), 0);
                return true;

            case ConsoleKey.G when key.Modifiers == ConsoleModifiers.Shift:
                currentLine = maxLine;
                return true;

            case ConsoleKey.G when key.Modifiers == 0:
                currentLine = 0;
                return true;

            case ConsoleKey.Q:
            case ConsoleKey.C when key.Modifiers == ConsoleModifiers.Control:
                quit = true;
                return false;

            default:
                return false;
        }
    }
}
