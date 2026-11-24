using System.Globalization;
using System.Text;
using System.Text.Json;
using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     The knowledge tool that gives an AI agent one way to read the help of a whole catalog: the index, a card, an
///     example, or a search.
/// </summary>
/// <remarks>
///     <para>
///         The tool is independent of any agent framework. A host registers <see cref="Name"/>, <see cref="Description"/>,
///         and <see cref="ParametersSchema"/> with its framework and passes the JSON arguments of each call to
///         <see cref="Invoke"/>. The tool is created with <see cref="HelpAgentTools.Create"/>.
///     </para>
///     <para>
///         <see cref="Invoke"/> never throws for bad input or a defective catalog: whatever the agent sends, the result is
///         text that says what was found or what to send instead. The tool reads the immutable catalog only, performs no
///         I/O, and is safe for concurrent use.
///     </para>
/// </remarks>
public sealed class HelpAgentTool
{
    /// <summary>
    ///     The JSON schema of the arguments, on one line. The text is part of the contract and is tested byte for byte.
    /// </summary>
    internal const string Schema =
        "{\"type\":\"object\",\"properties\":{"
        + "\"topic\":{\"type\":\"string\",\"description\":\"Card id or path such as svg/framed-figure; names and aliases work, case does not matter.\"},"
        + "\"example\":{\"type\":\"string\",\"description\":\"Example name, or card-id#name when several cards have an example of that name. With topic, only that card and the cards below it are searched.\"},"
        + "\"query\":{\"type\":\"string\",\"description\":\"Words to search for in titles, summaries, tags, aliases, and ids.\"},"
        + "\"source\":{\"type\":\"string\",\"description\":\"Restrict the index or the search to one source (library) name.\"},"
        + "\"tag\":{\"type\":\"string\",\"description\":\"Restrict the index or the search to cards with this tag.\"},"
        + "\"maxResults\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":20,\"description\":\"The most search results to return.\"}"
        + "},\"additionalProperties\":false}";

    /// <summary>
    ///     The text that follows a cut answer, up to the number of characters that were left out.
    /// </summary>
    private const string TruncationFormat = "\n\n[Truncated: {0} characters omitted. Ask for a narrower topic or an example.]";

    /// <summary>
    ///     Answers a call from checked arguments.
    /// </summary>
    private readonly Func<AgentArguments, string> _answer;

    /// <summary>
    ///     Gives the line that is appended after a cut answer, or <see langword="null"/> for none.
    /// </summary>
    private readonly Func<AgentArguments, string?>? _notice;

    /// <summary>
    ///     The most characters of an answer before it is cut.
    /// </summary>
    private readonly int _maxOutputChars;

    /// <summary>
    ///     Creates a tool.
    /// </summary>
    /// <param name="name">The name of the tool.</param>
    /// <param name="description">The description for the agent.</param>
    /// <param name="answer">Answers a call from checked arguments; the dispatcher of the catalog.</param>
    /// <param name="maxOutputChars">The most characters of an answer before it is cut.</param>
    /// <param name="notice">
    ///     Gives a line that must follow the answer even when the answer is cut, or <see langword="null"/> for none.
    /// </param>
    internal HelpAgentTool(
        string name,
        string description,
        Func<AgentArguments, string> answer,
        int maxOutputChars,
        Func<AgentArguments, string?>? notice = null)
    {
        Name = name;
        Description = description;
        _answer = answer;
        _maxOutputChars = maxOutputChars;
        _notice = notice;
    }

    /// <summary>
    ///     Gets the name of the tool as the agent calls it.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the description the agent reads: what the tool knows and the three ways to call it.
    /// </summary>
    public string Description { get; }

    /// <summary>
    ///     Gets the JSON schema of the arguments, on one line.
    /// </summary>
    /// <remarks>
    ///     The schema is a JSON object with the optional string properties <c>topic</c> (a card id or path),
    ///     <c>example</c> (an example name, or <c>card-id#name</c>), <c>query</c> (words to search for), <c>source</c>
    ///     and <c>tag</c> (filters for the index and the search), and the integer <c>maxResults</c> (1 to 20), with
    ///     <c>additionalProperties</c> false. Register it as the parameter schema of the tool with your agent
    ///     framework; the text is a constant, the same for every catalog.
    /// </remarks>
    public string ParametersSchema { get; } = Schema;

    /// <summary>
    ///     Answers one call of the tool.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The arguments are a JSON object with the optional properties <c>topic</c>, <c>example</c>, <c>query</c>,
    ///         <c>source</c>, <c>tag</c>, and <c>maxResults</c>; without any property the answer is the index. Arguments that
    ///         cannot be used, names that match nothing, and a catalog that cannot answer all give a plain-text message that
    ///         says what to send instead. An answer longer than the limit of the options is cut at a line end and says how
    ///         much was left out. The method does not catch out-of-memory, cancellation, or stack overflow conditions.
    ///     </para>
    ///     <para>
    ///         The three call shapes are: the index (no property, or only <c>source</c> and <c>tag</c>), a card or an
    ///         example (<c>topic</c>, <c>example</c>, or both), and a search (<c>query</c>). The <c>topic</c> is the
    ///         global id of a card, that is the id with the mount in front when the source was added with
    ///         <c>AddMountedSource</c> (<c>core/getting-started</c>). A bare local id (<c>getting-started</c>) also works
    ///         when exactly one card has it; when several do, the answer lists their global ids. A card is answered in
    ///         Markdown with its examples in full, links written in the plain style (<c>the card `id`</c>).
    ///     </para>
    /// </remarks>
    /// <example>
    ///     A catalog with one flat source and one call of each kind. The output follows the code.
    ///     <code language="csharp">
    /// using System;
    /// using System.Collections.Generic;
    /// using DemaConsulting.HelpCards.Catalog;
    /// using DemaConsulting.HelpCards.Integration;
    /// using DemaConsulting.HelpCards.Loading;
    ///
    /// HelpSource cards = HelpSource.FromFiles(new Dictionary&lt;string, string&gt;
    /// {
    ///     ["_source.md"] = "---\nname: demo\ntitle: Demo tool\nsummary: Help cards of the demo tool.\n---\nThe demo tool loads data files.",
    ///     ["getting-started.md"] = "---\ntitle: Getting started\nsummary: Install the tool and run it for the first time.\ntags:\n  - tutorial\nexamples:\n  - name: load\n    kind: command\n    language: shell\n    content: '{{tool}} load data.csv'\n    description: Load a data file.\n---\nInstall the package, then run [[#load]]. Next, see [[reports]].",
    ///     ["reports.md"] = "---\ntitle: Reports\nsummary: Write a report from loaded data.\n---\nReports are written as Markdown files.",
    /// }, name: "demo");
    ///
    /// HelpCatalog catalog = HelpCatalog.Create(builder => builder.AddSource(cards).ToolName("tool"));
    /// HelpAgentTool tool = HelpAgentTools.Create(catalog, new HelpAgentToolOptions { Name = "tool_help" });
    ///
    /// string?[] calls =
    /// [
    ///     null,                                          // the index
    ///     """{"topic":"getting-started"}""",             // a card
    ///     """{"example":"load"}""",                      // one example, found by name
    ///     """{"query":"report"}""",                      // a search
    ///     """{"topic":"nothing"}""",                     // a miss: the answer says what to send
    /// ];
    ///
    /// foreach (string? call in calls)
    /// {
    ///     Console.WriteLine($"-- {call ?? "(no arguments)"}");
    ///     Console.WriteLine(tool.Invoke(call));
    /// }
    /// </code>
    ///     <code language="text">
    /// -- (no arguments)
    /// Help index. Read a card with the topic argument, get an example with the example argument, or search with the query argument.
    ///
    /// getting-started  Install the tool and run it for the first time.
    /// reports          Write a report from loaded data.
    /// -- {"topic":"getting-started"}
    /// # Getting started
    ///
    /// Install the tool and run it for the first time.
    /// **Tags:** tutorial
    ///
    /// Install the package, then run the example `load`. Next, see the card `reports`.
    ///
    /// ## Examples
    ///
    /// ### load [command]
    ///
    /// Load a data file.
    ///
    /// ```shell
    /// tool load data.csv
    /// ```
    /// -- {"example":"load"}
    /// Example `load` of `getting-started` (command, shell): Load a data file.
    ///
    /// ```shell
    /// tool load data.csv
    /// ```
    /// -- {"query":"report"}
    /// Cards matching 'report':
    ///
    /// reports  Write a report from loaded data.
    ///
    /// Read one with the topic argument.
    /// -- {"topic":"nothing"}
    /// No help card matches 'nothing'. Call with no arguments for the index.
    /// </code>
    /// </example>
    /// <param name="argumentsJson">The arguments as JSON text, or <see langword="null"/> for none.</param>
    /// <returns>The answer; never <see langword="null"/>.</returns>
    public string Invoke(string? argumentsJson)
    {
        try
        {
            var read = AgentArgumentReader.Read(argumentsJson);
            if (read.Arguments is not { } arguments)
            {
                return Truncate(read.Error!, _maxOutputChars);
            }

            // The notice is appended after the cut, so a long answer cannot cut it away; its room is reserved
            var notice = _notice?.Invoke(arguments);
            var limit = notice is null ? _maxOutputChars : Math.Max(1, _maxOutputChars - notice.Length - 1);
            var answer = Truncate(_answer(arguments), limit);
            return notice is null ? answer : $"{answer}\n{notice}";
        }
        catch (Exception exception) when (exception is HelpCatalogException
            or ArgumentException
            or InvalidOperationException
            or KeyNotFoundException
            or FormatException
            or JsonException)
        {
            // The message of the exception is left out: it could hold text that the agent sent or a path
            return $"The help tool could not answer ({exception.GetType().Name}).";
        }
    }

    /// <summary>
    ///     Cuts an answer that exceeds the limit at the last line end at or before it, and says how much was left out.
    /// </summary>
    /// <param name="text">The answer.</param>
    /// <param name="limit">The most characters to keep.</param>
    /// <returns>The answer, cut when it was too long.</returns>
    private static string Truncate(string text, int limit)
    {
        if (text.Length <= limit)
        {
            return text;
        }

        var cut = text.LastIndexOf('\n', limit);
        if (cut < 0)
        {
            // A single very long line: cut at the limit without splitting a surrogate pair
            cut = char.IsHighSurrogate(text[limit - 1]) ? limit - 1 : limit;
        }

        var omitted = text.Length - cut;
        return text[..cut] + string.Format(CultureInfo.InvariantCulture, TruncationFormat, omitted);
    }

    /// <summary>
    ///     Writes the description of a tool from its catalog: what the sources hold and how to call it.
    /// </summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="toolName">The name of the product, or <see langword="null"/> for the tool name of the catalog.</param>
    /// <returns>The description.</returns>
    internal static string Describe(HelpCatalog catalog, string? toolName)
    {
        var product = toolName ?? catalog.Scope.Data.ToolName;
        var builder = new StringBuilder();
        builder.Append(product is null ? "Searchable help cards." : $"Searchable help cards for {product}.");
        builder.Append(" Sources:");
        foreach (var source in catalog.Sources)
        {
            builder.Append("\n- ").Append(source.Name).Append(": ").Append(source.Summary);
        }

        builder.Append("\nCall it with no arguments for the index of topics; with topic to read a card or example to get one example; ");
        builder.Append("or with query to search by words, optionally limited by source or tag.");
        return builder.ToString();
    }
}
