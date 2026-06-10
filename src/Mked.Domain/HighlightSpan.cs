namespace Mked.Domain;

/// <summary>Associates a <see cref="TextRange"/> in the document source with a <see cref="HighlightKind"/>.</summary>
public readonly record struct HighlightSpan(TextRange Range, HighlightKind Kind);
