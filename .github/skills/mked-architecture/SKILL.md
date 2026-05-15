---
name: mked-architecture
description: "Review code changes in mked for Clean Architecture layer boundary violations, Railway-Oriented Programming correctness, and Result<T,E> / Option<T> usage. Use when asked to review architecture, check layer boundaries, or audit ROP usage in mked."
---

# mked Architecture Review Skill

Review the provided code for compliance with mked's architectural conventions.

## Step 1 — Identify Changed Files

Determine which projects the changed files belong to:
- `Mked.Domain` — inner layer (entities, value objects, interfaces, result types)
- `Mked.Application` — use cases
- `Mked.Infrastructure` — I/O adapters
- `Mked.Console` — Presentation / entry point

## Step 2 — Check Layer Dependencies

For each file, verify that its `using` statements and project references obey the dependency rule:

| Project | May reference | Must NOT reference |
|---|---|---|
| `Mked.Domain` | BCL only | Everything else |
| `Mked.Application` | `Mked.Domain` | `Mked.Infrastructure`, `Mked.Console`, Spectre.Console, `System.IO` |
| `Mked.Infrastructure` | `Mked.Domain`, BCL, I/O libs | `Mked.Application`, `Mked.Console` |
| `Mked.Console` | All | *(composition root — can reference all)* |

Report any violation as: `[VIOLATION] {file}: {project} references {forbidden-project}`

## Step 3 — ROP / Result<T,E> Audit

Scan Application and Infrastructure for:

**Missing Result return:**
```csharp
// BAD — throws instead of returning Result
public async Task<MarkdownDocument> OpenAsync(string path)
{
    if (!File.Exists(path)) throw new FileNotFoundException(path);
```

**Should be:**
```csharp
public async Task<Result<MarkdownDocument, MkedError>> OpenAsync(string path)
{
    if (!File.Exists(path)) return Result.Err<MarkdownDocument, MkedError>(
        new MkedError.IoError(path, "File not found"));
```

**Unsafe Unwrap:**
```csharp
// BAD — can throw at runtime
var doc = result.Unwrap();
```

**Match not exhaustive** (only handling success, ignoring failure):
```csharp
// BAD
if (result.IsOk) { /* use result */ }
```

## Step 4 — Option<T> vs null

In Domain and Application, `null` should not be used for intentional optionality. Flag:
```csharp
// BAD in Domain/Application
public FrontMatter? FrontMatter { get; }

// GOOD
public Option<FrontMatter> FrontMatter { get; }
```

## Step 5 — Report

Summarise findings as:

```
## Architecture Review

### Layer Boundary: ✅ / ⚠️ violations found
- [list violations or "No violations"]

### ROP / Result<T,E>: ✅ / ⚠️ issues found
- [list issues or "No issues"]

### Option<T> vs null: ✅ / ⚠️ issues found
- [list issues or "No issues"]

### Recommendation
[Overall: Approved / Needs changes + summary]
```
