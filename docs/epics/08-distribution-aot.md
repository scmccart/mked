# Epic 08 — Distribution & AOT

Publish mked as a self-contained, NativeAOT-compiled single-file binary for each target platform,
as a dotnet tool on nuget.org, and eventually as a WinGet package. AOT safety and startup speed are
non-negotiable requirements throughout.

## Features

### Feature: NativeAOT Publish Profiles

Produce a single-file, self-contained binary for each supported platform.

- As a user, I can download a pre-built binary for my platform (Windows x64, Linux x64, Linux arm64, macOS arm64, macOS x64) and run it with no .NET runtime installed
- As a developer, each platform has a `.pubxml` publish profile committed to the repository
- As a developer, `global.json` pins the SDK version so all profiles produce reproducible outputs
- As a developer, the startup time is ≤ 50 ms cold on each target platform

### Feature: Trim Safety Audit

Ensure the binary is free of trim warnings and reflection-based code paths.

- As a developer, `dotnet publish` produces zero trim warnings for the `mked` project
- As a developer, all `Regex` usages carry `[GeneratedRegex]`
- As a developer, all JSON serialisation uses source-generated contexts (`[JsonSerializable]`)
- As a developer, any unavoidable reflection is annotated with `[DynamicDependency]` or `[RequiresUnreferencedCode]`
- As a developer, the AOT safety checklist in `docs/architecture/` is updated as new dependencies are added

### Feature: dotnet Tool Packaging

Publish `mked` as a `dotnet tool` installable from nuget.org.

- As a user, I can install mked with `dotnet tool install -g mked`
- As a user, I can update to a new version with `dotnet tool update -g mked`
- As a developer, the project carries `<PackAsTool>true</PackAsTool>` and correct tool metadata
- As a developer, the tool is pushed to nuget.org by the release workflow on a version tag

### Feature: GitHub Actions Release Workflow

Automate the build, publish, and release process end-to-end.

- As a developer, pushing a `v*` tag triggers a matrix build that AOT-publishes all platform binaries
- As a developer, a GitHub Release is created automatically with all platform binaries and `sha256` checksum files attached
- As a developer, `Mked.Controls` and the `mked` tool package are pushed to nuget.org in the same workflow
- As a developer, a CI smoke test installs the tool, runs `mked view --plain`, and asserts exit code 0

### Feature: WinGet Manifest

Make mked discoverable and installable via the Windows Package Manager.

- As a Windows user, I can install mked with `winget install mked`
- As a developer, a WinGet manifest (`manifests/`) is scaffolded and submitted to the community repository on release
- As a developer, the manifest references the GitHub Release binary URL and its `sha256` checksum
