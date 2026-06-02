namespace Mked.Application;

/// <summary>Display options passed to every renderer invocation.</summary>
public sealed record RenderContext(bool ShowFrontmatter, bool PlainLinks);
