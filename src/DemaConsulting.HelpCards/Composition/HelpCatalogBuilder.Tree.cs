using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Composition;

/// <summary>
///     Placing cards in the tree and checking the tree, for the catalog builder (pipeline steps 4 and 5).
/// </summary>
public sealed partial class HelpCatalogBuilder
{
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
    ///     Gives every card of a source its place in the tree and orders the siblings (pipeline steps 4 and 5).
    /// </summary>
    /// <remarks>
    ///     A card whose parent is not a card of its source is reported once; the cards below it are left out
    ///     without further reports. The report is <c>tree-foreign</c> when the parent id is a card of another
    ///     source, and <c>parent-missing</c> otherwise. A parent that failed to load, or whose global id was taken
    ///     by another source, is not reported again.
    /// </remarks>
    /// <param name="entry">The source, with its cards read.</param>
    /// <param name="owners">The source that owns each claimed global id.</param>
    /// <param name="problems">Receives the problems found.</param>
    /// <returns>The tree.</returns>
    private static SourceTree PlaceCards(
        SourceEntry entry,
        IReadOnlyDictionary<string, SourceEntry> owners,
        ICollection<HelpProblem> problems)
    {
        var definition = entry.Definition;
        var suppressed = new HashSet<string>(definition.FailedIds, StringComparer.Ordinal);
        suppressed.UnionWith(entry.Dropped);
        var placed = new Dictionary<string, CardDefinition>(StringComparer.Ordinal);
        var groups = new Dictionary<string, List<CardDefinition>> { [string.Empty] = [] };

        // Ordinal order puts every parent before its children
        foreach (var card in definition.Cards.OrderBy(card => card.LocalId, StringComparer.Ordinal))
        {
            if (entry.Dropped.Contains(card.LocalId))
            {
                continue;
            }

            var parentId = ParentOf(card.LocalId);
            if (parentId is not null && !placed.ContainsKey(parentId))
            {
                if (!suppressed.Contains(parentId))
                {
                    problems.Add(ReportMissingParent(entry, card, parentId, owners));
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
    ///     Creates the problem for a card whose parent is not a card of its source.
    /// </summary>
    /// <param name="entry">The source of the card.</param>
    /// <param name="card">The card.</param>
    /// <param name="parentId">The local id of the missing parent.</param>
    /// <param name="owners">The source that owns each claimed global id.</param>
    /// <returns>A <c>tree-foreign</c> problem when another source owns the parent id; otherwise <c>parent-missing</c>.</returns>
    private static HelpProblem ReportMissingParent(
        SourceEntry entry,
        CardDefinition card,
        string parentId,
        IReadOnlyDictionary<string, SourceEntry> owners)
    {
        var parentGlobalId = Mounter.GlobalId(entry, parentId);
        if (owners.TryGetValue(parentGlobalId, out var owner) && !ReferenceEquals(owner, entry))
        {
            return HelpProblem.CreateError(
                RuleIds.TreeForeign,
                entry.Name,
                card.File,
                0,
                $"The card '{card.LocalId}' sits under '{parentGlobalId}', which belongs to the source '{owner.Name}'; "
                + "a card cannot sit under a card of another source. Extend the card with an overlay instead.");
        }

        return HelpProblem.CreateError(
            RuleIds.ParentMissing,
            entry.Name,
            card.File,
            0,
            $"The card '{card.LocalId}' needs a parent card '{parentId}' ('{parentId}.md'), which does not exist.");
    }
    /// <summary>
    ///     Checks the tags of every card against the tag vocabulary (pipeline step 5).
    /// </summary>
    /// <param name="entry">The source whose placed cards are checked.</param>
    /// <param name="problems">Receives <c>tag-unknown</c> and <c>tag-required</c> problems.</param>
    private void CheckTags(SourceEntry entry, ICollection<HelpProblem> problems)
    {
        if (_allowedTags is null)
        {
            return;
        }

        var definition = entry.Definition;
        var tree = entry.Tree;

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
