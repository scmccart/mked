namespace Mked.Domain;

/// <summary>
/// A stable scroll anchor expressed as a 0-based index into a document's top-level block list.
/// </summary>
public readonly record struct ViewportAnchor(int BlockIndex);
