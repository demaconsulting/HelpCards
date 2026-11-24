namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     The set of cards a view shows, and the tree those cards form when hidden cards are lifted out.
/// </summary>
/// <remarks>
///     <para>
///         A scope is created over a <see cref="CatalogData"/> and never changes. The scope of an unfiltered
///         catalog shows every card. Restricting a scope gives a new scope that shows only the cards that were
///         visible and also pass the restriction, which is how views compose by intersection. A hidden card does
///         not hide its children: each visible descendant is lifted to its nearest visible ancestor, or to the
///         roots when it has none, keeping its order.
///     </para>
///     <para>
///         All members are safe for concurrent use because everything is computed when the scope is created.
///     </para>
/// </remarks>
internal sealed class CatalogScope
{
    /// <summary>
    ///     The ids of the visible cards.
    /// </summary>
    private readonly HashSet<string> _visible;

    /// <summary>
    ///     The visible children of each visible card, after lifting.
    /// </summary>
    private readonly Dictionary<string, List<CardNode>> _children = new(StringComparer.Ordinal);

    /// <summary>
    ///     The id of the nearest visible ancestor of each visible card; null for a lifted root.
    /// </summary>
    private readonly Dictionary<string, string?> _parents = new(StringComparer.Ordinal);

    /// <summary>
    ///     Creates a scope that shows the given cards.
    /// </summary>
    /// <param name="data">The data the scope is over.</param>
    /// <param name="visible">The ids of the visible cards.</param>
    private CatalogScope(CatalogData data, HashSet<string> visible)
    {
        Data = data;
        _visible = visible;

        var cards = new List<CardNode>();
        var roots = new List<CardNode>();
        foreach (var rootId in data.RootIds)
        {
            Walk(data.Find(rootId)!, null, cards, roots);
        }

        Cards = CardNode.Freeze(cards);
        Roots = CardNode.Freeze(roots);
    }

    /// <summary>
    ///     Gets the data the scope is over.
    /// </summary>
    public CatalogData Data { get; }

    /// <summary>
    ///     Gets the visible cards in depth-first order.
    /// </summary>
    public IReadOnlyList<CardNode> Cards { get; }

    /// <summary>
    ///     Gets the visible cards that have no visible ancestor, in order.
    /// </summary>
    public IReadOnlyList<CardNode> Roots { get; }

    /// <summary>
    ///     Gets a value that says whether every card of the data is visible.
    /// </summary>
    public bool ShowsEverything => _visible.Count == Data.Cards.Count;

    /// <summary>
    ///     Creates the scope that shows every card.
    /// </summary>
    /// <param name="data">The data. Must not be null.</param>
    /// <returns>The scope.</returns>
    public static CatalogScope All(CatalogData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new CatalogScope(data, data.Cards.Select(node => node.Id).ToHashSet(StringComparer.Ordinal));
    }

    /// <summary>
    ///     Creates a scope that shows the cards of this scope that also pass a restriction.
    /// </summary>
    /// <remarks>
    ///     The restriction is evaluated once per visible card, in depth-first order, on the calling thread,
    ///     and is not kept.
    /// </remarks>
    /// <param name="keep">Decides whether a visible card stays visible. Must not be null.</param>
    /// <returns>The new scope; this scope is unchanged.</returns>
    public CatalogScope Restrict(Func<CardNode, bool> keep)
    {
        ArgumentNullException.ThrowIfNull(keep);
        var visible = Cards.Where(keep).Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
        return new CatalogScope(Data, visible);
    }

    /// <summary>
    ///     Determines whether a card is visible.
    /// </summary>
    /// <param name="id">The global id of the card, as stored in the data.</param>
    /// <returns><see langword="true"/> when the card is visible.</returns>
    public bool IsVisible(string id)
    {
        return _visible.Contains(id);
    }

    /// <summary>
    ///     Gets the visible children of a card after lifting hidden cards.
    /// </summary>
    /// <param name="node">The card.</param>
    /// <returns>The visible children in order; empty when the card is not visible or has none.</returns>
    public IReadOnlyList<CardNode> GetChildren(CardNode node)
    {
        return _children.TryGetValue(node.Id, out var children) ? children : [];
    }

    /// <summary>
    ///     Gets the global id of the nearest visible ancestor of a visible card.
    /// </summary>
    /// <param name="node">The card.</param>
    /// <returns>The id, or <see langword="null"/> when the card is a root of the scope or not visible.</returns>
    public string? GetParentId(CardNode node)
    {
        return _parents.GetValueOrDefault(node.Id);
    }

    /// <summary>
    ///     Walks the tree of the data and records the lifted structure.
    /// </summary>
    /// <param name="node">The card to visit.</param>
    /// <param name="visibleAncestor">The nearest visible ancestor of the card, or <see langword="null"/>.</param>
    /// <param name="cards">Receives the visible cards in depth-first order.</param>
    /// <param name="roots">Receives the visible cards that have no visible ancestor.</param>
    private void Walk(CardNode node, CardNode? visibleAncestor, List<CardNode> cards, List<CardNode> roots)
    {
        var visible = _visible.Contains(node.Id);
        if (visible)
        {
            cards.Add(node);
            _parents[node.Id] = visibleAncestor?.Id;
            _children[node.Id] = [];
            if (visibleAncestor is null)
            {
                roots.Add(node);
            }
            else
            {
                _children[visibleAncestor.Id].Add(node);
            }
        }

        foreach (var childId in node.ChildIds)
        {
            Walk(Data.Find(childId)!, visible ? node : visibleAncestor, cards, roots);
        }
    }
}
