using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Presentation;

namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     The lookups of a help command: sources, tags, cards, and examples, with the failures for names that match nothing
///     or several things.
/// </summary>
internal sealed partial class CliDispatcher
{
    /// <summary>
    ///     Matches the source names of a request to the sources of the catalog.
    /// </summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="names">The names the user gave.</param>
    /// <param name="failure">Receives the failure for the first unknown name.</param>
    /// <returns>The names spelled as the catalog spells them.</returns>
    private static IReadOnlyList<string> ResolveSources(HelpCatalog catalog, IReadOnlyList<string> names, out CliFailure? failure)
    {
        failure = null;
        var result = new List<string>();
        var known = catalog.Sources.Select(source => source.Name).ToList();
        foreach (var name in names)
        {
            var match = known.FirstOrDefault(candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase));
            if (match is null)
            {
                failure = new CliFailure(
                    JsonDocumentWriter.NotFoundCode,
                    $"Unknown help source '{name}'.",
                    $"Known sources: {string.Join(", ", known)}.",
                    known);
                return [];
            }

            result.Add(match);
        }

        return result;
    }

    /// <summary>
    ///     Matches the tags of a request to the tags of the cards of a view.
    /// </summary>
    /// <remarks>
    ///     When no card of the view has a tag there is nothing to check against and nothing to find, so the result is
    ///     empty and is not a failure.
    /// </remarks>
    /// <param name="view">The catalog view.</param>
    /// <param name="tags">The tags the user gave.</param>
    /// <param name="failure">Receives the failure for the first unknown tag.</param>
    /// <returns>The tags spelled as the cards spell them.</returns>
    private static IReadOnlyList<string> ResolveTags(HelpCatalog view, IReadOnlyList<string> tags, out CliFailure? failure)
    {
        failure = null;
        var result = new List<string>();
        if (view.TagNames.Count == 0)
        {
            return result;
        }

        foreach (var tag in tags)
        {
            var match = view.TagNames.FirstOrDefault(candidate => string.Equals(candidate, tag, StringComparison.OrdinalIgnoreCase));
            if (match is null)
            {
                failure = new CliFailure(
                    JsonDocumentWriter.NotFoundCode,
                    $"Unknown tag '{tag}'.",
                    $"Known tags: {string.Join(", ", view.TagNames)}.",
                    view.TagNames);
                return [];
            }

            result.Add(match);
        }

        return result;
    }

    /// <summary>
    ///     Finds the card that the words of a request name.
    /// </summary>
    /// <param name="view">The catalog view.</param>
    /// <param name="words">The words of the path.</param>
    /// <param name="failure">Receives the failure when no card was found.</param>
    /// <returns>The card, or <see langword="null"/>.</returns>
    private HelpCard? FindCard(HelpCatalog view, IReadOnlyList<string> words, out CliFailure? failure)
    {
        var resolution = view.Resolve(string.Join(' ', words));
        failure = resolution.Card is null ? CreateNotFound(resolution) : null;
        return resolution.Card;
    }

    /// <summary>
    ///     Writes the failure for a path that names no card, with the closest name and the names that were valid.
    /// </summary>
    /// <param name="resolution">The resolution that failed.</param>
    /// <returns>The failure.</returns>
    private CliFailure CreateNotFound(HelpPathResolution resolution)
    {
        var parent = resolution.ParentId;
        var message = parent is null
            ? $"Unknown help card '{resolution.UnknownSegment}'."
            : $"Unknown help card '{resolution.UnknownSegment}' under '{parent}'.";
        var candidates = resolution.ValidNames.Select(name => parent is null ? name : $"{parent}/{name}").ToList();

        var hint = new List<string>();
        if (resolution.Suggestion is not null)
        {
            hint.Add($"Did you mean '{resolution.Suggestion}'?");
        }

        var where = parent is null ? CommandFor(string.Empty).Trim() : CommandFor(parent);
        if (resolution.ValidNames.Count > 0)
        {
            var label = parent is null ? "Help topics" : $"Cards under '{parent}'";
            hint.Add($"{label}: {string.Join(", ", resolution.ValidNames)}. Run '{where}' for their summaries.");
        }
        else if (parent is not null)
        {
            hint.Add($"'{parent}' has no cards below it.");
        }

        return new CliFailure(
            JsonDocumentWriter.NotFoundCode,
            message,
            hint.Count == 0 ? null : string.Join(' ', hint),
            candidates);
    }

    /// <summary>
    ///     Prints one example of a card or of the cards below it.
    /// </summary>
    /// <param name="view">The catalog view.</param>
    /// <param name="card">The card to start at.</param>
    /// <param name="name">The example name, or <c>card#name</c>.</param>
    /// <param name="render">The render options.</param>
    /// <returns>The content of the example, the example document, or a failure.</returns>
    private CliOutcome ShowExample(HelpCatalog view, HelpCard card, string name, HelpRenderOptions render)
    {
        var matches = view.FindExamples(name, card.Id);
        if (matches.Count == 0)
        {
            return CliOutcome.Fail(CreateNoExample(view, card, name));
        }

        if (matches.Count > 1)
        {
            var choices = matches.Select(match => $"{match.CardId}#{match.Example.Name}").ToList();
            return CliOutcome.Fail(new CliFailure(
                JsonDocumentWriter.AmbiguousCode,
                $"The example name '{name}' matches several examples.",
                $"Use one of: {string.Join(", ", choices)}.",
                choices));
        }

        var owner = view.Find(matches[0].CardId)!;
        return CliOutcome.Success(view.RenderExample(owner, matches[0].Example.Name, render));
    }

    /// <summary>
    ///     Writes the failure for an example that does not exist, with the examples that do.
    /// </summary>
    /// <param name="view">The catalog view.</param>
    /// <param name="card">The card the search started at.</param>
    /// <param name="name">The name the user gave.</param>
    /// <returns>The failure.</returns>
    private CliFailure CreateNoExample(HelpCatalog view, HelpCard card, string name)
    {
        var available = new List<HelpExampleReference>();
        Collect(view, card, available);
        var choices = available.Select(item => $"{item.CardId}#{item.Example.Name}").ToList();
        var hint = available.Count == 0
            ? $"'{card.Id}' has no examples."
            : $"Examples: {string.Join(", ", available.Select(item => item.Example.Name))}. Run '{CommandFor(card.Id)} --examples' to see them.";
        return new CliFailure(
            JsonDocumentWriter.NotFoundCode,
            $"The help card '{card.Id}' has no example '{name}'.",
            hint,
            choices);
    }

    /// <summary>
    ///     Collects the examples of a card and of the cards below it, depth first.
    /// </summary>
    /// <param name="view">The catalog view.</param>
    /// <param name="card">The card.</param>
    /// <param name="result">Receives the examples.</param>
    private static void Collect(HelpCatalog view, HelpCard card, List<HelpExampleReference> result)
    {
        result.AddRange(card.Examples.Select(example => new HelpExampleReference(card.Id, example)));
        foreach (var child in view.GetChildren(card))
        {
            Collect(view, child, result);
        }
    }

    /// <summary>
    ///     Writes the command that prints a card, from the command template.
    /// </summary>
    /// <param name="id">The global id of the card.</param>
    /// <returns>The command.</returns>
    private string CommandFor(string id)
    {
        return _commandTemplate
            .Replace("{tool}", _options.ToolName, StringComparison.Ordinal)
            .Replace("{path}", id.Replace('/', ' '), StringComparison.Ordinal);
    }
}
