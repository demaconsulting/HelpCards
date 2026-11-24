using DemaConsulting.HelpCards.Presentation;

namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     The rendering members of the catalog: cards, indexes, examples, search results, and card lists as text,
///     Markdown, or JSON.
/// </summary>
/// <remarks>
///     The bodies of these members live in the Presentation subsystem; the type stays in the Catalog subsystem so
///     that one object offers lookup and rendering. Rendering is a pure function of the immutable catalog view and
///     the options, so every member is safe for concurrent use, nothing is cached, and the output is byte-identical
///     on every machine. A card is rendered as this catalog view shows it, whichever view the card object came from.
/// </remarks>
public sealed partial class HelpCatalog
{
    /// <summary>
    ///     Renders a card as text, Markdown, or JSON.
    /// </summary>
    /// <param name="card">The card to render. Must not be null and must be visible in this catalog.</param>
    /// <param name="options">The options, or <see langword="null"/> for the defaults.</param>
    /// <returns>The document, with LF line endings and no trailing line feed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="card"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the card is not visible in this catalog.</exception>
    public string Render(HelpCard card, HelpRenderOptions? options = null)
    {
        var context = RenderContext.Create(this, options);
        var own = context.Own(card);
        return context.Options.Format == HelpFormat.Json
            ? JsonDocumentWriter.WriteCard(context, own)
            : TextRenderer.RenderCard(context, own);
    }

    /// <summary>
    ///     Renders the index: one entry per visible card down to a depth.
    /// </summary>
    /// <param name="options">The options, or <see langword="null"/> for the defaults.</param>
    /// <param name="maxDepth">The deepest level to show; 0 shows only the roots. Must not be negative.</param>
    /// <returns>The document, with LF line endings and no trailing line feed.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxDepth"/> is negative.</exception>
    public string RenderIndex(HelpRenderOptions? options = null, int maxDepth = int.MaxValue)
    {
        return RenderIndex(options, maxDepth, null);
    }

    /// <summary>
    ///     Renders the examples of a card in full, and lists the cards below it that have examples.
    /// </summary>
    /// <param name="card">The card. Must not be null and must be visible in this catalog.</param>
    /// <param name="options">The options, or <see langword="null"/> for the defaults.</param>
    /// <returns>The document, with LF line endings and no trailing line feed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="card"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the card is not visible in this catalog.</exception>
    public string RenderExamples(HelpCard card, HelpRenderOptions? options = null)
    {
        var context = RenderContext.Create(this, options);
        var own = context.Own(card);
        return context.Options.Format == HelpFormat.Json
            ? JsonDocumentWriter.WriteExamplesDocument(context, own)
            : TextRenderer.RenderExamples(context, own);
    }

    /// <summary>
    ///     Renders one example of a card or of the cards below it.
    /// </summary>
    /// <remarks>
    ///     For text and Markdown the result is exactly the content of the example, with no header. For JSON it is
    ///     the <c>example</c> document.
    /// </remarks>
    /// <param name="card">The card to search from. Must not be null and must be visible in this catalog.</param>
    /// <param name="name">The example name, compared ignoring case. Must not be null or white space.</param>
    /// <param name="options">The options, or <see langword="null"/> for the defaults.</param>
    /// <returns>The content or the document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="card"/> is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="name"/> is null or white space, the card is not visible, or no such example
    ///     exists.
    /// </exception>
    public string RenderExample(HelpCard card, string name, HelpRenderOptions? options = null)
    {
        var context = RenderContext.Create(this, options);
        return context.Options.Format == HelpFormat.Json
            ? JsonDocumentWriter.WriteExampleDocument(context, card, name)
            : TextRenderer.RenderExample(context, card, name);
    }

    /// <summary>
    ///     Renders search results that were found elsewhere, one entry each.
    /// </summary>
    /// <param name="query">The query the results answer. Must not be null.</param>
    /// <param name="results">The results in the order to show. Must not be null.</param>
    /// <param name="options">The options, or <see langword="null"/> for the defaults.</param>
    /// <returns>The document, with LF line endings and no trailing line feed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/> or <paramref name="results"/> is null.</exception>
    public string RenderSearch(string query, IReadOnlyList<HelpSearchResult> results, HelpRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(results);
        var context = RenderContext.Create(this, options);
        return context.Options.Format == HelpFormat.Json
            ? JsonDocumentWriter.WriteSearch(context, query, results)
            : TextRenderer.RenderSearch(context, query, results);
    }

    /// <summary>
    ///     Renders a list of cards, one entry each, without a tree.
    /// </summary>
    /// <param name="cards">The cards to list. Must not be null and must not contain null.</param>
    /// <param name="options">The options, or <see langword="null"/> for the defaults.</param>
    /// <returns>The document, with LF line endings and no trailing line feed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cards"/> is null or contains null.</exception>
    public string RenderCardList(IReadOnlyList<HelpCard> cards, HelpRenderOptions? options = null)
    {
        return RenderCardList(cards, [], [], options);
    }

    /// <summary>
    ///     Writes the body of a card with its links in the style of the options.
    /// </summary>
    /// <remarks>
    ///     The format of the options does not matter: the result is the Markdown body of the card. With the default
    ///     options it equals <see cref="HelpCard.Body"/>.
    /// </remarks>
    /// <param name="card">The card. Must not be null and must be visible in this catalog.</param>
    /// <param name="options">The options, or <see langword="null"/> for the defaults.</param>
    /// <returns>The body.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="card"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the card is not visible in this catalog.</exception>
    public string FormatBody(HelpCard card, HelpRenderOptions? options = null)
    {
        var context = RenderContext.Create(this, options);
        return LinkFormatter.FormatBody(context, context.Own(card));
    }

    /// <summary>
    ///     Renders the index and, for JSON, the usage lines of the tool.
    /// </summary>
    /// <param name="options">The options, or <see langword="null"/> for the defaults.</param>
    /// <param name="maxDepth">The deepest level to show; 0 shows only the roots. Must not be negative.</param>
    /// <param name="usageLines">The usage lines for the JSON document, or <see langword="null"/> to leave them out.</param>
    /// <returns>The document.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxDepth"/> is negative.</exception>
    internal string RenderIndex(HelpRenderOptions? options, int maxDepth, IReadOnlyList<string>? usageLines)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxDepth);
        var context = RenderContext.Create(this, options);
        return context.Options.Format == HelpFormat.Json
            ? JsonDocumentWriter.WriteIndex(context, maxDepth, usageLines)
            : TextRenderer.RenderIndex(context, maxDepth);
    }

    /// <summary>
    ///     Renders a list of cards and, for JSON, the filter that selected them.
    /// </summary>
    /// <param name="cards">The cards to list. Must not be null and must not contain null.</param>
    /// <param name="tags">The tags of the filter for the JSON document. Must not be null.</param>
    /// <param name="sources">The source names of the filter for the JSON document. Must not be null.</param>
    /// <param name="options">The options, or <see langword="null"/> for the defaults.</param>
    /// <returns>The document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null or <paramref name="cards"/> contains null.</exception>
    internal string RenderCardList(
        IReadOnlyList<HelpCard> cards,
        IReadOnlyList<string> tags,
        IReadOnlyList<string> sources,
        HelpRenderOptions? options)
    {
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(sources);

        // A null entry would fail later with no hint where it came from, so name the argument here
        if (cards.Any(card => card is null))
        {
            throw new ArgumentNullException(nameof(cards), "The list of cards must not contain null.");
        }

        var context = RenderContext.Create(this, options);
        return context.Options.Format == HelpFormat.Json
            ? JsonDocumentWriter.WriteCardList(context, cards, tags, sources)
            : TextRenderer.RenderCardList(context, cards);
    }
}
