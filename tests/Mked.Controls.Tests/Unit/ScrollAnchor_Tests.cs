namespace Mked.Controls.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="ScrollAnchor.RemapTopLine"/>.
/// Tests use synthetic hash lists to exercise each scroll-anchoring case in isolation.
/// </summary>
public sealed class ScrollAnchor_Tests
{
    // Representative document: 10 lines, distinct non-zero hashes, viewport height = 4.
    // topLineIndex = 3 means the viewport shows lines 3-6 (hashes 40, 50, 60, 70).
    private static readonly IReadOnlyList<int> BaseDoc =
        [10, 20, 30, 40, 50, 60, 70, 80, 90, 100];
    private const int Vh = 4;     // viewport height

    // ─── Fast paths ───────────────────────────────────────────────────────────

    [Fact]
    public void AlreadyAtTop_ReturnsZero()
    {
        // When topLineIndex == 0 we must always stay at the top regardless of changes.
        IReadOnlyList<int> newDoc = [11, 12, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100];
        ScrollAnchor.RemapTopLine(BaseDoc, newDoc, topLineIndex: 0, Vh).Should().Be(0);
    }

    [Fact]
    public void EmptyOldHashes_ReturnsZero()
    {
        ScrollAnchor.RemapTopLine([], BaseDoc, topLineIndex: 3, Vh).Should().Be(0);
    }

    [Fact]
    public void EmptyNewHashes_ReturnsZero()
    {
        ScrollAnchor.RemapTopLine(BaseDoc, [], topLineIndex: 3, Vh).Should().Be(0);
    }

    // ─── Change below the viewport ────────────────────────────────────────────

    [Fact]
    public void ChangeBelowViewport_PositionPreserved()
    {
        // Lines 7-9 changed; the viewport top (line 3, hash 40) is untouched.
        // The top line should stay at the same index.
        IReadOnlyList<int> newDoc = [10, 20, 30, 40, 50, 60, 70, 81, 91, 101];
        int result = ScrollAnchor.RemapTopLine(BaseDoc, newDoc, topLineIndex: 3, Vh);
        result.Should().Be(3);
    }

    // ─── Insertion above the viewport ─────────────────────────────────────────

    [Fact]
    public void InsertTwoLinesAbove_PositionShiftsDown()
    {
        // Two new lines inserted at the top; hash 40 (old index 3) is now at new index 5.
        IReadOnlyList<int> newDoc = [11, 12, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100];
        int result = ScrollAnchor.RemapTopLine(BaseDoc, newDoc, topLineIndex: 3, Vh);
        result.Should().Be(5);
    }

    [Fact]
    public void InsertOneLineAbove_PositionShiftsDownByOne()
    {
        IReadOnlyList<int> newDoc = [11, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100];
        int result = ScrollAnchor.RemapTopLine(BaseDoc, newDoc, topLineIndex: 3, Vh);
        result.Should().Be(4);
    }

    // ─── Deletion above the viewport ─────────────────────────────────────────

    [Fact]
    public void DeleteTwoLinesAbove_PositionShiftsUp()
    {
        // Lines 0 and 1 deleted; hash 40 (old index 3) is now at new index 1.
        IReadOnlyList<int> newDoc = [30, 40, 50, 60, 70, 80, 90, 100];
        int result = ScrollAnchor.RemapTopLine(BaseDoc, newDoc, topLineIndex: 3, Vh);
        result.Should().Be(1);
    }

    // ─── Change within the viewport, below the top line ───────────────────────

    [Fact]
    public void ChangeWithinViewportBelowTopLine_TopLineFixed()
    {
        // Lines 4, 5, 6 changed (within viewport but below top); top line hash 40 is intact.
        IReadOnlyList<int> newDoc = [10, 20, 30, 40, 51, 61, 71, 80, 90, 100];
        int result = ScrollAnchor.RemapTopLine(BaseDoc, newDoc, topLineIndex: 3, Vh);
        result.Should().Be(3);
    }

    // ─── Top line itself changed ──────────────────────────────────────────────

    [Fact]
    public void TopLineChanged_AnchorsToPreviousStableLineAbove()
    {
        // Hash at index 3 changed from 40 to 41; algorithm walks up to index 2 (hash 30)
        // which still exists at new index 2. Offset = 3-2 = 1 → result = 2+1 = 3.
        IReadOnlyList<int> newDoc = [10, 20, 30, 41, 50, 60, 70, 80, 90, 100];
        int result = ScrollAnchor.RemapTopLine(BaseDoc, newDoc, topLineIndex: 3, Vh);
        result.Should().Be(3);
    }

    [Fact]
    public void TopAndAboveLinesChanged_AnchorsFurtherUp()
    {
        // Both index 3 (40→41) and index 2 (30→31) changed.
        // Probe walks to index 1 (hash 20) → still at new index 1. Offset = 3-1 = 2 → result = 3.
        IReadOnlyList<int> newDoc = [10, 20, 31, 41, 50, 60, 70, 80, 90, 100];
        int result = ScrollAnchor.RemapTopLine(BaseDoc, newDoc, topLineIndex: 3, Vh);
        result.Should().Be(3);
    }

    // ─── Combination: simultaneous changes above AND below ────────────────────

    [Fact]
    public void InsertAboveAndChangeBelowViewport_ExactNewPosition()
    {
        // Insert 3 lines at the top AND change a line below the original viewport.
        // Old top hash 40 was at index 3; it should now appear at index 6.
        // The below-viewport change should not affect the top-line relocation.
        IReadOnlyList<int> newDoc = [11, 12, 13, 10, 20, 30, 40, 50, 61, 70, 80, 90, 100];
        int result = ScrollAnchor.RemapTopLine(BaseDoc, newDoc, topLineIndex: 3, Vh);
        result.Should().Be(6);
    }

    [Fact]
    public void DeleteAboveAndInsertBelow_ExactNewPosition()
    {
        // Delete lines 0-1 (shrink start) and insert a line below the viewport end.
        // Old hash 40 at index 3 is now at index 1 (deleted 2 lines above).
        IReadOnlyList<int> newDoc = [30, 40, 50, 60, 70, 80, 90, 91, 100];
        int result = ScrollAnchor.RemapTopLine(BaseDoc, newDoc, topLineIndex: 3, Vh);
        result.Should().Be(1);
    }

    // ─── Repeated content / window disambiguation ─────────────────────────────

    [Fact]
    public void RepeatedHash_WindowMatchPicksCorrectOccurrence()
    {
        // Hash 20 appears at old indices 1 and 4 (duplicate content).
        // topLineIndex = 4 (probe 4 → hash 20).
        // New doc has hash 20 at indices 1 and 4. The window from old[4..] = [20, 50, 60]
        // matches new[4..] = [20, 50, 60], but not new[1..] = [20, 30, 40, 20, 50, 60].
        IReadOnlyList<int> oldDoc = [10, 20, 30, 40, 20, 50, 60, 70];
        IReadOnlyList<int> newDoc = [10, 20, 30, 40, 20, 50, 60, 70];
        int result = ScrollAnchor.RemapTopLine(oldDoc, newDoc, topLineIndex: 4, viewportHeight: 3);
        result.Should().Be(4);
    }

    [Fact]
    public void RepeatedHash_AfterInsertionAbove_PicksShiftedOccurrence()
    {
        // Old: hash 20 at indices 1 and 4. topLineIndex = 4.
        // New: insert one line at start → hash 20 now at indices 2 and 5.
        // The window from old[4..] = [20, 50, 60] should match new[5..] = [20, 50, 60].
        IReadOnlyList<int> oldDoc = [10, 20, 30, 40, 20, 50, 60, 70];
        IReadOnlyList<int> newDoc = [11, 10, 20, 30, 40, 20, 50, 60, 70];
        int result = ScrollAnchor.RemapTopLine(oldDoc, newDoc, topLineIndex: 4, viewportHeight: 3);
        result.Should().Be(5);
    }

    // ─── Blank lines near the anchor ─────────────────────────────────────────

    [Fact]
    public void BlankTopLine_WalksUpToFirstNonBlankReference()
    {
        // Index 3 is blank (0). Probe skips it and uses index 2 (hash 30) as reference.
        // Hash 30 is at new index 2; offset = 3-2 = 1 → result = 3.
        IReadOnlyList<int> oldDoc = [10, 20, 30, 0, 50, 60, 70];
        IReadOnlyList<int> newDoc = [10, 20, 30, 0, 50, 60, 70];
        int result = ScrollAnchor.RemapTopLine(oldDoc, newDoc, topLineIndex: 3, viewportHeight: 3);
        result.Should().Be(3);
    }

    // ─── Clamping ─────────────────────────────────────────────────────────────

    [Fact]
    public void AnchoredPositionPastEof_ClampedToNewMax()
    {
        // Old doc 10 lines, top at index 8.  New doc truncated to 5 lines.
        // Even if anchor math suggests a position past EOF, result is clamped.
        IReadOnlyList<int> oldDoc = [10, 20, 30, 40, 50, 60, 70, 80, 90, 100];
        IReadOnlyList<int> newDoc = [10, 20, 30, 40, 50];
        int result = ScrollAnchor.RemapTopLine(oldDoc, newDoc, topLineIndex: 8, viewportHeight: 3);
        int newMax = Math.Max(0, newDoc.Count - 3);
        result.Should().BeLessThanOrEqualTo(newMax);
    }

    [Fact]
    public void NoStableReferenceFound_KeepsPositionClamped()
    {
        // All lines in old doc were replaced; no hashes match. Fall back to clamped original.
        IReadOnlyList<int> oldDoc = [10, 20, 30, 40, 50];
        IReadOnlyList<int> newDoc = [11, 21, 31];  // completely different content
        int result = ScrollAnchor.RemapTopLine(oldDoc, newDoc, topLineIndex: 3, viewportHeight: 2);
        int newMax = Math.Max(0, newDoc.Count - 2);
        result.Should().BeLessThanOrEqualTo(newMax);
        result.Should().BeGreaterThanOrEqualTo(0);
    }
}
