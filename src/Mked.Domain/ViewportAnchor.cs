namespace Mked.Domain;

/// <summary>
/// A stable scroll anchor expressed as a 0-based index into a document's top-level block list.
/// </summary>
public readonly record struct ViewportAnchor(int BlockIndex)
{
    /// <summary>
    /// Represents the absence of a block anchor. Returned by <see cref="ViewerState"/> when
    /// the underlying document has no blocks.
    /// </summary>
    public static readonly ViewportAnchor None = new(-1);

    /// <summary>Returns <see langword="true"/> when this anchor does not refer to a block.</summary>
    public bool IsNone => BlockIndex < 0;
}
