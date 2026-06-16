# Epic 07 — Controls Library (NuGet): Technical Design

> **Epic**: [`docs/epics/07-controls-library.md`](../epics/07-controls-library.md)
> **Status**: Complete
> **Date**: 2026-06-16

---

## Goals

1. Publish `Mked.Controls` as a proper NuGet package to the GitHub Packages feed
   (`nuget.pkg.github.com/scmccart`) on every `v*` version tag.
2. Lock down the public API to a minimal, intentional v1 surface — all implementation-detail
   types become `internal`.
3. All public members have XML doc comments (already enforced by `GenerateDocumentationFile=true`;
   this epic audits for gaps and fills them).
4. A `src/Mked.Controls/README.md` is authored and surfaced on the package page via
   `<PackageReadmeFile>`.
5. Version is derived from git tags via **MinVer** (e.g. tag `v0.1.0` → package `0.1.0`).

## Non-Goals

- nuget.org publishing — deferred to **Epic 9 (v1 readiness)**.
- AOT / release binaries / WinGet — Epic 8.
- A standalone sample project — `Mked.Console` is the reference usage.
- Adding a `RunAsync()` host-loop method to `MarkdownEditor`.
- New viewer or editor features.

---

## Architecture Overview

This epic is purely a packaging and hygiene epic. No Clean Architecture layer boundaries move;
no cross-layer dependencies are added.

| Layer | Project | Changes |
|-------|---------|---------|
| Domain | `Mked.Domain` | None |
| Application | `Mked.Application` | None |
| Infrastructure | `Mked.Infrastructure` | None |
| Presentation | `Mked.Console` | Recompile-verify after Controls lockdown; no source changes expected |
| Library | `Mked.Controls` | Package metadata, MinVer, README, public-surface lockdown |
| Build infra | `Directory.Build.props`, `Directory.Packages.props` | Shared package metadata, MinVer pin |
| CI | `.github/workflows/release.yml` (new) | Tag-triggered pack + push to GitHub Packages |

**No new NuGet dependencies are added to the transitive package closure.** MinVer is a
`PrivateAssets="all"` build-time tool; it does not appear in the published `.nuspec`.

---

## Key Types and Interfaces

### Public Surface (v1 — keep public)

These types form the intended public API. All others in `Mked.Controls` become `internal`.

| Type | Kind | Purpose |
|------|------|---------|
| `MarkdownViewer` | `sealed record class` | Full-screen Markdown pager; accepts `string Markdown`; implements `IRenderable`. Primary viewer widget. |
| `MarkdownViewerScrollInfo` | `sealed record` | Scroll metadata returned by `MarkdownViewer.ScrollInfo`. Read-only; consumed by host UIs to drive scroll indicators. |
| `MarkdownEditor` | `sealed class` | Stateful interactive Markdown editor; implements `IRenderable`. Hosts supply the input loop via `HandleKey` + `BufferChanged`. |
| `MarkdownEditorWidget` | `sealed class` | Low-level stateless renderer for the editor buffer; implements `IRenderable`. May be used standalone by advanced consumers. |
| `EditorStatusLine` | (verify kind) | Status-line renderable returned by `MarkdownEditor.StatusLine()`. |
| `StyledSpan` | (verify kind) | Syntax-highlight span type; consumed by widget rendering. |

> **Note on naming:** the epic text refers to `MarkdownViewerWidget` and `MarkdownEditorWidget`,
> but the implemented types are `MarkdownViewer` and `MarkdownEditor` (with `MarkdownEditorWidget`
> as a separate lower-level type). The design uses the actual type names. The epic spec is updated
> in Task 6.

### Types to Make `internal`

| Type | Reason |
|------|--------|
| `BufferOperations` | Internal text-mutation helpers; not part of the widget contract |
| `CursorNavigation` | Internal cursor-movement helpers |
| `EditorState` | Encapsulates editor buffer/undo state; opaque to consumers |
| `HighlightMapper` | Maps Markdig AST to highlight spans; internal pipeline detail |
| `MarkdownBlockRenderer` | Internal Spectre block-rendering engine |
| Highlight-layer types (e.g. `*HighlightLayer`, `*Layer`) | Internal rendering pipeline |

Exact list is confirmed by grepping `Mked.Console` for usages before changing access modifiers
(Task 3 of the implementation plan).

### Modified Types

| Type | Change | Reason |
|------|--------|--------|
| Various `Mked.Controls` helpers | `public` → `internal` | Minimal v1 API surface |
| `Mked.Controls.csproj` | Add `PackageId`, `IsPackable`, `Description`, `PackageTags`, `PackageReadmeFile`, MinVer ref | NuGet packaging |
| `Directory.Build.props` | Add shared `Authors`, `PackageLicenseExpression`, `RepositoryUrl`, `RepositoryType`, `PublishRepositoryUrl` | Common across all packable projects |
| `Directory.Packages.props` | Pin `MinVer` version | Central Package Management |

---

## Public API Contract

### `MarkdownViewer`

```csharp
namespace Mked.Controls;

/// <summary>A Spectre.Console renderable that displays a Markdown document as a styled pager.</summary>
public sealed record class MarkdownViewer(string Markdown) : IRenderable
{
    /// <summary>When <see langword="true"/>, YAML frontmatter is rendered above the document.</summary>
    public bool ShowFrontmatter { get; init; }

    /// <summary>When <see langword="true"/>, hyperlinks are rendered as plain text.</summary>
    public bool PlainLinks { get; init; }

    /// <summary>The index of the first visible block (0-based).</summary>
    public int TopLineIndex { get; init; }

    /// <summary>The number of terminal rows available to the widget. <see langword="null"/> uses the full console height.</summary>
    public int? ViewportHeight { get; init; }

    /// <summary>The total number of rendered blocks in the document.</summary>
    public int BlockCount { get; }

    /// <summary>Scroll metadata for the current viewport.</summary>
    public MarkdownViewerScrollInfo ScrollInfo { get; }
}
```

### `MarkdownEditor` (host-driven model)

```csharp
namespace Mked.Controls;

/// <summary>
/// A Spectre.Console renderable Markdown editor. Consumers own the input/render loop;
/// feed keystrokes via <see cref="HandleKey"/> and react to edits via <see cref="BufferChanged"/>.
/// </summary>
public sealed class MarkdownEditor : IRenderable
{
    /// <param name="initialBuffer">Optional pre-seeded content.</param>
    public MarkdownEditor(string initialBuffer = "");

    /// <summary>Raised after each buffer mutation with the new buffer text.</summary>
    public event Action<string>? BufferChanged;

    /// <summary>Current buffer text.</summary>
    public string Buffer { get; }

    /// <summary>Current cursor position.</summary>
    public (int Line, int Column) Cursor { get; }

    public bool IsDirty { get; }
    public int WordCount { get; }
    public bool CanUndo { get; }
    public bool CanRedo { get; }
    public bool HasFocus { get; set; }
    public int? ViewportHeight { get; set; }

    /// <summary>Replaces the buffer and resets undo history, cursor, and dirty flag.</summary>
    public void LoadDocument(string buffer);

    /// <summary>Clears the dirty flag without modifying the buffer.</summary>
    public void MarkClean();

    /// <summary>Returns a renderable status line (word count, cursor position, dirty indicator).</summary>
    public IRenderable StatusLine();

    /// <summary>Processes a keystroke. Returns <see langword="true"/> if the key was handled.</summary>
    public bool HandleKey(ConsoleKeyInfo key);
}
```

### Host-loop pattern (reference usage: `Mked.Console/EditCommand.cs`)

```csharp
var editor = new MarkdownEditor(initialBuffer: file.Source);
editor.BufferChanged += text => previewPane.Update(text);
editor.HasFocus = true;

await AnsiConsole.Live(layout).StartAsync(async ctx =>
{
    while (!done)
    {
        ctx.Refresh();
        var key = Console.ReadKey(intercept: true);
        if (!editor.HandleKey(key))
            HandleHostKey(key, ref done);
    }
});
```

---

## Data Flow / Sequence

### Use Case: Consumer embeds `MarkdownViewer`

1. Consumer creates `new MarkdownViewer(markdownText) { ViewportHeight = 30 }`.
2. Passes it to any Spectre layout (panel, live context, etc.).
3. Spectre calls `IRenderable.Render(...)` internally.
4. Consumer reads `viewer.ScrollInfo` to update a scroll indicator after each render.

### Use Case: Consumer embeds `MarkdownEditor`

1. Consumer creates `new MarkdownEditor(initialBuffer: priorContent)`.
2. Subscribes `editor.BufferChanged += HandleChange;` for real-time integration.
3. Consumer owns a `while (!done)` loop: reads `Console.ReadKey`, passes to `editor.HandleKey`.
4. On exit, reads `editor.Buffer` for the final content.

### Use Case: Tag-triggered GitHub Packages publish

1. Developer creates and pushes a `v0.1.0` git tag.
2. `release.yml` triggers on `push: tags: v*`.
3. MinVer resolves `0.1.0` from the tag; workflow restores, builds, tests.
4. `dotnet pack -c Release` produces `Mked.Controls.0.1.0.nupkg` with README + XML docs.
5. `dotnet nuget push` to `https://nuget.pkg.github.com/scmccart/index.json` with `GITHUB_TOKEN`.
6. Package appears on the repo's GitHub Packages page.

---

## Error Handling Strategy

This epic introduces no new error types or ROP boundaries. The Controls library itself does not
use `Result<T,E>` — it is a UI widget library operating on in-memory state. No new `MkedError`
variants are needed.

- **New `MkedError` variants**: none.
- **Error production boundaries**: none changed.
- **User-visible failures**: none introduced by this epic.

---

## Testing Approach

- **Unit tests — existing `Mked.Controls.Tests`**: must stay green after the API lockdown. If
  any test directly references a now-internal type, add `[assembly: InternalsVisibleTo("Mked.Controls.Tests")]`
  to `Mked.Controls` so tests retain access. Prefer extracting tests of internals to test the
  behaviour through the public API instead where practical.
- **Architecture tests**: add or verify an ArchUnitNet rule that `Mked.Controls` has **zero
  project references** (it must not reference Domain, Application, Infrastructure, or Console).
- **Pack smoke-test**: `dotnet pack src/Mked.Controls -c Release` is green and the resulting
  `.nupkg` contains `Mked.Controls.dll`, `Mked.Controls.xml`, `README.md`, and a `.nuspec`
  with the expected `id`, `description`, `license`, `tags`, and only Markdig + Spectre.Console
  as dependencies.
- **Build integration**: `dotnet build mked.slnx` and `dotnet test` remain green after lockdown,
  confirming `Mked.Console` is unaffected.

---

## Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | Do any test methods in `Mked.Controls.Tests` reference types that will become `internal`? | Resolve during Task 3 — grep test project before changing access modifiers |
| 2 | Does `EditorStatusLine` need to stay `public` (it is returned by `MarkdownEditor.StatusLine()`)? | Almost certainly yes — resolve during Task 3 audit |
| 3 | Should `MarkdownViewerScrollInfo.Empty` (currently `internal`) be exposed publicly? | Leave `internal` for now; expose if a consumer use case demands it |
| 4 | MinVer tag prefix: use bare `v` prefix (e.g. `v0.1.0`) or tag-only (e.g. `0.1.0`)? | Use `v` prefix (MinVer default); consistent with ecosystem norms |
