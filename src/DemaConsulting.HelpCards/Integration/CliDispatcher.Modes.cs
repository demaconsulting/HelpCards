using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Presentation;

namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     The answers of the modes of a help command: the index, a card, examples, one example, a search, and a list of the
///     cards that have a tag.
/// </summary>
internal sealed partial class CliDispatcher
{
    /// <summary>
    ///     Selects the answer of the mode of a request.
    /// </summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="request">The request.</param>
    /// <param name="render">The render options of the request.</param>
    /// <returns>The text to write or the failure.</returns>
    private CliOutcome Execute(HelpCatalog catalog, CliRequest request, HelpRenderOptions render)
    {
        // The sources of the request restrict every mode, so unknown names are reported before any work is done
        var sources = ResolveSources(catalog, request.Sources, out var sourceFailure);
        if (sourceFailure is not null)
        {
            return CliOutcome.Fail(sourceFailure);
        }

        var view = sources.Count == 0 ? catalog : catalog.FilterSource(sources);
        return request.Mode switch
        {
            CliMode.Index => CliOutcome.Success(RenderIndex(view, request, render)),
            CliMode.Search => Search(view, sources, request, render),
            CliMode.Tags => ListTagged(view, sources, request, render),
            _ => ShowCard(view, request, render),
        };
    }

    /// <summary>
    ///     Writes the index: the usage, the preamble, the sections of the tool, and the topics.
    /// </summary>
    /// <param name="view">The catalog view.</param>
    /// <param name="request">The request.</param>
    /// <param name="render">The render options.</param>
    /// <returns>The document.</returns>
    private string RenderIndex(HelpCatalog view, CliRequest request, HelpRenderOptions render)
    {
        // Only the roots are listed unless the whole tree was asked for
        var depth = request.All ? int.MaxValue : 0;
        if (request.Format == HelpFormat.Json)
        {
            return view.RenderIndex(render, depth, _options.UsageLines);
        }

        var blocks = new List<string>();
        if (_options.UsageLines is { Count: > 0 } usage)
        {
            blocks.Add("Usage:\n" + string.Join("\n", usage.Select(line => "  " + line)));
        }

        if (!string.IsNullOrWhiteSpace(_options.IndexPreamble))
        {
            blocks.Add(_options.IndexPreamble);
        }

        foreach (var section in _options.IndexSections ?? [])
        {
            blocks.Add(string.Join("\n", section.Lines.Select(line => "  " + line).Prepend(section.Heading)));
        }

        blocks.Add("Help topics:\n" + view.RenderIndex(render, depth));
        blocks.Add(Footer);
        return string.Join("\n\n", blocks);
    }

    /// <summary>
    ///     Answers a search.
    /// </summary>
    /// <param name="view">The catalog view, restricted to the sources of the request.</param>
    /// <param name="sources">The source names of the request, spelled as the catalog spells them.</param>
    /// <param name="request">The request.</param>
    /// <param name="render">The render options.</param>
    /// <returns>The results as text or JSON.</returns>
    private static CliOutcome Search(HelpCatalog view, IReadOnlyList<string> sources, CliRequest request, HelpRenderOptions render)
    {
        var tags = ResolveTags(view, request.Tags, out var tagFailure);
        if (tagFailure is not null)
        {
            return CliOutcome.Fail(tagFailure);
        }

        // Tags that nobody uses cannot match, whether or not the catalog has a vocabulary
        var results = request.Tags.Count > 0 && tags.Count == 0
            ? []
            : view.Search(request.Name!, new HelpSearchOptions { Tags = tags, Sources = sources });
        return CliOutcome.Success(view.RenderSearch(request.Name!, results, render));
    }

    /// <summary>
    ///     Lists the cards that have any of the tags.
    /// </summary>
    /// <param name="view">The catalog view, restricted to the sources of the request.</param>
    /// <param name="sources">The source names of the request, spelled as the catalog spells them.</param>
    /// <param name="request">The request.</param>
    /// <param name="render">The render options.</param>
    /// <returns>The list as text or JSON.</returns>
    private static CliOutcome ListTagged(HelpCatalog view, IReadOnlyList<string> sources, CliRequest request, HelpRenderOptions render)
    {
        var tags = ResolveTags(view, request.Tags, out var tagFailure);
        if (tagFailure is not null)
        {
            return CliOutcome.Fail(tagFailure);
        }

        IReadOnlyList<HelpCard> cards = tags.Count == 0 ? [] : view.Filter(tags).Cards;
        return CliOutcome.Success(view.RenderCardList(cards, tags, sources, render));
    }

    /// <summary>
    ///     Answers the modes that start at a card: the card itself, its examples, or one example.
    /// </summary>
    /// <param name="view">The catalog view.</param>
    /// <param name="request">The request.</param>
    /// <param name="render">The render options.</param>
    /// <returns>The document or the failure.</returns>
    private CliOutcome ShowCard(HelpCatalog view, CliRequest request, HelpRenderOptions render)
    {
        var card = FindCard(view, request.Path, out var failure);
        if (card is null)
        {
            return CliOutcome.Fail(failure!);
        }

        return request.Mode switch
        {
            CliMode.Examples => CliOutcome.Success(view.RenderExamples(card, render)),
            CliMode.Example => ShowExample(view, card, request.Name!, render),
            _ => CliOutcome.Success(view.Render(card, render)),
        };
    }
}
