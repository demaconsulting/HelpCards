namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     A link of a card body after composition resolved it against the final tree.
/// </summary>
/// <remarks>
///     Resolution turns the local ids the author wrote into global ids and finds the card that owns a linked
///     example. Links to a source that is not present never reach this type; they become plain text parts.
///     A view later decides whether the targets are visible. The record is immutable.
/// </remarks>
/// <param name="CardId">The global id of the card the link names, or <see langword="null"/> for <c>[[#example]]</c>.</param>
/// <param name="ExampleName">The example name, or <see langword="null"/> for a card link.</param>
/// <param name="ExampleCardId">The global id of the card that owns the example, or <see langword="null"/>.</param>
/// <param name="SourceName">The declared source name of an external link, or <see langword="null"/>.</param>
/// <param name="Text">The display text of an external link, or <see langword="null"/>.</param>
internal sealed record ResolvedLink(
    string? CardId,
    string? ExampleName,
    string? ExampleCardId,
    string? SourceName,
    string? Text);
