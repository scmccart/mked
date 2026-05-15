# Spectre.Console.Cli

## What It Is

`Spectre.Console.Cli` is the command-line parsing component of [Spectre.Console](https://spectreconsole.net/). It provides a strongly-typed, convention-based approach to defining CLI commands, mapping arguments to settings classes, and executing command handlers.

## Why mked Uses It

mked has two top-level modes — `view` and `edit` — each with their own arguments and options. Spectre.Console.Cli is the natural choice because:

- It is already a transitive dependency of Spectre.Console.
- It generates consistent help output styled to match the rest of the console UI.
- It maps CLI arguments to strongly-typed settings classes, which is AOT-friendly with source generators.

## Core Concepts

### Commands and Settings

Each mode is a `Command<TSettings>` subclass. The settings class is a plain POCO annotated with `[CommandArgument]` and `[CommandOption]` attributes:

```csharp
public sealed class ViewSettings : CommandSettings
{
    [CommandArgument(0, "[file]")]
    public string? File { get; init; }

    [CommandOption("--follow|-f")]
    public bool Follow { get; init; }
}

public sealed class ViewCommand : Command<ViewSettings>
{
    public override int Execute(CommandContext context, ViewSettings settings) { ... }
}
```

### App Registration

Commands are registered on a `CommandApp`:

```csharp
var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<ViewCommand>("view");
    config.AddCommand<EditCommand>("edit");
});
return app.Run(args);
```

## mked Command Structure

| Command | Description |
|---|---|
| `mked view [file]` | Open file (or stdin) in viewer mode |
| `mked edit <file>` | Open file in editor mode |

### View Options

| Option | Description |
|---|---|
| `--follow` / `-f` | Tail file for changes (file input only) |
| `--stream` | Force stdin stream mode (auto-detected by default) |

### Edit Options

| Option | Description |
|---|---|
| `--split` | Start with split editor/preview layout |

## AOT Considerations

Spectre.Console.Cli uses reflection for settings binding. When publishing as NativeAOT, ensure `[DynamicDependency]` or source-generator-based binding is used. Track [spectreconsole/spectre.console#1439](https://github.com/spectreconsole/spectre.console/issues/1439) for native AOT support status.
