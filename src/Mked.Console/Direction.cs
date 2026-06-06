namespace Mked.Console;

/// <summary>Cardinal directions used by cursor movement actions.</summary>
public enum Direction
{
    /// <summary>Move toward the beginning of the line.</summary>
    Left,

    /// <summary>Move toward the end of the line.</summary>
    Right,

    /// <summary>Move to the previous line.</summary>
    Up,

    /// <summary>Move to the next line.</summary>
    Down,
}
