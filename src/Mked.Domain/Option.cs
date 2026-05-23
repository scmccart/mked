namespace Mked.Domain;

/// <summary>
/// Represents an optional value: either <see cref="Some"/> containing a value
/// or <see cref="None"/> representing absence. Use in place of <see langword="null"/>
/// for intentional optionality.
/// </summary>
public abstract record Option<T>
{
    /// <summary>Represents a present value.</summary>
    public sealed record Some(T Value) : Option<T>;

    /// <summary>Represents an absent value.</summary>
    public sealed record None : Option<T>;

    /// <summary>Returns <see langword="true"/> when a value is present.</summary>
    public bool IsSome => this is Some;

    /// <summary>Returns <see langword="true"/> when no value is present.</summary>
    public bool IsNone => this is None;
}

/// <summary>Factory methods for creating <see cref="Option{T}"/> values.</summary>
public static class Option
{
    /// <summary>Creates an <see cref="Option{T}"/> containing <paramref name="value"/>.</summary>
    public static Option<T> Some<T>(T value) => new Option<T>.Some(value);

    /// <summary>Creates an absent <see cref="Option{T}"/>.</summary>
    public static Option<T> None<T>() => new Option<T>.None();
}

