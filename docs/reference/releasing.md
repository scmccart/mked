# Releasing

## Overview

A release is triggered by pushing a version tag to `main`. The
[`release.yml`](../../.github/workflows/release.yml) workflow then builds, tests, packs, and
pushes `Mked.Controls` to the GitHub Packages NuGet feed automatically.

## Prerequisites

- Write access to the repository (to push tags)
- `GITHUB_TOKEN` is used by the workflow automatically — no additional secrets are needed

## Versioning

`Mked.Controls` uses **MinVer** for git-tag–driven versioning:

| Situation | Version produced |
|---|---|
| Tag `v1.2.3` on the tagged commit | `1.2.3` |
| Tag `v1.2.3` + 4 commits ahead | `1.2.3-alpha.0.4` (pre-release) |
| No tags reachable | `0.0.0-alpha.0.<commits>` |

Use the `v` prefix (e.g. `v0.1.0`, `v1.0.0`). MinVer ignores tags without this prefix.

## Step-by-step: creating a release

1. **Ensure `main` is green** — all CI checks must pass before tagging.

2. **Choose a version** following [Semantic Versioning](https://semver.org/):
   - `MAJOR` — breaking public-API change
   - `MINOR` — new backwards-compatible functionality
   - `PATCH` — backwards-compatible bug fixes

3. **Create and push the tag:**

   ```sh
   git tag v0.2.0
   git push origin v0.2.0
   ```

4. **Watch the workflow** — navigate to the repository's **Actions** tab and select the
   **Release** workflow run triggered by your tag. It will:
   - Restore, build, and test the solution
   - Pack `Mked.Controls` into a `.nupkg`
   - Push the package to `https://nuget.pkg.github.com/scmccart/index.json`

5. **Verify** — after the workflow completes, the package should appear on the repository's
   [Packages](https://github.com/scmccart/mked/packages) page with the correct version number.

## Re-running a failed workflow

The workflow uses `--skip-duplicate` when pushing, so re-running a failed job for an already-
published version will not fail with a duplicate-package error.

## Deleting or moving a tag

If you need to retag (e.g. to fix a build error before any consumers have installed the package):

```sh
git tag -d v0.2.0               # delete locally
git push origin :refs/tags/v0.2.0  # delete remotely
git tag v0.2.0                  # recreate on the correct commit
git push origin v0.2.0
```

> **Note:** deleting a tag after the package is already published to GitHub Packages does not
> unpublish the package. Contact the repository owner to manage published package versions.

## Future: nuget.org publishing

Publishing to nuget.org is planned for **Epic 9 (v1 readiness)**. When that is implemented, the
`release.yml` workflow will be extended to push both to GitHub Packages and to nuget.org.
