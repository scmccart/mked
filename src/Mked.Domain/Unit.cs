namespace Mked.Domain;

/// <summary>Represents a void return value in ROP pipelines that produce no meaningful result.</summary>
public readonly record struct Unit
{
    /// <summary>The singleton unit value.</summary>
    public static readonly Unit Value = new();
}
