namespace Mked.Controls;

/// <summary>
/// Helpers for stable viewport positioning across document re-renders in follow mode.
/// </summary>
public static class ScrollAnchor
{
    /// <summary>Number of lines inspected ahead of a candidate line when scoring matches.</summary>
    private const int WindowSize = 8;

    /// <summary>
    /// Returns the new top-line index that keeps the content at the top of the viewport in
    /// roughly the same on-screen position after a document re-render.
    /// </summary>
    /// <remarks>
    /// The algorithm walks upward from <paramref name="topLineIndex"/> to find the nearest
    /// non-blank reference line that still exists in the new render, using a forward window
    /// of consecutive line hashes to disambiguate repeated content. Once a stable reference
    /// is found, the original offset between it and the top line is preserved.
    /// </remarks>
    /// <param name="oldHashes">
    /// Per-line content hashes from the previous render
    /// (see <see cref="MarkdownViewerScrollInfo.LineHashes"/>).
    /// </param>
    /// <param name="newHashes">
    /// Per-line content hashes from the new render
    /// (see <see cref="MarkdownViewerScrollInfo.LineHashes"/>).
    /// </param>
    /// <param name="topLineIndex">
    /// Current 0-based top-line index into <paramref name="oldHashes"/>.
    /// </param>
    /// <param name="viewportHeight">Number of visible terminal rows.</param>
    /// <returns>
    /// A clamped top-line index into <paramref name="newHashes"/> that preserves the
    /// on-screen position of the content previously shown at <paramref name="topLineIndex"/>.
    /// </returns>
    public static int RemapTopLine(
        IReadOnlyList<int> oldHashes,
        IReadOnlyList<int> newHashes,
        int topLineIndex,
        int viewportHeight)
    {
        int newMax = Math.Max(0, newHashes.Count - viewportHeight);

        // Fast paths — nothing to anchor to, or already at the top.
        if (topLineIndex <= 0 || oldHashes.Count == 0 || newHashes.Count == 0)
            return 0;

        // Pre-compute hash → candidate positions in the new document (non-blank only).
        var candidateMap = BuildCandidateMap(newHashes);
        int predictedDelta = newHashes.Count - oldHashes.Count;

        // Walk upward from topLineIndex to find the nearest non-blank stable reference line
        // that still appears in the new document, then translate the offset.
        int probe = Math.Min(topLineIndex, oldHashes.Count - 1);
        while (probe >= 0)
        {
            int refHash = oldHashes[probe];
            if (refHash != 0)   // blank lines (0) are too ambiguous to anchor on
            {
                int predicted = probe + predictedDelta;
                int matchInNew = FindBestMatch(oldHashes, newHashes, candidateMap, probe, predicted);
                if (matchInNew >= 0)
                {
                    int offset = topLineIndex - probe;
                    return Math.Clamp(matchInNew + offset, 0, newMax);
                }
            }
            probe--;
        }

        // No stable reference found — keep position (clamped to new document size).
        return Math.Clamp(topLineIndex, 0, newMax);
    }

    /// <summary>
    /// Builds a lookup from content hash to all line positions in <paramref name="hashes"/>.
    /// Blank lines (hash == 0) are excluded because they are too common to be useful anchors.
    /// </summary>
    private static Dictionary<int, List<int>> BuildCandidateMap(IReadOnlyList<int> hashes)
    {
        var map = new Dictionary<int, List<int>>();
        for (int i = 0; i < hashes.Count; i++)
        {
            int h = hashes[i];
            if (h == 0) continue;
            if (!map.TryGetValue(h, out var list))
                map[h] = list = [];
            list.Add(i);
        }
        return map;
    }

    /// <summary>
    /// Returns the best-matching position in the new document for the reference line at
    /// <paramref name="probe"/> in the old document. When there is exactly one candidate
    /// for the reference hash, it is returned directly. When there are multiple, the forward
    /// window score (consecutive matching lines) is used, tiebroken by proximity to
    /// <paramref name="predicted"/>. Returns -1 when the hash does not appear in the new
    /// document.
    /// </summary>
    private static int FindBestMatch(
        IReadOnlyList<int> oldHashes,
        IReadOnlyList<int> newHashes,
        Dictionary<int, List<int>> candidateMap,
        int probe,
        int predicted)
    {
        if (!candidateMap.TryGetValue(oldHashes[probe], out var candidates))
            return -1;

        // Single occurrence — unambiguous match.
        if (candidates.Count == 1)
            return candidates[0];

        // Multiple occurrences — pick the candidate with the longest consecutive forward
        // window match, tiebreaking toward the expected (delta-shifted) position.
        int bestScore = 0;
        int bestCandidate = -1;

        foreach (int j in candidates)
        {
            int score = WindowScore(oldHashes, newHashes, probe, j);
            bool better = score > bestScore
                || (score == bestScore
                    && (bestCandidate < 0
                        || Math.Abs(j - predicted) < Math.Abs(bestCandidate - predicted)));
            if (better)
            {
                bestScore = score;
                bestCandidate = j;
            }
        }

        return bestCandidate;
    }

    /// <summary>
    /// Counts how many consecutive lines starting at <paramref name="oldStart"/> in the old
    /// document have the same hash as the lines starting at <paramref name="newStart"/> in the
    /// new document, up to <see cref="WindowSize"/> lines.
    /// </summary>
    private static int WindowScore(
        IReadOnlyList<int> oldHashes,
        IReadOnlyList<int> newHashes,
        int oldStart,
        int newStart)
    {
        int count = 0;
        for (int k = 0; k < WindowSize; k++)
        {
            int oi = oldStart + k;
            int ni = newStart + k;
            if (oi >= oldHashes.Count || ni >= newHashes.Count) break;
            if (oldHashes[oi] != newHashes[ni]) break;
            count++;
        }
        return count;
    }
}
