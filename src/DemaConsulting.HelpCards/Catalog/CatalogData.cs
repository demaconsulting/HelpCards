using System.Globalization;

namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     The immutable result of catalog composition, shared by a catalog and all of its views.
/// </summary>
/// <remarks>
///     <para>
///         The data holds every card in depth-first order, the roots, the sources, the informational
///         diagnostics, the tag names, the culture, the optional tool name and version, and the optional closed tag
///         vocabulary. It is created once, by composition, and never changes, so any number of threads may read it
///         and any number of views may share it. Lookups by id ignore case.
///     </para>
///     <para>
///         The data does not know about visibility; a <see cref="CatalogScope"/> decides which cards a view shows.
///     </para>
/// </remarks>
internal sealed class CatalogData
{
    /// <summary>
    ///     The nodes by global id, ignoring case.
    /// </summary>
    private readonly Dictionary<string, CardNode> _byId;

    /// <summary>
    ///     The search index, built on the first search.
    /// </summary>
    private readonly Lazy<SearchIndex> _search;

    /// <summary>
    ///     Creates the data.
    /// </summary>
    /// <param name="culture">The culture the catalog was configured with.</param>
    /// <param name="cards">The nodes in depth-first order over the roots.</param>
    /// <param name="rootIds">The global ids of the roots in order.</param>
    /// <param name="sources">The sources in registration order.</param>
    /// <param name="diagnostics">The informational problems in display order.</param>
    /// <param name="toolName">The tool name set on the builder, or <see langword="null"/>.</param>
    /// <param name="toolVersion">The tool version set on the builder, or <see langword="null"/>.</param>
    /// <param name="allowedTags">The closed tag vocabulary, or <see langword="null"/> when tags are free-form.</param>
    public CatalogData(
        CultureInfo culture,
        IReadOnlyList<CardNode> cards,
        IReadOnlyList<string> rootIds,
        IReadOnlyList<HelpSourceInfo> sources,
        IReadOnlyList<HelpProblem> diagnostics,
        string? toolName,
        string? toolVersion,
        IReadOnlySet<string>? allowedTags)
    {
        Culture = culture;
        Cards = CardNode.Freeze(cards);
        RootIds = CardNode.Freeze(rootIds);
        Sources = CardNode.Freeze(sources);
        Diagnostics = CardNode.Freeze(diagnostics);
        ToolName = toolName;
        ToolVersion = toolVersion;
        AllowedTags = allowedTags;
        _search = new Lazy<SearchIndex>(() => SearchIndex.Build(this));
        _byId = cards.ToDictionary(node => node.Id, StringComparer.OrdinalIgnoreCase);
        TagNames = CardNode.Freeze(cards.SelectMany(node => node.Tags).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
    }

    /// <summary>
    ///     Gets the culture the catalog was configured with.
    /// </summary>
    public CultureInfo Culture { get; }

    /// <summary>
    ///     Gets every card in depth-first order over the roots.
    /// </summary>
    public IReadOnlyList<CardNode> Cards { get; }

    /// <summary>
    ///     Gets the global ids of the roots in order.
    /// </summary>
    public IReadOnlyList<string> RootIds { get; }

    /// <summary>
    ///     Gets the sources in registration order.
    /// </summary>
    public IReadOnlyList<HelpSourceInfo> Sources { get; }

    /// <summary>
    ///     Gets the informational problems that composition recorded.
    /// </summary>
    public IReadOnlyList<HelpProblem> Diagnostics { get; }

    /// <summary>
    ///     Gets the tool name set on the builder, or <see langword="null"/> when none was set.
    /// </summary>
    public string? ToolName { get; }

    /// <summary>
    ///     Gets the tool version set on the builder, or <see langword="null"/> when none was set.
    /// </summary>
    public string? ToolVersion { get; }

    /// <summary>
    ///     Gets the closed tag vocabulary, or <see langword="null"/> when tags are free-form.
    /// </summary>
    public IReadOnlySet<string>? AllowedTags { get; }

    /// <summary>
    ///     Gets the distinct tags of all cards in ordinal order.
    /// </summary>
    public IReadOnlyList<string> TagNames { get; }

    /// <summary>
    ///     Gets the search index of the cards, which is built once, on first use, and then shared by every view.
    /// </summary>
    public SearchIndex Search => _search.Value;

    /// <summary>
    ///     Finds a node by global id, ignoring case.
    /// </summary>
    /// <param name="id">The global id.</param>
    /// <returns>The node, or <see langword="null"/> when no card has the id.</returns>
    public CardNode? Find(string id)
    {
        return _byId.GetValueOrDefault(id);
    }
}
