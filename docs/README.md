# Documentation

Reference material, architectural decisions, and design documents for `mked`.

## Reference

User-facing reference material for the shipped `mked view` command.

| Document | Contents |
|---|---|
| [Keyboard bindings](reference/keyboard-bindings.md) | Every key binding for both viewer and (planned) editor mode |
| [Supported Markdown](reference/supported-markdown.md) | Which CommonMark elements and Markdig extensions are rendered, and how |
| [Controls public API](reference/controls-public-api.md) | `Mked.Controls` NuGet widget API — `MarkdownViewer`, `MarkdownViewerScrollInfo` |
| [Configuration](reference/configuration.md) | Configuration options (none in v1) |

## Architecture

Design decisions and patterns used throughout the codebase.

| Document | Contents |
|---|---|
| [Clean architecture](architecture/clean-architecture.md) | Layer boundaries and dependency rules |
| [Solution structure](architecture/solution-structure.md) | Projects, namespaces, and build layout |
| [Result types](architecture/result-types.md) | `Result<T,E>` and `Maybe<T>` — railway-oriented programming foundations |
| [Railway-oriented programming](architecture/railway-oriented-programming.md) | How errors flow through use-case pipelines |
| [AOT / trim safety](architecture/aot-trim-safety.md) | NativeAOT and IL trimming constraints |
| [Testing conventions](architecture/testing-conventions.md) | Test project layout and naming rules |

## Epics

Feature epics ordered by build dependency (innermost layers first).

See [epics/README.md](epics/README.md) for the full table, or jump directly to an epic:

| # | Epic | Status |
|---|---|---|
| 01 | [Domain Core](epics/01-domain-core.md) | ✅ Complete |
| 02 | [Infrastructure Adapters](epics/02-infrastructure-adapters.md) | ✅ Complete |
| 03 | [Application Use Cases](epics/03-application-use-cases.md) | ✅ Complete |
| 04 | [Markdown Viewer](epics/04-markdown-viewer.md) | ✅ Complete |
| 05 | [Markdown Editor](epics/05-markdown-editor.md) | 🔲 Planned |
| 06 | [CLI & Presentation](epics/06-cli-presentation.md) | 🔲 Planned |
| 07 | [Controls Library (NuGet)](epics/07-controls-library.md) | 🔲 Planned |
| 08 | [Distribution & AOT](epics/08-distribution-aot.md) | 🔲 Planned |

## Technical designs

Detailed technical designs for each epic.

| Document | Epic |
|---|---|
| [Domain core design](designs/01-domain-core-design.md) | Epic 01 |
| [Infrastructure adapters design](designs/02-infrastructure-adapters-design.md) | Epic 02 |
| [Application use cases design](designs/03-application-use-cases-design.md) | Epic 03 |
| [Markdown viewer design](designs/04-markdown-viewer-design.md) | Epic 04 |

## Implementation plans

Step-by-step task plans for each epic.

| Document | Epic |
|---|---|
| [Domain core plan](plans/01-domain-core-plan.md) | Epic 01 |
| [Infrastructure adapters plan](plans/02-infrastructure-adapters-plan.md) | Epic 02 |
| [Application use cases plan](plans/03-application-use-cases-plan.md) | Epic 03 |
| [Markdown viewer plan](plans/04-markdown-viewer-plan.md) | Epic 04 |

## Libraries

Notes on third-party libraries used by mked.

| Document | Library |
|---|---|
| [Markdig](libraries/markdig.md) | Markdown parsing |
| [Spectre.Console](libraries/spectre-console.md) | Terminal rendering |
| [Spectre.Console CLI](libraries/spectre-console-cli.md) | Command-line parsing |
| [Spectre.Console custom widgets](libraries/spectre-console-custom-widgets.md) | `IRenderable` widget authoring |

## Patterns

| Document | Contents |
|---|---|
| [Design patterns](patterns/design-patterns.md) | Recurring patterns applied across the codebase |

## Vision

[vision.md](vision.md) — Project goals and long-term direction.
