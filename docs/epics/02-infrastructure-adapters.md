# Epic 02 — Infrastructure Adapters

Implement the domain interfaces against the real operating system. This layer translates native I/O
exceptions into `MkedError` domain errors at the boundary, keeping Application and Domain clean of
OS-specific concerns.

## Features

### Feature: File System Adapters

Provide concrete implementations of `IFileReader` and `IFileWriter` backed by `System.IO`.

- As a user, my file is read and displayed even when it contains non-ASCII characters (UTF-8 by default)
- As a developer, `FileSystemReader` catches `IOException`, `UnauthorizedAccessException`, and `FileNotFoundException` and maps them to `MkedError.IoError`
- As a developer, `FileSystemWriter` writes atomically (write to temp file, then rename) to avoid data loss on crash
- As a developer, both adapters are AOT-safe — no reflection, no runtime code generation

### Feature: Standard Input Stream

Implement `IInputStream` so the viewer and editor can read from stdin as an async chunk stream.

- As a user, I can pipe content to `mked view` and have it rendered as it arrives
- As a developer, `StdinInputStream` yields lines via `IAsyncEnumerable<string>`
- As a developer, a clean EOF is treated as success; a broken pipe maps to `MkedError.StreamError`
- As a developer, the adapter detects non-interactive stdin automatically (no TTY check required in Application)

### Feature: File Watcher

Provide a `FileWatcherAdapter` that notifies the viewer when a file changes on disk (powers `--follow`).

- As a user, when I run `mked view --follow file.md`, the view refreshes automatically when the file changes
- As a developer, `FileWatcherAdapter` wraps `FileSystemWatcher` and exposes change events as an `IAsyncEnumerable`
- As a developer, rapid successive saves are debounced so the viewer does not redraw on every partial write
- As a developer, the adapter disposes `FileSystemWatcher` cleanly when the viewer session ends
