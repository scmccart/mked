namespace Mked.Application;

/// <summary>Return payload of <see cref="StreamInputUseCase"/>.</summary>
/// <param name="Source">The accumulated Markdown source text at the time of this snapshot.</param>
/// <param name="Parsed">The parsed document corresponding to <paramref name="Source"/>.</param>
public sealed record StreamedDocument(string Source, MarkdownDocument Parsed);
