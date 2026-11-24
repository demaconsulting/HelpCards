namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     One piece of a card body as a view shows it: literal text or a link to a visible target.
/// </summary>
/// <remarks>
///     Pieces apply the hiding rules of a view to the resolved body of a card: a link whose target is hidden, or
///     whose source is not present, has already been replaced by plain text. Exactly one of the two values is not
///     null and adjacent text is merged. Renderers write each link in the style the caller asks for.
/// </remarks>
/// <param name="Text">The literal Markdown text, or <see langword="null"/> for a link piece.</param>
/// <param name="Link">The link to a visible target, or <see langword="null"/> for a text piece.</param>
internal sealed record BodyPiece(string? Text, HelpLink? Link);
