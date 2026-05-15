# Epic 08 — Distribution & AOT

Publish mked as a self-contained, NativeAOT-compiled single-file binary for each target platform,
as a dotnet tool on nuget.org, and eventually as a WinGet package. AOT safety and startup speed are
non-negotiable requirements throughout.

## Features

- NativeAOT publish profiles for `win-x64`, `linux-x64`, `linux-arm64`, `osx-arm64`, `osx-x64`
- Trim-safe audit: no reflection, no `dynamic`, no unattributed `JsonSerializer`, `[GeneratedRegex]` on all regex
- Startup time target: ≤ 50 ms cold start on each target platform
- `dotnet tool install -g mked` packaging: `<PackAsTool>true</PackAsTool>`, tool manifest, version scheme
- GitHub Actions release workflow: build, AOT-publish, create GitHub Release with platform binaries attached
- nuget.org push for `Mked.Controls` and `mked` tool on version tag
- Checksum (`sha256`) files alongside each binary in GitHub Releases
- WinGet manifest scaffolding (planned): `winget install mked`
- `global.json` pins SDK version to ensure reproducible AOT builds
- CI smoke test: install tool, run `mked view --plain`, assert exit code 0
