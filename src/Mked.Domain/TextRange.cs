namespace Mked.Domain;

/// <summary>A contiguous selection expressed as a start and end <see cref="CursorPosition"/>.</summary>
public readonly record struct TextRange(CursorPosition Start, CursorPosition End);
