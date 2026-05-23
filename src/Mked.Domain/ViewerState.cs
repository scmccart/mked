namespace Mked.Domain;

/// <summary>
/// Entity representing an active viewing session. Tracks the current scroll anchor
/// and whether the viewer auto-follows new content.
/// </summary>
public sealed class ViewerState
{
    private readonly MarkdownDocument _document;

    /// <summary>
    /// Creates a <see cref="ViewerState"/>. Anchor defaults to the first block, or
    /// <see cref="ViewportAnchor.None"/> when the document is empty.
    /// </summary>
    public ViewerState(MarkdownDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _document = document;
        Anchor = document.IsEmpty ? ViewportAnchor.None : new ViewportAnchor(0);
    }

    /// <summary>The current scroll position expressed as a top-level block index.</summary>
    public ViewportAnchor Anchor { get; private set; }

    /// <summary>
    /// Returns <see langword="true"/> when the viewer automatically advances the anchor
    /// as new blocks arrive.
    /// </summary>
    public bool IsFollowing { get; private set; }

    /// <summary>
    /// Sets the scroll anchor to <paramref name="anchor"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="anchor"/> is <see cref="ViewportAnchor.None"/>, when
    /// the document is empty, or when <see cref="ViewportAnchor.BlockIndex"/> is out of range.
    /// </exception>
    public void SetAnchor(ViewportAnchor anchor)
    {
        if (anchor.IsNone || anchor.BlockIndex >= _document.Blocks.Count)
            throw new ArgumentOutOfRangeException(
                nameof(anchor),
                anchor.BlockIndex,
                _document.IsEmpty
                    ? "Cannot set anchor on an empty document."
                    : $"Block index must be between 0 and {_document.Blocks.Count - 1}.");
        Anchor = anchor;
    }

    /// <summary>Enables or disables follow mode.</summary>
    public void SetFollowMode(bool follow) => IsFollowing = follow;
}
