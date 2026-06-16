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
