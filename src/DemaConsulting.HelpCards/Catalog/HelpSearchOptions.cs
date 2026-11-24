namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     The options of a search over a catalog.
/// </summary>
/// <remarks>
///     The options are immutable once created. A search shows at most <see cref="MaxResults"/> results, and may be
///     limited to cards of some sources and cards that have some tags.
/// </remarks>
public sealed class HelpSearchOptions
{
    /// <summary>
    ///     Gets the most results a search returns; 8 unless set. A search rejects a value below one.
    /// </summary>
    public int MaxResults { get; init; } = 8;

    /// <summary>
    ///     Gets the names of the sources to search, or <see langword="null"/> or empty to search all of them. A name
    ///     that is not a source of the catalog makes the search throw <see cref="ArgumentException"/>.
    /// </summary>
    public IReadOnlyList<string>? Sources { get; init; }

    /// <summary>
    ///     Gets the tags a card must have at least one of, or <see langword="null"/> or empty for no tag restriction. A
    ///     tag outside a closed tag vocabulary makes the search throw <see cref="ArgumentException"/>.
    /// </summary>
    public IReadOnlyList<string>? Tags { get; init; }
}
