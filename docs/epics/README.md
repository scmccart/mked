# Epics

Feature delivery epics for mked, ordered by build dependency (innermost layers first).

| # | Epic | Features | Summary |
|---|---|---|---|
| 01 | [Domain Core](01-domain-core.md) | Result & Maybe Types · Error Types · Markdown Document Model · Editor State · Viewer State · Value Objects · Domain Interfaces | Primitive types, entities, value objects, and I/O interfaces |
| 02 | [Infrastructure Adapters](02-infrastructure-adapters.md) | File System Adapters · Standard Input Stream · File Watcher | OS-facing implementations of domain interfaces |
| 03 | [Application Use Cases](03-application-use-cases.md) | Open File · Save File · Stream Input · New Document · Render Document | Named, testable use cases — no direct I/O |
| 04 | [Markdown Viewer](04-markdown-viewer.md) | Static Document Rendering · Frontmatter Handling · Rendering Strategies · Scrolling & Navigation · Viewport Stability · Streaming & Tail Mode | Rich read-only terminal document display |
| 05 | [Markdown Editor](05-markdown-editor.md) | Editor Buffer & Cursor · Syntax Highlighting · Undo & Redo · Keyboard Shortcuts & File Operations · Split-Pane Layout · Status Line | Keyboard-driven, syntax-highlighted in-terminal editor |
| 06 | [CLI & Presentation](06-cli-presentation.md) | View Command · Edit Command · DI Composition Root · Terminal Lifecycle · Error Rendering & Exit Codes | Executable entry point, command wiring, and terminal management |
| 07 | [Controls Library (NuGet)](07-controls-library.md) | Viewer Widget Public API · Editor Widget Public API · NuGet Packaging · Sample Project | `Mked.Controls` NuGet package for embedding widgets in third-party apps |
| 08 | [Distribution & AOT](08-distribution-aot.md) | NativeAOT Publish Profiles · Trim Safety Audit · dotnet Tool Packaging · GitHub Actions Release Workflow · WinGet Manifest | Single-file AOT binaries, tool packaging, and automated release |
