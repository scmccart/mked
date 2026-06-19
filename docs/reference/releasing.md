# Releasing mked

This document covers the full release process: from cutting a version tag through monitoring the
release workflow to submitting the WinGet manifest for the first time.

## Prerequisites

- `gh` CLI authenticated (`gh auth status`)
- Git push access to `scmccart/mked`
- .NET 10 SDK (for local verification before tagging)

## Release checklist

1. **Ensure `main` is green.** The release workflow runs tests, but a broken `main` before tagging
   wastes a release attempt.

2. **Pick a version.** mked uses [Semantic Versioning](https://semver.org). MinVer derives the
   package version automatically from the `v*` tag — no manual edits to project files are needed.

3. **Tag and push.**

   ```powershell
   git tag v1.0.0
   git push origin v1.0.0
   ```

   The release workflow triggers immediately on the tag push.

4. **Monitor the workflow.**

   ```powershell
   gh run watch --repo scmccart/mked
   ```

   Or open the Actions tab in the browser. The pipeline runs five jobs in order:

   | Job | What it does |
   |-----|-------------|
   | `test` | Builds and tests the solution on Ubuntu |
   | `build` (×5) | AOT-compiles a native binary per platform in parallel |
   | `release` | Creates the GitHub Release and attaches all binaries + checksums |
   | `publish` | Packs both NuGet packages and pushes to GitHub Packages |
   | `smoke` | Installs the tool from the package feed and runs `mked view --plain` |

5. **Verify the release.** Once the workflow is green:
   - Check [github.com/scmccart/mked/releases](https://github.com/scmccart/mked/releases) for the
     new release with 5 binaries and 5 `.sha256` files.
   - Confirm both `mked` and `Mked.Controls` appear in
     [github.com/scmccart/mked/packages](https://github.com/scmccart/mked/packages).

## Installing from GitHub Packages

```powershell
dotnet nuget add source https://nuget.pkg.github.com/scmccart/index.json `
  --name scmccart-github `
  --username scmccart `
  --password <PAT with read:packages>

dotnet tool install --global mked
```

> **Note:** Until mked is published to nuget.org (Epic 9), users need to add this source first.

## WinGet submission

WinGet manifests live in `manifests/s/scmccart/mked/<version>/`. The `0.0.0` directory contains
templates with placeholders — copy it to a new directory for each release.

### First-time setup

Install the `wingetcreate` tool:

```powershell
winget install Microsoft.WingetCreate
```

### Submitting a new version

1. **Copy the template** to the new version folder:

   ```powershell
   $ver = "1.0.0"
   Copy-Item -Recurse manifests/s/scmccart/mked/0.0.0 manifests/s/scmccart/mked/$ver
   ```

2. **Get the release asset URL and checksum** from the GitHub Release:

   ```powershell
   $ver = "1.0.0"
   $url = "https://github.com/scmccart/mked/releases/download/v$ver/mked-win-x64.exe"
   # Download the .sha256 file to get the hash
   $hash = (Invoke-WebRequest "https://github.com/scmccart/mked/releases/download/v$ver/mked-win-x64.exe.sha256").Content.Split()[0]
   ```

3. **Fill in the manifest files** in `manifests/s/scmccart/mked/$ver/`:
   - In all three files: replace `0.0.0` with `$ver`
   - In `scmccart.mked.installer.yaml`: replace `<RELEASE_URL>` with the release download URL
     base and `<SHA256>` with the hash from the `.sha256` file

4. **Validate the manifests locally:**

   ```powershell
   wingetcreate validate manifests/s/scmccart/mked/$ver
   ```

5. **Submit to the WinGet community repository:**

   ```powershell
   wingetcreate submit manifests/s/scmccart/mked/$ver `
     --token <PAT with public_repo on microsoft/winget-pkgs>
   ```

   `wingetcreate submit` opens a PR against
   [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) automatically.

6. **Monitor the PR** in the microsoft/winget-pkgs repository. The WinGet bot validates the
   manifest and merges it; this typically takes 24–72 hours.

## Rolling back a release

To delete a mistaken release and re-tag:

```powershell
gh release delete v1.0.0 --repo scmccart/mked --yes
git tag -d v1.0.0
git push origin --delete v1.0.0
```

Fix the issue, then re-tag and push. Packages already pushed to GitHub Packages cannot be
deleted (GitHub Packages policy) — push a patch release instead.
