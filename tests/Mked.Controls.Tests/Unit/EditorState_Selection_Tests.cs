namespace Mked.Controls.Tests;

public class EditorState_Selection_Tests
{
    // ─── BeginSelection ──────────────────────────────────────────────────────

    [Fact]
    public void BeginSelection_SetsAnchorToCursor()
    {
        var state = new EditorState("hello");
        state.MoveCursorRight(); // cursor at (1,2)

        state.BeginSelection();

        state.Anchor.Should().Be(new CursorPosition(1, 2));
    }

    [Fact]
    public void BeginSelection_CalledTwice_DoesNotMoveAnchor()
    {
        // Anchor is locked on first call; repeated Shift+Arrow must not shift the anchor.
        var state = new EditorState("hello");
        state.MoveCursorRight(); // cursor at (1,2)
        state.BeginSelection();
        state.MoveCursorRight(); // cursor at (1,3)
        state.BeginSelection(); // should be no-op

        state.Anchor.Should().Be(new CursorPosition(1, 2));
    }

    // ─── ClearSelection ──────────────────────────────────────────────────────

    [Fact]
    public void ClearSelection_RemovesAnchor()
    {
        var state = new EditorState("hello");
        state.BeginSelection();

        state.ClearSelection();

        state.Anchor.Should().BeNull();
    }

    // ─── HasSelection ────────────────────────────────────────────────────────

    [Fact]
    public void HasSelection_FalseWhenNoAnchor()
    {
        var state = new EditorState("hello");

        state.HasSelection.Should().BeFalse();
    }

    [Fact]
    public void HasSelection_FalseWhenAnchorEqualsCurrentCursor()
    {
        // Anchor set at (1,1), cursor still at (1,1) → zero-length selection
        var state = new EditorState("hello");
        state.BeginSelection();

        state.HasSelection.Should().BeFalse();
    }

    [Fact]
    public void HasSelection_TrueWhenAnchorDifferentFromCursor()
    {
        var state = new EditorState("hello");
        state.BeginSelection();
        state.MoveCursorRight(); // cursor now at (1,2), anchor at (1,1)

        state.HasSelection.Should().BeTrue();
    }

    // ─── SelectionRange ──────────────────────────────────────────────────────

    [Fact]
    public void SelectionRange_IsNormalisedWhenForwardSelection()
    {
        var state = new EditorState("hello");
        state.BeginSelection();         // anchor at (1,1)
        state.MoveCursorRight();
        state.MoveCursorRight();        // cursor at (1,3)

        var range = state.SelectionRange;

        range.Start.Should().Be(new CursorPosition(1, 1));
        range.End.Should().Be(new CursorPosition(1, 3));
    }

    [Fact]
    public void SelectionRange_IsNormalisedWhenBackwardSelection()
    {
        // Move cursor right first, then anchor and extend back left
        var state = new EditorState("hello");
        state.MoveCursorRight();
        state.MoveCursorRight();         // cursor at (1,3)
        state.BeginSelection();          // anchor at (1,3)
        state.MoveCursorLeft();          // cursor at (1,2) — anchor > cursor

        var range = state.SelectionRange;

        range.Start.Should().Be(new CursorPosition(1, 2));
        range.End.Should().Be(new CursorPosition(1, 3));
    }

    // ─── SelectedText ────────────────────────────────────────────────────────

    [Fact]
    public void SelectedText_EmptyStringWhenNoSelection()
    {
        var state = new EditorState("hello");

        state.SelectedText.Should().BeEmpty();
    }

    [Fact]
    public void SelectedText_ReturnsSelectedChars()
    {
        var state = new EditorState("hello");
        state.BeginSelection();         // anchor at (1,1)
        state.MoveCursorRight();        // selects 'h'
        state.MoveCursorRight();        // selects 'he'

        state.SelectedText.Should().Be("he");
    }

    [Fact]
    public void SelectedText_WorksForBackwardSelection()
    {
        var state = new EditorState("hello");
        state.MoveCursorRight();
        state.MoveCursorRight();         // cursor at (1,3)
        state.BeginSelection();          // anchor at (1,3)
        state.MoveCursorLeft();          // cursor at (1,2)

        state.SelectedText.Should().Be("e"); // second character
    }

    // ─── ReplaceRange ────────────────────────────────────────────────────────

    [Fact]
    public void ReplaceRange_ReplacesSelectedTextWithNewText()
    {
        var state = new EditorState("hello world");
        // Select "hello" (offsets 0–5)
        var range = new TextRange(new CursorPosition(1, 1), new CursorPosition(1, 6));

        state.ReplaceRange(range, "goodbye");

        state.Buffer.Should().Be("goodbye world");
    }

    [Fact]
    public void ReplaceRange_PositionsCursorAtEndOfInsertedText()
    {
        var state = new EditorState("abcde");
        // Replace 'b' at offset 1 (positions 1,2–1,3)
        var range = new TextRange(new CursorPosition(1, 2), new CursorPosition(1, 3));

        state.ReplaceRange(range, "XY");

        // 'b' is gone, 'XY' inserted; cursor should be after 'Y' → col 4
        state.Cursor.Should().Be(new CursorPosition(1, 4));
    }

    [Fact]
    public void ReplaceRange_ClearsAnchor()
    {
        var state = new EditorState("hello");
        state.BeginSelection();
        state.MoveCursorRight();

        state.ReplaceRange(state.SelectionRange, "X");

        state.HasSelection.Should().BeFalse();
    }

    [Fact]
    public void ReplaceRange_PushesToUndoStack()
    {
        var state = new EditorState("hello");
        var range = new TextRange(new CursorPosition(1, 1), new CursorPosition(1, 3));

        state.ReplaceRange(range, "AB");

        state.CanUndo.Should().BeTrue();
    }

    [Fact]
    public void ReplaceRange_UndoRestoresPreviousBuffer()
    {
        var state = new EditorState("hello");
        var range = new TextRange(new CursorPosition(1, 1), new CursorPosition(1, 6));

        state.ReplaceRange(range, "world");
        state.Undo();

        state.Buffer.Should().Be("hello");
    }

    [Fact]
    public void ReplaceRange_WithEmptyRange_ActsAsInsert()
    {
        // Passing an empty range (Start == End) should insert without deleting anything.
        var state = new EditorState("abc");
        var range = new TextRange(new CursorPosition(1, 2), new CursorPosition(1, 2));

        state.ReplaceRange(range, "X");

        state.Buffer.Should().Be("aXbc");
    }

    [Fact]
    public void ReplaceRange_WithEmptyText_ActsAsDelete()
    {
        var state = new EditorState("hello");
        var range = new TextRange(new CursorPosition(1, 1), new CursorPosition(1, 4));

        state.ReplaceRange(range, string.Empty);

        state.Buffer.Should().Be("lo");
    }

    [Fact]
    public void ReplaceRange_NotifiesObserverOfBufferAndCursorChange()
    {
        var state = new EditorState("hello");
        var spy = new SpyObserver();
        state.Subscribe(spy);
        var range = new TextRange(new CursorPosition(1, 1), new CursorPosition(1, 3));

        state.ReplaceRange(range, "AB");

        spy.BufferCallCount.Should().Be(1);
        spy.CursorCallCount.Should().Be(1);
    }

    // ─── DeleteSelection ─────────────────────────────────────────────────────

    [Fact]
    public void DeleteSelection_RemovesSelectedText()
    {
        var state = new EditorState("hello");
        state.BeginSelection();
        state.MoveCursorRight();
        state.MoveCursorRight();
        state.MoveCursorRight(); // selects "hel"

        state.DeleteSelection();

        state.Buffer.Should().Be("lo");
    }

    [Fact]
    public void DeleteSelection_NoOpWhenNoSelection()
    {
        var state = new EditorState("hello");

        state.DeleteSelection(); // should not throw

        state.Buffer.Should().Be("hello");
    }

    // ─── MoveCursorTo clears anchor ──────────────────────────────────────────

    [Fact]
    public void MoveCursorTo_ClearsSelectionAnchor()
    {
        var state = new EditorState("hello world");
        state.BeginSelection();
        state.MoveCursorRight(); // has selection now

        state.MoveCursorTo(new CursorPosition(1, 5));

        state.HasSelection.Should().BeFalse();
    }

    // ─── Undo / Redo clear anchor ─────────────────────────────────────────────

    [Fact]
    public void Undo_ClearsSelectionAnchor()
    {
        var state = new EditorState("hello");
        state.Insert(new CursorPosition(1, 6), " world");
        state.BeginSelection();
        state.MoveCursorLeft();

        state.Undo();

        state.HasSelection.Should().BeFalse();
    }

    [Fact]
    public void Redo_ClearsSelectionAnchor()
    {
        var state = new EditorState("hello");
        state.Insert(new CursorPosition(1, 6), " world");
        state.Undo();
        state.BeginSelection();
        state.MoveCursorLeft();

        state.Redo();

        state.HasSelection.Should().BeFalse();
    }
}
