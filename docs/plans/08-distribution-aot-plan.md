# Epic 08 — Distribution & AOT: Implementation Plan

> **Epic**: [`docs/epics/08-distribution-aot.md`](../../docs/epics/08-distribution-aot.md)
> **Design**: [`docs/designs/08-distribution-aot-design.md`](../../docs/designs/08-distribution-aot-design.md)
> **Status**: Draft

---

## Overview

Task 1 is the unlock: moving AOT properties out of the unconditional `<PropertyGroup>` and adding
`PackAsTool` metadata to `Mked.Console.csproj` is what lets the same project produce both a
framework-dependent dotnet tool (via `dotnet pack`) and native AOT binaries (via per-RID publish
profiles). Everything downstream — the profiles themselves, the trim audit, the release workflow,
and the WinGet scaffold — depends on that csproj being correct. Tasks 1–3 are the foundational
build configuration; Tasks 4–6 are the release automation built on top of it; Task 7 is the
documentation and WinGet scaffolding that caps the epic.

---

## Task List

- [ ] **Task 1: dotnet tool packaging config**
  Refactor `src/Mked.Console/Mked.Console.csproj`: remove `PublishAot`, `PublishSingleFile`,
  and `SelfContained` from the unconditional `<PropertyGroup>` (they move into `.pubxml` profiles
  in Task 2) so `dotnet pack` produces a clean framework-dependent IL assembly. Add `PackAsTool`,
  `ToolCommandName=mked`, `PackageId=mked`, `IsPackable=true`, tool description and tags, and a
  `MinVer` package reference (`PrivateAssets=all`) for git-tag-driven versioning. Done when
  `dotnet pack src/Mked.Console -c Release -o ./artifacts` emits `mked.<version>.nupkg` without
  AOT errors, and `dotnet tool install --global --add-source ./artifacts mked && mked --help`
  succeeds.

- [ ] **Task 2: NativeAOT publish profiles (per RID)**
  Create `src/Mked.Console/Properties/PublishProfiles/` and add five `.pubxml` files —
  `win-x64.pubxml`, `linux-x64.pubxml`, `linux-arm64.pubxml`, `osx-arm64.pubxml`,
  `osx-x64.pubxml`. Each profile sets `RuntimeIdentifier`, `PublishAot=true`,
  `PublishSingleFile=true`, `SelfContained=true`, `InvariantGlobalization=true`,
  `StripSymbols=true`, and `Configuration=Release`. Done when
  `dotnet publish src/Mked.Console -p:PublishProfile=<host-rid>` produces a self-contained native
  binary that runs `mked view --plain` with no installed .NET runtime.
  Depends on: Task 1

- [ ] **Task 3: Trim safety audit**
  Run `dotnet publish` with each locally buildable RID and confirm zero IL warnings. Review the
  existing `NoWarn=IL2026;IL2104;IL3000;IL3050` in `Mked.Console.csproj` — trim to only the
  warnings still emitted by Spectre, and add an inline comment for each retained suppression
  explaining which Spectre type is responsible. Add `<IsTrimmable>true</IsTrimmable>` and
  `<IsAotCompatible>true</IsAotCompatible>` to `src/Mked.Controls/Mked.Controls.csproj` and
  resolve any new warnings that surfaces. Update `docs/architecture/aot-trim-safety.md` with the
  current publish command (profile-based), the final suppression rationale, and the dependency
  checklist state. Done when a clean `dotnet publish` produces zero warnings and the doc is current.
  Depends on: Task 2

- [ ] **Task 4: Release workflow — matrix AOT build + GitHub Release**
  Expand `.github/workflows/release.yml` with a `build` matrix job covering the five
  (runner, RID) pairs: `windows-latest`/`win-x64`, `ubuntu-latest`/`linux-x64`,
  `ubuntu-24.04-arm`/`linux-arm64`, `macos-latest`/`osx-arm64`, `macos-13`/`osx-x64`. Each job
  runs `dotnet restore`, `dotnet publish` with the matching profile, generates a `<rid>.sha256`
  checksum, and uploads both files as a workflow artifact named `binary-<rid>`. Add a downstream
  `release` job (`needs: build`, `permissions: contents: write`) that downloads all artifacts, creates
  the GitHub Release with `gh release create $TAG --generate-notes`, and attaches all 10 files
  (5 binaries + 5 checksums). Done when pushing a `v*` tag yields a GitHub Release with all
  platform binaries and checksum files attached.
  Depends on: Task 2

- [ ] **Task 5: Release workflow — package push (Controls + tool)**
  Extend `release.yml` with a `publish` job (`needs: release`) that checks out the repo, sets up
  .NET 10, packs both `src/Mked.Console/Mked.Console.csproj` and
  `src/Mked.Controls/Mked.Controls.csproj` into `./artifacts`, then pushes all `.nupkg` files to
  GitHub Packages (`--source https://nuget.pkg.github.com/scmccart/index.json`,
  `--api-key ${{ secrets.GITHUB_TOKEN }}`, `--skip-duplicate`). The existing `packages: write`
  permission already covers this. Done when a release tag publishes both `mked` and `Mked.Controls`
  NuGet packages to GitHub Packages.
  Depends on: Task 1, Task 4

- [ ] **Task 6: CI smoke test**
  Add a `smoke` job (`needs: publish`) to `release.yml`. The job checks out the repo, sets up
  .NET 10, downloads the `packages` artifact, installs the tool with
  `dotnet tool install --global --add-source ./artifacts mked`, then runs
  `mked view --plain tests/fixtures/smoke-fixture.md` and asserts exit code 0. Also commit a
  minimal `tests/fixtures/smoke-fixture.md` (a few lines of valid Markdown) for the command to
  consume. Done when the workflow end-to-end passes and a broken binary/tool causes the smoke job
  to fail.
  Depends on: Task 5

- [ ] **Task 7: WinGet manifest scaffold + release docs**
  Create `manifests/s/scmccart/mked/0.0.0/` with three placeholder YAML files following WinGet
  schema 1.6.0: `scmccart.mked.yaml` (version manifest), `scmccart.mked.installer.yaml`
  (win-x64 installer entry referencing the GitHub Release binary URL and sha256 checksum — use
  explicit placeholders such as `<RELEASE_URL>` and `<SHA256>`), and
  `scmccart.mked.locale.en-US.yaml` (display name, description, publisher, links). Write
  `docs/reference/releasing.md` documenting: the full `v*` tag → Release → GitHub Packages →
  WinGet sequence; how to verify the release workflow passes; and the manual `wingetcreate` /
  `microsoft/winget-pkgs` PR flow for the first WinGet submission. Done when manifests are
  committed, the document is complete, and a new contributor could follow it to cut a release and
  submit to WinGet.
  Depends on: Task 4

---

## Notes

- **linux-arm64 runner confirmed available** (`ubuntu-24.04-arm`); no `continue-on-error`
  fallback required in the matrix.
- **WinGet scope confirmed win-x64 native binary only** — the installer manifest targets the
  `.exe` from the GitHub Release, not the NuGet tool package.
- **Verification pipeline:** after all tasks, push a pre-release tag (e.g. `v0.0.1-alpha.1`)
  on the feature branch to exercise the full workflow end-to-end before merging.
- **Existing tests** (`dotnet test mked.slnx`) must remain green throughout; the ArchUnitNet
  layer dependency rules are unaffected by any change in this epic.
