namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     The outcome of walking a path through the tree, before cards are wrapped for callers.
/// </summary>
/// <remarks>
///     A successful walk has a <paramref name="Card"/> and nothing else. A failed walk names the parent that was
///     searched, the segment that matched nothing, and the visible names at that level. The record is immutable.
/// </remarks>
/// <param name="Card">The card, or <see langword="null"/> when the path did not resolve to a visible card.</param>
/// <param name="ParentId">The global id of the card whose children were searched, or <see langword="null"/>.</param>
/// <param name="UnknownSegment">The segment that matched nothing, or <see langword="null"/>.</param>
/// <param name="ValidNames">The names of the visible cards at the level that failed.</param>
internal sealed record PathMatch(
    CardNode? Card,
    string? ParentId,
    string? UnknownSegment,
    IReadOnlyList<string> ValidNames);
