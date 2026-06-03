# Result Types

## Overview

mked uses hand-rolled `Result<T,E>` and `Option<T>` types for Railway-Oriented Programming. There are no external library dependencies. The types are defined in the `Mked.Domain` project and used throughout the codebase.

## Result\<T,E\>

Represents an operation that either succeeds with a value of type `T` or fails with an error of type `E`.

### Definition

```csharp
namespace Mked.Domain;

public abstract class Result<T, E>
{
    public sealed class Ok(T value) : Result<T, E>
    {
        public T Value { get; } = value;
    }

    public sealed class Err(E error) : Result<T, E>
    {
        public E Error { get; } = error;
    }

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
        Func<T, TOut> onSuccess,
        Func<E, TOut> onFailure);

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

## Option\<T\>

Represents a value that may or may not be present. Use in place of `null` for intentional optionality.

### Definition

```csharp
namespace Mked.Domain;

public abstract class Option<T>
{
    public sealed class Some(T value) : Option<T>
    {
        public T Value { get; } = value;
    }

    public sealed class None : Option<T> { }

    public bool IsSome => this is Some;
    public bool IsNone => this is None;
}
```

### Core Extensions

```csharp
public static class OptionExtensions
{
    public static Option<B> Map<A, B>(this Option<A> option, Func<A, B> mapper);
    public static Option<B> Bind<A, B>(this Option<A> option, Func<A, Option<B>> binder);

    public static TOut Match<T, TOut>(
        this Option<T> option,
        Func<T, TOut> onSome,
        Func<TOut> onNone);

    public static T UnwrapOr<T>(this Option<T> option, T fallback);

    // Bridge: treat missing value as a specific error
    public static Result<T, E> OkOrErr<T, E>(this Option<T> option, E error);
}
```

## Error Types

Define domain-specific error types as discriminated unions in `Mked.Domain`:

```csharp
public abstract class MkedError
{
    public sealed class IoError(string path, string reason) : MkedError;
    public sealed class ParseError(int line, int column, string message) : MkedError;
    public sealed class ValidationError(string field, string message) : MkedError;
    public sealed class StreamError(string reason) : MkedError;
}
```

## Conventions

1. **Never throw for expected failures.** Return `Result.Err(...)` instead.
2. **Use `Option<T>` for intentional absence**, not `null`. Reserve `null` for uninitialised state that should never reach user code.
3. **Keep error types in the Domain layer.** Infrastructure and Application translate native exceptions (e.g., `IOException`) into domain errors at the boundary.
4. **Prefer `Match` over `if IsOk`.** Pattern matching is exhaustive and safer.
5. **Async pipelines use `BindAsync` / `MapAsync`.** Do not `await` inside a `Bind` lambda — use the async variants.
