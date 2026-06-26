namespace Mked.Controls.Tests;

public class MarkdownEditor_Selection_Tests
{
    private static ConsoleKeyInfo Key(ConsoleKey key, char keyChar = '\0', ConsoleModifiers modifiers = 0)
        => new ConsoleKeyInfo(keyChar, key, modifiers.HasFlag(ConsoleModifiers.Shift),
            modifiers.HasFlag(ConsoleModifiers.Alt), modifiers.HasFlag(ConsoleModifiers.Control));

    // ─── Shift+Arrow creates selection ───────────────────────────────────────

    [Fact]
    public void ShiftRight_CreatesSelection()
    {
        var editor = new MarkdownEditor("hello");

        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Shift));

        editor.HasSelection.Should().BeTrue();
        editor.SelectedText.Should().Be("h");
    }

    [Fact]
    public void ShiftRight_MultipleKeystrokes_ExtendsSelection()
    {
        var editor = new MarkdownEditor("hello");

        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Shift));
        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Shift));
        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Shift));

        editor.SelectedText.Should().Be("hel");
    }

    [Fact]
    public void ShiftLeft_StartsFromCurrentPosition_SelectsLeftward()
    {
        var editor = new MarkdownEditor("hello");
        // Advance cursor to column 4
        editor.HandleKey(Key(ConsoleKey.RightArrow));
        editor.HandleKey(Key(ConsoleKey.RightArrow));
        editor.HandleKey(Key(ConsoleKey.RightArrow));

        editor.HandleKey(Key(ConsoleKey.LeftArrow, modifiers: ConsoleModifiers.Shift));
        editor.HandleKey(Key(ConsoleKey.LeftArrow, modifiers: ConsoleModifiers.Shift));

        editor.SelectedText.Should().Be("el");
    }

    [Fact]
    public void ShiftDown_ExtendsSelectionDownOneLine()
    {
        var editor = new MarkdownEditor("hello\nworld");

        editor.HandleKey(Key(ConsoleKey.DownArrow, modifiers: ConsoleModifiers.Shift));

        editor.HasSelection.Should().BeTrue();
        editor.SelectedText.Should().Be("hello\n");
    }

    [Fact]
    public void ShiftUp_ExtendsSelectionUpOneLine()
    {
        var editor = new MarkdownEditor("hello\nworld");
        // Move to line 2
        editor.HandleKey(Key(ConsoleKey.DownArrow));

        editor.HandleKey(Key(ConsoleKey.UpArrow, modifiers: ConsoleModifiers.Shift));

        editor.HasSelection.Should().BeTrue();
    }

    [Fact]
    public void ShiftHome_SelectsToLineStart()
    {
        var editor = new MarkdownEditor("hello");
        // Advance cursor to middle
        editor.HandleKey(Key(ConsoleKey.RightArrow));
        editor.HandleKey(Key(ConsoleKey.RightArrow));
        editor.HandleKey(Key(ConsoleKey.RightArrow));

        editor.HandleKey(Key(ConsoleKey.Home, modifiers: ConsoleModifiers.Shift));

        editor.SelectedText.Should().Be("hel");
    }

    [Fact]
    public void ShiftEnd_SelectsToLineEnd()
    {
        var editor = new MarkdownEditor("hello");

        editor.HandleKey(Key(ConsoleKey.End, modifiers: ConsoleModifiers.Shift));

        editor.SelectedText.Should().Be("hello");
    }

    [Fact]
    public void CtrlShiftRight_SelectsNextWord()
    {
        var editor = new MarkdownEditor("hello world");

        editor.HandleKey(Key(ConsoleKey.RightArrow,
            modifiers: ConsoleModifiers.Control | ConsoleModifiers.Shift));

        editor.HasSelection.Should().BeTrue();
        editor.SelectedText.Length.Should().BeGreaterThan(0);
    }

    // ─── Plain arrow collapses selection ─────────────────────────────────────

    [Fact]
    public void PlainArrow_AfterSelection_CollapsesSelection()
    {
        var editor = new MarkdownEditor("hello");
        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Shift));
        editor.HasSelection.Should().BeTrue();

        editor.HandleKey(Key(ConsoleKey.RightArrow));

        editor.HasSelection.Should().BeFalse();
    }

    [Fact]
    public void PlainArrow_AfterSelection_ReturnsTrue_ForcingRedraw()
    {
        var editor = new MarkdownEditor("hello");
        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Shift));

        bool handled = editor.HandleKey(Key(ConsoleKey.LeftArrow));

        // Must return true because the selection state changed.
        handled.Should().BeTrue();
    }

    [Fact]
    public void PlainHome_AfterSelection_CollapsesSelection()
    {
        var editor = new MarkdownEditor("hello");
        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Shift));

        editor.HandleKey(Key(ConsoleKey.Home));

        editor.HasSelection.Should().BeFalse();
    }

    // ─── Selection-aware editing: typing ─────────────────────────────────────

    [Fact]
    public void TypeChar_WhenSelectionActive_ReplacesSelection()
    {
        var editor = new MarkdownEditor("hello");
        // Select "hel"
        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Shift));
        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Shift));
        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Shift));

        editor.HandleKey(Key(ConsoleKey.A, 'a'));

        editor.Buffer.Should().Be("alo");
        editor.HasSelection.Should().BeFalse();
    }

    [Fact]
    public void TypeChar_WhenSelectionActive_IsOneUndoStep()
    {
        var editor = new MarkdownEditor("hello");
        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Shift));
        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Shift));

        editor.HandleKey(Key(ConsoleKey.X, 'x'));
        editor.HandleKey(Key(ConsoleKey.Z, '\x1a', ConsoleModifiers.Control)); // Ctrl+Z

        editor.Buffer.Should().Be("hello");
    }

    [Fact]
    public void Enter_WhenSelectionActive_ReplacesSelectionWithNewline()
    {
        var editor = new MarkdownEditor("hello");
        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Shift));
        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Shift));
        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Shift));

        editor.HandleKey(Key(ConsoleKey.Enter, '\r'));

        editor.Buffer.Should().Be("\nlo");
    }

    [Fact]
    public void Tab_WhenSelectionActive_ReplacesSelectionWithTwoSpaces()
    {
        var editor = new MarkdownEditor("hello");
        editor.HandleKey(Key(ConsoleKey.End, modifiers: ConsoleModifiers.Shift));

        editor.HandleKey(Key(ConsoleKey.Tab, '\t'));

        editor.Buffer.Should().Be("  ");
        editor.HasSelection.Should().BeFalse();
    }

    // ─── Selection-aware editing: delete ─────────────────────────────────────

    [Fact]
    public void Backspace_WhenSelectionActive_DeletesSelectedText()
    {
        var editor = new MarkdownEditor("hello world");
        editor.HandleKey(Key(ConsoleKey.End, modifiers: ConsoleModifiers.Shift));

        editor.HandleKey(Key(ConsoleKey.Backspace, '\b'));

        editor.Buffer.Should().Be(string.Empty);
    }

    [Fact]
    public void Delete_WhenSelectionActive_DeletesSelectedText()
    {
        var editor = new MarkdownEditor("hello world");
        editor.HandleKey(Key(ConsoleKey.End, modifiers: ConsoleModifiers.Shift));

        editor.HandleKey(Key(ConsoleKey.Delete));

        editor.Buffer.Should().Be(string.Empty);
    }

    [Fact]
    public void Backspace_WhenSelectionActive_IsOneUndoStep()
    {
        var editor = new MarkdownEditor("hello");
        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Shift));
        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Shift));

        editor.HandleKey(Key(ConsoleKey.Backspace, '\b'));
        editor.HandleKey(Key(ConsoleKey.Z, '\x1a', ConsoleModifiers.Control)); // Ctrl+Z

        editor.Buffer.Should().Be("hello");
    }

    // ─── InsertText ──────────────────────────────────────────────────────────

    [Fact]
    public void InsertText_NoSelection_InsertsAtCursor()
    {
        var editor = new MarkdownEditor("hello");
        editor.HandleKey(Key(ConsoleKey.RightArrow));
        editor.HandleKey(Key(ConsoleKey.RightArrow)); // cursor at (1,3)

        editor.InsertText("XY");

        editor.Buffer.Should().Be("heXYllo");
    }

    [Fact]
    public void InsertText_NoSelection_AdvancesCursorPastText()
    {
        var editor = new MarkdownEditor("abc");
        editor.InsertText("XY");

        editor.Cursor.Should().Be((1, 3)); // cursor after 'XY' at col 1+2=3
    }

    [Fact]
    public void InsertText_WithSelection_ReplacesSelection()
    {
        var editor = new MarkdownEditor("hello world");
        editor.HandleKey(Key(ConsoleKey.End, modifiers: ConsoleModifiers.Shift));

        editor.InsertText("goodbye");

        editor.Buffer.Should().Be("goodbye");
    }

    [Fact]
    public void InsertText_Multiline_InsertsNewlinesCorrectly()
    {
        var editor = new MarkdownEditor("abc");

        editor.InsertText("x\ny\nz");

        // "x\ny\nz" inserted at (1,1) in "abc" → "x\ny\nzabc"; cursor ends after 'z'
        editor.Buffer.Should().Be("x\ny\nzabc");
    }

    [Fact]
    public void InsertText_EmptyString_IsNoOp()
    {
        var editor = new MarkdownEditor("hello");

        editor.InsertText(string.Empty);

        editor.Buffer.Should().Be("hello");
    }

    // ─── DeleteSelection ─────────────────────────────────────────────────────

    [Fact]
    public void DeleteSelection_WhenSelectionActive_DeletesSelectedText()
    {
        var editor = new MarkdownEditor("hello world");
        editor.HandleKey(Key(ConsoleKey.End, modifiers: ConsoleModifiers.Shift));

        editor.DeleteSelection();

        editor.Buffer.Should().Be(string.Empty);
        editor.HasSelection.Should().BeFalse();
    }

    [Fact]
    public void DeleteSelection_WhenNoSelection_IsNoOp()
    {
        var editor = new MarkdownEditor("hello");

        editor.DeleteSelection(); // should not throw

        editor.Buffer.Should().Be("hello");
    }

    // ─── CtrlWord movement clears selection ──────────────────────────────────

    [Fact]
    public void CtrlLeft_AfterSelection_CollapsesSelection()
    {
        var editor = new MarkdownEditor("hello world");
        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Shift));
        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Shift));

        editor.HandleKey(Key(ConsoleKey.LeftArrow, modifiers: ConsoleModifiers.Control));

        editor.HasSelection.Should().BeFalse();
    }

    [Fact]
    public void CtrlRight_AfterSelection_CollapsesSelection()
    {
        var editor = new MarkdownEditor("hello world");
        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Shift));
        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Shift));

        editor.HandleKey(Key(ConsoleKey.RightArrow, modifiers: ConsoleModifiers.Control));

        editor.HasSelection.Should().BeFalse();
    }
}
