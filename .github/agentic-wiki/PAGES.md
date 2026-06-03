# Home

*{ Provide an overview of mked — what it is, who it is for, and the two deliverables (Mked.Controls NuGet library and mked dotnet tool). Include distribution channels and guiding principles. }*

# Getting Started

*{ Write a getting-started guide covering installation (dotnet tool install, GitHub Releases), basic usage of `mked view` and `mked edit`, and piping stdin into the viewer. }*

# Architecture

*{ Describe the Clean Architecture used in mked: the four layers (Domain, Application, Infrastructure, Presentation), how dependencies flow inward, and why this matters for testability and AOT compatibility. Include a Mermaid diagram of the layer relationships. }*

## Domain Layer

*{ Document the Domain layer (Mked.Domain): entities (EditorState, ViewerState, MarkdownDocument), value objects (CursorPosition, TextRange, ViewportAnchor), interfaces (IFileReader, IFileWriter, IInputReader, IFileWatcher), and the Result/Option types. Note the no-external-dependency constraint. }*

## Application Layer

*{ Document the Application layer (Mked.Application): use cases (OpenFileUseCase, SaveFileUseCase, StreamInputUseCase, RenderDocumentUseCase), how they compose domain interfaces, and the Result<T,E> return convention. }*

## Infrastructure Layer

*{ Document the Infrastructure layer (Mked.Infrastructure): adapters (FileSystemReader, FileSystemWriter, StdinInputReader, FileWatcherAdapter), the boundary pattern of translating OS exceptions into MkedError, and AOT compatibility notes. }*

# Core Concepts

*{ Introduce the two foundational abstractions in mked: Railway-Oriented Programming with Result<T,E>/Option<T>, and the Observer pattern for live preview. Explain why these were chosen over exceptions and events. }*

## Result Types

*{ Document Result<T,E> and Option<T> in detail: definitions, factory methods, core extension methods (Map, Bind, MapError, Match, async variants), MkedError discriminated union, and the five usage conventions. Include code examples. }*

## Design Patterns

*{ Document the three design patterns used in mked: Observer (EditorState/IEditorObserver for live preview), Strategy (IMarkdownRenderer for rendering backends), and Decorator (syntax-highlighting layers). Show code examples for each. }*

# Controls Library

*{ Describe Mked.Controls — the NuGet library extending Spectre.Console. Cover MarkdownViewer (IRenderable, properties, extension method) and MarkdownEditor (IPrompt<string>, properties, extension method). Note that the implementation is in progress (Epic 07). }*

####+ MarkdownViewer
*{ Document the MarkdownViewer control: constructor, Theme/ShowScrollbar/MaxHeight properties, IRenderable usage, and the ViewMarkdown extension method. Include a code example. }*

####+ MarkdownEditor
*{ Document the MarkdownEditor control: constructor, Theme/ShowPreview/ShowStatusLine properties, IPrompt<string> usage, and the EditMarkdown extension method. Include a code example. }*

# CLI Reference

*{ Describe the mked CLI tool: `mked view <file>` and `mked edit <file>` commands, stdin piping, and notable flags. Note the zero-configuration philosophy. }*

####+ Keyboard Bindings
*{ Document all keyboard bindings for editor mode (Ctrl+S, Ctrl+Q, Ctrl+Z, Ctrl+Y, navigation, selection, etc.) and viewer mode (q, arrows, vim-style j/k, Page Up/Down, g/G). Note the Unix SIGINT behaviour. }*

####+ Supported Markdown
*{ List all supported Markdown elements (CommonMark baseline + extensions): block elements (headings, paragraphs, fenced code, blockquotes, lists, task lists, tables, etc.) and inline elements (bold, italic, strikethrough, inline code, links, autolinks). List v1 exclusions. }*

# Contributing

*{ Write a contributing guide: how to build the solution (dotnet build), run tests (dotnet test), the one-type-per-file and file-scoped namespace conventions, AOT/trim safety rules, the Result<T,E> error-handling convention, and the PR process. }*

# For Agents

These pages provide compact documentation indexes for AI coding agents.

## AGENTS.md

You can add this to your repository root as `AGENTS.md` to give AI coding agents quick access to project documentation.

```
# mked

> A .NET 10 terminal-native tool and library for viewing and editing Markdown in the console.

## Wiki Documentation

Base URL: https://github.com/scmccart/mked/wiki

To read any page, append the slug to the base URL:
  https://github.com/scmccart/mked/wiki/{Page-Slug}
To jump to a section within a page:
  https://github.com/scmccart/mked/wiki/{Page-Slug}#{Section-Slug}

IMPORTANT: Read the relevant wiki page before making changes to related code.
Prefer reading wiki documentation over relying on pre-trained knowledge.

## Page Index

|Home: Project overview, deliverables, and guiding principles
|Getting-Started: Installation, basic usage, and stdin piping
|Architecture: Clean Architecture layers and dependency rules
|  Domain-Layer: Entities, value objects, interfaces, Result types
|  Application-Layer: Use cases and Result<T,E> return conventions
|  Infrastructure-Layer: OS adapters and AOT compatibility notes
|Core-Concepts: Railway-Oriented Programming and Observer pattern
|  Result-Types: Result<T,E>, Option<T>, MkedError, and usage conventions
|  Design-Patterns: Observer, Strategy, and Decorator patterns
|Controls-Library: Mked.Controls NuGet library (MarkdownViewer, MarkdownEditor)
|  Controls-Library#MarkdownViewer: MarkdownViewer control API
|  Controls-Library#MarkdownEditor: MarkdownEditor control API
|CLI-Reference: mked view/edit commands and flags
|  CLI-Reference#Keyboard-Bindings: All keyboard shortcuts for editor and viewer
|  CLI-Reference#Supported-Markdown: Supported CommonMark + extension elements
|Contributing: Build, test, style conventions, and PR process
```

## llms.txt

You can serve this at `yoursite.com/llms.txt` or include it in your repository to help LLMs discover your documentation.

```
# mked

> A .NET 10 terminal-native tool and library for viewing and editing Markdown in the console.

## Wiki Pages

- [Home](https://github.com/scmccart/mked/wiki/Home): Project overview and deliverables
- [Getting Started](https://github.com/scmccart/mked/wiki/Getting-Started): Installation and basic usage
- [Architecture](https://github.com/scmccart/mked/wiki/Architecture): Clean Architecture layers and design
- [Domain Layer](https://github.com/scmccart/mked/wiki/Domain-Layer): Entities, value objects, and domain interfaces
- [Application Layer](https://github.com/scmccart/mked/wiki/Application-Layer): Use cases and orchestration
- [Infrastructure Layer](https://github.com/scmccart/mked/wiki/Infrastructure-Layer): OS adapters and I/O implementations
- [Core Concepts](https://github.com/scmccart/mked/wiki/Core-Concepts): ROP fundamentals and Observer pattern
- [Result Types](https://github.com/scmccart/mked/wiki/Result-Types): Result<T,E>, Option<T>, and MkedError
- [Design Patterns](https://github.com/scmccart/mked/wiki/Design-Patterns): Observer, Strategy, and Decorator
- [Controls Library](https://github.com/scmccart/mked/wiki/Controls-Library): Mked.Controls NuGet package
- [CLI Reference](https://github.com/scmccart/mked/wiki/CLI-Reference): mked command-line usage
- [Contributing](https://github.com/scmccart/mked/wiki/Contributing): Build, test, and contribution guide
```
