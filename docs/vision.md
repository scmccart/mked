# mked — Vision Statement

## What It Is

**mked** is a .NET 10 terminal-native tool and library for viewing and editing Markdown. It is built on [Spectre.Console](https://spectreconsole.net/) and delivers two complementary artefacts:

- **`Mked.Controls`** — a NuGet library extending Spectre.Console with a `MarkdownEditor` control (live syntax-highlighted text area) and a `MarkdownViewer` control (richly styled document renderer).
- **`mked`** — a self-contained, AOT-compiled dotnet tool (and eventually a WinGet package) providing `view` and `edit` modes from the command line.

## Who It Is For

Developers, writers, and power users who live in the terminal and want a first-class Markdown experience without leaving it. If you reach for `vim`, `less`, or `bat` as your daily drivers, mked is built for you.

## Why It Exists

Existing terminal Markdown tools either render read-only output (no editing) or offer a full GUI editor that pulls you out of the terminal workflow. mked sits in the gap: a keyboard-driven, distraction-free environment where you can both *read* and *write* Markdown without reaching for the mouse or switching to a GUI.

Key motivations:

- **Live syntax highlighting** while typing — not a preview pane you toggle, but colour and style applied in real time as you type.
- **Viewport stability** — when content is refreshed (streaming input, live preview), the visible area stays anchored to a recognisable Markdown element rather than jumping to the top.
- **Zero configuration** — sensible defaults out of the box; no config files required to start working.

## How It Is Distributed

| Artefact | Channel |
|---|---|
| `Mked.Controls` NuGet library | GitHub Packages (nuget.org planned — Epic 9) |
| `mked` dotnet tool | nuget.org (`dotnet tool install -g mked`) |
| Self-contained single-file executable | GitHub Releases |
| WinGet package | WinGet (planned) |

The tool executable is published as a **self-contained, trimmed, NativeAOT single-file binary** for each target platform. No .NET runtime installation is required to run it.

## Guiding Principles

1. **Speed** — startup must feel instant; AOT compilation and trim-safe design are non-negotiable.
2. **Simplicity** — the surface area stays small. Two modes (view, edit), one file at a time.
3. **Liveness** — syntax highlighting and preview updates happen as you type, not on a delay.
4. **Viewport pleasantness** — content reflows and redraws never disorient the user; the cursor and visible region are preserved as faithfully as possible.
5. **Terminal citizenship** — respects terminal conventions, colour themes, and keyboard shortcuts for the host OS.
