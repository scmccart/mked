# Epic 02 — Infrastructure Adapters: Technical Design

> **Epic**: [`docs/epics/02-infrastructure-adapters.md`](../../docs/epics/02-infrastructure-adapters.md)
> **Status**: Draft
> **Date**: 2026-05-23

---

## Goals

1. Create `Mked.Infrastructure` as a new class library project that references only `Mked.Domain`.
2. Implement `FileSystemReader` — a trim-safe `IFileReader` backed by `System.IO.File` that maps OS exceptions to `MkedError.IoError`.
3. Implement `FileSystemWriter` — an `IFileWriter` with atomic write semantics (write to temp file, then rename) to prevent data loss on crash.
4. Implement `StdinInputReader` — an `IInputReader` backed by `Console.In` (or any injected `TextReader`) that yields lines as an async stream and detects non-interactive stdin.
5. Add `IFileWatcher` to `Mked.Domain` and implement `FileWatcherAdapter` — wraps `FileSystemWatcher`, debounces rapid saves via a bounded channel, and exposes file-change notifications as `IAsyncEnumerable<string>`.
6. Create `Mked.Infrastructure.Tests` with integration tests against a real temp directory and unit tests using injected test doubles.

## Non-Goals

- Application use cases that consume these adapters (Epic 03).
- Spectre.Console rendering or CLI wiring (Epics 04–06).
- Watching multiple files simultaneously.
- Creating a full nested directory tree in `FileSystemWriter` — `Directory.CreateDirectory` handles the immediate parent; deeply nested new paths are unusual and an `IoError` in that case is acceptable.
- YAML frontmatter parsing or structured content interpretation.

---

## Architecture Overview

`Mked.Infrastructure` is created from scratch. One new interface (`IFileWatcher`) is added to `Mked.Domain`; no existing domain types are modified. No other projects are touched.

| Layer | Project | Role |
|-------|---------|------|
| Domain | `Mked.Domain` | Gains `IFileWatcher` — the only change to an existing layer |
| Application | `Mked.Application` | Not touched |
| Infrastructure | `Mked.Infrastructure` | New project; implements all four domain interfaces |
| Presentation | `Mked.Console` | Not touched |

**AOT/Trim**: `Mked.Infrastructure` uses only BCL types (`System.IO`, `System.Threading.Channels`, `System.Text`). No reflection, no `dynamic`, no `Activator.CreateInstance`, no `JsonSerializer`, no `Regex`. `FileSystemWatcher` and `Channel<T>` are trim-safe and AOT-compatible. No new NuGet dependencies are required.

---

## Key Types and Interfaces

### New Types

| Type | Kind | Project | Purpose |
|------|------|---------|---------|
| `IFileWatcher` | interface | `Mked.Domain` | Contract for file-change notifications; `IAsyncEnumerable<string> WatchAsync(CancellationToken)` plus `IDisposable` |
| `FileSystemReader` | sealed class | `Mked.Infrastructure` | Implements `IFileReader` via `File.ReadAllTextAsync`; maps OS exceptions to `MkedError.IoError` |
| `FileSystemWriter` | sealed class | `Mked.Infrastructure` | Implements `IFileWriter`; atomic write via temp file + `File.Move` |
| `StdinInputStream` | sealed class | `Mked.Infrastructure` | Implements `IInputStream`; accepts an injected `TextReader` (defaults to `Console.In`); returns empty stream when stdin is not redirected |
| `FileWatcherAdapter` | sealed class | `Mked.Infrastructure` | Implements `IFileWatcher`; wraps `FileSystemWatcher`; debounces via bounded `Channel<string>` (capacity 1, drop-on-full) |

### Modified Types

| Type | Change | Reason |
|------|--------|--------|
| `Mked.Domain` | New file `IFileWatcher.cs` added | Clean Architecture requires Infrastructure adapters to implement Domain interfaces; the watcher must have a domain contract so Application can depend on the abstraction |

---

## Data Flow / Sequence

### Use Case: FileSystemReader.ReadAsync

1. Caller invokes `ReadAsync(path)`.
2. Adapter calls `File.ReadAllTextAsync(path, Encoding.UTF8)`.
3. On success → `Result.Ok(content)`.
4. `FileNotFoundException` → `Result.Err(new MkedError.IoError(path, "File not found"))`.
5. `UnauthorizedAccessException` → `Result.Err(new MkedError.IoError(path, "Access denied"))`.
6. Any other `IOException` → `Result.Err(new MkedError.IoError(path, ex.Message))`.

All exception handling is in a single `try/catch` block at the boundary; no exceptions escape the adapter.

### Use Case: FileSystemWriter.WriteAsync (atomic)

1. Caller invokes `WriteAsync(path, content)`.
2. Adapter derives the target directory via `Path.GetDirectoryName(path)` and calls `Directory.CreateDirectory(directory)` — a no-op if the directory already exists.
3. Adapter derives a temp path in the same directory: `Path.Combine(directory, $".{Guid.NewGuid():N}.tmp")`. Using the same directory guarantees the same drive, which makes the subsequent rename atomic on Windows and Linux.
4. `File.WriteAllTextAsync(tempPath, content, Encoding.UTF8)` — writes full content to the temp file.
5. `File.Move(tempPath, path, overwrite: true)` — atomically replaces the target. If the target file does not exist, this creates it.
6. On success → `Result.Ok(Unit.Value)`.
7. On any `IOException` or `UnauthorizedAccessException`: attempt to delete the temp file (best-effort, ignore cleanup errors), then return `Result.Err(new MkedError.IoError(path, ex.Message))`.

### Use Case: StdinInputStream.ReadChunksAsync

1. On construction, `StdinInputStream` accepts an optional `TextReader reader` (defaults to `Console.In`) and an optional `bool isRedirected` flag (defaults to `Console.IsInputRedirected`).
2. If `isRedirected` is `false`, the enumeration completes immediately without yielding anything — stdin is an interactive TTY, not a pipe.
3. Otherwise, the adapter loops `reader.ReadLineAsync()`:
   - Non-null line → yield `Result.Ok(line)`.
   - `null` (clean EOF) → complete enumeration normally.
   - `IOException` (broken pipe or unexpected close) → yield `Result.Err(new MkedError.StreamError(ex.Message))`, then complete.

### Use Case: FileWatcherAdapter.WatchAsync

1. Caller creates `new FileWatcherAdapter(filePath)`.
   - Constructor creates a `FileSystemWatcher` targeting the file's directory, filtered by the file name.
   - An internal `Channel<string>` is created with `BoundedChannelOptions { Capacity = 1, FullMode = BoundedChannelFullMode.DropWrite }`.
   - `Changed`, `Created`, and `Renamed` events each call a shared handler that tries to write the file path to the channel; if the channel is full (rapid saves), the write is silently dropped.
2. Caller enumerates `WatchAsync(cancellationToken)`.
   - Each iteration reads the next item from the channel's reader via `ReadAllAsync(cancellationToken)`.
   - Yields the file path string for each notification.
3. Caller disposes the adapter (`IDisposable.Dispose`):
   - `FileSystemWatcher.EnableRaisingEvents` is set to `false` and the watcher is disposed.
   - `Channel.Writer.TryComplete()` signals to the consumer that no more items will arrive; the enumeration terminates after draining any buffered item.
4. `CancellationToken` cancellation → `ReadAllAsync` throws `OperationCanceledException`, which propagates to the caller. The adapter should then be disposed.

**Debounce rationale**: With capacity 1 and `DropWrite`, the channel holds at most one pending notification. If the consumer has not yet processed the first notification, all subsequent saves are dropped. The consumer re-reads the file after processing the notification, at which point the file is already in its final state. This is sufficient for editor-save patterns (save on every keystroke would be unusual).

---

## Error Handling Strategy

- **New `MkedError` variants**: None — `IoError` and `StreamError` are defined in Domain (Epic 01). Infrastructure is their primary producer.
- **Error production boundaries**:
  - `FileSystemReader` and `FileSystemWriter` produce `MkedError.IoError` at the System.IO call boundary.
  - `StdinInputStream` produces `MkedError.StreamError` on broken-pipe `IOException`.
  - `FileWatcherAdapter` does not produce domain errors. A `FileSystemWatcher` `Error` event (e.g., buffer overflow due to extreme event volume) logs a trace-level diagnostic and continues — the consumer is not interrupted.
- **User-visible failures**: None in this epic. Error rendering is Presentation's responsibility (Epic 06).

---

## Testing Approach

All tests live in `Mked.Infrastructure.Tests` (xUnit, AwesomeAssertions; Moq not required — test doubles are injected via constructors), organized into subfolders that mirror the category split:

```
tests/Mked.Infrastructure.Tests/
├── Unit/
│   └── StdinInputStream_*.cs
└── Integration/
    ├── FileSystemReader_*.cs
    ├── FileSystemWriter_*.cs
    └── FileWatcherAdapter_*.cs
```

Integration tests are marked `[Trait("Category", "Integration")]` on the class; unit tests carry no trait. This allows the two categories to be run independently:

```powershell
dotnet test tests/Mked.Infrastructure.Tests --filter "Category!=Integration"  # unit only
dotnet test tests/Mked.Infrastructure.Tests --filter "Category=Integration"   # integration only
```

**Unit tests** — no file system, no OS resources:

- **`StdinInputStream`** (injected `StringReader` / custom `TextReader` subclass):
  - Two-line reader → two `Result.Ok` items, then complete.
  - Empty reader → enumeration completes immediately, no items.
  - `isRedirected = false` → enumeration completes immediately with no items.
  - Reader throwing `IOException` → one `Result.Err(MkedError.StreamError)`, then complete.

**Integration tests** — real temp directory / file system (`[Trait("Category", "Integration")]`):

- **`FileSystemReader`**:
  - Read an existing file → content matches written bytes.
  - Read a UTF-8 file with non-ASCII characters (e.g., `"Héllo wörld"`) → round-trips correctly.
  - Read a non-existent path → `Err(MkedError.IoError)` with path in the error.
  - Read a file with restricted permissions (Windows ACL deny; skipped when running as admin) → `Err(MkedError.IoError)`.

- **`FileSystemWriter`**:
  - Write to a new path in an existing directory → file created with correct content.
  - Write to a path whose parent directory does not exist → directory is created, file written correctly.
  - Overwrite an existing file → content replaced; no temp file left behind.
  - Failure mid-write → no temp file left behind.

- **`FileWatcherAdapter`**:
  - Modify the watched file → notification received within a reasonable timeout.
  - Rapid successive writes → single notification received (bounded-channel debounce).
  - Dispose → no further notifications delivered after disposal.

**Architecture tests** (ArchUnitNet, no trait — these are fast and pure):

- `Mked.Infrastructure` types must not reference `Mked.Application` or `Mked.Console`.
- `Mked.Infrastructure` must reference `Mked.Domain` (positive assertion).

---

## Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | **`FileSystemWriter` missing directory**: if `Path.GetDirectoryName(path)` returns a directory that does not exist, should the adapter create it or return `IoError`? **Resolution: create the directory** via `Directory.CreateDirectory` before writing the temp file. | Resolved |
| 2 | **`FileWatcherAdapter` `Error` event handling**: the `Error` event fires when `FileSystemWatcher`'s internal OS event buffer (default 8 KB, `InternalBufferSize`) overflows due to extreme event volume. Since `FileWatcherAdapter` watches a single file, this overflow is effectively impossible in practice. **Resolution: log-and-continue** (write a trace-level diagnostic; do not complete the channel). | Resolved |
| 3 | **`StdinInputStream` non-interactive behavior**: is returning an empty enumerable (no items, immediate completion) the right contract when stdin is not redirected? **Resolution: return empty enumerable** — Application controls whether stdin reading is attempted; non-interactive piped use is the primary target. | Resolved |
