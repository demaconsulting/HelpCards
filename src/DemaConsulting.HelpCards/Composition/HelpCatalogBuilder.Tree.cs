using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Composition;

/// <summary>
///     Placing cards in the tree and checking the tree, for the catalog builder (pipeline steps 4 and 5).
/// </summary>
public sealed partial class HelpCatalogBuilder
{
    /// <summary>
    ///     The cards of one source placed in their tree.
    /// </summary>
    /// <param name="Cards">The cards that have a place in the tree, by local id.</param>
    /// <param name="Children">The ordered children of each parent by local id; the empty string holds the roots.</param>
    /// <param name="Suppressed">The ids of files that failed to load or have no place in the tree.</param>
    private sealed record SourceTree(
        IReadOnlyDictionary<string, CardDefinition> Cards,
        IReadOnlyDictionary<string, IReadOnlyList<CardDefinition>> Children,
        IReadOnlySet<string> Suppressed)
    {
        /// <summary>
        ///     Gets the roots in order.
        /// </summary>
        public IReadOnlyList<CardDefinition> Roots => Children[string.Empty];
    }

    /// <summary>
    ///     Gets the local id of the parent of a card.
    /// </summary>
    /// <param name="localId">The local id.</param>
    /// <returns>The local id of the parent, or <see langword="null"/> for a root.</returns>
    private static string? ParentOf(string localId)
    {
        var slash = localId.LastIndexOf('/');
        return slash < 0 ? null : localId[..slash];
    }

    /// <summary>
    ///     Gives every card its place in the tree and orders the siblings (pipeline steps 4 and 5).
    /// </summary>
    /// <remarks>
    ///     A card whose parent is not a card is reported once as <c>parent-missing</c>; the cards below it are
    ///     left out without further reports. A parent that failed to load is not reported again.
    /// </remarks>
    /// <param name="definition">The definition of the source.</param>
    /// <param name="problems">Receives the problems found.</param>
    /// <returns>The tree.</returns>
    private static SourceTree PlaceCards(SourceDefinition definition, ICollection<HelpProblem> problems)
    {
        var suppressed = new HashSet<string>(definition.FailedIds, StringComparer.Ordinal);
        var placed = new Dictionary<string, CardDefinition>(StringComparer.Ordinal);
        var groups = new Dictionary<string, List<CardDefinition>> { [string.Empty] = [] };

        // Ordinal order puts every parent before its children
        foreach (var card in definition.Cards.OrderBy(card => card.LocalId, StringComparer.Ordinal))
        {
            var parentId = ParentOf(card.LocalId);
            if (parentId is not null && !placed.ContainsKey(parentId))
            {
                if (!suppressed.Contains(parentId))
                {
                    problems.Add(HelpProblem.CreateError(
                        RuleIds.ParentMissing,
                        definition.Name,
                        card.File,
                        0,
                        $"The card '{card.LocalId}' needs a parent card '{parentId}' ('{parentId}.md'), which does not exist."));
                }

                suppressed.Add(card.LocalId);
                continue;
            }

            placed[card.LocalId] = card;
            var key = parentId ?? string.Empty;
            if (!groups.TryGetValue(key, out var siblings))
            {
                siblings = [];
                groups[key] = siblings;
            }

            siblings.Add(card);
        }

        var children = new Dictionary<string, IReadOnlyList<CardDefinition>>(StringComparer.Ordinal);
        children[string.Empty] = SiblingOrder.Order(groups[string.Empty], null, definition.Children, suppressed, definition.Name, problems);
        foreach (var card in placed.Values.OrderBy(card => card.LocalId, StringComparer.Ordinal))
        {
            children[card.LocalId] = SiblingOrder.Order(
                groups.GetValueOrDefault(card.LocalId) ?? [],
                card.LocalId,
                card.Children,
                suppressed,
                definition.Name,
                problems);
        }

        return new SourceTree(placed, children, suppressed);
    }

    /// <summary>
    ///     Checks the tags of every card against the tag vocabulary (pipeline step 5).
    /// </summary>
    /// <param name="definition">The definition of the source.</param>
    /// <param name="tree">The placed cards.</param>
    /// <param name="problems">Receives <c>tag-unknown</c> and <c>tag-required</c> problems.</param>
    private void CheckTags(SourceDefinition definition, SourceTree tree, ICollection<HelpProblem> problems)
    {
        if (_allowedTags is null)
        {
            return;
        }

        foreach (var card in tree.Cards.Values.OrderBy(card => card.LocalId, StringComparer.Ordinal))
        {
            foreach (var tag in card.Tags.Where(tag => !_allowedTags.Contains(tag.Value)))
            {
                problems.Add(HelpProblem.CreateError(
                    RuleIds.TagUnknown,
                    definition.Name,
                    tag.File,
                    tag.Line,
                    $"The tag '{tag.Value}' is not in the tag vocabulary; allowed tags are: "
                    + string.Join(", ", _allowedTags.Order(StringComparer.Ordinal)) + "."));
            }

            if (_requiredTags.Count > 0 && !card.Tags.Any(tag => _requiredTags.Contains(tag.Value)))
            {
                problems.Add(HelpProblem.CreateError(
                    RuleIds.TagRequired,
                    definition.Name,
                    card.File,
                    0,
                    $"The card '{card.LocalId}' needs at least one of these tags: "
                    + string.Join(", ", _requiredTags.Order(StringComparer.Ordinal)) + "."));
            }
        }
    }
}
