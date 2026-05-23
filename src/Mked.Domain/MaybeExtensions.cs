using System.Diagnostics;

namespace Mked.Domain;

/// <summary>Composition extensions for <see cref="Maybe{T}"/>.</summary>
public static class MaybeExtensions
{
    /// <summary>Transforms the present value; passes <c>None</c> through unchanged.</summary>
    public static Maybe<B> Map<A, B>(this Maybe<A> maybe, Func<A, B> mapper) =>
        maybe switch
        {
            Maybe<A>.Some(var value) => Maybe.Some(mapper(value)),
            Maybe<A>.None => Maybe.None<B>(),
            _ => throw new UnreachableException(),
        };

    /// <summary>Chains a step that may produce nothing; passes <c>None</c> through unchanged.</summary>
    public static Maybe<B> Bind<A, B>(this Maybe<A> maybe, Func<A, Maybe<B>> binder) =>
        maybe switch
        {
            Maybe<A>.Some(var value) => binder(value),
            Maybe<A>.None => Maybe.None<B>(),
            _ => throw new UnreachableException(),
        };

    /// <summary>Exhaustively consumes the maybe by applying one of two functions.</summary>
    public static TOut Match<T, TOut>(
        this Maybe<T> maybe,
        Func<T, TOut> onSome,
        Func<TOut> onNone) =>
        maybe switch
        {
            Maybe<T>.Some(var value) => onSome(value),
            Maybe<T>.None => onNone(),
            _ => throw new UnreachableException(),
        };

    /// <summary>Returns the present value, or <paramref name="fallback"/> when absent.</summary>
    public static T UnwrapOr<T>(this Maybe<T> maybe, T fallback) =>
        maybe switch
        {
            Maybe<T>.Some(var value) => value,
            Maybe<T>.None => fallback,
            _ => throw new UnreachableException(),
        };

    /// <summary>Converts <c>None</c> to <c>Err</c>; passes <c>Some</c> through as <c>Ok</c>.</summary>
    public static Result<T, E> OkOrErr<T, E>(this Maybe<T> maybe, E error) =>
        maybe switch
        {
            Maybe<T>.Some(var value) => Result.Ok<T, E>(value),
            Maybe<T>.None => Result.Err<T, E>(error),
            _ => throw new UnreachableException(),
        };
}
