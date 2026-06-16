# Epic 06 — CLI & Presentation: Implementation Plan

> **Epic**: [`docs/epics/06-cli-presentation.md`](../epics/06-cli-presentation.md)
> **Design**: [`docs/designs/06-cli-presentation-design.md`](../designs/06-cli-presentation-design.md)
> **Status**: Draft

---

## Overview

The composition root (Task 1) is the mandatory foundation: commands can't receive injected
dependencies until `TypeRegistrar`/`TypeResolver` exist and `Program.cs` builds the
container. Once that lands, the remaining five feature slices (Tasks 2–6) are independent
of each other and can be implemented in any order, though they're sequenced here from
lowest to highest risk. The `Mked.Console.Tests` project (Task 7) is gated behind all
feature slices so `CommandAppTester` can exercise the fully-wired commands. AOT verification
(Task 8) is the final gate before the epic is considered complete, because NativeAOT
warnings are cheapest to fix immediately after the code that caused them.

---

## Task List

- [ ] **Task 1: DI composition root**
  Add `Microsoft.Extensions.DependencyInjection` to `Mked.Console.csproj`. Implement
  `TypeRegistrar` and `TypeResolver` per the design contract. Update `Program.cs` to build
  `ServiceCollection`, register all ports → adapters and use cases, and pass a
  `TypeRegistrar` to `new CommandApp(registrar)`. Convert `ViewCommand` and `EditCommand`
  to constructor-injected dependencies (remove inline `new OpenFileUseCase(new
  FileSystemReader())` field initializers). Done when `dotnet build` is clean and both
  commands still function identically to before.

- [ ] **Task 2: `ExitCode` constants and `MkedError.IoError` enrichment**
  Add `ExitCode` static class with `Success`, `Usage`, `Io`, `Parse` constants. Add the
  `IoKind` top-level enum (`ReadNotFound`, `ReadAccessDenied`, `ReadGeneric`,
  `WriteAccessDenied`, `WriteGeneric`) in `Mked.Domain`; add a `Kind` property to
  `MkedError.IoError`.
  Update `FileSystemReader` and `FileSystemWriter` to produce the appropriate `IoKind`
  values in their exception handlers. Update all existing pattern-match exhaustion sites
  in tests to include the new property (all existing tests must remain green).
  Depends on: Task 1

- [ ] **Task 3: `ErrorPresenter` — centralized error rendering**
  Implement `ErrorPresenter.Show(MkedError) : int` per the design contract (styled Spectre
  `Panel`, red header, returns the `ExitCode`). Replace all three inline error-rendering
  sites in `ViewCommand` (lines 22, 38, 161) and all two in `EditCommand` (lines 32, 241,
  314) with a single `return ErrorPresenter.Show(error)` call. Configure Spectre's
  `SetExceptionHandler` in `Program.cs` to use exit code 1 for usage/parse failures. Done
  when every error path prints a consistent panel and no `FormatError` private methods remain.
  Depends on: Task 2

- [ ] **Task 4: `--plain` repurposing and `PlainTextRenderer`**
  Rename `ViewSettings.PlainLinks` → `Plain`; update the XML doc and `[Description]`
  attribute to reflect "plain-text output mode, no pager". Remove `PlainLinks` from
  `BuildViewer` / `RenderContext` call sites in `ViewCommand`. Implement `RendererSelector`
  and `PlainTextRenderer` per the design contracts. Update `ViewCommand.ExecuteAsync` to
  check `RendererSelector.IsPlainMode(settings)` before entering any interactive live loop:
  in plain mode, open the file, call `PlainTextRenderer.RenderAsync`, and return 0 — no
  pager, no key loop. Done when `mked view --plain file.md` and `mked view file.md | cat`
  both write the raw Markdown source to stdout with no ANSI codes.
  Depends on: Task 1

- [ ] **Task 5: stdin auto-detect**
  Update `ViewCommand.ExecuteAsync` dispatch order: when `settings.Path is null` and
  `Console.IsInputRedirected` is `true`, route to the stdin/stream path (same
  `RunStreamModeAsync`) rather than returning a usage error. The `--stream` flag continues
  to work as an explicit override. `--follow` with no path remains a usage error (requires
  a file). Done when `echo "# Hello" | mked view` opens the stdin stream pager and
  `mked view` with no pipe and no path prints the usage error and returns exit code 1.
  Depends on: Task 1

- [ ] **Task 6: `TerminalLifecycle` — signal handling and cursor restore**
  Implement `TerminalLifecycle` per the design contract (`CancelKeyPress` + SIGTERM via
  `PosixSignalRegistration`; `Dispose` unregisters both and restores
  `Console.CursorVisible = true`). Wrap the interactive entry in both `ViewCommand` and
  `EditCommand` in `using var lifecycle = new TerminalLifecycle(cts)` with a `try/finally`
  that sets `Console.CursorVisible = true`. The existing polled-keystroke Ctrl+C handling
  inside the Live loops is left in place. Done when sending `kill -SIGTERM <pid>` and
  pressing Ctrl+C during a `SelectionPrompt` both exit cleanly with cursor restored.
  Depends on: Task 1

- [ ] **Task 7: `Mked.Console.Tests` project**
  Create the `Mked.Console.Tests` xUnit project; add it to the solution. Add
  `PackageReference`s for xUnit, AwesomeAssertions, and Spectre.Console.Testing
  (`CommandAppTester`). Write unit tests: `ErrorPresenter_Tests` (each `MkedError` variant
  → panel + exit code), `RendererSelector_Tests` (plain-flag and redirect-state
  combinations, using the testable overload that accepts `bool isRedirected`),
  `PlainTextRenderer_Tests` (with/without frontmatter). Write `CommandAppTester`-based
  integration tests: `ViewCommand_Returns2_WhenFileNotFound`,
  `EditCommand_Returns2_WhenFileNotFound`, `ViewCommand_Returns1_WhenNoArgAndNoStdin`.
  Write an ArchUnitNet test confirming only the composition-root namespace references
  `Mked.Infrastructure`. Done when `dotnet test` is green across all projects.
  Depends on: Task 3, Task 4, Task 5, Task 6

- [ ] **Task 8: AOT publish verification**
  Run `dotnet publish src/Mked.Console/Mked.Console.csproj -r win-x64 --self-contained
  -p:PublishAot=true -c Release` and resolve any ILLink or AOT warnings introduced by this
  epic (MS.DI registration, `TerminalLifecycle` signal APIs, `PlainTextRenderer`'s
  `[GeneratedRegex]`). Update `docs/architecture/solution-structure.md` to note the addition
  of `Mked.Console.Tests` (breaking the prior "no Console test project" convention). Done
  when the publish is warning-free and the output binary runs `mked view --help` without
  errors.
  Depends on: Task 7

---

## Notes

- **Task ordering:** Tasks 2–6 depend only on Task 1, not on each other. In practice, Task
  2 (IoKind enrichment) should land before Task 3 (ErrorPresenter) because `ErrorPresenter`
  pattern-matches on `IoKind`. Beyond that, order is flexible.
- **`PlainLinks` removal:** The old behavior (render link text, omit URL) is dropped with
  no replacement flag. If the `MarkdownViewer` widget internally uses a `PlainLinks`
  property for styling purposes, that internal property can remain — only the CLI-facing
  `ViewSettings` property is renamed and repurposed.
- **`RendererSelector` testability:** `Console.IsOutputRedirected` is a static property;
  `RendererSelector.IsPlainMode` should expose an internal overload that accepts a
  `bool isRedirected` parameter so tests can inject both states without reflection.
- **`CommandAppTester` + DI:** Spectre's `CommandAppTester` accepts a `CommandApp`
  instance; construct it the same way `Program.cs` does (with a `TypeRegistrar`) in test
  setup. Infrastructure dependencies (file system, stdin) should be registered as fakes in
  the test container.
- **AOT + MS.DI:** If AOT warnings appear related to `ServiceProvider`, add
  `<RuntimeHostConfigurationOption Include="System.Runtime.Loader.UseRidGraph" Value="true"
  Trim="true"/>` or scope warnings with `[UnconditionalSuppressMessage]` only as a last
  resort after verifying correctness. Document any suppressions.
