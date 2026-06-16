namespace Mked.Controls;

/// <summary>A contiguous selection expressed as a start and end <see cref="CursorPosition"/>.</summary>
internal readonly record struct TextRange(CursorPosition Start, CursorPosition End);
