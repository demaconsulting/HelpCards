using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Presentation;

namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     The lookups of a call of the knowledge tool: cards by topic, examples by name, and search results, with the
///     messages that tell an agent what to send when nothing or too much was found.
/// </summary>
internal sealed partial class AgentDispatcher
{
    /// <summary>
    ///     Answers a topic with the card in Markdown, or says what to send instead.
    /// </summary>
    /// <param name="topic">The card id or path, or an unqualified local id.</param>
    /// <returns>The card with the content of its examples, or the message.</returns>
    private string AnswerTopic(string topic)
    {
        var (card, candidates) = ResolveTopic(topic);
        return card is not null ? _catalog.Render(card, MarkdownOptions()) : TopicFailure(topic, candidates);
    }

    /// <summary>
    ///     Finds the card a topic names: an exact global id or path, else an unambiguous unqualified local id.
    /// </summary>
    /// <param name="topic">The topic.</param>
    /// <returns>The card, or the global ids of the cards the topic could mean when it is ambiguous; both empty when it names nothing.</returns>
    private (HelpCard? Card, IReadOnlyList<string> Candidates) ResolveTopic(string topic)
    {
        var exact = _catalog.Find(topic);
        if (exact is not null)
        {
            return (exact, []);
        }

        var local = _catalog.Cards
            .Where(card => string.Equals(card.LocalId, topic, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return local.Count == 1 ? (local[0], []) : (null, local.Select(card => card.Id).ToList());
    }

    /// <summary>
    ///     Writes the message for a topic that names no card or several.
    /// </summary>
    /// <param name="topic">The topic.</param>
    /// <param name="candidates">The ids of the cards it could mean when it is ambiguous.</param>
    /// <returns>The message.</returns>
    private string TopicFailure(string topic, IReadOnlyList<string> candidates)
    {
        if (candidates.Count > 1)
        {
            return $"The topic '{topic}' matches several cards: {string.Join(", ", candidates)}. Ask for one of these ids.";
        }

        var suggestions = _catalog.Suggest(topic, SuggestionCount);
        var didYouMean = suggestions.Count == 0 ? string.Empty : $" Did you mean: {string.Join(", ", suggestions)}?";
        return $"No help card matches '{topic}'.{didYouMean} Call with no arguments for the index.";
    }

    /// <summary>
    ///     Answers an example request: the one example that matches, the choices when several do, or a message.
    /// </summary>
    /// <param name="arguments">The arguments; the example is set and the topic, if any, limits the search.</param>
    /// <returns>The answer.</returns>
    private string AnswerExample(AgentArguments arguments)
    {
        var name = arguments.Example!;
        string? scope = null;
        if (arguments.Topic is not null)
        {
            var (card, candidates) = ResolveTopic(arguments.Topic);
            if (card is null)
            {
                return TopicFailure(arguments.Topic, candidates);
            }

            scope = card.Id;
        }

        var matches = _catalog.FindExamples(name, scope);
        if (matches.Count == 0)
        {
            return NoExample(name, scope);
        }

        if (matches.Count > 1)
        {
            var choices = matches.Select(match => $"{match.CardId}#{match.Example.Name}");
            return $"The example name '{name}' matches several examples. Use one of: {string.Join(", ", choices)}.";
        }

        return DescribeExample(matches[0]);
    }

    /// <summary>
    ///     Writes the message for an example that does not exist, with the closest example names.
    /// </summary>
    /// <param name="name">The name the agent sent.</param>
    /// <param name="scope">The id of the card whose subtree was searched, or <see langword="null"/>.</param>
    /// <returns>The message.</returns>
    private string NoExample(string name, string? scope)
    {
        // The closest example names come first; when none is close the agent may have meant a card
        var word = name[(name.IndexOf('#', StringComparison.Ordinal) + 1)..];
        var known = _catalog.Cards
            .Where(card => scope is null || card.Id == scope || card.Id.StartsWith(scope + "/", StringComparison.Ordinal))
            .SelectMany(card => card.Examples.Select(example => new HelpExampleReference(card.Id, example)))
            .ToList();
        var closest = SearchIndex.Suggest(word, known.Select(item => item.Example.Name), SuggestionCount);
        var suggestions = known
            .Where(item => closest.Contains(item.Example.Name, StringComparer.Ordinal))
            .Select(item => $"{item.CardId}#{item.Example.Name}")
            .Take(SuggestionCount)
            .ToList();
        if (suggestions.Count == 0)
        {
            suggestions = [.. _catalog.Suggest(name, SuggestionCount)];
        }

        var where = scope is null ? string.Empty : $" under '{scope}'";
        var didYouMean = suggestions.Count == 0 ? string.Empty : $" Did you mean: {string.Join(", ", suggestions)}?";
        return $"No example named '{name}' was found{where}.{didYouMean} Search with the query argument to find examples.";
    }

    /// <summary>
    ///     Writes one example: a line that says what it is, and its content in a fenced block.
    /// </summary>
    /// <param name="match">The example with the id of its card.</param>
    /// <returns>The answer.</returns>
    private static string DescribeExample(HelpExampleReference match)
    {
        var example = match.Example;
        var details = example.FileName is null
            ? $"{example.Kind}, {example.Language}"
            : $"{example.Kind}, {example.Language}, {example.FileName}";

        // The fence is one backtick longer than the longest run in the content, so the content cannot end it early
        var longest = 0;
        var run = 0;
        foreach (var character in example.Content)
        {
            run = character == '`' ? run + 1 : 0;
            longest = Math.Max(longest, run);
        }

        var fence = new string('`', Math.Max(3, longest + 1));
        return $"Example `{example.Name}` of `{match.CardId}` ({details}): {example.Description}\n\n"
            + $"{fence}{example.Language}\n{example.Content}\n{fence}";
    }

    /// <summary>
    ///     Answers a search with the best cards, or says that nothing matched.
    /// </summary>
    /// <param name="arguments">The arguments; the query is set.</param>
    /// <returns>The answer.</returns>
    private string AnswerQuery(AgentArguments arguments)
    {
        var view = Restrict(arguments, out var failure);
        if (view is null)
        {
            return failure!;
        }

        var query = arguments.Query!;
        var results = view.Search(query, new HelpSearchOptions { MaxResults = arguments.MaxResults ?? _options.MaxResults });
        if (results.Count == 0)
        {
            return $"No cards match '{query}'. Call with no arguments for the index.";
        }

        var options = new HelpRenderOptions { Format = HelpFormat.Markdown, LinkStyle = HelpLinkStyle.Plain };
        return $"Cards matching '{query}':\n\n{view.RenderSearch(query, results, options)}\n\nRead one with the topic argument.";
    }
}
