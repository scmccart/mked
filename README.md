# mked

A .NET 10 terminal-native tool and library for viewing and editing Markdown.

## Overview

**mked** delivers two complementary artefacts:

- **`Mked.Controls`** — a NuGet library extending [Spectre.Console](https://spectreconsole.net/) with a `MarkdownViewer` and a `MarkdownEditor` control.
- **`mked`** — a self-contained, AOT-compiled dotnet tool providing `view` and `edit` commands from the terminal.

## Features

- **Live syntax highlighting** while typing
- **Viewport stability** — redraws keep the visible region anchored to a recognisable Markdown block
- **Zero configuration** — sensible defaults out of the box
- **AOT-compiled** — instant startup, no .NET runtime required for the standalone executable

## Installation

```sh
# Install as a dotnet tool
dotnet tool install -g mked

# Or download a self-contained binary from GitHub Releases
```

## Usage

```sh
# View a Markdown file
mked view README.md

# Edit a Markdown file
mked edit README.md

# Pipe Markdown from stdin (streaming / tail mode)
cat NOTES.md | mked view
```

## Distribution

| Artefact | Channel |
|---|---|
| `Mked.Controls` NuGet library | nuget.org |
| `mked` dotnet tool | nuget.org |
| Self-contained single-file executable | GitHub Releases |
| WinGet package | WinGet *(planned)* |

## Development

**Prerequisites**: .NET 10 SDK (`global.json` pins the version).

```sh
# Build
dotnet build

# Run tests
dotnet test

# Restore tools and dependencies
dotnet restore
```

See the [`docs/`](docs/) directory for architecture decisions, design documents, and epics.

## License

[MIT](LICENSE)
