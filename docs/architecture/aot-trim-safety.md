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

## Current suppressions

The IL linker and AOT compiler emit warnings when they detect patterns that _might_ strip code
needed at runtime. When a warning comes from mked's own code it must be fixed — either by
annotating the type with `[DynamicallyAccessedMembers]`, switching to a source-generated
alternative (`[GeneratedRegex]`, `[JsonSerializable]`), or restructuring the code.

When a warning comes from inside a third-party library that mked cannot modify, suppression is
the correct response — provided we have verified (via `TrimmerRoots.xml` and manual publish
testing) that the code path the warning refers to _is_ reachable at runtime and the types it
needs _are_ preserved. `<NoWarn>` is the standard MSBuild mechanism for project-wide suppression
of specific warning codes; `[UnconditionalSuppressMessage]` is used for targeted per-call-site
suppression when the affected code is in mked itself.

The following IL warnings are suppressed in `Mked.Console.csproj` via `<NoWarn>` because they
originate inside Spectre.Console.Cli's own assemblies, not in mked's code:

| Warning | Cause |
|---------|-------|
| `IL2026` | Spectre.Console.Cli methods call into code that requires unreferenced code for settings resolution |
| `IL2104` | Assembly-level roll-up: Spectre.Console.Cli produces trim warnings internally |
| `IL3000` | Spectre.Console.Cli accesses `Assembly.Location` at runtime |
| `IL3050` | Spectre.Console.Cli calls members annotated with `[RequiresDynamicCode]` for the settings binder |

In addition, `TypeRegistrar.cs` carries a targeted `[UnconditionalSuppressMessage("Trimming", "IL2067")]`
on the `Register` method because `ITypeRegistrar.Register` (defined by Spectre) lacks the
`[DynamicallyAccessedMembers]` annotation that our implementation satisfies structurally — the
types registered are always one of our explicitly annotated command or settings types.

The `TrimmerRoots.xml` linker descriptor forces the IL linker to preserve
`ViewCommand`, `ViewSettings`, `EditCommand`, `EditSettings`, and `AsyncCommand<TSettings>` in
full, because Spectre's `ConfiguredCommand.FromType<TCommand>()` walks the base-class generic
argument chain via reflection at startup to discover `TSettings`.

`Mked.Controls` declares `<IsTrimmable>true</IsTrimmable>` and `<IsAotCompatible>true</IsAotCompatible>`
so the compiler verifies the library is trim-safe when consumed by the AOT executable.

## Testing the publish

Use the committed publish profiles to produce a native binary and inspect the output for trim and AOT warnings:

```powershell
dotnet publish src/Mked.Console/Mked.Console.csproj -p:PublishProfile=win-x64
```

Replace `win-x64` with the RID matching your development machine
(`linux-x64`, `linux-arm64`, `osx-arm64`, `osx-x64`). The binary lands in
`src/Mked.Console/publish/<rid>/`. Trim warnings appear inline during the build.
Any `ILLink` or `AOT` warning that is not in the suppression table above must be resolved before merging.

## CI enforcement

`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` is set globally in `Directory.Build.props`
and applies to all projects including `Mked.Console`. The matrix AOT publish in the release
workflow therefore fails the build automatically if any new trim or AOT warning appears.
Suppressions in `Mked.Console.csproj` are intentional and documented above — do not add new ones
without updating this table.
