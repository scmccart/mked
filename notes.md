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
- `MarkdownViewer.BlockCount` cached in `RenderStateHolder` since 2026-06-03 (was O(n) LINQ per call)

## Optimization Backlog
1. **[HIGH] StreamInputUseCase quadratic re-parsing** - re-parses growing buffer on every chunk; O(n²) work for large piped documents. Could batch/debounce. Low risk.

## Completed Work
- 2026-06-03 (run 1): Implemented `EditorState.IsDirty` caching (O(n)→O(1))
- 2026-06-03 (run 2): Pushed `MarkdownViewer.BlockCount` caching to PR #32 (O(n) LINQ per keypress → O(1))

## Last Run Tasks
- 2026-06-03 14:41 UTC: Task 3 (implement optimization on PR branch), Task 5 (comment on PR), Task 7 (monthly summary)

## Monthly Activity Issue
- Created: "[perf-improver] Monthly Activity 2026-06" (June 2026 run 2)

## Checked Off by Maintainer
(none yet)
