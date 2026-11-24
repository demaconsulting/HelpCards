using System.Globalization;
using DemaConsulting.HelpCards.Composition;

namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     An immutable, thread-safe catalog of help cards, with lookups and filtered views.
/// </summary>
/// <remarks>
///     <para>
///         A catalog is created once with <see cref="Create"/>, which reads the configured sources, validates
///         them, and freezes the result. After that it never reads a file again and never changes. A view
///         (<see cref="Filter"/>, <see cref="FilterSource"/>, <see cref="Where"/>, <see cref="Without"/>) is
///         another catalog that shares the same frozen data and shows fewer cards; views of views show the
///         intersection of their filters. A view cannot add sources.
///     </para>
///     <para>
///         Every public member is safe for concurrent use. A caller-supplied predicate runs on the calling
///         thread and is not kept. Defective sources make <see cref="Create"/> throw a
///         <see cref="HelpCatalogException"/> that lists every problem; bad arguments throw
///         <see cref="ArgumentException"/> or <see cref="ArgumentNullException"/>.
///     </para>
/// </remarks>
public sealed partial class HelpCatalog
{
    /// <summary>
    ///     The cards of this view by global id.
    /// </summary>
    private readonly Dictionary<string, HelpCard> _byId;

    /// <summary>
    ///     Creates a catalog over a scope.
    /// </summary>
    /// <param name="scope">The cards the catalog shows.</param>
    private HelpCatalog(CatalogScope scope)
    {
        Scope = scope;
        var cards = scope.Cards.Select(node => new HelpCard(node, scope)).ToList();
        _byId = cards.ToDictionary(card => card.Id, StringComparer.Ordinal);
        Cards = CardNode.Freeze(cards);
        Roots = CardNode.Freeze(scope.Roots.Select(node => _byId[node.Id]));
        var data = scope.Data;
        Sources = scope.ShowsEverything
            ? data.Sources
            : CardNode.Freeze(data.Sources.Where(source => cards.Any(card => card.SourceName == source.Name)));
        TagNames = scope.ShowsEverything
            ? data.TagNames
            : CardNode.Freeze(cards.SelectMany(card => card.Tags).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
    }

    /// <summary>
    ///     Gets the culture the catalog was configured with; the invariant culture unless one was set.
    /// </summary>
    public CultureInfo Culture => Scope.Data.Culture;

    /// <summary>
    ///     Gets every visible card in depth-first order over <see cref="Roots"/>.
    /// </summary>
    public IReadOnlyList<HelpCard> Cards { get; }

    /// <summary>
    ///     Gets the visible cards that have no visible ancestor, in order.
    /// </summary>
    public IReadOnlyList<HelpCard> Roots { get; }

    /// <summary>
    ///     Gets the sources that contributed visible cards, in registration order.
    /// </summary>
    public IReadOnlyList<HelpSourceInfo> Sources { get; }

    /// <summary>
    ///     Gets the informational problems recorded while the catalog was created; never errors or warnings.
    /// </summary>
    public IReadOnlyList<HelpProblem> Diagnostics => Scope.Data.Diagnostics;

    /// <summary>
    ///     Gets the distinct tags of the visible cards in ordinal order.
    /// </summary>
    public IReadOnlyList<string> TagNames { get; }

    /// <summary>
    ///     Gets the scope that decides which cards this catalog shows.
    /// </summary>
    internal CatalogScope Scope { get; }

    /// <summary>
    ///     Creates a catalog from the sources and settings the callback configures.
    /// </summary>
    /// <remarks>
    ///     The callback receives a builder, which is used only during this call. Creation reads every file of
    ///     every source, collects all problems, and fails after the last check when any error exists.
    /// </remarks>
    /// <param name="configure">Configures the builder. Must not be null.</param>
    /// <returns>The immutable catalog.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the builder was configured with an invalid argument.</exception>
    /// <exception cref="HelpCatalogException">Thrown when a source or the composition has errors.</exception>
    public static HelpCatalog Create(Action<HelpCatalogBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new HelpCatalogBuilder();
        configure(builder);
        var result = builder.Compose();
        if (result.Data is null)
        {
            throw new HelpCatalogException(result.Errors);
        }

        return new HelpCatalog(CatalogScope.All(result.Data));
    }

    /// <summary>
    ///     Creates a catalog that shows every card of already composed data.
    /// </summary>
    /// <remarks>
    ///     The validator composes data itself so that it can report problems without an exception, and uses this
    ///     method to inspect the cards of the result with the same model that adopters see.
    /// </remarks>
    /// <param name="data">The frozen data.</param>
    /// <returns>The catalog.</returns>
    internal static HelpCatalog FromData(CatalogData data)
    {
        return new HelpCatalog(CatalogScope.All(data));
    }

    /// <summary>
    ///     Finds a visible card by path.
    /// </summary>
    /// <remarks>
    ///     The path is split on <c>/</c> and white space. The first segment is the name or an alias of a root and
    ///     each later segment the name or an alias of a child; case is ignored. A hidden card is not found, but a
    ///     hidden parent does not stop the walk to a visible child.
    /// </remarks>
    /// <param name="path">The path, such as <c>requirements/tags</c> or <c>requirements tags</c>. Must not be null.</param>
    /// <returns>The card, or <see langword="null"/> when the path names no visible card.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    public HelpCard? Find(string path)
    {
        var node = PathResolver.Find(Scope, path);
        return node is null ? null : _byId[node.Id];
    }

    /// <summary>
    ///     Resolves a path and reports where and why it failed.
    /// </summary>
    /// <param name="path">The path, as for <see cref="Find"/>. Must not be null.</param>
    /// <returns>The card when the path resolved; otherwise the failing parent, segment, and valid names.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    public HelpPathResolution Resolve(string path)
    {
        var match = PathResolver.Resolve(Scope, path);
        var card = match.Card is null ? null : _byId[match.Card.Id];
        return new HelpPathResolution(card, match.ParentId, match.UnknownSegment, CardNode.Freeze(match.ValidNames), null);
    }

    /// <summary>
    ///     Gets the visible children of a card in sibling order.
    /// </summary>
    /// <remarks>
    ///     The card is matched by id, so a card from another view of the same catalog works too. In a filtered
    ///     view hidden children are replaced by their nearest visible descendants.
    /// </remarks>
    /// <param name="card">The card. Must not be null.</param>
    /// <returns>The children; empty when the card is not visible in this catalog or has none.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="card"/> is null.</exception>
    public IReadOnlyList<HelpCard> GetChildren(HelpCard card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return _byId.TryGetValue(card.Id, out var own)
            ? CardNode.Freeze(own.ChildIds.Select(id => _byId[id]))
            : [];
    }

    /// <summary>
    ///     Gets the visible see-also cards of a card in the order they were written.
    /// </summary>
    /// <param name="card">The card. Must not be null.</param>
    /// <returns>The related cards; empty when the card is not visible in this catalog or has none.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="card"/> is null.</exception>
    public IReadOnlyList<HelpCard> GetSeeAlso(HelpCard card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return _byId.TryGetValue(card.Id, out var own)
            ? CardNode.Freeze(own.SeeAlsoIds.Select(id => _byId[id]))
            : [];
    }

    /// <summary>
    ///     Finds every visible example with a name.
    /// </summary>
    /// <remarks>
    ///     The name may be written <c>card#name</c>, with the card as a global id or a path, to search only that
    ///     card and the cards below it. <paramref name="scopeCardId"/> limits the search to a card and the cards
    ///     below it in the same way. Names are compared ignoring case. An unknown or hidden card gives no result.
    /// </remarks>
    /// <param name="name">The example name, or <c>card#name</c>. Must not be null or white space.</param>
    /// <param name="scopeCardId">The card whose subtree is searched, or <see langword="null"/> for all cards.</param>
    /// <returns>Every match in depth-first order, each with the id of its card.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or white space.</exception>
    public IReadOnlyList<HelpExampleReference> FindExamples(string name, string? scopeCardId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return CardNode.Freeze(ExampleResolver.FindAll(Scope, name, scopeCardId));
    }

    /// <summary>
    ///     Finds the first example with a name in a card and the cards below it.
    /// </summary>
    /// <remarks>
    ///     The search is depth first: the card's own examples, then those of its children in order.
    /// </remarks>
    /// <param name="card">The card to start at. Must not be null.</param>
    /// <param name="name">The example name. Must not be null or white space.</param>
    /// <returns>The example with its card, or <see langword="null"/> when there is none.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="card"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or white space.</exception>
    public HelpExampleReference? FindExample(HelpCard card, string name)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var node = Scope.Data.Find(card.Id);
        return node is null || !Scope.IsVisible(node.Id)
            ? null
            : ExampleResolver.FindInSubtree(node, name, Scope.GetChildren);
    }

    /// <summary>
    ///     Gets a visible card by global id.
    /// </summary>
    /// <param name="id">The global id.</param>
    /// <returns>The card, or <see langword="null"/> when no visible card has the id.</returns>
    internal HelpCard? GetCardById(string id)
    {
        return _byId.GetValueOrDefault(id);
    }
}
