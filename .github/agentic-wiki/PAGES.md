# Home

*{ Write a concise project overview for mked: what it is (terminal-native Markdown viewer and editor for .NET), what problems it solves, who it is for, and its two main commands (mked view, mked edit). Include a quick-start snippet showing dotnet tool installation and a first use. Reference the guiding principles: speed (AOT), simplicity (two modes, one file), liveness (real-time highlighting), viewport stability, and terminal citizenship. }*

####+ Key Features
*{ List and briefly describe the key features of mked: live syntax highlighting in the editor, viewport-stable viewer, streaming stdin support (--stream), live file watching (--follow), live preview pane toggle in editor (Ctrl+P), unlimited undo/redo, and AOT-compiled self-contained binary. Use a short description per feature. }*

####+ Distribution
*{ Describe how mked is distributed: dotnet tool (dotnet tool install -g mked), Mked.Controls NuGet library for embedding widgets in other Spectre.Console apps, self-contained single-file binaries on GitHub Releases, and planned WinGet package. }*

# Getting Started

*{ Write a complete getting-started guide covering: prerequisites (.NET SDK or just runtime for the tool), three installation methods (dotnet tool, self-contained binary download, build from source with dotnet build), and a first-steps walkthrough showing how to view a Markdown file, watch a file for changes, pipe stdin into the viewer, open the editor, and start editing with the preview pane. }*

#### Prerequisites
*{ List what is needed: .NET 10 SDK for building from source; no runtime required for the self-contained binary. List supported platforms. }*

#### Installation
*{ Show all three installation methods with exact commands: dotnet tool install -g mked, downloading the self-contained binary from GitHub Releases, and building from source with dotnet build and dotnet run. }*

#### First Steps
*{ Provide a short walkthrough of the most common usage patterns: viewing a file, watching for changes, piping from stdin, and opening the editor. Use short code blocks for each. }*

# CLI Reference

*{ Write an overview of the mked CLI: it is built with Spectre.Console.Cli and exposes two subcommands — view and edit. Describe global behavior (how to invoke help, version flag). }*

####+ view Command
*{ Document the mked view command fully: synopsis (mked view <path> [options]), all flags (--follow/-f, --stream/-s, --show-frontmatter, --plain/-p) with descriptions, and practical usage examples. }*

####+ edit Command
*{ Document the mked edit command fully: synopsis (mked edit [path] [options]), the --split flag, behavior when path is omitted (blank document), and practical usage examples. }*

## Keyboard Shortcuts

*{ Provide a comprehensive reference of all keyboard shortcuts for both viewer mode and editor mode. Use two separate tables. For viewer mode: navigation keys (j/k, Shift+j/Shift+k, PageUp/PageDown via Ctrl+D/Ctrl+U, g/G, q). For editor mode: character input, navigation keys (arrows, Ctrl+arrow, Home, End), editing actions (Enter, Backspace, Delete), undo/redo (Ctrl+Z/Ctrl+Y), and file operations (Ctrl+S, Ctrl+O, Ctrl+N, Ctrl+P, Ctrl+Q). }*

# Architecture

*{ Write a concise overview of mked's Clean Architecture: four layers (Domain, Application, Infrastructure, Console/Presentation), how dependencies flow inward, why this design enables AOT compilation and fast unit testing, and a dependency diagram showing the project references. }*

## Domain Layer

*{ Document the Domain layer (Mked.Domain): its role as the innermost layer containing entities, value objects, interfaces, and functional types. Cover the main types: EditorState (mutable editing session with buffer, cursor, undo/redo, observer pattern), ViewerState, MarkdownDocument, CursorPosition, TextRange, ViewportAnchor, and the domain interfaces (IFileReader, IFileWriter, IInputReader, IFileWatcher). }*

####+ EditorState
*{ Describe EditorState in detail: what it owns (buffer, cursor, dirty flag, undo/redo stacks, observer list), its public API (UpdateBuffer, UpdateCursor, Insert, Delete, MoveCursor* methods, Undo, Redo, MarkClean, Subscribe), dirty-tracking mechanics (reference comparison via _cleanBuffer), and the observer pattern (IEditorObserver with OnBufferChanged and OnCursorMoved callbacks). }*

####+ Result and Maybe Types
*{ Document the Result<T,E> and Maybe<T> types: purpose (Railway-Oriented Programming, explicit failure), type definitions, factory methods (Result.Ok/Result.Err, Maybe.Some/Maybe.None), core extension methods (Map, Bind, MapError, Match, BindAsync, MapAsync, OkOrErr), the MkedError discriminated union (IoError, ParseError, ValidationError, StreamError), and the five conventions for using them correctly. }*

####+ Highlight Pipeline
*{ Document the syntax highlighting pipeline: the IHighlightLayer interface, HighlightSpan and HighlightKind types, the five built-in layers (HeadingHighlightLayer, EmphasisHighlightLayer, LinkHighlightLayer, FrontMatterDimLayer, CodeFenceLayer), how the pipeline is composed and run, and how HighlightMapper translates Domain HighlightSpan values to Mked.Controls StyledSpan values for rendering. }*

## Application Layer

*{ Document the Application layer (Mked.Application): its role as the orchestration layer, the use cases it provides (OpenFileUseCase, SaveFileUseCase, StreamInputUseCase, NewDocumentUseCase, RenderDocumentUseCase), how use cases depend only on domain interfaces (no I/O), how they return Result<T,E> for fallible operations, and how Railway-Oriented Programming (Map, Bind, BindAsync) composes them into pipelines for file I/O, stream input, and editor save flows. }*

## Infrastructure Layer

*{ Document the Infrastructure layer (Mked.Infrastructure): its role as the OS-facing adapter layer implementing domain interfaces. Cover the three adapters: FileSystemReader (wraps System.IO.File.ReadAllText), FileSystemWriter (wraps System.IO.File.WriteAllText), StdinInputReader (wraps Console.OpenStandardInput for streaming), and FileWatcherAdapter (wraps FileSystemWatcher for --follow mode). Note AOT compatibility constraints: no reflection-based serialization, direct System.IO usage only. }*

# Controls Library

*{ Write an overview of Mked.Controls — the standalone Spectre.Console extension NuGet package. Describe its four public types: MarkdownViewer, MarkdownEditorWidget, EditorStatusLine, and StyledSpan. Explain that it is intentionally decoupled from Mked.Domain (no dependency) so it can be published and used independently. Show how to install it with dotnet add package Mked.Controls. }*

## Markdown Viewer

*{ Document the MarkdownViewer widget in full: constructor (takes a Markdown string), all properties (ShowFrontmatter, PlainLinks, TopLineIndex, ViewportHeight), read-only members (BlockCount, ScrollInfo/MarkdownViewerScrollInfo), the immutable scroll pattern (with-copy to change TopLineIndex), the render cache keyed on (width, ShowFrontmatter, PlainLinks), and a usage example showing it inside a Spectre.Console Live display. Also document MarkdownViewerScrollInfo (TotalLineCount, BlockStartLines) and how to use BlockStartLines for block-boundary navigation. }*

## Markdown Editor

*{ Document the MarkdownEditorWidget and EditorStatusLine widgets. For MarkdownEditorWidget: constructor parameters (buffer, cursor, highlights, topLineIndex, viewportHeight), rendering notes (no trailing LineBreak, overlapping spans left-to-right, block cursor rendering), and a usage example. For EditorStatusLine: constructor parameters (cursor, isDirty, wordCount) and a usage example. Also document StyledSpan (StartOffset, Length, SpectreStyle) and explain the HighlightMapper bridge that converts Domain HighlightSpan values to StyledSpan values. }*

# Supported Markdown

*{ Document the Markdown syntax supported by mked. Organize into: (1) supported block elements (ATX and setext headings, paragraphs, fenced/indented code blocks, blockquotes, unordered/ordered/task lists, GFM tables, horizontal rules), (2) supported inline elements (bold, italic, bold+italic, strikethrough, inline code, links, autolinks, images), (3) Markdig extensions enabled (Tables, TaskLists, Strikethrough, AutoLinks, YamlFrontMatter via UseAdvancedExtensions), (4) features not supported in v1 (raw HTML, math, footnotes, definition lists) with notes on behavior, and (5) terminal rendering notes (images as alt+URL, link URLs in parentheses, --plain flag, code blocks with dim styling, tables with Spectre.Console layout). }*

# Contributing

*{ Write a concise contributing overview: how to clone and build (dotnet build), how to run tests (dotnet test), and where to find architecture documentation. }*

####+ Architecture Constraints
*{ Document AOT and trim safety constraints that all contributors must follow: forbidden patterns (reflection without annotations, dynamic, Activator.CreateInstance without registration, JsonSerializer without source generation, Regex without [GeneratedRegex], Assembly.Load), safe patterns (new T(), generic methods, interface dispatch, LINQ to objects, Span<T>, source-generated code), Spectre.Console.Cli explicit registration requirement, how to evaluate new NuGet dependencies for trim and AOT compatibility, and how to run a NativeAOT publish to verify. }*

####+ Testing
*{ Document testing conventions: the test stack (xUnit, Moq, AwesomeAssertions), test naming convention ({ClassUnderTest}_{Scenario}_{ExpectedOutcome}), the Arrange/Act/Assert pattern, when to prefer in-memory fakes vs Moq mocks, layer-specific guidance (what to test in Domain vs Application vs Infrastructure vs Controls), integration vs unit test categories (the Integration trait), and the commands to run unit-only or integration-only tests. }*

# For Agents

These pages provide compact documentation indexes for AI coding agents.

## AGENTS.md

You can add this to your repository root as `AGENTS.md` to give AI coding agents quick access to project documentation.

```
# mked

> A terminal-native Markdown viewer and editor for .NET — AOT-compiled, keyboard-driven, with live syntax highlighting and viewport-stable rendering.

## Wiki Documentation

Base URL: https://github.com/scmccart/mked/wiki

To read any page, append the slug to the base URL:
  https://github.com/scmccart/mked/wiki/{Page-Slug}
To jump to a section within a page:
  https://github.com/scmccart/mked/wiki/{Page-Slug}#{Section-Slug}

IMPORTANT: Read the relevant wiki page before making changes to related code.
Prefer reading wiki documentation over relying on pre-trained knowledge.

## Page Index

|Home: Project overview, key features, and quick start
|  Home#Key-Features: Live highlighting, viewport stability, streaming, AOT binary
|  Home#Distribution: dotnet tool, NuGet library, self-contained binary, WinGet
|Getting-Started: Installation and first steps
|CLI-Reference: mked view and mked edit command reference
|  CLI-Reference#view-Command: --follow, --stream, --show-frontmatter, --plain flags
|  CLI-Reference#edit-Command: --split flag and blank-document mode
|  Keyboard-Shortcuts: All viewer and editor keyboard shortcuts
|Architecture: Clean Architecture layers and dependency graph
|  Domain-Layer: EditorState, Result/Maybe types, highlight pipeline
|    Domain-Layer#EditorState: Buffer, cursor, undo/redo, observer pattern
|    Domain-Layer#Result-and-Maybe-Types: Result<T,E>, Maybe<T>, MkedError, ROP conventions
|    Domain-Layer#Highlight-Pipeline: IHighlightLayer, HighlightSpan, built-in layers
|  Application-Layer: Use cases, Railway-Oriented Programming pipelines
|  Infrastructure-Layer: FileSystemReader, FileSystemWriter, StdinInputReader, FileWatcherAdapter
|Controls-Library: Mked.Controls NuGet package — Spectre.Console widgets
|  Markdown-Viewer: MarkdownViewer widget API and scroll pattern
|  Markdown-Editor: MarkdownEditorWidget, EditorStatusLine, StyledSpan
|Supported-Markdown: CommonMark + extensions; unsupported features; terminal rendering notes
|Contributing: Build, test, architecture constraints, AOT/trim safety, testing conventions
|  Contributing#Architecture-Constraints: Forbidden and safe AOT/trim patterns
|  Contributing#Testing: xUnit, Moq, AwesomeAssertions, test naming, fake vs mock
```

## llms.txt

You can serve this at `yoursite.com/llms.txt` or include it in your repository to help LLMs discover your documentation.

```
# mked

> A terminal-native Markdown viewer and editor for .NET — AOT-compiled, keyboard-driven, with live syntax highlighting and viewport-stable rendering.

## Wiki Pages

- [Home](https://github.com/scmccart/mked/wiki/Home): Project overview, key features, and quick start
- [Getting Started](https://github.com/scmccart/mked/wiki/Getting-Started): Installation and first steps
- [CLI Reference](https://github.com/scmccart/mked/wiki/CLI-Reference): mked view and mked edit command reference
- [Keyboard Shortcuts](https://github.com/scmccart/mked/wiki/Keyboard-Shortcuts): All viewer and editor keyboard shortcuts
- [Architecture](https://github.com/scmccart/mked/wiki/Architecture): Clean Architecture layers and dependency graph
- [Domain Layer](https://github.com/scmccart/mked/wiki/Domain-Layer): EditorState, Result/Maybe types, and highlight pipeline
- [Application Layer](https://github.com/scmccart/mked/wiki/Application-Layer): Use cases and Railway-Oriented Programming pipelines
- [Infrastructure Layer](https://github.com/scmccart/mked/wiki/Infrastructure-Layer): File system and I/O adapters
- [Controls Library](https://github.com/scmccart/mked/wiki/Controls-Library): Mked.Controls NuGet package — Spectre.Console widgets
- [Markdown Viewer](https://github.com/scmccart/mked/wiki/Markdown-Viewer): MarkdownViewer widget API and scroll pattern
- [Markdown Editor](https://github.com/scmccart/mked/wiki/Markdown-Editor): MarkdownEditorWidget, EditorStatusLine, and StyledSpan
- [Supported Markdown](https://github.com/scmccart/mked/wiki/Supported-Markdown): CommonMark + extensions and terminal rendering notes
- [Contributing](https://github.com/scmccart/mked/wiki/Contributing): Build, test, architecture constraints, and AOT/trim safety
```
