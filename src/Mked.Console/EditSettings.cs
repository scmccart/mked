using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Mked.Console;

/// <summary>CLI settings for the <c>edit</c> command.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class EditSettings : CommandSettings
{
    /// <summary>Path to the Markdown file to edit. Omit to start with a new empty document.</summary>
    [CommandArgument(0, "[path]")]
    [Description("Path to the Markdown file to edit. Omit to start with a new empty document.")]
    public string? Path { get; init; }

    /// <summary>Show a live preview pane alongside the editor.</summary>
    [CommandOption("--split")]
    [Description("Show a live preview pane alongside the editor.")]
    public bool Split { get; init; }
}
