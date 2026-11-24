namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     One link found in a card body.
/// </summary>
/// <remarks>
///     A link is syntax only: it names a card, an example, or both, in the form the author wrote it. It is
///     not checked against any catalog here; that happens in a final pass over the whole tree because
///     forward references are valid. Card ids are local ids (not global ids) and the source name is present
///     only for external links.
/// </remarks>
/// <param name="SourceName">The declared source name of an external link, or <see langword="null"/> for a local link.</param>
/// <param name="CardId">The local card id, or <see langword="null"/> for <c>[[#example]]</c>.</param>
/// <param name="ExampleName">The example name, or <see langword="null"/> for a card link.</param>
/// <param name="Text">The display text of an external link, or <see langword="null"/> for a local link.</param>
/// <param name="Line">The one-based line of the file the link starts on.</param>
internal sealed record ParsedLink(string? SourceName, string? CardId, string? ExampleName, string? Text, int Line);
