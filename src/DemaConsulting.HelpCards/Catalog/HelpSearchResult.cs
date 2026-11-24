namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     One hit of a search over a catalog.
/// </summary>
/// <remarks>
///     One hit of a search over a catalog. The record is immutable and compares by value. The ranking and the
///     matched field names are defined by <see cref="HelpCatalog.Search"/>; a caller may also build results itself
///     to have them rendered.
/// </remarks>
/// <param name="Id">The global id of the card that matched.</param>
/// <param name="Title">The title of the card.</param>
/// <param name="Summary">The summary of the card.</param>
/// <param name="SourceName">The name of the source of the card.</param>
/// <param name="Score">The relevance score; higher is better.</param>
/// <param name="MatchedOn">The names of the fields that matched, such as <c>id</c> or <c>title</c>.</param>
public sealed record HelpSearchResult(
    string Id,
    string Title,
    string Summary,
    string SourceName,
    int Score,
    IReadOnlyList<string> MatchedOn);
