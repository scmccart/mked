namespace Mked.Application;

/// <summary>
/// Payload returned by <see cref="OpenFileUseCase"/>. Carries both the raw Markdown source
/// and the parsed document so callers can take either the viewer or the editor path.
/// </summary>
public sealed record OpenedFile(string Source, MarkdownDocument Parsed);
