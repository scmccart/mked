# Result Types

## Overview

mked uses hand-rolled `Result<T,E>` and `Maybe<T>` types for Railway-Oriented Programming. There are no external library dependencies. The types are defined in the `Mked.Domain` project and used throughout the codebase.

## Result\<T,E\>

Represents an operation that either succeeds with a value of type `T` or fails with an error of type `E`.

### Definition

```csharp
namespace Mked.Domain;

public abstract record Result<T, E>
{
    public sealed record Ok(T Value) : Result<T, E>;
    public sealed record Err(E Error) : Result<T, E>;

    public bool IsOk => this is Ok;
    public bool IsErr => this is Err;
}
```

### Factory Methods

```csharp
public static class Result
{
    public static Result<T, E> Ok<T, E>(T value)    => new Result<T, E>.Ok(value);
    public static Result<T, E> Err<T, E>(E error)   => new Result<T, E>.Err(error);
}
```

### Core Extensions

```csharp
public static class ResultExtensions
{
    // Transform success value
    public static Result<B, E> Map<A, B, E>(
        this Result<A, E> result, Func<A, B> mapper);

    // Chain fallible operations
    public static Result<B, E> Bind<A, B, E>(
        this Result<A, E> result, Func<A, Result<B, E>> binder);

    // Transform error type
    public static Result<T, F> MapError<T, E, F>(
        this Result<T, E> result, Func<E, F> mapper);

    // Pattern match
    public static TOut Match<T, E, TOut>(
        this Result<T, E> result,
        Func<T, TOut> onOk,
        Func<E, TOut> onErr);

    // Async variants
    public static Task<Result<B, E>> MapAsync<A, B, E>(...);
    public static Task<Result<B, E>> BindAsync<A, B, E>(...);
}
```

### Unwrapping (use sparingly)

```csharp
// Throws InvalidOperationException if Err — only use when failure is a programming error
T value = result.Unwrap();

// Safe unwrap with fallback
T value = result.UnwrapOr(defaultValue);
```

## Maybe\<T\>

Represents a value that may or may not be present. Use in place of `null` for intentional optionality.

### Definition

```csharp
namespace Mked.Domain;

public abstract record Maybe<T>
{
    public sealed record Some(T Value) : Maybe<T>;
    public sealed record None : Maybe<T>;

    public bool IsSome => this is Some;
    public bool IsNone => this is None;
}
```

### Factory Methods

```csharp
public static class Maybe
{
    public static Maybe<T> Some<T>(T value) => new Maybe<T>.Some(value);
    public static Maybe<T> None<T>()        => new Maybe<T>.None();
}
```

### Core Extensions

```csharp
public static class MaybeExtensions
{
    public static Maybe<B> Map<A, B>(this Maybe<A> maybe, Func<A, B> mapper);
    public static Maybe<B> Bind<A, B>(this Maybe<A> maybe, Func<A, Maybe<B>> binder);

    public static TOut Match<T, TOut>(
        this Maybe<T> maybe,
        Func<T, TOut> onSome,
        Func<TOut> onNone);

    public static T UnwrapOr<T>(this Maybe<T> maybe, T fallback);

    // Bridge: treat missing value as a specific error
    public static Result<T, E> OkOrErr<T, E>(this Maybe<T> maybe, E error);
}
```

## Error Types

Domain-specific error types are defined as a discriminated union in `Mked.Domain`:

```csharp
public abstract record MkedError
{
    public sealed record IoError(string Path, string Reason) : MkedError;
    public sealed record ParseError(int Line, int Column, string Message) : MkedError;
    public sealed record ValidationError(string Field, string Message) : MkedError;
    public sealed record StreamError(string Reason) : MkedError;
}
```

## Conventions

1. **Never throw for expected failures.** Return `Result.Err(...)` instead.
2. **Use `Maybe<T>` for intentional absence**, not `null`. Reserve `null` for uninitialised state that should never reach user code.
3. **Keep error types in the Domain layer.** Infrastructure and Application translate native exceptions (e.g., `IOException`) into domain errors at the boundary.
4. **Prefer `Match` over `if IsOk`.** Pattern matching is exhaustive and safer.
5. **Async pipelines use `BindAsync` / `MapAsync`.** Do not `await` inside a `Bind` lambda — use the async variants.
