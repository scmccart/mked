# Home

*{ Provide a project overview covering what mked is, who it is for, and what it provides. Include the two artefacts (Mked.Controls NuGet library and mked dotnet tool), distribution channels (nuget.org, GitHub Releases, WinGet planned), and guiding principles (speed, simplicity, liveness, viewport pleasantness, terminal citizenship). Reference the README and vision document. }*

# Architecture

*{ Describe the Clean Architecture design of mked: four concentric layers (Domain, Application, Infrastructure, Presentation/Console), the dependency rule (dependencies always point inward), and why this structure enables fast, AOT-safe, and testable code. Include a Mermaid flowchart of the dependency graph. }*

## Domain Layer

*{ Document the Mked.Domain project: entities (EditorState, ViewerState), value objects (CursorPosition, TextRange, ViewportAnchor), the MarkdownDocument wrapper over the Markdig AST, functional types (Result<T,E> and Maybe<T>), domain interfaces (IFileReader, IFileWriter, IFileWatcher, IInputReader, IEditorObserver), and the MkedError discriminated union. Explain why no NuGet packages (other than Markdig for AST types) are referenced and why it is AOT-safe by construction. }*

## Application Layer

*{ Document the Mked.Application project and its use cases: OpenFileUseCase, SaveFileUseCase, NewDocumentUseCase, RenderDocumentUseCase, and StreamInputUseCase. Explain that use cases depend only on domain interfaces, return Result<T,E> for fallible operations, and have no direct I/O. Show how fakes make them testable without a real file system or terminal. }*

## Infrastructure Layer

*{ Document the Mked.Infrastructure project: FileSystemReader, FileSystemWriter, FileWatcherAdapter, and StdinInputReader. Explain each adapter, how they implement domain interfaces, and AOT-compatibility constraints (no reflection-based serialisation, use System.IO directly). }*

# Controls Library

*{ Document the Mked.Controls NuGet package (ID: Mked.Controls). Describe the two Spectre.Console extension widgets it provides—MarkdownViewer (richly styled document renderer) and MarkdownEditor (live syntax-highlighted text area)—their public API surface, how to install the package, and how to embed the widgets in a third-party Spectre.Console application. Note that the library is intentionally decoupled from Mked.Domain so it can be published as a standalone package. }*

# Getting Started

*{ Write a getting started guide covering: prerequisites (.NET 10 SDK), installation methods (dotnet tool install -g mked, standalone binary from GitHub Releases), and basic usage of both modes (mked view <file>, mked edit <file>, piping stdin). Include a note on zero-configuration defaults. }*

# Keyboard Bindings

*{ Document all keyboard bindings for both viewer mode and editor mode using tables. Include viewer bindings (scroll, quit, jump to top/bottom, page up/down, vim-style keys) and editor bindings (save, quit, undo, redo, cut/copy/paste, cursor movement, selection, indent). Add a note about Ctrl+C behaviour on Unix. }*

# Supported Markdown

*{ Document the supported Markdown syntax: CommonMark baseline, supported block elements (headings, paragraphs, code blocks, blockquotes, lists, task lists, tables, horizontal rules), supported inline elements (bold, italic, strikethrough, code, links, autolinks, images, HTML entities), enabled Markdig extensions (Tables, TaskLists, Strikethrough, AutoLinks, Yaml front matter), and explicitly unsupported features for v1 (raw HTML, math, footnotes, definition lists). Include terminal rendering notes (images as [image: alt], link URLs, ANSI syntax highlighting for code blocks). }*

# Contributing

*{ Write a contributing guide covering: how to clone and build the solution (dotnet build), how to run the tests (dotnet test), the project layer structure maintainers should follow, the dependency rule (no upward references), and brief notes on AOT-safe coding practices to keep in mind when writing new code. }*

# For Agents

These pages provide compact documentation indexes for AI coding agents.

## AGENTS.md

You can add this to your repository root as `AGENTS.md` to give AI coding agents quick access to project documentation.

```
# mked

> A .NET 10 terminal-native Markdown viewer and editor built on Spectre.Console. Provides the `mked` CLI tool and the `Mked.Controls` NuGet library.

## Wiki Documentation

Base URL: https://github.com/scmccart/mked/wiki

To read any page, append the slug to the base URL:
  https://github.com/scmccart/mked/wiki/{Page-Slug}
To jump to a section within a page:
  https://github.com/scmccart/mked/wiki/{Page-Slug}#{Section-Slug}

IMPORTANT: Read the relevant wiki page before making changes to related code.
Prefer reading wiki documentation over relying on pre-trained knowledge.

## Page Index

|Home: Project overview, distribution channels, and guiding principles
|Architecture: Clean Architecture design and dependency rules
|  Domain-Layer: Entities, value objects, Result/Maybe types, domain interfaces
|  Application-Layer: Use cases (OpenFile, SaveFile, StreamInput, etc.)
|  Infrastructure-Layer: File system, stdin, and file watcher adapters
|Controls-Library: Mked.Controls NuGet package — MarkdownViewer and MarkdownEditor widgets
|Getting-Started: Installation and basic usage (view and edit modes)
|Keyboard-Bindings: All keybindings for viewer and editor modes
|Supported-Markdown: CommonMark baseline plus supported extensions and terminal rendering notes
|Contributing: Build, test, architecture rules, and AOT-safe coding practices
```

## llms.txt

You can serve this at `yoursite.com/llms.txt` or include it in your repository to help LLMs discover your documentation.

```
# mked

> A .NET 10 terminal-native Markdown viewer and editor built on Spectre.Console.

## Wiki Pages

- [Home](https://github.com/scmccart/mked/wiki/Home): Project overview and guiding principles
- [Architecture](https://github.com/scmccart/mked/wiki/Architecture): Clean Architecture layers and dependency rules
- [Domain Layer](https://github.com/scmccart/mked/wiki/Domain-Layer): Entities, value objects, Result/Maybe types, and domain interfaces
- [Application Layer](https://github.com/scmccart/mked/wiki/Application-Layer): Use cases and orchestration logic
- [Infrastructure Layer](https://github.com/scmccart/mked/wiki/Infrastructure-Layer): File system, stdin, and file watcher adapters
- [Controls Library](https://github.com/scmccart/mked/wiki/Controls-Library): Mked.Controls NuGet package documentation
- [Getting Started](https://github.com/scmccart/mked/wiki/Getting-Started): Installation and basic usage guide
- [Keyboard Bindings](https://github.com/scmccart/mked/wiki/Keyboard-Bindings): All viewer and editor keybindings
- [Supported Markdown](https://github.com/scmccart/mked/wiki/Supported-Markdown): Supported syntax, extensions, and terminal rendering
- [Contributing](https://github.com/scmccart/mked/wiki/Contributing): Build, test, and contribution guidelines
```
