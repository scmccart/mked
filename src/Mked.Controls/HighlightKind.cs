namespace Mked.Controls;

/// <summary>Identifies the syntactic role of a highlighted region in a Markdown document.</summary>
internal enum HighlightKind
{
    /// <summary>ATX heading marker and text (<c># Heading</c>).</summary>
    Heading,

    /// <summary>Bold emphasis (<c>**bold**</c>).</summary>
    Bold,

    /// <summary>Italic emphasis (<c>*italic*</c>).</summary>
    Italic,

    /// <summary>Inline code span (<c>`code`</c>).</summary>
    InlineCode,

    /// <summary>The display text portion of a link (<c>[text]</c>).</summary>
    LinkText,

    /// <summary>The URL portion of a link (<c>(url)</c>).</summary>
    LinkUrl,

    /// <summary>YAML front-matter block.</summary>
    FrontmatterBlock,

    /// <summary>Fenced code block.</summary>
    CodeFence,
}
