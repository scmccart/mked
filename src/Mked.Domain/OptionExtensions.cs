using System.Diagnostics;

namespace Mked.Domain;

/// <summary>Composition extensions for <see cref="Option{T}"/>.</summary>
public static class OptionExtensions
{
    /// <summary>Transforms the present value; passes <c>None</c> through unchanged.</summary>
    public static Option<B> Map<A, B>(this Option<A> option, Func<A, B> mapper) =>
        option switch
        {
            Option<A>.Some(var value) => Option.Some(mapper(value)),
            Option<A>.None => Option.None<B>(),
            _ => throw new UnreachableException(),
        };

    /// <summary>Chains a step that may produce nothing; passes <c>None</c> through unchanged.</summary>
    public static Option<B> Bind<A, B>(this Option<A> option, Func<A, Option<B>> binder) =>
        option switch
        {
            Option<A>.Some(var value) => binder(value),
            Option<A>.None => Option.None<B>(),
            _ => throw new UnreachableException(),
        };

    /// <summary>Exhaustively consumes the option by applying one of two functions.</summary>
    public static TOut Match<T, TOut>(
        this Option<T> option,
        Func<T, TOut> onSome,
        Func<TOut> onNone) =>
        option switch
        {
            Option<T>.Some(var value) => onSome(value),
            Option<T>.None => onNone(),
            _ => throw new UnreachableException(),
        };

    /// <summary>Returns the present value, or <paramref name="fallback"/> when absent.</summary>
    public static T UnwrapOr<T>(this Option<T> option, T fallback) =>
        option switch
        {
            Option<T>.Some(var value) => value,
            _ => fallback,
        };

    /// <summary>Converts <c>None</c> to <c>Err</c>; passes <c>Some</c> through as <c>Ok</c>.</summary>
    public static Result<T, E> OkOrErr<T, E>(this Option<T> option, E error) =>
        option switch
        {
            Option<T>.Some(var value) => Result.Ok<T, E>(value),
            _ => Result.Err<T, E>(error),
        };
}
