namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     One piece of a resolved card body: literal text or a resolved link.
/// </summary>
/// <remarks>
///     Exactly one of the two values is not null. The pieces appear in body order.
/// </remarks>
/// <param name="Text">The literal Markdown text, or <see langword="null"/> for a link piece.</param>
/// <param name="Link">The resolved link, or <see langword="null"/> for a text piece.</param>
internal sealed record ResolvedPart(string? Text, ResolvedLink? Link);
