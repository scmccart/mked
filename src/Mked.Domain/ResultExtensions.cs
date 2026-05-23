using System.Diagnostics;

namespace Mked.Domain;

/// <summary>Composition extensions for <see cref="Result{T,E}"/>.</summary>
public static class ResultExtensions
{
    /// <summary>Transforms the success value; passes <c>Err</c> through unchanged.</summary>
    public static Result<B, E> Map<A, B, E>(this Result<A, E> result, Func<A, B> mapper) =>
        result switch
        {
            Result<A, E>.Ok(var value) => Result.Ok<B, E>(mapper(value)),
            Result<A, E>.Err(var error) => Result.Err<B, E>(error),
            _ => throw new UnreachableException(),
        };

    /// <summary>Chains a fallible step; short-circuits on <c>Err</c>.</summary>
    public static Result<B, E> Bind<A, B, E>(this Result<A, E> result, Func<A, Result<B, E>> binder) =>
        result switch
        {
            Result<A, E>.Ok(var value) => binder(value),
            Result<A, E>.Err(var error) => Result.Err<B, E>(error),
            _ => throw new UnreachableException(),
        };

    /// <summary>Transforms the error type; passes <c>Ok</c> through unchanged.</summary>
    public static Result<T, F> MapError<T, E, F>(this Result<T, E> result, Func<E, F> mapper) =>
        result switch
        {
            Result<T, E>.Ok(var value) => Result.Ok<T, F>(value),
            Result<T, E>.Err(var error) => Result.Err<T, F>(mapper(error)),
            _ => throw new UnreachableException(),
        };

    /// <summary>Exhaustively consumes the result, applying one of two functions.</summary>
    public static TOut Match<T, E, TOut>(
        this Result<T, E> result,
        Func<T, TOut> onOk,
        Func<E, TOut> onErr) =>
        result switch
        {
            Result<T, E>.Ok(var value) => onOk(value),
            Result<T, E>.Err(var error) => onErr(error),
            _ => throw new UnreachableException(),
        };

    /// <summary>
    /// Unwraps the success value; throws <see cref="InvalidOperationException"/> if the result is
    /// <c>Err</c>. Only use when a failure is a programming error, not a runtime condition.
    /// </summary>
    public static T Unwrap<T, E>(this Result<T, E> result) =>
        result switch
        {
            Result<T, E>.Ok(var value) => value,
            Result<T, E>.Err(var error) =>
                throw new InvalidOperationException($"Called Unwrap on Err: {error}"),
            _ => throw new UnreachableException(),
        };

    /// <summary>Returns the success value, or <paramref name="fallback"/> when the result is <c>Err</c>.</summary>
    public static T UnwrapOr<T, E>(this Result<T, E> result, T fallback) =>
        result switch
        {
            Result<T, E>.Ok(var value) => value,
            Result<T, E>.Err => fallback,
            _ => throw new UnreachableException(),
        };

    /// <summary>Asynchronously transforms the success value of a <c>Task&lt;Result&gt;</c>.</summary>
    public static async Task<Result<B, E>> MapAsync<A, B, E>(
        this Task<Result<A, E>> resultTask,
        Func<A, B> mapper) =>
        (await resultTask).Map(mapper);

    /// <summary>Asynchronously chains a fallible step on a <c>Task&lt;Result&gt;</c>.</summary>
    public static async Task<Result<B, E>> BindAsync<A, B, E>(
        this Task<Result<A, E>> resultTask,
        Func<A, Task<Result<B, E>>> binder)
    {
        var result = await resultTask;
        return result switch
        {
            Result<A, E>.Ok(var value) => await binder(value),
            Result<A, E>.Err(var error) => Result.Err<B, E>(error),
            _ => throw new UnreachableException(),
        };
    }
}
