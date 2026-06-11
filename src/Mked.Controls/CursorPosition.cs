namespace Mked.Controls;

/// <summary>A 1-based line and column location within a text buffer.</summary>
public readonly record struct CursorPosition(int Line, int Column);
