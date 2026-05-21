# mked — Project Guidelines

## Project Overview

**mked** is a .NET 10 terminal-native tool and library for viewing and editing Markdown in the console.

Two deliverables:
- **`Mked.Controls`** — NuGet library extending Spectre.Console with `MarkdownEditor` and `MarkdownViewer` controls.
- **`mked`** — self-contained, AOT-compiled dotnet tool (and eventual WinGet package) with `view` and `edit` modes.

## Tech Stack

| Concern | Choice |
|---|---|
| Runtime | .NET 10 |
| Console UI | Spectre.Console |
| CLI parsing | Spectre.Console.Cli |
| Markdown parsing | Markdig |
| Error handling | Hand-rolled `Result<T,E>` / `Option<T>` (no external ROP library) |
| Publishing | Self-contained, trimmed, NativeAOT single-file executable |

## Architecture

Clean Architecture with four layers:

```
Mked.Console (Presentation)  →  Mked.Application  →  Mked.Domain
Mked.Infrastructure                                →  Mked.Domain
```

- **Domain** (`Mked.Domain`): entities, value objects, interfaces, `Result<T,E>`, `Option<T>`
- **Application** (`Mked.Application`): use cases — returns `Result<T,E>`, no I/O
- **Infrastructure** (`Mked.Infrastructure`): file system, stdin, file watcher
- **Presentation** (`Mked.Console`): Spectre.Console.Cli commands, DI wiring

Dependencies flow inward only. Application and Domain never reference Infrastructure or Console.

## Error Handling (ROP)

All operations that can fail return `Result<T,E>` — not exceptions. Use `Result.Ok(value)` and `Result.Err(error)` factory methods. Chain with `.Bind()`, `.Map()`, `.MapError()`. For async, use `.BindAsync()` / `.MapAsync()`. See `docs/architecture/result-types.md`.

## Code Style

- **File-scoped namespaces** (`namespace Mked.Domain;` not `namespace Mked.Domain { }`)
- **Line endings**: `lf`
- **Indent**: 4 spaces for C#, 2 for JSON/YAML/Markdown
- **`var`**: use when type is apparent from the right-hand side; prefer explicit type otherwise
- **Primary constructors** for DI injection where appropriate
- **XML doc comments** on all public API members
- **No `this.` qualification**

## AOT / Trim Safety

The tool is published as NativeAOT. Generated code must be trim-safe:

- Avoid reflection, `dynamic`, `Activator.CreateInstance`, `Type.GetMethod` on non-attributed types
- Avoid `JsonSerializer` without source generation (`[JsonSerializable]`)
- Avoid `Regex` without `[GeneratedRegex]` attribute
- Spectre.Console.Cli: track AOT support status; use source-generator binding when available
- Test publish with `--self-contained --aot` before merging features that add new dependencies

## Testing

- Framework: **MSTest v3** (already configured in the solution)
- Mocking: **NSubstitute** preferred (AOT-compatible)
- Pattern: Arrange / Act / Assert
- Application use cases are unit-testable via in-memory fakes of Domain interfaces — no file system, no terminal

## PowerShell Scripting Conventions

When generating PowerShell commands that embed text containing backtick characters (e.g., Markdown inline code passed to `gh pr create`, `gh pr edit`, `gh issue comment`, etc.):

- **Use single-quoted here-strings** (`@'...'@`) for multi-line string values. Single-quoted here-strings perform no escape or variable substitution, so backticks pass through literally.
- **Never use backslash as an escape character** in PowerShell — the escape character is `` ` `` (backtick).
- To include a literal backtick inside a *double-quoted* string, double it: ` `` `.

```powershell
# CORRECT — single-quoted here-string, backticks are literal
$body = @'
Use `Result<T,E>` for all fallible operations.
'@
gh pr edit 6 --body $body

# WRONG — backslash-backtick is not a valid escape sequence
gh pr edit 6 --body "Use \`Result<T,E>\` for all fallible operations."
```

## Documentation

Reference docs live in `docs/`:
- `docs/vision.md` — project vision
- `docs/libraries/` — Spectre.Console, Spectre.Console.Cli, Markdig
- `docs/architecture/` — Clean Architecture, ROP, Result types
- `docs/patterns/` — Observer, Strategy, Decorator, Command patterns as used in mked
