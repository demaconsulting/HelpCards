namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     Finds examples by name in a card, in the subtree of a card, or in a whole catalog.
/// </summary>
/// <remarks>
///     <para>
///         Searches are depth first: a card's own examples come before those of its children, and children are
///         visited in sibling order. Example names are compared ignoring case. Composition uses the same walk
///         while it resolves links, with the children of the final tree; callers use it through a catalog, with
///         the children of a view, so hidden cards never contribute examples.
///     </para>
///     <para>
///         The class is static and stateless and therefore safe for concurrent use.
///     </para>
/// </remarks>
internal static class ExampleResolver
{
    /// <summary>
    ///     Finds the first example of a name in a card and its subtree.
    /// </summary>
    /// <param name="start">The card to start at.</param>
    /// <param name="name">The example name.</param>
    /// <param name="children">Gives the children of a card in order.</param>
    /// <returns>The example with its owner, or <see langword="null"/> when none matches.</returns>
    public static HelpExampleReference? FindInSubtree(
        CardNode start,
        string name,
        Func<CardNode, IReadOnlyList<CardNode>> children)
    {
        return Enumerate(start, children)
            .SelectMany(node => node.Examples.Where(example => IsNamed(example, name)).Select(example => new HelpExampleReference(node.Id, example)))
            .FirstOrDefault();
    }

    /// <summary>
    ///     Finds every example of a name in a catalog view, optionally limited to one card's subtree.
    /// </summary>
    /// <remarks>
    ///     The name may be written <c>card#name</c> to limit the search to the subtree of a card given as a
    ///     global id or a path. When a scope card is also given, the matches must lie in both subtrees. A card
    ///     that does not exist or is hidden yields no matches.
    /// </remarks>
    /// <param name="scope">The scope that decides what is visible.</param>
    /// <param name="name">The example name, or <c>card#name</c>.</param>
    /// <param name="scopeCardId">The card to limit the search to, or <see langword="null"/>.</param>
    /// <returns>Every match in depth-first order.</returns>
    public static IReadOnlyList<HelpExampleReference> FindAll(CatalogScope scope, string name, string? scopeCardId)
    {
        var exampleName = name;
        var limits = new List<CardNode>();
        var hash = name.IndexOf('#', StringComparison.Ordinal);
        if (hash >= 0)
        {
            exampleName = name[(hash + 1)..];
            var owner = PathResolver.Find(scope, name[..hash]);
            if (owner is null)
            {
                return [];
            }

            limits.Add(owner);
        }

        if (!string.IsNullOrWhiteSpace(scopeCardId))
        {
            var limit = PathResolver.Find(scope, scopeCardId);
            if (limit is null)
            {
                return [];
            }

            limits.Add(limit);
        }

        var candidates = limits.Count == 0
            ? scope.Cards
            : Enumerate(limits[0], scope.GetChildren).Where(node => limits.All(limit => IsInSubtree(scope, node, limit))).ToList();
        return candidates
            .SelectMany(node => node.Examples.Where(example => IsNamed(example, exampleName)).Select(example => new HelpExampleReference(node.Id, example)))
            .ToList();
    }

    /// <summary>
    ///     Lists a card and every card below it in depth-first order.
    /// </summary>
    /// <param name="start">The card to start at.</param>
    /// <param name="children">Gives the children of a card in order.</param>
    /// <returns>The cards.</returns>
    private static IEnumerable<CardNode> Enumerate(CardNode start, Func<CardNode, IReadOnlyList<CardNode>> children)
    {
        yield return start;
        foreach (var child in children(start))
        {
            foreach (var node in Enumerate(child, children))
            {
                yield return node;
            }
        }
    }

    /// <summary>
    ///     Determines whether a card is a limit card or lies below it in a view.
    /// </summary>
    /// <param name="scope">The scope that decides the tree.</param>
    /// <param name="node">The card to test.</param>
    /// <param name="limit">The limit card.</param>
    /// <returns><see langword="true"/> when the card is the limit or below it.</returns>
    private static bool IsInSubtree(CatalogScope scope, CardNode node, CardNode limit)
    {
        for (string? id = node.Id; id is not null; id = scope.GetParentId(scope.Data.Find(id)!))
        {
            if (string.Equals(id, limit.Id, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Determines whether an example has a name, ignoring case.
    /// </summary>
    /// <param name="example">The example.</param>
    /// <param name="name">The name.</param>
    /// <returns><see langword="true"/> when the names match.</returns>
    private static bool IsNamed(HelpExample example, string name)
    {
        return string.Equals(example.Name, name, StringComparison.OrdinalIgnoreCase);
    }
}
