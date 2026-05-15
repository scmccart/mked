---
name: mked-aot-safety
description: "Audit C# code in mked for NativeAOT and PublishTrimmed compatibility. Detects reflection, dynamic code, missing source generators, and other patterns that break AOT compilation. Use when asked to check AOT safety, trim compatibility, or before adding a new NuGet dependency."
---

# mked AOT Safety Skill

Audit the provided code for NativeAOT (`/p:PublishAot=true`) and trim (`/p:PublishTrimmed=true`) compatibility.

## Step 1 — Reflection Patterns (FAIL)

Scan for:

| Pattern | Risk | Fix |
|---|---|---|
| `Activator.CreateInstance(type)` | Trim removes types | Use DI or factory |
| `Type.GetMethod(name)` | Trim removes methods | Use `[DynamicDependency]` or avoid |
| `Assembly.GetTypes()` | Trim removes types | Avoid; use explicit registration |
| `typeof(T).GetProperties()` | Trim removes properties | Use source generators |
| `Delegate.CreateDelegate(type, ...)` | AOT can't JIT | Use explicit delegates |

## Step 2 — JSON Serialization (FAIL without source gen)

```csharp
// UNSAFE
JsonSerializer.Deserialize<FrontMatter>(json);
JsonSerializer.Serialize(obj);

// SAFE — requires:
[JsonSerializable(typeof(FrontMatter))]
internal partial class MkedJsonContext : JsonSerializerContext { }

JsonSerializer.Deserialize(json, MkedJsonContext.Default.FrontMatter);
```

Flag any `JsonSerializer.Deserialize<T>` or `JsonSerializer.Serialize<T>` without a `JsonSerializerContext`.

## Step 3 — Regex (WARN without source gen)

```csharp
// WARN — compiled at runtime
new Regex(@"^#{1,6}\s");

// SAFE
[GeneratedRegex(@"^#{1,6}\s")]
private static partial Regex HeadingPattern();
```

Flag `new Regex(...)` and `Regex.IsMatch(input, pattern)` as warnings.

## Step 4 — Dynamic Code

```csharp
// FAIL
dynamic obj = GetValue();
Expression.Lambda(...).Compile();
ILGenerator il = ...;
```

Flag any `dynamic` usage, `System.Linq.Expressions.Expression.Compile()`, and `System.Reflection.Emit`.

## Step 5 — Spectre.Console.Cli

Spectre.Console.Cli uses reflection for settings binding. Flag command settings classes that:
- Use complex property types without `[TypeConverter]`
- Rely on runtime type discovery

Reference the Spectre.Console AOT tracking issue for current status.

## Step 6 — New NuGet Dependencies

When a new package is added, check:
1. Does the package have a `PublishAot` or `IsTrimmable` property in its targets?
2. Does the package README mention AOT/trim support?
3. Run `dotnet build /p:PublishTrimmed=true` and check for ILLink warnings.

## Step 7 — Report

```
## AOT Safety Audit

### Reflection: ✅ Clean / ❌ {N} violations
- [list or "None"]

### JSON serialization: ✅ Clean / ❌ {N} violations
- [list or "None"]

### Regex: ✅ Clean / ⚠️ {N} warnings
- [list or "None"]

### Dynamic code: ✅ Clean / ❌ {N} violations
- [list or "None"]

### NuGet dependencies: ✅ AOT-safe / ⚠️ unverified / ❌ known unsafe
- [list or "None"]

### Verdict: ✅ AOT-safe / ⚠️ Review needed / ❌ Will break AOT
```
