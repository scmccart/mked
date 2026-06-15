using System.Diagnostics.CodeAnalysis;

namespace Mked.Console;

/// <summary>CLI settings for the <c>view</c> command.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class ViewSettings : CommandSettings
{
    /// <summary>Path to the Markdown file to view. Required unless <see cref="Stream"/> is set.</summary>
    [CommandArgument(0, "[path]")]
    public string? Path { get; init; }

    /// <summary>Read Markdown from stdin and update the viewer as data arrives.</summary>
    [CommandOption("-s|--stream")]
    public bool Stream { get; init; }

    /// <summary>Re-read and redisplay the file each time it changes on disk.</summary>
    [CommandOption("-f|--follow")]
    public bool Follow { get; init; }

    /// <summary>Display the YAML front matter block above the document body.</summary>
    [CommandOption("--show-frontmatter")]
    public bool ShowFrontmatter { get; init; }

    /// <summary>Write plain text to stdout with no pager or ANSI codes. Auto-enabled when stdout is redirected.</summary>
    [CommandOption("-p|--plain")]
    public bool Plain { get; init; }
}
