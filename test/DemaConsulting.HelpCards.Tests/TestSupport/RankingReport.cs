using System.Globalization;
using System.Text;
using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Tests.TestSupport;

/// <summary>
///     Writes the ranking of a set of queries as text, so that a golden file can pin the scoring of the search.
/// </summary>
/// <remarks>
///     One block is written per query: the query on a line, then one line per result with the id, the score, and the
///     fields that matched. Scores are written with the invariant culture, so the text is the same on every machine.
/// </remarks>
internal static class RankingReport
{
    /// <summary>
    ///     The queries that the golden file pins; they cover ids, aliases, titles, tags, summaries, prefixes, plurals,
    ///     several words, a path, and a query without a match.
    /// </summary>
    public static readonly string[] Queries =
    [
        "build",
        "tests",
        "program package",
        "ship",
        "build clean",
        "build/clean",
        "rel",
        "unit test",
        "make the program",
        "zebra",
    ];

    /// <summary>
    ///     Describes the ranking of every query of <see cref="Queries"/> over a catalog.
    /// </summary>
    /// <param name="catalog">The catalog to search.</param>
    /// <returns>The report, with LF line endings and no trailing line feed.</returns>
    public static string Describe(HelpCatalog catalog)
    {
        var builder = new StringBuilder();
        foreach (var query in Queries)
        {
            builder.Append("? ").Append(query).Append('\n');
            foreach (var result in catalog.Search(query, new HelpSearchOptions { MaxResults = 20 }))
            {
                builder.Append("  ")
                    .Append(result.Id)
                    .Append(' ')
                    .Append(result.Score.ToString(CultureInfo.InvariantCulture))
                    .Append(" [")
                    .Append(string.Join(' ', result.MatchedOn))
                    .Append("]\n");
            }
        }

        return builder.ToString().TrimEnd('\n');
    }
}
