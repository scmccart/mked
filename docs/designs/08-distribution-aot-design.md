# Epic 08 — Distribution & AOT: Technical Design

> **Epic**: [`docs/epics/08-distribution-aot.md`](../../docs/epics/08-distribution-aot.md)
> **Status**: Implemented
> **Date**: 2026-06-18

---

## Goals

- Produce a self-contained, NativeAOT-compiled single-file binary for each target platform
  (win-x64, linux-x64, linux-arm64, osx-arm64, osx-x64) via committed publish profiles.
- Package `mked` as a `dotnet tool` installable from GitHub Packages.
- Automate the full release pipeline via a tag-triggered GitHub Actions workflow that: runs a
  matrix AOT build, creates a GitHub Release with all platform binaries and `sha256` checksum
  files, and pushes both `mked` and `Mked.Controls` NuGet packages to GitHub Packages.
- Add a CI smoke test that installs the tool package and asserts `mked view --plain` exits 0.
- Ensure zero trim warnings on publish and update the AOT safety checklist.
- Scaffold WinGet manifest templates (`manifests/`) and document the manual first-submission flow.

## Non-Goals

- Publishing to **nuget.org** (deferred to Epic 9 / v1 readiness; GitHub Packages only this epic).
- Automated WinGet submission to `microsoft/winget-pkgs` (scaffold + manual first PR only).
- A startup-time benchmark CI gate (the ≤ 50 ms goal is verified manually post-publish; automating
  it across all platform runners is disproportionate effort for this epic).
- Any changes to Domain, Application, or Infrastructure logic.

---

## Architecture Overview

This epic is a **build / packaging / CI epic**. There are no new Clean Architecture layers, no new
domain types, and no changes to runtime behaviour. The changes land in:

| Layer | Project | Role |
|-------|---------|------|
| Presentation | `Mked.Console` | Gains tool metadata (`PackAsTool`, `PackageId`, `ToolCommandName`) and moves AOT properties from unconditional props to `.pubxml` publish profiles |
| Controls library | `Mked.Controls` | Gains `<IsTrimmable>true</IsTrimmable>` and `<IsAotCompatible>true</IsAotCompatible>` so the compiler verifies the library is AOT-safe when consumed by the AOT exe |
| CI / Release | `.github/workflows/release.yml` | Restructured into a matrix of 5 platform jobs + a release-assembly job + a smoke-test job |
| Packaging | `manifests/` (new) | WinGet manifest templates |
| Documentation | `docs/architecture/aot-trim-safety.md`, `docs/reference/releasing.md` | Audit update; release runbook |

### The packaging split

`PackAsTool` (framework-dependent IL assembly) and `PublishAot` (native binary) are mutually
exclusive in one `dotnet` invocation. The single `Mked.Console` project therefore produces **two
entirely separate outputs** via different MSBuild configurations:

```
Mked.Console.csproj
 ├─ dotnet pack  (PackAsTool mode — no AOT flags)
 │    → mked.<version>.nupkg (IL, framework-dependent)
 │    → pushed to GitHub Packages
 │
 └─ dotnet publish -p:PublishProfile=<rid>  (AOT mode — flags in .pubxml)
      → native single-file binary + .sha256
      → uploaded to GitHub Release
      → referenced by WinGet manifest
```

To make this work, `PublishAot`, `PublishSingleFile`, and `SelfContained` must be removed from the
unconditional `<PropertyGroup>` in `Mked.Console.csproj` and placed exclusively in each `.pubxml`
profile. The `dotnet pack` path then sees a plain executable project and emits a clean tool
package; the `dotnet publish -p:PublishProfile=<rid>` path gets the AOT flags from the profile.

### MinVer versioning

MinVer (already configured for `Mked.Controls`) is added to `Mked.Console` as well. Both packages
derive their version from the triggering `v*` git tag — no manual version properties needed.

### GitHub Actions matrix

NativeAOT cannot cross-compile (the native compiler must run on the target OS family). The
release workflow therefore runs a matrix of five (runner, RID) pairs:

| Runner | RID |
|--------|-----|
| `windows-latest` | `win-x64` |
| `ubuntu-latest` | `linux-x64` |
| `ubuntu-24.04-arm` | `linux-arm64` |
| `macos-latest` | `osx-arm64` |
| `macos-13` | `osx-x64` |

Each matrix job: checks out the repo (full history for MinVer), sets up .NET 10, restores,
publishes with the matching profile, computes the `sha256` checksum, and uploads both files
as workflow artifacts. A downstream release-assembly job downloads all artifacts and creates
the GitHub Release. A final smoke-test job installs the tool from the packed nupkg and
asserts correct behaviour.

---

## Key Types and Interfaces

This epic introduces no new C# types. The key artefacts are MSBuild/YAML configuration files.

### New files

| Artefact | Kind | Location | Purpose |
|----------|------|----------|---------|
| `win-x64.pubxml` | MSBuild publish profile | `src/Mked.Console/Properties/PublishProfiles/` | AOT publish for Windows x64 |
| `linux-x64.pubxml` | MSBuild publish profile | `src/Mked.Console/Properties/PublishProfiles/` | AOT publish for Linux x64 |
| `linux-arm64.pubxml` | MSBuild publish profile | `src/Mked.Console/Properties/PublishProfiles/` | AOT publish for Linux arm64 |
| `osx-arm64.pubxml` | MSBuild publish profile | `src/Mked.Console/Properties/PublishProfiles/` | AOT publish for macOS arm64 |
| `osx-x64.pubxml` | MSBuild publish profile | `src/Mked.Console/Properties/PublishProfiles/` | AOT publish for macOS x64 |
| `scmccart.mked.yaml` | WinGet version manifest | `manifests/s/scmccart/mked/<version>/` | WinGet version entry |
| `scmccart.mked.installer.yaml` | WinGet installer manifest | same | Installer URLs + sha256 per platform |
| `scmccart.mked.locale.en-US.yaml` | WinGet locale manifest | same | Display name, description, links |
| `smoke-fixture.md` | Markdown sample | `tests/fixtures/` | Input file for `mked view --plain` smoke test |

### Modified files

| File | Change |
|------|--------|
| `src/Mked.Console/Mked.Console.csproj` | Remove unconditional AOT props; add `PackAsTool`, `ToolCommandName`, `PackageId=mked`, `IsPackable`, tool description/tags, `MinVer` reference |
| `src/Mked.Controls/Mked.Controls.csproj` | Add `<IsTrimmable>true</IsTrimmable>` and `<IsAotCompatible>true</IsAotCompatible>` |
| `.github/workflows/release.yml` | Restructure into matrix-build + release-assembly + package-push + smoke-test jobs |
| `docs/architecture/aot-trim-safety.md` | Update publish command; document current suppressions with rationale |
| `docs/reference/releasing.md` | Full release runbook: tag, monitor, WinGet submission steps |

---

## Data Flow / Sequence

### Use Case: Release tag → GitHub Release with platform binaries

1. Developer pushes a `v*` tag to the remote.
2. `release.yml` fires on `push: tags: 'v*'`.
3. **Matrix job (×5):** each (runner, RID) pair:
   a. Checks out repo with `fetch-depth: 0` (MinVer requires full history).
   b. Sets up .NET 10 SDK (`10.0.x`).
   c. `dotnet restore mked.slnx`.
   d. `dotnet publish src/Mked.Console/Mked.Console.csproj -p:PublishProfile=<rid>` — AOT flags
      come from the profile; binary lands in `./publish/<rid>/`.
   e. Computes `sha256` of the binary → `<rid>.sha256` text file.
   f. Uploads both binary and checksum as a workflow artifact named `binary-<rid>`.
4. **Release-assembly job** (needs: all matrix jobs):
   a. Downloads all `binary-*` artifacts.
   b. Creates the GitHub Release for the tag (`gh release create $TAG --title "mked $TAG"
      --generate-notes`).
   c. Attaches all 5 binaries and 5 checksums to the release.
5. **Package-push job** (needs: release-assembly):
   a. `dotnet pack src/Mked.Console/Mked.Console.csproj -c Release -o ./artifacts`
      → `mked.<version>.nupkg` (tool, framework-dependent).
   b. `dotnet pack src/Mked.Controls/Mked.Controls.csproj -c Release -o ./artifacts`
      → `Mked.Controls.<version>.nupkg`.
   c. `dotnet nuget push ./artifacts/*.nupkg --source https://nuget.pkg.github.com/scmccart/index.json
      --api-key ${{ secrets.GITHUB_TOKEN }} --skip-duplicate`.
6. **Smoke-test job** (needs: package-push):
   a. `dotnet tool install --global --add-source ./artifacts mked`.
   b. `mked view --plain tests/fixtures/smoke-fixture.md` → assert exit code 0.

### Use Case: WinGet submission (manual, post-release)

1. Developer downloads the `win-x64` binary from the GitHub Release.
2. Copies the sha256 from the corresponding checksum file.
3. Opens `manifests/s/scmccart/mked/<version>/scmccart.mked.installer.yaml`, fills in
   `PackageVersion`, release asset URL, and sha256.
4. Follows the `wingetcreate`/PR flow documented in `docs/reference/releasing.md`.

---

## Error Handling Strategy

There are no new runtime error paths — this epic is build tooling. The relevant failure modes are:

- **AOT publish warnings/errors:** `TreatWarningsAsErrors=true` (global in `Directory.Build.props`)
  means any IL trim warning fails the matrix build job. The audit in Task 3 ensures zero warnings
  exist before the workflow is wired up.
- **Package push failure:** `--skip-duplicate` prevents a re-run from failing on an already-pushed
  version. Any other push failure is surfaced as a failed workflow step.
- **Smoke test failure:** the job fails and the release is left without a green smoke-test badge.
  The release is still created (smoke-test `needs` package-push, which `needs` release-assembly)
  so assets are available for investigation. Operators can re-trigger the smoke-test job manually
  once the issue is fixed.
- **linux-arm64 runner unavailable:** if `ubuntu-24.04-arm` is not available in the Actions pool,
  the matrix includes a `continue-on-error: true` guard for that one RID, and the release-assembly
  job skips the missing artifact with a logged warning. Document this fall-back in the release runbook.

---

## Testing Approach

- **Unit tests:** no new unit tests — no new C# types.
- **AOT publish smoke (manual):** `dotnet publish src/Mked.Console -p:PublishProfile=<host-rid>` on
  a dev machine; confirm zero IL warnings and the binary runs `mked view --plain`.
- **Tool install smoke (CI):** the smoke-test workflow job installs the freshly packed tool from
  the artifacts folder and runs `mked view --plain tests/fixtures/smoke-fixture.md`; exit 0 is asserted.
- **Trim regression guard (CI):** `TreatWarningsAsErrors=true` already in effect; the matrix AOT
  publish step will fail the build if any trim/AOT warning is introduced.
- **Existing test suite:** `dotnet test mked.slnx` must continue to pass; ArchUnitNet layer
  dependency rules are unaffected (no new production references).

---

## Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | Is `ubuntu-24.04-arm` available as a hosted runner for this repo/org, or does linux-arm64 require self-hosting or a cross-compile workaround? | Closed - it is available |
| 2 | Should the WinGet manifest target the GitHub Release binary (`.exe`/ELF) or the NuGet tool package? Convention is the native binary for WinGet; confirm scope is win-x64 binary only | Closed - Confirmed win-x64 binary only |
