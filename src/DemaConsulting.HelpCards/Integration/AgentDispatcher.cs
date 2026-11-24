using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Presentation;

namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     Answers one call of the knowledge tool from checked arguments: an example, a card, search results, or the index.
/// </summary>
/// <remarks>
///     <para>
///         The first rule that applies decides the answer: an <c>example</c>, else a <c>topic</c>, else a <c>query</c>,
///         else the index. When two of these were given, the answer ends with one line that says which was ignored. Every
///         answer is plain text that is also valid Markdown, and every miss says what to send instead, so an agent can
///         recover without a person.
///     </para>
///     <para>
///         The dispatcher reads the immutable catalog only and does no I/O. It holds no state besides the catalog and the
///         options, so any number of calls may run at the same time. This part holds the choice of the answer and the
///         index; the other part holds the lookups of cards, examples, and search results.
///     </para>
/// </remarks>
internal sealed partial class AgentDispatcher
{
    /// <summary>
    ///     The number of suggestions in an answer that found nothing.
    /// </summary>
    private const int SuggestionCount = 5;

    /// <summary>
    ///     The catalog the tool answers from.
    /// </summary>
    private readonly HelpCatalog _catalog;

    /// <summary>
    ///     The settings of the tool.
    /// </summary>
    private readonly HelpAgentToolOptions _options;

    /// <summary>
    ///     Creates a dispatcher.
    /// </summary>
    /// <param name="catalog">The catalog. Must not be null.</param>
    /// <param name="options">The settings, already checked. Must not be null.</param>
    public AgentDispatcher(HelpCatalog catalog, HelpAgentToolOptions options)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(options);
        _catalog = catalog;
        _options = options;
    }

    /// <summary>
    ///     Answers a call.
    /// </summary>
    /// <param name="arguments">The checked arguments. Must not be null.</param>
    /// <returns>The answer text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="arguments"/> is null.</exception>
    public string Answer(AgentArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (arguments.Example is not null)
        {
            return AnswerExample(arguments);
        }

        if (arguments.Topic is not null)
        {
            return AnswerTopic(arguments.Topic);
        }

        return arguments.Query is not null ? AnswerQuery(arguments) : AnswerIndex(arguments);
    }

    /// <summary>
    ///     Gets the line that tells the agent that its query was not used, when a topic or an example was given too.
    /// </summary>
    /// <remarks>
    ///     The line is not part of <see cref="Answer"/> so that the tool can append it after it cuts a long answer.
    /// </remarks>
    /// <param name="arguments">The arguments. Must not be null.</param>
    /// <returns>The line, or <see langword="null"/> when no query was ignored.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="arguments"/> is null.</exception>
    public static string? IgnoredNotice(AgentArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (arguments.Query is null)
        {
            return null;
        }

        string? given = null;
        if (arguments.Example is not null)
        {
            given = "example";
        }
        else if (arguments.Topic is not null)
        {
            given = "topic";
        }

        return given is null ? null : $"Ignored 'query' because '{given}' was given.";
    }

    /// <summary>
    ///     Gets the options of a Markdown rendering for an agent: plain links and the content of every example.
    /// </summary>
    /// <returns>The options.</returns>
    private static HelpRenderOptions MarkdownOptions()
    {
        return new HelpRenderOptions
        {
            Format = HelpFormat.Markdown,
            LinkStyle = HelpLinkStyle.Plain,
            IncludeExampleContent = true,
        };
    }

    /// <summary>
    ///     Answers a call that asks for no card, example, or search: the index, possibly restricted.
    /// </summary>
    /// <param name="arguments">The arguments.</param>
    /// <returns>The index, one block for each source when there are several and no restriction.</returns>
    private string AnswerIndex(AgentArguments arguments)
    {
        var view = Restrict(arguments, out var failure);
        if (view is null)
        {
            return failure!;
        }

        var options = new HelpRenderOptions { Format = HelpFormat.Markdown, LinkStyle = HelpLinkStyle.Plain };
        var blocks = new List<string> { "Help index. Read a card with the topic argument, get an example with the example argument, or search with the query argument." };
        var unrestricted = arguments.Source is null && arguments.Tag is null;
        if (unrestricted && view.Sources.Count > 1)
        {
            foreach (var source in view.Sources)
            {
                var block = view.FilterSource([source.Name]);
                blocks.Add($"## {source.Title}\n\n{source.Summary}\n\n{block.RenderIndex(options, _options.IndexDepth)}");
            }
        }
        else
        {
            blocks.Add(view.RenderIndex(options, _options.IndexDepth));
        }

        return string.Join("\n\n", blocks);
    }

    /// <summary>
    ///     Restricts the catalog to the source and the tag of the arguments.
    /// </summary>
    /// <param name="arguments">The arguments.</param>
    /// <param name="failure">Receives the message when the source or the tag is unknown.</param>
    /// <returns>The view, or <see langword="null"/> when a name is unknown.</returns>
    private HelpCatalog? Restrict(AgentArguments arguments, out string? failure)
    {
        failure = null;
        var view = _catalog;
        if (arguments.Source is not null)
        {
            var source = view.Sources.FirstOrDefault(info => string.Equals(info.Name, arguments.Source, StringComparison.OrdinalIgnoreCase));
            if (source is null)
            {
                failure = $"Unknown source '{arguments.Source}'. Valid sources: {string.Join(", ", view.Sources.Select(info => info.Name))}.";
                return null;
            }

            view = view.FilterSource([source.Name]);
        }

        if (arguments.Tag is not null)
        {
            var tag = view.TagNames.FirstOrDefault(name => string.Equals(name, arguments.Tag, StringComparison.OrdinalIgnoreCase));
            if (tag is null)
            {
                failure = view.TagNames.Count > 0
                    ? $"Unknown tag '{arguments.Tag}'. Valid tags: {string.Join(", ", view.TagNames)}."
                    : $"No cards have the tag '{arguments.Tag}'; this help has no tags.";
                return null;
            }

            view = view.Filter([tag]);
        }

        return view;
    }
}
