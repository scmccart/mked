namespace Mked.Controls;

/// <summary>Scroll metadata for a rendered <see cref="MarkdownViewer"/> instance.</summary>
/// <param name="TotalLineCount">Total number of rendered terminal lines.</param>
/// <param name="BlockStartLines">
/// Maps each top-level block index to the first rendered line index for that block.
/// </param>
public sealed record MarkdownViewerScrollInfo(
    int TotalLineCount,
    IReadOnlyList<int> BlockStartLines)
{
    internal static readonly MarkdownViewerScrollInfo Empty =
        new(0, Array.Empty<int>());

    /// <summary>
    /// Per-line content hash for each rendered terminal line, parallel to the rendered output.
    /// A hash of <c>0</c> denotes a blank line (used as a block separator or inside a block).
    /// Non-blank lines never produce <c>0</c>. Used by scroll anchoring in follow mode to
    /// relocate the viewport after a file reload.
    /// </summary>
    public IReadOnlyList<int> LineHashes { get; init; } = Array.Empty<int>();
}
