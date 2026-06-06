namespace Mked.Controls;

/// <summary>A styled region in a text buffer expressed as a character offset and length.</summary>
public readonly record struct StyledSpan(int StartOffset, int Length, Style SpectreStyle);
