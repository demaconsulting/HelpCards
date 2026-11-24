namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     One hit of a search over a catalog.
/// </summary>
/// <remarks>
///     In this release the type is data only: the renderers format a list of results that a caller supplies, so
///     a search result can be rendered and tested without a search engine. The record is immutable and compares
///     by value.
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
