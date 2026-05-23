namespace Mked.Domain;

/// <summary>
/// Represents an optional value: either <see cref="Some"/> containing a value
/// or <see cref="None"/> representing absence. Use in place of <see langword="null"/>
/// for intentional optionality.
/// </summary>
public abstract record Maybe<T>
{
    /// <summary>Represents a present value.</summary>
    public sealed record Some(T Value) : Maybe<T>;

    /// <summary>Represents an absent value.</summary>
    public sealed record None : Maybe<T>;

    /// <summary>Returns <see langword="true"/> when a value is present.</summary>
    public bool IsSome => this is Some;

    /// <summary>Returns <see langword="false"/> when no value is present.</summary>
    public bool IsNone => this is None;
}

/// <summary>Factory methods for creating <see cref="Maybe{T}"/> values.</summary>
public static class Maybe
{
    /// <summary>Creates a <see cref="Maybe{T}"/> containing <paramref name="value"/>.</summary>
    public static Maybe<T> Some<T>(T value) => new Maybe<T>.Some(value);

    /// <summary>Creates an absent <see cref="Maybe{T}"/>.</summary>
    public static Maybe<T> None<T>() => new Maybe<T>.None();
}
