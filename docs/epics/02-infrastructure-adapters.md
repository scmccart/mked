# Epic 02 — Infrastructure Adapters

Implement the domain interfaces against the real operating system. This layer translates native I/O
exceptions into `MkedError` domain errors at the boundary, keeping Application and Domain clean of
OS-specific concerns.

## Features

- `FileSystemReader` — implements `IFileReader` using `System.IO.File.ReadAllTextAsync`
- `FileSystemWriter` — implements `IFileWriter` using `System.IO.File.WriteAllTextAsync`
- `StdinInputStream` — implements `IInputStream` by reading from `Console.OpenStandardInput()`
- `FileWatcherAdapter` — wraps `FileSystemWatcher` for `--follow` mode; emits reload events on change
- Exception-to-`MkedError` translation at each adapter boundary (`IOException` → `IoError`, etc.)
- AOT-safe implementations: no reflection, no runtime code generation, direct `System.IO` usage
