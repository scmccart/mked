# Epic 02 — Infrastructure Adapters: Implementation Plan

> **Epic**: [`docs/epics/02-infrastructure-adapters.md`](../../docs/epics/02-infrastructure-adapters.md)
> **Design**: [`docs/designs/02-infrastructure-adapters-design.md`](../../docs/designs/02-infrastructure-adapters-design.md)
> **Status**: Draft

---

## Overview

Project scaffolding lands first and is the prerequisite for everything else. The four adapter
slices (Tasks 2–5) are independent of each other and can be tackled in any order once the
projects exist. Architecture tests come last, after all adapters are in place, so the rules
have real types to check against.

---

## Task List

- [ ] **Task 1: Project scaffold**
  Create `Mked.Infrastructure` (class library, references `Mked.Domain`) and
  `Mked.Infrastructure.Tests` (xUnit test project, references `Mked.Infrastructure`). Add both
  to `mked.sln`. Create `Unit/` and `Integration/` subfolders under `Mked.Infrastructure.Tests`
  and add a `.gitkeep` placeholder in each. "Done" means `dotnet build` and `dotnet test` both
  pass with zero errors and zero warnings on the new projects.

- [ ] **Task 2: `FileSystemReader`**
  Implement `FileSystemReader` in `Mked.Infrastructure` — `IFileReader` backed by
  `File.ReadAllTextAsync(path, Encoding.UTF8)`, mapping `FileNotFoundException`,
  `UnauthorizedAccessException`, and `IOException` to `MkedError.IoError`. Write integration
  tests in `Integration/` under `Mked.Infrastructure.Tests`: existing file round-trips correctly,
  UTF-8 non-ASCII content round-trips correctly, missing path returns `Err(IoError)`, and (where
  feasible) restricted-permissions path returns `Err(IoError)`. All integration test classes
  carry `[Trait("Category", "Integration")]`.
  Depends on: Task 1

- [ ] **Task 3: `FileSystemWriter`**
  Implement `FileSystemWriter` in `Mked.Infrastructure` — `IFileWriter` with atomic write
  semantics: derive target directory, call `Directory.CreateDirectory`, write to a GUID-named
  temp file in that directory, then `File.Move(temp, path, overwrite: true)`. On failure,
  delete the temp file (best-effort) and return `Err(IoError)`. Write integration tests in
  `Integration/`: write to a new path in an existing directory, write to a path whose parent
  directory does not yet exist, overwrite an existing file, and verify no temp file is left
  behind in any outcome.
  Depends on: Task 1

- [ ] **Task 4: `StdinInputStream`**
  Implement `StdinInputStream` in `Mked.Infrastructure` — `IInputStream` that accepts a
  `TextReader` (defaults to `Console.In`) and an `isRedirected` flag (defaults to
  `Console.IsInputRedirected`). When not redirected, `ReadChunksAsync` completes immediately
  with no items. Otherwise it loops `ReadLineAsync`: non-null lines yield `Result.Ok(line)`;
  `null` completes normally; `IOException` yields `Result.Err(StreamError)` then completes.
  Write unit tests in `Unit/` using injected `StringReader` and a custom `TextReader` subclass
  that throws `IOException`. No `[Trait]` on unit test classes.
  Depends on: Task 1

- [ ] **Task 5: `FileWatcherAdapter`**
  Add `IFileWatcher` to `Mked.Domain` (`IAsyncEnumerable<string> WatchAsync(CancellationToken)`
  plus `IDisposable`). Implement `FileWatcherAdapter` in `Mked.Infrastructure`: constructor
  creates a `FileSystemWatcher` on the file's directory filtered by file name and a bounded
  `Channel<string>` (capacity 1, `DropWrite`); `Changed`, `Created`, and `Renamed` events post
  the file path to the channel; `Error` events log a trace diagnostic and continue; `Dispose`
  stops and disposes the watcher and completes the channel writer. Write integration tests in
  `Integration/`: file modification produces a notification, rapid successive writes produce a
  single notification, disposal stops further notifications.
  Depends on: Task 1

- [ ] **Task 6: Architecture tests**
  Add an `ArchitectureTests.cs` (no trait) to `Mked.Infrastructure.Tests` using ArchUnitNet.
  Assert that no type in `Mked.Infrastructure` references `Mked.Application` or `Mked.Console`,
  and that `Mked.Infrastructure` references `Mked.Domain` (positive assertion). All rules must
  pass as part of the standard `dotnet test` run.
  Depends on: Task 1, Task 2, Task 3, Task 4, Task 5

---

## Notes

- AOT safety: all four adapters use BCL types only (`System.IO`, `System.Threading.Channels`,
  `System.Text`). No new NuGet packages are required for `Mked.Infrastructure`.
- `Mked.Infrastructure.Tests` needs the same xUnit/AwesomeAssertions/ArchUnitNet packages as
  `Mked.Domain.Tests`; no `Moq` reference is required because all test doubles are constructor-
  injected fakes.
- The permission-restricted file test in Task 2 should be skipped (`[Fact(Skip = ...)]` or
  a runtime guard) when the test process is running as an administrator, where ACL denies have
  no effect.
