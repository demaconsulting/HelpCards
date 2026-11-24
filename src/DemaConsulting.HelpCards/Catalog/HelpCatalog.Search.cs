namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     The search members of the catalog: ranked search and suggestions for a word that was not found.
/// </summary>
/// <remarks>
///     The work is done by the search index of the Catalog subsystem. The index belongs to the frozen data and is built
///     on the first search; a view only decides which cards the search may return, so it never changes a ranking.
/// </remarks>
public sealed partial class HelpCatalog
{
    /// <summary>
    ///     Searches the visible cards for a query and ranks them.
    /// </summary>
    /// <remarks>
    ///     The query is split into words. A card scores for each word that matches its aliases, ids, example names,
    ///     title, tags, or summary, more for a whole word than for the start of a word, and more when every word and
    ///     when the whole query match. Results are ordered by score, then by how near the card is to the top of its
    ///     source, then by id.
    /// </remarks>
    /// <param name="query">The words to search for. Must not be null; a query without a word gives no result.</param>
    /// <param name="options">The options, or <see langword="null"/> for the defaults.</param>
    /// <returns>At most <see cref="HelpSearchOptions.MaxResults"/> results, best first.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/> is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when the options name a source that does not exist or a tag outside a closed tag vocabulary, or when
    ///     <see cref="HelpSearchOptions.MaxResults"/> is less than one.
    /// </exception>
    public IReadOnlyList<HelpSearchResult> Search(string query, HelpSearchOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(query);
        options ??= new HelpSearchOptions();
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxResults, 1);

        // The restrictions are views, so the index scores only the cards that the restricted view shows
        var view = this;
        if (options.Sources is { Count: > 0 })
        {
            view = view.FilterSource(options.Sources);
        }

        if (options.Tags is { Count: > 0 })
        {
            view = view.Filter(options.Tags);
        }

        var results = Scope.Data.Search.Search(view.Scope.Cards, query);
        return CardNode.Freeze(results.Take(options.MaxResults));
    }

    /// <summary>
    ///     Suggests the names of visible cards that are close to a word that was not found.
    /// </summary>
    /// <remarks>
    ///     The candidates are the names, aliases, and ids of the visible cards. A candidate is close when its
    ///     case-insensitive Levenshtein distance from the word is at most the larger of two and a third of the length of
    ///     the word; the closest come first and ties are in ordinal order.
    /// </remarks>
    /// <param name="word">The word that was not found. Must not be null.</param>
    /// <param name="maxCount">The most suggestions to return; 3 unless given. Must be at least one.</param>
    /// <returns>The suggestions, closest first; empty when none is close.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="word"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxCount"/> is less than one.</exception>
    public IReadOnlyList<string> Suggest(string word, int maxCount = 3)
    {
        ArgumentNullException.ThrowIfNull(word);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCount, 1);
        var candidates = Scope.Cards.SelectMany(node => node.Aliases.Append(node.Name).Append(node.Id));
        return SearchIndex.Suggest(word, candidates, maxCount);
    }
}
