using System.Text;

namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     The search index of a catalog: the tokens of every card by field, and the scoring that ranks cards for a query.
/// </summary>
/// <remarks>
///     <para>
///         An index is built once for the frozen data of a catalog, on the first search, and then only read, so any
///         number of threads and any number of views can use it. A view does not own an index: it passes the cards it
///         shows and the index scores exactly those.
///     </para>
///     <para>
///         The scoring follows the specification. A query is split into lower-case tokens of letters and digits; a
///         token longer than three characters that ends in <c>s</c> loses that <c>s</c> when the culture of the catalog
///         is English or neutral. For each token the best score of any field counts: an exact match of a field token
///         scores more than a prefix match, and the fields are ranked alias, local id, example name, title, tag, and
///         summary. The sum of the tokens is raised by 50 when every token matched and by 100 when the whole query
///         equals an id, a name, or an alias of the card.
///     </para>
/// </remarks>
internal sealed partial class SearchIndex
{
    /// <summary>
    ///     The names of the indexed fields in the fixed order in which matches are reported.
    /// </summary>
    private static readonly string[] FieldNames = ["alias", "id", "example", "title", "tag", "summary"];

    /// <summary>
    ///     The score of an exact token match in each field, in the order of <see cref="FieldNames"/>.
    /// </summary>
    private static readonly int[] ExactScores = [40, 40, 25, 30, 20, 10];

    /// <summary>
    ///     The score of a prefix match in each field, in the order of <see cref="FieldNames"/>; zero for no prefix match.
    /// </summary>
    private static readonly int[] PrefixScores = [20, 20, 12, 15, 0, 5];

    /// <summary>
    ///     The least length of a query token that may match as a prefix.
    /// </summary>
    private const int MinimumPrefixLength = 3;

    /// <summary>
    ///     The bonus when every token of the query matched.
    /// </summary>
    private const int AllTokensBonus = 50;

    /// <summary>
    ///     The bonus when the whole query equals an id, a name, or an alias of the card.
    /// </summary>
    private const int WholeQueryBonus = 100;

    /// <summary>
    ///     The least length of a word that may lose a plural <c>s</c>.
    /// </summary>
    private const int StemLength = 4;

    /// <summary>
    ///     The indexed cards by global id.
    /// </summary>
    private readonly Dictionary<string, IndexedCard> _cards;

    /// <summary>
    ///     Whether English plurals are stemmed.
    /// </summary>
    private readonly bool _stem;

    /// <summary>
    ///     Creates an index over the cards of the data.
    /// </summary>
    /// <param name="data">The data. Must not be null.</param>
    private SearchIndex(CatalogData data)
    {
        // The plural rule is English; other cultures keep their words as they are written
        var language = data.Culture.TwoLetterISOLanguageName;
        _stem = language.Length == 0 || language == "iv" || string.Equals(language, "en", StringComparison.OrdinalIgnoreCase);
        _cards = data.Cards.ToDictionary(node => node.Id, node => Index(node), StringComparer.Ordinal);
    }

    /// <summary>
    ///     Builds the index of the data.
    /// </summary>
    /// <param name="data">The data. Must not be null.</param>
    /// <returns>The index.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is null.</exception>
    public static SearchIndex Build(CatalogData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new SearchIndex(data);
    }

    /// <summary>
    ///     Ranks cards for a query.
    /// </summary>
    /// <param name="cards">The cards to search, in any order. Must not be null.</param>
    /// <param name="query">The query. Must not be null.</param>
    /// <returns>
    ///     The cards with a score above zero, by score descending, then local depth ascending, then global id in
    ///     ordinal order; empty when the query has no token.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public IReadOnlyList<HelpSearchResult> Search(IEnumerable<CardNode> cards, string query)
    {
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(query);

        // A repeated word must not count twice
        var tokens = Tokenize(query).Distinct(StringComparer.Ordinal).ToList();
        if (tokens.Count == 0)
        {
            return [];
        }

        var whole = NormalizeWholeQuery(query);
        var scored = new List<(HelpSearchResult Result, int Depth)>();
        foreach (var node in cards)
        {
            var indexed = _cards[node.Id];
            var result = Score(indexed, tokens, whole);
            if (result is not null)
            {
                scored.Add((result, indexed.Depth));
            }
        }

        return CardNode.Freeze(scored
            .OrderByDescending(entry => entry.Result.Score)
            .ThenBy(entry => entry.Depth)
            .ThenBy(entry => entry.Result.Id, StringComparer.Ordinal)
            .Select(entry => entry.Result));
    }

    /// <summary>
    ///     Splits text into lower-case tokens of letters and digits without any stemming.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The tokens in order.</returns>
    private static IEnumerable<string> Split(string text)
    {
        var builder = new StringBuilder();
        foreach (var character in text)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
            else if (builder.Length > 0)
            {
                yield return builder.ToString();
                builder.Clear();
            }
        }

        if (builder.Length > 0)
        {
            yield return builder.ToString();
        }
    }

    /// <summary>
    ///     Brings a query into the form in which ids are compared: lower case, with every run of spaces and slashes
    ///     replaced by one slash and no slash at either end.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <returns>The normalized query.</returns>
    private static string NormalizeWholeQuery(string query)
    {
        var builder = new StringBuilder();
        foreach (var character in query.ToLowerInvariant())
        {
            if (character is ' ' or '/' or '\t' or '\r' or '\n')
            {
                if (builder.Length > 0 && builder[^1] != '/')
                {
                    builder.Append('/');
                }
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString().TrimEnd('/');
    }

    /// <summary>
    ///     Splits text into tokens and removes the plural <c>s</c> of the words that have one.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The tokens in order.</returns>
    private IEnumerable<string> Tokenize(string text)
    {
        return Split(text).Select(token =>
            _stem && token.Length >= StemLength && token[^1] == 's' ? token[..^1] : token);
    }

    /// <summary>
    ///     Reads the fields of a card into an indexed card.
    /// </summary>
    /// <param name="node">The card.</param>
    /// <returns>The indexed card.</returns>
    private IndexedCard Index(CardNode node)
    {
        // The local id has no mount prefix, so the name of a mount does not boost the cards below it
        var fields = new[]
        {
            node.Aliases.SelectMany(Tokenize).ToArray(),
            Tokenize(node.LocalId).ToArray(),
            node.Examples.SelectMany(example => Tokenize(example.Name)).ToArray(),
            Tokenize(node.Title).ToArray(),
            node.Tags.SelectMany(Tokenize).ToArray(),
            Tokenize(node.Summary).ToArray(),
        };
        var names = new List<string> { node.Id.ToLowerInvariant(), node.LocalId.ToLowerInvariant(), node.Name.ToLowerInvariant() };
        names.AddRange(node.Aliases.Select(alias => alias.ToLowerInvariant()));
        var depth = node.LocalId.Count(character => character == '/');
        return new IndexedCard(node, fields, names, depth);
    }

    /// <summary>
    ///     Scores one card for the tokens of a query.
    /// </summary>
    /// <param name="indexed">The card.</param>
    /// <param name="tokens">The distinct tokens of the query.</param>
    /// <param name="whole">The normalized whole query.</param>
    /// <returns>The result, or <see langword="null"/> when the score is zero.</returns>
    private static HelpSearchResult? Score(IndexedCard indexed, IReadOnlyList<string> tokens, string whole)
    {
        var total = 0;
        var matched = 0;
        var fieldMatched = new bool[FieldNames.Length];
        foreach (var token in tokens)
        {
            // For one token the best field counts, but every field that matched is reported
            var best = 0;
            for (var field = 0; field < FieldNames.Length; field++)
            {
                var score = ScoreField(indexed.Fields[field], token, field);
                if (score > 0)
                {
                    fieldMatched[field] = true;
                    best = Math.Max(best, score);
                }
            }

            if (best > 0)
            {
                total += best;
                matched++;
            }
        }

        if (matched == tokens.Count)
        {
            total += AllTokensBonus;
        }

        if (indexed.Names.Contains(whole, StringComparer.Ordinal))
        {
            total += WholeQueryBonus;
        }

        // Zero scores are not results
        if (total == 0)
        {
            return null;
        }

        var matchedOn = FieldNames.Where((_, field) => fieldMatched[field]);
        var node = indexed.Node;
        return new HelpSearchResult(node.Id, node.Title, node.Summary, node.SourceName, total, CardNode.Freeze(matchedOn));
    }

    /// <summary>
    ///     Scores one token against the tokens of one field.
    /// </summary>
    /// <param name="fieldTokens">The tokens of the field.</param>
    /// <param name="token">The query token.</param>
    /// <param name="field">The index of the field.</param>
    /// <returns>The exact score, else the prefix score, else zero.</returns>
    private static int ScoreField(string[] fieldTokens, string token, int field)
    {
        if (fieldTokens.Contains(token, StringComparer.Ordinal))
        {
            return ExactScores[field];
        }

        return token.Length >= MinimumPrefixLength
            && fieldTokens.Any(candidate => candidate.StartsWith(token, StringComparison.Ordinal))
                ? PrefixScores[field]
                : 0;
    }

    /// <summary>
    ///     A card with its tokens by field.
    /// </summary>
    /// <param name="Node">The card.</param>
    /// <param name="Fields">The tokens of each field in the order of the field names.</param>
    /// <param name="Names">The lower-case global id, local id, name, and aliases of the card.</param>
    /// <param name="Depth">The depth of the local id, zero for a root of its source.</param>
    private sealed record IndexedCard(CardNode Node, string[][] Fields, IReadOnlyList<string> Names, int Depth);
}
