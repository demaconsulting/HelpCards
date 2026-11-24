using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Composition;

/// <summary>
///     Puts the cards that share a parent into their one deterministic order.
/// </summary>
/// <remarks>
///     <para>
///         The order is the same for the children of a card and for the roots of a source. First come the cards
///         named in the <c>children</c> list, in the order listed. All other cards follow: those with an
///         <c>order</c> value by ascending value, then those without one, and cards that tie are put in ordinal
///         order of their name. <c>order</c> compares only the siblings of one source.
///     </para>
///     <para>
///         The <c>children</c> list is checked while it is applied: a repeated entry is a
///         <c>children-duplicate</c> problem and an entry that is not a direct child card is a
///         <c>children-unknown</c> problem, unless the card it names failed to load and has already been reported.
///         The class is static and stateless and therefore safe for concurrent use.
///     </para>
/// </remarks>
internal static class SiblingOrder
{
    /// <summary>
    ///     Gets the name of a card: the last segment of its local id.
    /// </summary>
    /// <param name="card">The card.</param>
    /// <returns>The name.</returns>
    public static string NameOf(CardDefinition card)
    {
        var slash = card.LocalId.LastIndexOf('/');
        return slash < 0 ? card.LocalId : card.LocalId[(slash + 1)..];
    }

    /// <summary>
    ///     Orders the cards that share one parent.
    /// </summary>
    /// <param name="siblings">The cards, all with the same parent. Must not be null.</param>
    /// <param name="parentId">The local id of the parent, or <see langword="null"/> for the roots of a source.</param>
    /// <param name="listed">The <c>children</c> entries of the parent, or of <c>_source.md</c> for roots. Must not be null.</param>
    /// <param name="suppressedIds">The local ids of files that failed to load, whose problems are already reported. Must not be null.</param>
    /// <param name="sourceName">The name of the source, for problems.</param>
    /// <param name="problems">Receives the <c>children-duplicate</c> and <c>children-unknown</c> problems. Must not be null.</param>
    /// <returns>The cards in order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static IReadOnlyList<CardDefinition> Order(
        IReadOnlyList<CardDefinition> siblings,
        string? parentId,
        IReadOnlyList<LocatedValue> listed,
        IReadOnlySet<string> suppressedIds,
        string sourceName,
        ICollection<HelpProblem> problems)
    {
        ArgumentNullException.ThrowIfNull(siblings);
        ArgumentNullException.ThrowIfNull(listed);
        ArgumentNullException.ThrowIfNull(suppressedIds);
        ArgumentNullException.ThrowIfNull(problems);

        var byName = siblings.ToDictionary(NameOf, StringComparer.Ordinal);
        var ordered = new List<CardDefinition>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in listed)
        {
            if (!seen.Add(entry.Value))
            {
                problems.Add(HelpProblem.CreateError(
                    RuleIds.ChildrenDuplicate,
                    sourceName,
                    entry.File,
                    entry.Line,
                    $"The entry '{entry.Value}' is listed twice in 'children'; list each card once."));
            }
            else if (byName.TryGetValue(entry.Value, out var card))
            {
                ordered.Add(card);
            }
            else if (!suppressedIds.Contains(parentId is null ? entry.Value : $"{parentId}/{entry.Value}"))
            {
                problems.Add(HelpProblem.CreateError(
                    RuleIds.ChildrenUnknown,
                    sourceName,
                    entry.File,
                    entry.Line,
                    $"The entry '{entry.Value}' in 'children' is not a direct child card; "
                    + "list only cards that sit directly below this card."));
            }
        }

        // The rest by order value (cards without one last), then by name
        var rest = siblings
            .Where(card => !seen.Contains(NameOf(card)))
            .OrderBy(card => card.Order.HasValue ? 0 : 1)
            .ThenBy(card => card.Order ?? 0)
            .ThenBy(NameOf, StringComparer.Ordinal);
        ordered.AddRange(rest);
        return ordered;
    }
}
