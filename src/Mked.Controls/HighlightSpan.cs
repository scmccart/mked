namespace Mked.Controls;

/// <summary>Associates a <see cref="TextRange"/> in the document source with a <see cref="HighlightKind"/>.</summary>
internal readonly record struct HighlightSpan(TextRange Range, HighlightKind Kind);
