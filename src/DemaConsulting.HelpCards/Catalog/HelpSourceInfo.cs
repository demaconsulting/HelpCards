namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     The description of one source that contributed to a catalog.
/// </summary>
/// <remarks>
///     The values come from the source's <c>_source.md</c> (with defaults applied) and from how the source was
///     registered. The record is immutable and compares by value.
/// </remarks>
/// <param name="Name">The declared name of the source.</param>
/// <param name="Mount">The mount id of the source, or <see langword="null"/> when the source is flat.</param>
/// <param name="Title">The title of the source; its name when none is declared.</param>
/// <param name="Summary">The summary of the source; <c>Help cards of {name}.</c> when none is declared.</param>
/// <param name="Language">The culture of the neutral files of the source, <c>en</c> by default.</param>
/// <param name="Schema">The card format major version the source declares.</param>
public sealed record HelpSourceInfo(string Name, string? Mount, string Title, string Summary, string Language, int Schema);
