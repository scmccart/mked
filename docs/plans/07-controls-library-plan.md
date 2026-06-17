# Epic 07 — Controls Library (NuGet): Implementation Plan

> **Epic**: [`docs/epics/07-controls-library.md`](../epics/07-controls-library.md)
> **Design**: [`docs/designs/07-controls-library-design.md`](../designs/07-controls-library-design.md)
> **Status**: Complete

---

## Overview

The build-infrastructure tasks (Tasks 2 and 3) are independent of each other and can land in
any order, but both must precede Task 4 (`Mked.Controls.csproj` metadata), which depends on
shared metadata being in place (Task 2) and a clean, locked-down API (Task 3). The release
workflow (Task 5) is the final technical deliverable and requires the package to be correctly
configured. The doc-sync (Task 6) is the last step, updating the epic spec and public API
reference to match what was actually built.

---

## Task List

- [x] **Task 1: plan-epic skill artifacts**
  Write `docs/designs/07-controls-library-design.md` and `docs/plans/07-controls-library-plan.md`
  from the plan-epic templates, populated from the approved plan. These files are the gated
  traceability deliverables for Epic 7. Done when both files exist on disk and match the
  templates in `assets/design-template.md` and `assets/plan-template.md`.

- [x] **Task 2: Shared package metadata + MinVer**
  Add common NuGet properties to `Directory.Build.props` inside a condition that excludes test
  projects (`<PropertyGroup Condition="'$(IsTestProject)' != 'true'">`):
  `Authors`, `Company` (Sean McCarthy), `PackageLicenseExpression=MIT`,
  `RepositoryUrl=https://github.com/scmccart/mked`, `RepositoryType=git`,
  `PackageProjectUrl=https://github.com/scmccart/mked`, `PublishRepositoryUrl=true`.
  Add `MinVer` to `Directory.Packages.props` (pin to latest stable). Wire MinVer into
  `Mked.Controls.csproj` as a `<PackageReference Include="MinVer" PrivateAssets="all"
  IncludeAssets="runtime;build;native;contentfiles;analyzers" />`. Done when
  `dotnet build mked.slnx` is clean and `dotnet pack src/Mked.Controls -c Release`
  produces a `.nupkg` with correct repository metadata in the `.nuspec`.

- [x] **Task 3: Lock down the public API**
  Audit all `public` types in `Mked.Controls` against the v1 surface defined in the design doc.
  Before changing any access modifier, grep `Mked.Console` for each candidate type to confirm it
  is not referenced there. Change implementation-detail types to `internal`:
  `BufferOperations`, `CursorNavigation`, `EditorState`, `HighlightMapper`, `MarkdownBlockRenderer`,
  and all highlight-layer types. If `Mked.Controls.Tests` tests any now-internal type directly,
  add `[assembly: InternalsVisibleTo("Mked.Controls.Tests")]` to `Mked.Controls`.
  Done when `dotnet build mked.slnx` is clean and `dotnet test` is fully green.
  Depends on: Task 1

- [x] **Task 4: `Mked.Controls.csproj` package metadata + package README**
  In `Mked.Controls.csproj` add: `<IsPackable>true</IsPackable>`, `<PackageId>Mked.Controls</PackageId>`,
  `<Description>Spectre.Console widgets for viewing and editing Markdown in the terminal.</Description>`,
  `<PackageTags>markdown;spectre-console;terminal;tui;editor;viewer;console</PackageTags>`,
  `<PackageReadmeFile>README.md</PackageReadmeFile>`, and an `<ItemGroup>` that includes
  `<None Include="README.md" Pack="true" PackagePath="\"/>`.
  Author `src/Mked.Controls/README.md` covering: install command, quick-start for `MarkdownViewer`
  (create, embed in a layout, read `ScrollInfo`), host-driven `MarkdownEditor` usage
  (`HandleKey` + `BufferChanged` pattern), link to the repo and to `Mked.Console` as a
  worked example.
  Done when `dotnet pack src/Mked.Controls -c Release` produces a `.nupkg` whose content
  (verified with `unzip -l` or `dotnet nuget verify`) contains `Mked.Controls.dll`,
  `Mked.Controls.xml`, and `README.md`, and whose `.nuspec` has the correct `id`, `description`,
  `license`, `tags`, and only `Markdig` + `Spectre.Console` as dependencies.
  Depends on: Task 2, Task 3

- [x] **Task 5: GitHub Packages release workflow**
  Add `.github/workflows/release.yml` with `on: push: tags: ['v*']`. The workflow needs
  `permissions: contents: read; packages: write`. Steps: checkout (with `fetch-depth: 0` so
  MinVer can walk the tag history), setup .NET 10 SDK, `dotnet restore`, `dotnet build -c Release
  --no-restore`, `dotnet test --no-build`, `dotnet pack src/Mked.Controls -c Release --no-build
  -o ./artifacts`, `dotnet nuget push ./artifacts/*.nupkg --source
  https://nuget.pkg.github.com/scmccart/index.json --api-key ${{ secrets.GITHUB_TOKEN }}`.
  Add a note to `src/Mked.Controls/README.md`'s "Install" section explaining that GitHub Packages
  requires a PAT/`nuget.config` for consumers (link to GitHub docs).
  Done when the workflow file is valid YAML (`gh workflow view release` or `act --dry-run`),
  and a `v0.1.0` tag pushed to the repo results in the package appearing on the GitHub
  Packages page.
  Depends on: Task 4

- [x] **Task 6: Revise epic spec + sync docs**
  Update `docs/epics/07-controls-library.md`:
  — Editor feature: replace the `await editor.RunAsync()` acceptance criterion with the
    host-driven contract (`HandleKey` / `BufferChanged` / `LoadDocument`).
  — NuGet Packaging feature: change "nuget.org" to "GitHub Packages feed"; add a note that
    nuget.org is deferred to Epic 9 (v1 readiness).
  — Remove the "Sample Project" feature entirely (Mked.Console is the reference usage).
  Update `docs/reference/controls-public-api.md` to reflect the v1 locked-down surface
  (remove internal types, mark public types, document the host-driven editor pattern).
  Done when both files accurately describe the shipped implementation.
  Depends on: Task 3, Task 4, Task 5

---

## Notes

- **`fetch-depth: 0` in CI is required for MinVer.** A shallow clone (`fetch-depth: 1`, the
  GitHub Actions default) means MinVer cannot find the most recent tag and falls back to
  `0.0.0-alpha.0.x`. Always set `fetch-depth: 0` on the checkout step in the release workflow.
- **Pre-release versions:** before the first `v*` tag, MinVer yields `0.0.0-alpha.0.<commits>`.
  This is expected on dev builds and CI non-tag runs.
- **Console coupling (Task 3):** if `Mked.Console` turns out to reference an internal candidate
  (e.g. a highlight-layer type), decide whether to keep it `public` (if it's a genuine contract)
  or refactor Console to not depend on it (if it's an implementation detail that leaked).
- **GitHub Packages consumer authentication:** consuming a GitHub Packages feed requires a PAT
  with `read:packages` scope and a `nuget.config` pointing at `nuget.pkg.github.com/scmccart`.
  This is a known friction point that Epic 9 (nuget.org) resolves for public consumers.
- **Branch name:** current branch is `feature/epic-7`; prior epics used `feat/epic-N`. No rename
  assumed — cosmetic difference only.
