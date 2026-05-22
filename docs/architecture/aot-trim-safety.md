# AOT and trim safety

mked is published as a NativeAOT, trimmed, self-contained single-file executable. This affects what .NET features are safe to use throughout the codebase.

## What NativeAOT and trimming do

**Trimming** runs the IL linker at publish time and removes any code that cannot be statically proven reachable. Code only reached via reflection may be silently stripped.

**NativeAOT** compiles the entire program ahead-of-time to native machine code. There is no JIT, no IL interpreter, and no runtime code generation. Any feature that emits or interprets IL at runtime will fail.

## Forbidden patterns

Avoid these in all `src/` projects.

### Reflection without annotations

```csharp
// WRONG — GetMethod may be trimmed away
var method = typeof(MyClass).GetMethod("DoThing");

// OK — annotate the type parameter if reflection is unavoidable
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
void Invoke(Type t) { ... }
```

### `dynamic`

`dynamic` requires the Dynamic Language Runtime, which relies on runtime IL generation.

```csharp
dynamic obj = GetSomething(); // WRONG
```

### `Activator.CreateInstance` without registration

```csharp
Activator.CreateInstance(typeof(MyWidget)); // WRONG — type may be trimmed

new MyWidget() // OK
```

Use `new T()` or a registered factory instead.

### `JsonSerializer` without source generation

```csharp
// WRONG — uses reflection to discover properties at runtime
JsonSerializer.Serialize(myObject);

// OK — compile-time generated serializer
[JsonSerializable(typeof(MyObject))]
internal partial class AppJsonContext : JsonSerializerContext { }

JsonSerializer.Serialize(myObject, AppJsonContext.Default.MyObject);
```

### `Regex` without `[GeneratedRegex]`

```csharp
// WRONG — RegexOptions.Compiled uses Reflection.Emit, unavailable under NativeAOT
var re = new Regex(@"\*\*(.+?)\*\*", RegexOptions.Compiled);

// TOLERATED but slow — interpreted Regex works under NativeAOT; avoid in hot paths
var re = new Regex(@"\*\*(.+?)\*\*");

// PREFERRED — source-generated at build time; fastest and fully AOT/trim-safe
[GeneratedRegex(@"\*\*(.+?)\*\*")]
private static partial Regex BoldPattern();
```

### `Assembly.Load` and late-bound type discovery

Any runtime assembly loading or plugin-style scanning is incompatible with NativeAOT.

## What is safe

- `new T()` with concrete types and static dispatch
- Generic methods with concrete type arguments
- Interface dispatch (vtable calls)
- LINQ to objects
- `Span<T>`, `Memory<T>`, `ReadOnlySpan<T>`
- Source-generated code (`[GeneratedRegex]`, `[JsonSerializable]`, etc.)
- Markdig pipeline APIs — build the pipeline explicitly with named extension methods
- Spectre.Console rendering pipeline — verify per release (see next section)

## Spectre.Console.Cli and AOT

Spectre.Console.Cli historically used reflection to bind command settings. Track the upstream AOT support issue and prefer explicit registration over auto-discovery. Register command types explicitly in the app host configuration rather than relying on assembly scanning.

## Evaluating a new NuGet dependency

Before adding any package:

1. Does the `.nuspec` / `.csproj` include `<IsTrimmable>true</IsTrimmable>`?
2. Is `<IsAotCompatible>true</IsAotCompatible>` declared?
3. Are there trim or AOT warnings in the package README or release notes?

If a package is not trim-safe, prefer an alternative or confine its use to a thin adapter that can be isolated with `[UnconditionalSuppressMessage]` and a clear comment explaining why it is safe.

## Testing the publish

Run a NativeAOT publish and inspect the output for trim and AOT warnings:

```powershell
dotnet publish src/Mked.Console/Mked.Console.csproj `
  -r win-x64 `
  --self-contained `
  -p:PublishAot=true `
  -c Release
```

Trim warnings appear inline during the build. Any `ILLink` or `AOT` warning must be resolved before merging.

## CI enforcement

The release workflow should set `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in `Mked.Console.csproj` so trim and AOT warnings fail the build automatically.
