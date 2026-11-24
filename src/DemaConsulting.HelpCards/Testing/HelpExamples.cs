using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Testing;

/// <summary>
///     Lists the examples of a catalog together with their cards, for theory-style tests.
/// </summary>
/// <remarks>
///     Examples are data, so a product tests them itself: it enumerates them here and, for each one, runs the
///     command, compiles the source, or compares the snippet with a guide. The library never runs an example.
///     The class is stateless and safe for concurrent use.
/// </remarks>
public static class HelpExamples
{
    /// <summary>
    ///     Enumerates the examples of the visible cards in catalog order.
    /// </summary>
    /// <remarks>
    ///     The order is the depth-first order of <see cref="HelpCatalog.Cards"/>, and the examples of a card
    ///     keep the order in which the card lists them, so the sequence is the same on every run. Arguments are
    ///     checked when the method is called, not when the sequence is enumerated.
    /// </remarks>
    /// <param name="catalog">The catalog or view. Must not be null.</param>
    /// <param name="kinds">
    ///     The kinds to keep (<see cref="HelpExample.CommandKind"/>, <see cref="HelpExample.SourceKind"/>,
    ///     <see cref="HelpExample.SnippetKind"/>), or <see langword="null"/> for every kind. Compared exactly.
    /// </param>
    /// <returns>A card and one of its examples for every example that has a requested kind.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="catalog"/> is null or a kind is null.</exception>
    public static IEnumerable<(HelpCard Card, HelpExample Example)> Enumerate(HelpCatalog catalog, IEnumerable<string>? kinds = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        // The kinds are copied now so that a lazy caller sequence is read once and a null entry fails at the call
        var wanted = CopyKinds(kinds);
        return catalog.Cards
            .SelectMany(card => card.Examples.Select(example => (Card: card, Example: example)))
            .Where(pair => wanted is null || wanted.Contains(pair.Example.Kind));
    }

    /// <summary>
    ///     Copies the requested kinds into a set and rejects a null entry.
    /// </summary>
    /// <param name="kinds">The kinds, or <see langword="null"/> for every kind.</param>
    /// <returns>The set, or <see langword="null"/> when every kind is wanted.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a kind is null.</exception>
    private static HashSet<string>? CopyKinds(IEnumerable<string>? kinds)
    {
        if (kinds is null)
        {
            return null;
        }

        var wanted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var kind in kinds)
        {
            if (kind is null)
            {
                throw new ArgumentNullException(nameof(kinds), "A kind must not be null.");
            }
            wanted.Add(kind);
        }

        return wanted;
    }
}
