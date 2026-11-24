using System.Text.Json;
using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Presentation;

/// <summary>
///     The documents for lists: <c>index</c>, <c>search</c>, and <c>cards</c>.
/// </summary>
internal static partial class JsonDocumentWriter
{
    /// <summary>
    ///     Writes the <c>index</c> document: the sources and the visible cards down to a depth.
    /// </summary>
    /// <remarks>
    ///     <c>tool</c> and <c>version</c> are written only when they were set on the builder, and <c>usage</c> only
    ///     when the caller supplies usage lines; the command-line helper is the caller that does.
    /// </remarks>
    /// <param name="context">The settings of the call. Must not be null.</param>
    /// <param name="maxDepth">The deepest level to include; 0 includes only the roots.</param>
    /// <param name="usageLines">The usage lines, or <see langword="null"/> to leave <c>usage</c> out.</param>
    /// <returns>The document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    public static string WriteIndex(RenderContext context, int maxDepth, IReadOnlyList<string>? usageLines)
    {
        ArgumentNullException.ThrowIfNull(context);

        var data = context.Catalog.Scope.Data;
        return WriteDocument(context.Options, "index", true, writer =>
        {
            WriteOptionalString(writer, "tool", data.ToolName);
            WriteOptionalString(writer, "version", data.ToolVersion);
            if (usageLines is not null)
            {
                WriteStrings(writer, "usage", usageLines);
            }

            writer.WriteStartArray("sources");
            foreach (var source in context.Catalog.Sources)
            {
                writer.WriteStartObject();
                writer.WriteString("name", source.Name);
                writer.WriteString("title", source.Title);
                writer.WriteString("summary", source.Summary);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            WriteIndexCards(context, writer, maxDepth);
        });
    }

    /// <summary>
    ///     Writes the <c>search</c> document for results that were found elsewhere.
    /// </summary>
    /// <param name="context">The settings of the call. Must not be null.</param>
    /// <param name="query">The query. Must not be null.</param>
    /// <param name="results">The results in the order to show. Must not be null.</param>
    /// <returns>The document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static string WriteSearch(RenderContext context, string query, IReadOnlyList<HelpSearchResult> results)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(results);

        return WriteDocument(context.Options, "search", true, writer =>
        {
            writer.WriteString("query", query);
            writer.WriteStartArray("results");
            foreach (var result in results)
            {
                writer.WriteStartObject();
                writer.WriteString("id", result.Id);
                writer.WriteString("title", result.Title);
                writer.WriteString("summary", result.Summary);
                writer.WriteString("source", result.SourceName);
                writer.WriteNumber("score", result.Score);
                WriteStrings(writer, "matchedOn", result.MatchedOn);
                WriteCommand(context, writer, result.Id);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        });
    }

    /// <summary>
    ///     Writes the <c>cards</c> document: a list of cards with the filter that selected them.
    /// </summary>
    /// <param name="context">The settings of the call. Must not be null.</param>
    /// <param name="cards">The cards. Must not be null.</param>
    /// <param name="tags">The tags of the filter. Must not be null.</param>
    /// <param name="sources">The source names of the filter. Must not be null.</param>
    /// <returns>The document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static string WriteCardList(
        RenderContext context,
        IReadOnlyList<HelpCard> cards,
        IReadOnlyList<string> tags,
        IReadOnlyList<string> sources)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(sources);

        return WriteDocument(context.Options, "cards", true, writer =>
        {
            writer.WriteStartObject("filter");
            WriteStrings(writer, "tags", tags);
            WriteStrings(writer, "sources", sources);
            writer.WriteEndObject();
            writer.WriteStartArray("cards");
            foreach (var card in cards)
            {
                writer.WriteStartObject();
                writer.WriteString("id", card.Id);
                writer.WriteString("title", card.Title);
                writer.WriteString("summary", card.Summary);
                WriteStrings(writer, "tags", card.Tags);
                writer.WriteString("source", card.SourceName);
                WriteCommand(context, writer, card.Id);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        });
    }

    /// <summary>
    ///     Writes the <c>cards</c> array of the index document.
    /// </summary>
    /// <param name="context">The settings of the call.</param>
    /// <param name="writer">The writer.</param>
    /// <param name="maxDepth">The deepest level to include.</param>
    private static void WriteIndexCards(RenderContext context, Utf8JsonWriter writer, int maxDepth)
    {
        writer.WriteStartArray("cards");
        foreach (var (card, depth) in context.Flatten(maxDepth))
        {
            writer.WriteStartObject();
            writer.WriteString("id", card.Id);
            writer.WriteString("title", card.Title);
            writer.WriteString("summary", card.Summary);
            WriteStrings(writer, "aliases", card.Aliases);
            WriteStrings(writer, "tags", card.Tags);
            writer.WriteString("source", card.SourceName);
            writer.WriteNumber("depth", depth);
            WriteCommand(context, writer, card.Id);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }
}
