# Solution structure

## Repository layout

```
mked/
├── mked.sln
├── src/
│   ├── Mked.Domain/
│   │   └── Mked.Domain.csproj
│   ├── Mked.Application/
│   │   └── Mked.Application.csproj
│   ├── Mked.Infrastructure/
│   │   └── Mked.Infrastructure.csproj
│   ├── Mked.Controls/
│   │   └── Mked.Controls.csproj
│   └── Mked.Console/
│       └── Mked.Console.csproj
└── tests/
    ├── Mked.Domain.Tests/
    │   └── Mked.Domain.Tests.csproj
    ├── Mked.Application.Tests/
    │   └── Mked.Application.Tests.csproj
    ├── Mked.Infrastructure.Tests/
    │   └── Mked.Infrastructure.Tests.csproj
    └── Mked.Controls.Tests/
        └── Mked.Controls.Tests.csproj
```

## Projects

### Mked.Domain

The core domain layer. Contains entities, value objects, domain interfaces, `Result<T,E>`, and `Option<T>`.

- **Output type**: class library (not shipped independently)
- **NuGet dependencies**: Markdig (AST types used in domain model)
- **Project references**: none

### Mked.Application

Named, testable use cases. No I/O; depends only on domain interfaces.

- **Output type**: class library (not shipped independently)
- **NuGet dependencies**: none
- **Project references**: `Mked.Domain`

### Mked.Infrastructure

OS-facing adapters: file system, standard input, file watcher.

- **Output type**: class library (not shipped independently)
- **NuGet dependencies**: none
- **Project references**: `Mked.Domain`

### Mked.Controls

Spectre.Console extension library providing `MarkdownViewer` and `MarkdownEditor` widgets.

- **Output type**: NuGet package (`Mked.Controls`)
- **NuGet dependencies**: Spectre.Console, Markdig
- **Project references**: none

`Mked.Controls` is intentionally decoupled from `Mked.Domain` so it can be published as a standalone, reusable NuGet package. The widgets accept and return plain `string` values rather than internal domain types.

### Mked.Console

CLI entry point. Wires commands, DI, and the application together.

- **Output type**: dotnet tool (`mked`), NativeAOT single-file executable
- **NuGet dependencies**: Spectre.Console.Cli
- **Project references**: `Mked.Application`, `Mked.Infrastructure`, `Mked.Controls`

## Dependency graph

```
Mked.Console ──→ Mked.Application ──→ Mked.Domain
             ──→ Mked.Infrastructure ──→ Mked.Domain
             ──→ Mked.Controls
```

Dependencies flow inward. `Mked.Application` and `Mked.Domain` never reference infrastructure or presentation code. `Mked.Controls` sits outside the domain stack so it remains independently publishable.

## Test projects

Each `src/` project except `Mked.Console` has a corresponding test project under `tests/`. Test projects use xUnit, Moq, and AwesomeAssertions and are not AOT-compiled.

| Test project | Scope |
|---|---|
| `Mked.Domain.Tests` | Value objects, `Result`/`Option` logic, pure domain rules |
| `Mked.Application.Tests` | Use cases with in-memory fakes of domain interfaces |
| `Mked.Infrastructure.Tests` | Adapter behaviour against a temp directory or in-memory stream |
| `Mked.Controls.Tests` | Widget state transitions, render output, keyboard input handling |

`Mked.Console` has no dedicated unit test project; CLI behaviour is verified through integration tests or manual testing.

## NuGet package identities

| Package | Project | ID |
|---|---|---|
| Controls library | `Mked.Controls` | `Mked.Controls` |
| dotnet tool | `Mked.Console` | `mked` |
