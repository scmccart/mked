# Authoring Custom Widgets in Spectre.Console

This guide covers everything needed to build two kinds of custom controls for `Mked.Controls`:

1. A read-only `MarkdownViewer` — a passive `IRenderable` that composes inside panels, tables, and live displays.
2. An interactive `MarkdownEditor` — a full-screen prompt that takes over the terminal, processes raw key events, and returns a `string` result.

All API details reflect Spectre.Console v0.49+ (latest stable as of mid-2025). Sources: official documentation at spectreconsole.net, raw GitHub source files, and Context7 documentation queries.

---

## 1. The Rendering Model

### 1.1 How rendering works

When code calls `AnsiConsole.Write(renderable)`:

1. A render lock is acquired (thread safety for concurrent writers).
2. `RenderOptions` is created from the console's `Profile` (capabilities + terminal size).
3. `renderable.Render(options, width)` is called and produces a `IEnumerable<Segment>`.
4. The pipeline converts each `Segment` into ANSI/VT escape sequences (`\x1b[31;1mtext\x1b[0m`).
5. The byte stream is flushed to the underlying `TextWriter`.

On terminals without ANSI support, escape codes are omitted and plain text is output.

### 1.2 `IRenderable` — the core interface

```csharp
namespace Spectre.Console.Rendering;

public interface IRenderable
{
    // Returns the minimum and maximum column-width the widget needs.
    Measurement Measure(RenderOptions options, int maxWidth);

    // Produces the widget's visual output as a flat sequence of styled text atoms.
    IEnumerable<Segment> Render(RenderOptions options, int maxWidth);
}
```

`Measure` is called first — without emitting anything — so that container widgets (`Panel`, `Table`, `Columns`) can compute layout. `Render` is called once the container has resolved final widths.

**Extension method:** `renderable.GetSegments(IAnsiConsole console)` produces segments using the full render pipeline, including the pipeline's own pre/post processors.

### 1.3 `Measurement` struct

```csharp
namespace Spectre.Console.Rendering;

public readonly struct Measurement : IEquatable<Measurement>
{
    public int Min { get; }   // minimum width (never wrap below this)
    public int Max { get; }   // maximum width (ideal/unconstrained width)

    public Measurement(int min, int max) { ... }
}
```

Always return `new Measurement(min, min)` for fixed-width widgets, or `new Measurement(1, textLength)` for text that can be line-wrapped.

If your widget must respect `maxWidth`, clamp `Max`: `new Measurement(min, Math.Min(max, maxWidth))`.

### 1.4 `RenderOptions` record

```csharp
namespace Spectre.Console.Rendering;

public record class RenderOptions(IReadOnlyCapabilities Capabilities, Size ConsoleSize)
{
    // Terminal color depth: NoColors / Ansi / EightBit / TrueColor
    public ColorSystem ColorSystem => Capabilities.ColorSystem;

    // Whether VT/ANSI escape codes are supported
    public bool Ansi => Capabilities.Ansi;

    // Whether Unicode box-drawing characters are supported
    public bool Unicode => Capabilities.Unicode;

    // Horizontal justification hint (set by containers)
    public Justify? Justification { get; init; }

    // Height constraint (set by containers that clip vertically)
    public int? Height { get; init; }

    // (internal) Request to suppress line breaks — do not rely on this
    // internal bool SingleLine { get; init; }

    // Static factory: build from a console instance
    public static RenderOptions Create(IAnsiConsole console,
        IReadOnlyCapabilities? capabilities = null);
}
```

Use `options.Unicode` to switch between box-drawing glyphs and ASCII fallbacks. Use `options.ColorSystem` to decide whether to emit 24-bit colour or fall back to named colours.

---

## 2. `Segment` — the atomic output unit

### 2.1 Class definition

```csharp
namespace Spectre.Console.Rendering;

public class Segment
{
    public string Text        { get; }   // the raw text content
    public Style  Style       { get; }   // foreground, background, decoration
    public bool   IsLineBreak { get; }   // true for explicit newline segments
    public bool   IsWhiteSpace{ get; }   // true if Text is null/whitespace
    public bool   IsControlCode{ get; } // true for ANSI control sequences
}
```

### 2.2 Public constructors

```csharp
// Text with explicit style
new Segment(string text, Style style, Link? link = null)

// Plain text (no colour / decoration)
new Segment(string text)              // Style.Plain is applied automatically
```

### 2.3 Static factory properties / helpers

```csharp
Segment.LineBreak   // pre-built newline segment (IsLineBreak = true)
Segment.Empty       // zero-length, Style.Plain

// Split a flat segment stream into lines (handles wrapping at maxWidth)
Segment.SplitLines(IEnumerable<Segment> segments, int maxWidth, int? height = null)
    → List<SegmentLine>

// Strip trailing line endings from a single segment
segment.StripLineEndings() → Segment
```

### 2.4 `SegmentLine`

```csharp
namespace Spectre.Console.Rendering;

// A List<Segment> representing one terminal row
public sealed class SegmentLine : List<Segment>
{
    // Column-count of this line (sum of segment text lengths)
    public int Length { get; }
}
```

`SegmentLine` is the natural unit when building multi-line widgets: build one `SegmentLine` per visual row, yield all segments from each line followed by `Segment.LineBreak`.

---

## 3. `Style` and `Decoration`

### 3.1 `Style`

```csharp
// All parameters optional; null means "inherit"
new Style(
    Color?      foreground  = null,
    Color?      background  = null,
    Decoration? decoration  = null,
    string?     link        = null)

// Predefined styles
Style.Plain   // no colour, no decoration
```

Combine styles with `style.Combine(other)`.

### 3.2 `Decoration` — a `[Flags]` enum

| Value | Markup equivalent | Effect |
|---|---|---|
| `Decoration.None` | — | No decoration |
| `Decoration.Bold` | `[bold]` | Bold / bright |
| `Decoration.Dim` | `[dim]` | Dimmed |
| `Decoration.Italic` | `[italic]` | Italic |
| `Decoration.Underline` | `[underline]` | Underline |
| `Decoration.Invert` | `[invert]` | Swap fg/bg |
| `Decoration.Conceal` | `[conceal]` | Hidden text |
| `Decoration.SlowBlink` | `[slowblink]` | Slow blink |
| `Decoration.RapidBlink` | `[rapidblink]` | Rapid blink |
| `Decoration.Strikethrough` | `[strikethrough]` | Strikethrough |

Combine flags: `Decoration.Bold | Decoration.Underline`.

### 3.3 `Color`

Use named colours (`Color.Red`, `Color.Green`, …) for broad compatibility. For 24-bit colour, use `new Color(r, g, b)`, but check `options.ColorSystem >= ColorSystem.TrueColor` first or Spectre.Console will downgrade automatically.

---

## 4. Building `MarkdownViewer` — a read-only `IRenderable`

### 4.1 Skeleton

```csharp
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Mked.Controls;

/// <summary>
/// Renders a Markdown document as styled console output.
/// </summary>
public sealed class MarkdownViewer : IRenderable
{
    private readonly string _markdown;

    public MarkdownViewer(string markdown)
    {
        _markdown = markdown ?? throw new ArgumentNullException(nameof(markdown));
    }

    /// <inheritdoc/>
    public Measurement Measure(RenderOptions options, int maxWidth)
    {
        // Minimum: we can always render at some width (e.g. 20 cols)
        // Maximum: the longest line in the rendered output
        var lines = BuildLines(options, maxWidth);
        var maxLine = lines.Count == 0 ? 0 : lines.Max(l => l.Length);
        return new Measurement(Math.Min(20, maxWidth), Math.Min(maxLine, maxWidth));
    }

    /// <inheritdoc/>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        foreach (var line in BuildLines(options, maxWidth))
        {
            foreach (var segment in line)
                yield return segment;
            yield return Segment.LineBreak;
        }
    }

    private List<SegmentLine> BuildLines(RenderOptions options, int maxWidth)
    {
        // Parse markdown with Markdig, walk the AST, produce segments.
        // Use options.Unicode to choose border chars.
        // Use options.ColorSystem to decide colour depth.
        throw new NotImplementedException();
    }
}
```

### 4.2 Composing inside built-in containers

Because `MarkdownViewer` implements `IRenderable`, it composes freely:

```csharp
// Wrap in a Panel with a title
var viewer = new MarkdownViewer(markdownText);
var panel  = new Panel(viewer).Header("[bold]Preview[/]");
AnsiConsole.Write(panel);

// Embed inside a Table cell
var table = new Table().AddColumn("Markdown").AddColumn("Rendered");
table.AddRow(new Text(markdownText), viewer);
AnsiConsole.Write(table);
```

### 4.3 Live updating the viewer

```csharp
var viewer = new MarkdownViewer(initialMarkdown);

await AnsiConsole.Live(viewer)
    .AutoClear(false)
    .Overflow(VerticalOverflow.Ellipsis)
    .StartAsync(async ctx =>
    {
        // Whenever the document changes, swap the target and refresh
        await foreach (var updated in documentChanges)
        {
            ctx.UpdateTarget(new MarkdownViewer(updated));
            // UpdateTarget calls Refresh() internally
        }
    });
```

`ctx.UpdateTarget(IRenderable)` atomically replaces the displayed widget and triggers a repaint. For mutation-in-place (e.g. a `Table`), call `ctx.Refresh()` after modifying the object.

---

## 5. `LiveDisplay` and `LiveDisplayContext` — the live rendering API

### 5.1 `AnsiConsole.Live(IRenderable)`

```csharp
// Entry point — pass any IRenderable as the initial target
LiveDisplay liveDisplay = AnsiConsole.Live(renderable);
```

### 5.2 Configuration (fluent, before `Start`)

```csharp
liveDisplay
    .AutoClear(bool enabled)          // clear display when done (default: false)
    .Overflow(VerticalOverflow)       // Crop | Scroll | Ellipsis | Visible
    .Cropping(VerticalOverflowCropping); // Bottom | Top
```

### 5.3 Starting the loop

```csharp
// Synchronous
liveDisplay.Start(Action<LiveDisplayContext> action);

// Asynchronous (preferred for keyboard-driven editors)
await liveDisplay.StartAsync(Func<LiveDisplayContext, Task> action);

// Async with return value
T result = await liveDisplay.StartAsync<T>(Func<LiveDisplayContext, Task<T>> action);
```

### 5.4 `LiveDisplayContext` methods

```csharp
public sealed class LiveDisplayContext
{
    // Force a redraw of the current target immediately
    void Refresh();

    // Atomically replace the rendered widget and redraw
    void UpdateTarget(IRenderable? target);
}
```

---

## 6. `IAnsiConsoleInput` — keyboard input in a live loop

### 6.1 Interface

```csharp
namespace Spectre.Console;

public interface IAnsiConsoleInput
{
    // Non-blocking check: is a key waiting in the buffer?
    bool IsKeyAvailable();

    // Blocking read; intercept=true suppresses echoing the character
    ConsoleKeyInfo? ReadKey(bool intercept);

    // Async read; awaitable, respects cancellation
    Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken);
}
```

Access via `IAnsiConsole.Input`.

### 6.2 Keyboard loop pattern for `MarkdownEditor`

```csharp
await AnsiConsole.Live(editorWidget)
    .AutoClear(true)
    .StartAsync(async ctx =>
    {
        using var cts = new CancellationTokenSource();

        while (true)
        {
            // Async, non-echoing read
            var keyInfo = await console.Input.ReadKeyAsync(
                intercept: true,
                cancellationToken: cts.Token);

            if (keyInfo is null) continue;

            switch (keyInfo.Value.Key)
            {
                case ConsoleKey.Escape:
                    return; // exit live loop

                case ConsoleKey.Enter:
                    editorWidget.InsertNewLine();
                    break;

                case ConsoleKey.Backspace:
                    editorWidget.DeleteBack();
                    break;

                case ConsoleKey.LeftArrow:
                    editorWidget.MoveCursor(-1);
                    break;

                default:
                    if (!char.IsControl(keyInfo.Value.KeyChar))
                        editorWidget.Insert(keyInfo.Value.KeyChar);
                    break;
            }

            // Redraw after every keystroke
            ctx.Refresh();
        }
    });
```

**Note:** `ReadKey(intercept: true)` suppresses the default terminal echo. Always use `intercept: true` in full-screen editors or you get duplicate character output.

---

## 7. Building `MarkdownEditor` — an `IPrompt<string>`

### 7.1 `IPrompt<T>` interface

```csharp
namespace Spectre.Console;

public interface IPrompt<T>
{
    // Blocking; synchronous callers use this
    T Show(IAnsiConsole console);

    // Preferred; async callers use this
    Task<T> ShowAsync(IAnsiConsole console, CancellationToken cancellationToken);
}
```

`AnsiConsole.Prompt(IPrompt<T>)` calls `Show`. The default `Show` implementation in `TextPrompt<T>` calls `ShowAsync(...).GetAwaiter().GetResult()` — the same pattern is safe for `MarkdownEditor`.

### 7.2 Skeleton

```csharp
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Mked.Controls;

/// <summary>
/// A full-screen Markdown editor prompt that returns the edited string.
/// </summary>
public sealed class MarkdownEditor : IPrompt<string>
{
    private readonly string _initialContent;

    public MarkdownEditor(string initialContent = "")
    {
        _initialContent = initialContent;
    }

    /// <inheritdoc/>
    public string Show(IAnsiConsole console)
        => ShowAsync(console, CancellationToken.None).GetAwaiter().GetResult();

    /// <inheritdoc/>
    public async Task<string> ShowAsync(IAnsiConsole console,
                                        CancellationToken cancellationToken)
    {
        var buffer  = new EditorBuffer(_initialContent);
        var widget  = new MarkdownEditorWidget(buffer);
        var result  = string.Empty;

        await AnsiConsole.Live(widget)
            .AutoClear(true)
            .StartAsync(async ctx =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var key = await console.Input.ReadKeyAsync(
                        intercept: true, cancellationToken);

                    if (key is null) continue;

                    if (IsCommit(key.Value))
                    {
                        result = buffer.GetText();
                        return;
                    }

                    buffer.HandleKey(key.Value);
                    ctx.Refresh();
                }
            });

        return result;
    }

    private static bool IsCommit(ConsoleKeyInfo key)
        => key.Key == ConsoleKey.Escape
        || (key.Key == ConsoleKey.S && key.Modifiers.HasFlag(ConsoleModifiers.Control));
}
```

### 7.3 Calling it via `AnsiConsole.Prompt`

```csharp
var editor  = new MarkdownEditor(existingContent);
string text = AnsiConsole.Prompt(editor);
// or, with injection:
string text = console.Prompt(editor);
```

---

## 8. `IAnsiConsole` — what gets injected

```csharp
public interface IAnsiConsole
{
    Profile              Profile          { get; }  // Width, Height, Capabilities
    IAnsiConsoleCursor   Cursor           { get; }  // Show/Hide, Move
    IAnsiConsoleInput    Input            { get; }  // ReadKey / ReadKeyAsync
    IExclusivityMode     ExclusivityMode  { get; }  // mutex for exclusive-input modes
    RenderPipeline       Pipeline         { get; }  // pipeline processors
    void Clear(bool home);
    void Write(IRenderable renderable);
    void WriteAnsi(Action<AnsiWriter> action);
}
```

Always inject `IAnsiConsole` rather than using the static `AnsiConsole` class. This keeps controls testable (via `TestConsole`) and decoupled from the global singleton.

---

## 9. There is no `IWidget`

Spectre.Console has no separate `IWidget` interface. The single `IRenderable` interface serves every role: simple labels, complex tables, live-update targets, and prompt rendering surfaces. The distinction between a "widget" and a "renderable" is purely conceptual.

---

## 10. AOT / Trim Safety

### 10.1 `IRenderable` implementations are safe

Implementing `IRenderable` is pure interface + struct + `yield return` — no reflection, no dynamic dispatch at the rendering level. Trim-safe by default.

### 10.2 `Style`, `Segment`, `Measurement`

All plain C# value types and sealed classes. No reflection. Safe.

### 10.3 `Spectre.Console.Cli` — the one problem area

`CommandApp` uses reflection for settings binding (`[CommandArgument]`, `[CommandOption]`). Track [spectreconsole/spectre.console#1439](https://github.com/spectreconsole/spectre.console/issues/1439) for native AOT support status. Current workaround: annotate settings classes with `[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MySettings))]`.

The `Mked.Controls` library itself (just `IRenderable` + `IPrompt<T>`) has no AOT concerns.

### 10.4 `AnsiConsole.Live` — internal reflection

`LiveDisplay` does not use reflection. It coordinates writes via a render lock. Safe.

### 10.5 `Markup` rendering

`Markup` parses a BBCode-like syntax at runtime using `[GeneratedRegex]`-style scanning internally. It is trim-safe. Do not use `Markup` with dynamically constructed tag strings that include user-supplied content — use `Markup.Escape(text)` first.

### 10.6 Checklist before `dotnet publish --aot`

- [ ] No `Activator.CreateInstance` in widget code
- [ ] No `JsonSerializer` without `[JsonSerializable]` source context
- [ ] No bare `new Regex(...)` — use `[GeneratedRegex]`
- [ ] Settings types annotated with `[DynamicDependency]` or tracked for source-gen binding
- [ ] Zero `ILLink`/trim warnings in publish output

---

## 11. Quick Reference — Type Cheat-Sheet

| Type | Namespace | Purpose |
|---|---|---|
| `IRenderable` | `Spectre.Console.Rendering` | Core widget interface |
| `Measurement` | `Spectre.Console.Rendering` | Min/max width result from `Measure()` |
| `Segment` | `Spectre.Console.Rendering` | Atomic styled text atom |
| `SegmentLine` | `Spectre.Console.Rendering` | `List<Segment>` for one terminal row |
| `RenderOptions` | `Spectre.Console.Rendering` | Terminal capabilities + size, passed to both methods |
| `Style` | `Spectre.Console` | Foreground + background + `Decoration` flags |
| `Decoration` | `Spectre.Console` | `[Flags]` enum: Bold, Italic, Underline, … |
| `Color` | `Spectre.Console` | Named colour or `new Color(r,g,b)` |
| `IPrompt<T>` | `Spectre.Console` | Interactive prompt; `Show` / `ShowAsync` |
| `IAnsiConsole` | `Spectre.Console` | Console abstraction; inject this, not static class |
| `IAnsiConsoleInput` | `Spectre.Console` | `ReadKey` / `ReadKeyAsync` / `IsKeyAvailable` |
| `LiveDisplay` | `Spectre.Console` | Entry point for live-updating regions |
| `LiveDisplayContext` | `Spectre.Console` | `Refresh()` / `UpdateTarget()` inside a live loop |

---

## 12. Source Locations

All types live in the `Spectre.Console` NuGet package. Source reference:

- `IRenderable` — `src/Spectre.Console/Rendering/IRenderable.cs`
- `Segment` — `src/Spectre.Console/Rendering/Segment.cs`
- `SegmentLine` — `src/Spectre.Console/Rendering/SegmentLine.cs`
- `Measurement` — `src/Spectre.Console/Rendering/Measurement.cs`
- `RenderOptions` — `src/Spectre.Console/Rendering/RenderOptions.cs`
- `IPrompt<T>` — `src/Spectre.Console/Prompts/IPrompt.cs`
- `IAnsiConsole` — `src/Spectre.Console/IAnsiConsole.cs`
- `IAnsiConsoleInput` — `src/Spectre.Console/IAnsiConsoleInput.cs`
- `LiveDisplay` — `src/Spectre.Console/Live/LiveDisplay.cs`
- `LiveDisplayContext` — `src/Spectre.Console/Live/LiveDisplayContext.cs`
