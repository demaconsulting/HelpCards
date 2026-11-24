using System.Text;
using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Presentation;

/// <summary>
///     The index, card list, and search result documents of the text and Markdown renderer.
/// </summary>
internal static partial class TextRenderer
{
    /// <summary>
    ///     The two spaces that separate the label column of a list from the summary column.
    /// </summary>
    private const string ColumnGap = "  ";

    /// <summary>
    ///     The indent of each level of the index, in spaces.
    /// </summary>
    private const int IndexIndent = 2;

    /// <summary>
    ///     Renders the index: one line per visible card down to a depth.
    /// </summary>
    /// <remarks>
    ///     A line is the label of the card, indented by two spaces per level, padded to the widest label, two spaces,
    ///     and the summary wrapped at the hanging indent.
    /// </remarks>
    /// <param name="context">The settings of the call. Must not be null.</param>
    /// <param name="maxDepth">The deepest level to show; 0 shows only the roots.</param>
    /// <returns>The document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    public static string RenderIndex(RenderContext context, int maxDepth)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Each entry is a label, indented by its depth, and the summary of the card
        var entries = context.Flatten(maxDepth)
            .Select(entry => (
                Label: new string(' ', IndexIndent * entry.Depth)
                    + LinkFormatter.FormatLabel(context, entry.Card.Id, entry.Card.Title, entry.Card.SourceName),
                entry.Card.Summary))
            .ToList();
        return RenderColumns(context, entries, HelpStrings.NoCards);
    }

    /// <summary>
    ///     Renders a list of cards, one line each, without indentation.
    /// </summary>
    /// <param name="context">The settings of the call. Must not be null.</param>
    /// <param name="cards">The cards to list. Must not be null and must not contain null.</param>
    /// <returns>The document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static string RenderCardList(RenderContext context, IReadOnlyList<HelpCard> cards)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(cards);

        var entries = cards
            .Select(card => (LinkFormatter.FormatLabel(context, card.Id, card.Title, card.SourceName), card.Summary))
            .ToList();
        return RenderColumns(context, entries, HelpStrings.NoCards);
    }

    /// <summary>
    ///     Renders search results, one line each, with the label of the card and its summary.
    /// </summary>
    /// <param name="context">The settings of the call. Must not be null.</param>
    /// <param name="query">The query the results answer. Must not be null.</param>
    /// <param name="results">The results in the order to show. Must not be null.</param>
    /// <returns>The document; a sentence that names the query when there are no results.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static string RenderSearch(RenderContext context, string query, IReadOnlyList<HelpSearchResult> results)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(results);

        var entries = results
            .Select(result => (LinkFormatter.FormatLabel(context, result.Id, result.Title, result.SourceName), result.Summary))
            .ToList();
        return RenderColumns(context, entries, HelpStrings.NoResults(query));
    }

    /// <summary>
    ///     Renders two columns: labels padded to the widest one and summaries wrapped at the hanging indent.
    /// </summary>
    /// <param name="context">The settings of the call.</param>
    /// <param name="entries">The labels and summaries.</param>
    /// <param name="emptyText">The sentence written when there are no entries.</param>
    /// <returns>The document.</returns>
    private static string RenderColumns(
        RenderContext context,
        IReadOnlyList<(string Label, string Summary)> entries,
        string emptyText)
    {
        var builder = new StringBuilder();
        if (entries.Count == 0)
        {
            TextWrapper.Append(builder, emptyText, context.WrapWidth, string.Empty, string.Empty);
            return Finish(builder);
        }

        // Pad every label to the widest so the summaries line up and wrap at one hanging indent
        var width = entries.Max(entry => entry.Label.Length);
        foreach (var (label, summary) in entries)
        {
            AppendEntry(context, builder, label.PadRight(width) + ColumnGap, summary);
        }

        return Finish(builder);
    }
}
