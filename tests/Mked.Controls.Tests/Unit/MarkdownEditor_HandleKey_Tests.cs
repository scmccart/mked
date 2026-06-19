namespace Mked.Controls.Tests;

public class MarkdownEditor_HandleKey_Tests
{
    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ConsoleKeyInfo Key(ConsoleKey key, char keyChar = '\0', ConsoleModifiers modifiers = 0)
        => new ConsoleKeyInfo(keyChar, key, modifiers.HasFlag(ConsoleModifiers.Shift),
            modifiers.HasFlag(ConsoleModifiers.Alt), modifiers.HasFlag(ConsoleModifiers.Control));

    // ─── LoadDocument ─────────────────────────────────────────────────────────

    [Fact]
    public void LoadDocument_ResetsCursorToLineOneColumnOne()
    {
        var editor = new MarkdownEditor("line1\nline2\nline3");
        // Move cursor away from (1,1) via several key presses
        editor.HandleKey(Key(ConsoleKey.DownArrow));
        editor.HandleKey(Key(ConsoleKey.RightArrow));

        editor.LoadDocument("new document");

        editor.Cursor.Should().Be((1, 1));
    }

    [Fact]
    public void LoadDocument_LeavesCanUndoFalse()
    {
        var editor = new MarkdownEditor("initial content");
        editor.HandleKey(Key(ConsoleKey.A, 'a'));
        editor.CanUndo.Should().BeTrue();

        editor.LoadDocument("completely new document");

        editor.CanUndo.Should().BeFalse();
    }

    [Fact]
    public void LoadDocument_LeavesIsDirtyFalse()
    {
        var editor = new MarkdownEditor("original");
        editor.HandleKey(Key(ConsoleKey.A, 'a'));
        editor.IsDirty.Should().BeTrue();

        editor.LoadDocument("fresh load");

        editor.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void LoadDocument_CannotUndoBackIntoPreviousDocument()
    {
        var editor = new MarkdownEditor("doc1");
        // Simulate typing so there's undo history in doc1
        editor.HandleKey(Key(ConsoleKey.A, 'a'));

        editor.LoadDocument("doc2");

        // Undo should be a no-op — the editor must not regress to doc1
        editor.HandleKey(Key(ConsoleKey.Z, '\x1a', ConsoleModifiers.Control));
        editor.Buffer.Should().Be("doc2");
    }

    // ─── Tab — two-space indent ───────────────────────────────────────────────

    [Fact]
    public void HandleKey_Tab_InsertsTheTwoSpaces()
    {
        var editor = new MarkdownEditor("hello");
        // Move cursor to end of "hello" (5 right-arrow presses)
        editor.HandleKey(Key(ConsoleKey.RightArrow));
        editor.HandleKey(Key(ConsoleKey.RightArrow));
        editor.HandleKey(Key(ConsoleKey.RightArrow));
        editor.HandleKey(Key(ConsoleKey.RightArrow));
        editor.HandleKey(Key(ConsoleKey.RightArrow));

        bool handled = editor.HandleKey(Key(ConsoleKey.Tab, '\t'));

        handled.Should().BeTrue();
        editor.Buffer.Should().Be("hello  ");
    }

    [Fact]
    public void HandleKey_Tab_AdvancesCursorByTwoColumns()
    {
        var editor = new MarkdownEditor("x");
        editor.HandleKey(Key(ConsoleKey.RightArrow)); // cursor at (1,2) — after 'x'

        editor.HandleKey(Key(ConsoleKey.Tab, '\t'));

        editor.Cursor.Should().Be((1, 4)); // two columns forward
    }

    [Fact]
    public void HandleKey_Tab_AtStartOfLine_ProducesIndentedLine()
    {
        var editor = new MarkdownEditor("text");
        // Cursor is at (1,1)

        editor.HandleKey(Key(ConsoleKey.Tab, '\t'));

        editor.Buffer.Should().Be("  text");
    }

    // ─── Ctrl+Tab / Shift+Tab — not consumed by the editor ───────────────────

    [Fact]
    public void HandleKey_CtrlTab_ReturnsFalse()
    {
        var editor = new MarkdownEditor("content");

        bool handled = editor.HandleKey(Key(ConsoleKey.Tab, '\t', ConsoleModifiers.Control));

        handled.Should().BeFalse();
    }

    [Fact]
    public void HandleKey_ShiftTab_ReturnsFalse()
    {
        // Shift+Tab is handled at the host level (pane focus switch); the editor
        // must not consume it so the host loop sees it.
        var editor = new MarkdownEditor("content");

        bool handled = editor.HandleKey(Key(ConsoleKey.Tab, '\t', ConsoleModifiers.Shift));

        handled.Should().BeFalse();
    }
}
