namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     One piece of a parsed body: either literal Markdown text or a link.
/// </summary>
/// <remarks>
///     A body is stored as a list of parts so renderers can write every link in the style the caller asks
///     for. Exactly one of <paramref name="Text"/> and <paramref name="Link"/> is not null; the parts appear in
///     body order and adjacent text is not split.
/// </remarks>
/// <param name="Text">The literal Markdown text, or <see langword="null"/> for a link part.</param>
/// <param name="Link">The link, or <see langword="null"/> for a text part.</param>
internal sealed record BodyPart(string? Text, ParsedLink? Link);
