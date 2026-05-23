namespace Mked.Domain;

/// <summary>
/// Represents an operation that either succeeds with a value of type <typeparamref name="T"/>
/// or fails with an error of type <typeparamref name="E"/>.
/// </summary>
public abstract record Result<T, E>
{
    /// <summary>Represents a successful result.</summary>
    public sealed record Ok(T Value) : Result<T, E>;

    /// <summary>Represents a failed result.</summary>
    public sealed record Err(E Error) : Result<T, E>;

    /// <summary>Returns <see langword="true"/> when this result is <see cref="Ok"/>.</summary>
    public bool IsOk => this is Ok;

    /// <summary>Returns <see langword="true"/> when this result is <see cref="Err"/>.</summary>
    public bool IsErr => this is Err;
}

/// <summary>Factory methods for creating <see cref="Result{T,E}"/> values.</summary>
public static class Result
{
    /// <summary>Creates a successful result containing <paramref name="value"/>.</summary>
    public static Result<T, E> Ok<T, E>(T value) => new Result<T, E>.Ok(value);

    /// <summary>Creates a failed result containing <paramref name="error"/>.</summary>
    public static Result<T, E> Err<T, E>(E error) => new Result<T, E>.Err(error);
}

