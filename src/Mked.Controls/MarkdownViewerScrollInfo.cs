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
}
