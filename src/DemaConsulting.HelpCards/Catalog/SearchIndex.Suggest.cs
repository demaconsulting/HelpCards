namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     The suggestion part of the search index: the closest names to a word that was not found.
/// </summary>
internal sealed partial class SearchIndex
{
    /// <summary>
    ///     The distance that is always accepted, however short the word is.
    /// </summary>
    private const int MinimumAcceptedDistance = 2;

    /// <summary>
    ///     The divisor of the length of a word that gives the accepted distance of a longer word.
    /// </summary>
    private const int LengthDivisor = 3;

    /// <summary>
    ///     Finds the candidates that are closest to a word.
    /// </summary>
    /// <remarks>
    ///     The distance is the case-insensitive Levenshtein distance. A candidate is accepted when its distance is at
    ///     most the larger of two and a third of the length of the word. The result is ordered by distance and then by
    ///     the candidate in ordinal order, and every spelling appears once.
    /// </remarks>
    /// <param name="word">The word that was not found. Must not be null.</param>
    /// <param name="candidates">The names, aliases, and ids to choose from. Must not be null.</param>
    /// <param name="maxCount">The most suggestions to return. Must be at least one.</param>
    /// <returns>The suggestions, closest first; empty when none is close enough.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="word"/> or <paramref name="candidates"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxCount"/> is less than one.</exception>
    public static IReadOnlyList<string> Suggest(string word, IEnumerable<string> candidates, int maxCount)
    {
        ArgumentNullException.ThrowIfNull(word);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCount, 1);

        var lower = word.Trim().ToLowerInvariant();
        var accepted = Math.Max(MinimumAcceptedDistance, lower.Length / LengthDivisor);
        var found = new List<(int Distance, string Candidate)>();
        foreach (var candidate in candidates.Distinct(StringComparer.Ordinal))
        {
            // A candidate whose length differs by more than the accepted distance cannot be close, so skip the work
            var other = candidate.ToLowerInvariant();
            if (Math.Abs(other.Length - lower.Length) > accepted)
            {
                continue;
            }

            var distance = Distance(lower, other, accepted);
            if (distance <= accepted)
            {
                found.Add((distance, candidate));
            }
        }

        return CardNode.Freeze(found
            .OrderBy(entry => entry.Distance)
            .ThenBy(entry => entry.Candidate, StringComparer.Ordinal)
            .Take(maxCount)
            .Select(entry => entry.Candidate));
    }

    /// <summary>
    ///     Computes the Levenshtein distance between two strings, giving up early once it exceeds a limit.
    /// </summary>
    /// <param name="first">The first string.</param>
    /// <param name="second">The second string.</param>
    /// <param name="limit">The distance above which the exact value does not matter.</param>
    /// <returns>The distance, or a value above <paramref name="limit"/> when the distance exceeds it.</returns>
    private static int Distance(string first, string second, int limit)
    {
        var previous = new int[second.Length + 1];
        var current = new int[second.Length + 1];
        for (var column = 0; column <= second.Length; column++)
        {
            previous[column] = column;
        }

        for (var row = 1; row <= first.Length; row++)
        {
            current[0] = row;
            var rowMinimum = current[0];
            for (var column = 1; column <= second.Length; column++)
            {
                var cost = first[row - 1] == second[column - 1] ? 0 : 1;
                current[column] = Math.Min(Math.Min(previous[column] + 1, current[column - 1] + 1), previous[column - 1] + cost);
                rowMinimum = Math.Min(rowMinimum, current[column]);
            }

            // The smallest value of a row never decreases, so a row above the limit settles the result
            if (rowMinimum > limit)
            {
                return limit + 1;
            }

            (previous, current) = (current, previous);
        }

        return previous[second.Length];
    }
}
