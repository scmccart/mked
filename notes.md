# Perf Improver Memory - scmccart/mked

## Build/Test Commands (validated 2026-06-03)
- Build: `dotnet build mked.slnx`
- Test: `dotnet test mked.slnx`
- Test domain only: `dotnet test tests/Mked.Domain.Tests/`
- No benchmark suite exists yet

## Known Flaky Tests
- `Mked.Infrastructure.Tests.Integration.FileWatcherAdapter_WatchAsync_Tests.RapidWrites_YieldsSingleNotification` - timing-dependent, flaky

## Performance Notes
- `MarkdownDocument` uses a static `MarkdownPipeline` (good - no repeated allocation)
- `StreamInputUseCase` re-parses full accumulated buffer on every stdin chunk (O(n²) for large inputs - opportunity)
- `FileSystemReader` uses `File.ReadAllTextAsync` with explicit UTF-8 (good)

## Optimization Backlog
1. **[HIGH] StreamInputUseCase quadratic re-parsing** - re-parses growing buffer on every chunk; O(n²) work for large piped documents. Could batch/debounce. Low risk.
2. **[DONE] EditorState.IsDirty caching** - was O(n) string compare, now O(1) bool (PR pending)

## Completed Work
- 2026-06-03: PR created - cache `IsDirty` in `EditorState` (O(n)→O(1))

## Last Run Tasks
- 2026-06-03: Task 1 (discover commands), Task 2 (identify opportunities), Task 3 (implement), Task 7 (monthly summary)

## Checked Off by Maintainer
(none yet)
