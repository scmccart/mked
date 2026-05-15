# Epic 06 — CLI & Presentation

Wire the application together into a runnable executable. Register Spectre.Console.Cli commands,
build the DI container, handle terminal lifecycle events, and ensure the presentation layer never
leaks infrastructure concerns into Application or Domain.

## Features

### Feature: View Command

Expose the viewer as a first-class CLI command with all its options.

- As a user, `mked view file.md` opens the file in viewer mode
- As a user, `mked view` (no argument) reads from stdin automatically
- As a user, `--follow`/`-f` enables file-follow mode
- As a user, `--stream` forces stdin stream mode when auto-detection is insufficient
- As a user, `--plain` switches to plain-text output
- As a user, `--show-frontmatter` renders the YAML frontmatter block
- As a developer, `ViewSettings` is a strongly-typed settings class annotated with `[CommandArgument]` and `[CommandOption]`

### Feature: Edit Command

Expose the editor as a first-class CLI command.

- As a user, `mked edit file.md` opens the file in editor mode
- As a user, `mked edit` (no argument) opens a blank new document
- As a user, `--split` starts the editor with the preview pane visible
- As a developer, `EditSettings` is a strongly-typed settings class annotated with `[CommandArgument]` and `[CommandOption]`

### Feature: DI Composition Root

Wire all dependencies together at startup without leaking infrastructure into inner layers.

- As a developer, all use cases receive their `IFileReader`, `IFileWriter`, and `IInputStream` implementations via constructor injection
- As a developer, the renderer strategy is selected at startup based on stdout interactivity and `--plain` flag
- As a developer, the composition root is the only place that references both Application and Infrastructure
- As a developer, DI registration uses `Microsoft.Extensions.DependencyInjection` (or manual wiring)

### Feature: Terminal Lifecycle

Handle terminal events that require the application to respond gracefully.

- As a user, pressing `Ctrl+C` exits cleanly from both viewer and editor modes
- As a user, resizing the terminal reflows the layout without corrupting the display
- As a developer, `Console.CancelKeyPress` and `SIGTERM` trigger clean shutdown with cursor restoration
- As a developer, `Console.WindowWidth` / `Console.WindowHeight` changes are detected and layout is re-rendered

### Feature: Error Rendering & Exit Codes

Present errors to the user in a consistent, legible style and communicate failure to the shell.

- As a user, file-not-found errors display a helpful message with the attempted path
- As a user, permission errors distinguish between read and write failures
- As a user, parse errors show the offending line and column
- As a developer, `MkedError` variants are mapped to styled Spectre.Console error panels
- As a developer, exit codes are: `0` success, `1` usage error, `2` file/IO error, `3` parse error
