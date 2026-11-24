using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Composition;

/// <summary>
///     The cards of one source placed in their tree, by local id.
/// </summary>
/// <remarks>
///     The tree uses local ids because a source is read and checked on its own; the global ids that a mount
///     adds are applied when the nodes of the catalog are created. The record is immutable.
/// </remarks>
/// <param name="Cards">The cards that have a place in the tree, by local id.</param>
/// <param name="Children">The ordered children of each parent by local id; the empty string holds the roots.</param>
/// <param name="Suppressed">The ids of files that failed to load or have no place in the tree.</param>
internal sealed record SourceTree(
    IReadOnlyDictionary<string, CardDefinition> Cards,
    IReadOnlyDictionary<string, IReadOnlyList<CardDefinition>> Children,
    IReadOnlySet<string> Suppressed)
{
    /// <summary>
    ///     An empty tree, for a source that contributes no cards.
    /// </summary>
    public static SourceTree Empty { get; } = new(
        new Dictionary<string, CardDefinition>(),
        new Dictionary<string, IReadOnlyList<CardDefinition>> { [string.Empty] = [] },
        new HashSet<string>());

    /// <summary>
    ///     Gets the roots in order.
    /// </summary>
    public IReadOnlyList<CardDefinition> Roots => Children[string.Empty];
}
