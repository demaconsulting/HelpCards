namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     The decoded parts of a card file: the front-matter entries and the Markdown body.
/// </summary>
/// <remarks>
///     The body is normalized to LF line endings and has its trailing line feeds removed; one blank line
///     directly after the closing delimiter is not part of it.
/// </remarks>
/// <param name="Entries">The front-matter entries in file order.</param>
/// <param name="Body">The Markdown body.</param>
/// <param name="BodyLine">The one-based line of the file the body starts on.</param>
internal sealed record FrontMatterDocument(IReadOnlyList<FrontMatterEntry> Entries, string Body, int BodyLine);
