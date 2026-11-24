using System.Text.RegularExpressions;
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Testing;

/// <summary>
///     The quality rules of the validator.
/// </summary>
public static partial class HelpCardValidator
{
    /// <summary>
    ///     The headings that the renderer writes itself, which a body must not contain at level two.
    /// </summary>
    private static readonly string[] ReservedHeadings = ["Cards", "Examples", "See also"];

    /// <summary>
    ///     Applies every quality rule and every rule of the options to the visible cards of a catalog.
    /// </summary>
    /// <param name="catalog">The catalog or view.</param>
    /// <param name="options">The limits and switches.</param>
    /// <returns>The warnings, the problems of the rules of the options, and the missing-source errors.</returns>
    private static List<HelpProblem> CheckCatalog(HelpCatalog catalog, HelpValidationOptions options)
    {
        var problems = new List<HelpProblem>();
        var data = catalog.Scope.Data;
        var tool = data.ToolName ?? data.Sources[0].Name;
        foreach (var card in catalog.Cards)
        {
            CheckSummary(card, options, problems);
            CheckSize(catalog, card, options, problems);
            CheckBody(card, options, problems);
            CheckCommands(card, tool, problems);
            foreach (var rule in options.Rules)
            {
                problems.AddRange(rule.Check(card) ?? []);
            }
        }

        if (options.RequireExternalSources)
        {
            problems.AddRange(catalog.Diagnostics
                .Where(problem => problem.RuleId == RuleIds.ExternalSourceAbsent)
                .Select(CreateMissingSource));
        }

        return problems;
    }

    /// <summary>
    ///     Reports the tokens that a source defines but no file of the source uses.
    /// </summary>
    /// <remarks>
    ///     Every file of the source is scanned, because a token is replaced in the title, summary, examples, and
    ///     body of any card and overlay. The built-in tokens are never reported. Registered sources and the
    ///     sources of the composed catalog are matched by position.
    /// </remarks>
    /// <param name="sources">The sources in registration order.</param>
    /// <param name="infos">The descriptions of the composed sources, in the same order.</param>
    /// <returns>One warning for every unused token.</returns>
    private static IEnumerable<HelpProblem> CheckTokens(IReadOnlyList<HelpSource> sources, IReadOnlyList<HelpSourceInfo> infos)
    {
        // A token of a source may be used by the sources that import it, so usage is collected over all sources
        if (sources.All(source => source.Tokens.Count == 0))
        {
            yield break;
        }

        var used = sources
            .SelectMany(source => source.Provider.ListFiles().SelectMany(path => TokenResolver.FindNames(source.Provider.ReadText(path))))
            .ToHashSet(StringComparer.Ordinal);
        for (var index = 0; index < sources.Count && index < infos.Count; index++)
        {
            var source = sources[index];
            if (source.Tokens.Count == 0)
            {
                continue;
            }

            foreach (var name in source.Tokens.Keys.Order(StringComparer.Ordinal).Where(name => !used.Contains(name)))
            {
                yield return Warning(
                    RuleIds.UnusedToken,
                    infos[index].Name,
                    string.Empty,
                    0,
                    $"The token '{name}' is defined for the source but no file uses it; remove it or write {{{{{name}}}}} where it belongs.");
            }
        }
    }

    /// <summary>
    ///     Checks that a summary is within the limit.
    /// </summary>
    /// <remarks>
    ///     A summary is a single line by construction: the front-matter decoder does not allow a line break in a
    ///     scalar, so a summary that spans lines is already an error of the build.
    /// </remarks>
    /// <param name="card">The card.</param>
    /// <param name="options">The limits.</param>
    /// <param name="problems">Receives the warning.</param>
    private static void CheckSummary(HelpCard card, HelpValidationOptions options, List<HelpProblem> problems)
    {
        var limit = card.ParentId is null ? options.MaxRootSummaryChars : options.MaxSummaryChars;
        if (card.Summary.Length > limit)
        {
            problems.Add(Warning(
                RuleIds.SummaryLength,
                card.SourceName,
                card.File,
                0,
                $"The summary of '{card.Id}' has {card.Summary.Length} characters; shorten it to at most {limit}."));
        }
    }

    /// <summary>
    ///     Checks that the rendered text of a card fits the size limit.
    /// </summary>
    /// <remarks>
    ///     The size is the length of the card rendered as text with the default options. A root card that has
    ///     cards below it is an index page and has its own limit.
    /// </remarks>
    /// <param name="catalog">The catalog that renders the card.</param>
    /// <param name="card">The card.</param>
    /// <param name="options">The limits.</param>
    /// <param name="problems">Receives the warning.</param>
    private static void CheckSize(HelpCatalog catalog, HelpCard card, HelpValidationOptions options, List<HelpProblem> problems)
    {
        var limit = card.ParentId is null && card.ChildIds.Count > 0 ? options.MaxRootIndexChars : options.MaxCardChars;
        var length = catalog.Render(card).Length;
        if (length > limit)
        {
            problems.Add(Warning(
                RuleIds.CardSize,
                card.SourceName,
                card.File,
                0,
                $"The card '{card.Id}' renders as {length} characters of text; shorten it to at most {limit} or split it."));
        }
    }

    /// <summary>
    ///     Checks the headings, the fenced blocks, and the line length of a body.
    /// </summary>
    /// <param name="card">The card.</param>
    /// <param name="options">The limits.</param>
    /// <param name="problems">Receives the warnings.</param>
    private static void CheckBody(HelpCard card, HelpValidationOptions options, List<HelpProblem> problems)
    {
        // The first body line is at BodyLine in the file, and links never change the number of lines
        var lines = HelpCardMarkdown.Scan(card.Body, out var unclosedLine);
        foreach (var line in lines)
        {
            var fileLine = card.BodyLine + line.Number - 1;
            if (line.Kind == MarkdownLineKind.Text)
            {
                CheckHeading(card, line, fileLine, problems);
            }
            else if (line.Kind == MarkdownLineKind.FenceOpen && line.Info.Length == 0)
            {
                problems.Add(Warning(
                    RuleIds.FenceLanguage,
                    card.SourceName,
                    card.BodyFile,
                    fileLine,
                    "A fenced code block names no language; write the language after the opening fence, such as ```text."));
            }

            if (options.MaxLineLength is { } max && line.Text.Length > max)
            {
                problems.Add(Warning(
                    RuleIds.LineLength,
                    card.SourceName,
                    card.BodyFile,
                    fileLine,
                    $"The line has {line.Text.Length} characters; wrap it at {max}."));
            }
        }

        if (unclosedLine > 0)
        {
            problems.Add(Warning(
                RuleIds.FenceUnclosed,
                card.SourceName,
                card.BodyFile,
                card.BodyLine + unclosedLine - 1,
                "The fenced code block is never closed; add a closing fence with at least as many characters."));
        }
    }

    /// <summary>
    ///     Checks that a line outside fences is not a heading that the renderer reserves.
    /// </summary>
    /// <param name="card">The card.</param>
    /// <param name="line">The line.</param>
    /// <param name="fileLine">The line number in the file.</param>
    /// <param name="problems">Receives the warning.</param>
    private static void CheckHeading(HelpCard card, MarkdownLine line, int fileLine, List<HelpProblem> problems)
    {
        if (!HelpCardMarkdown.TryParseHeading(line.Text, out var level, out var text))
        {
            return;
        }

        if (level == 1)
        {
            problems.Add(Warning(
                RuleIds.HeadingReserved,
                card.SourceName,
                card.BodyFile,
                fileLine,
                "A level-one heading is reserved for the title of the card; start the headings of a body at '##'."));
        }
        else if (level == 2 && ReservedHeadings.Contains(text, StringComparer.Ordinal))
        {
            problems.Add(Warning(
                RuleIds.HeadingReserved,
                card.SourceName,
                card.BodyFile,
                fileLine,
                $"The heading '## {text}' is written by the renderer; use another heading."));
        }
    }

    /// <summary>
    ///     Checks that every command example parses with <see cref="ExampleCommandLine"/>.
    /// </summary>
    /// <param name="card">The card.</param>
    /// <param name="tool">The tool name that every command starts with.</param>
    /// <param name="problems">Receives the warnings.</param>
    private static void CheckCommands(HelpCard card, string tool, List<HelpProblem> problems)
    {
        foreach (var example in card.Examples.Where(example => example.Kind == HelpExample.CommandKind))
        {
            if (!ExampleCommandLine.TryParse(example.Content, tool, out _, out var error))
            {
                problems.Add(Warning(
                    RuleIds.CommandParse,
                    card.SourceName,
                    card.File,
                    0,
                    $"The command example '{example.Name}' of '{card.Id}' cannot be run as written: {error}."));
            }
        }
    }

    /// <summary>
    ///     Turns a note about an absent external source into an error.
    /// </summary>
    /// <param name="note">The informational problem that the catalog recorded.</param>
    /// <returns>The error with the same place.</returns>
    private static HelpProblem CreateMissingSource(HelpProblem note)
    {
        var match = AbsentSourcePattern().Match(note.Message);
        var name = match.Success ? $"'{match.Groups["name"].Value}'" : "of a link";
        return HelpProblem.CreateError(
            RuleIds.ExternalSourceMissing,
            note.SourceName,
            note.File,
            note.Line,
            $"The external source {name} is required but is not part of the catalog.");
    }

    /// <summary>
    ///     Creates a warning.
    /// </summary>
    /// <param name="ruleId">The rule.</param>
    /// <param name="sourceName">The source.</param>
    /// <param name="file">The file, or an empty string.</param>
    /// <param name="line">The line, or 0.</param>
    /// <param name="message">The message.</param>
    /// <returns>The warning.</returns>
    private static HelpProblem Warning(string ruleId, string sourceName, string file, int line, string message)
    {
        return new HelpProblem(HelpSeverity.Warning, ruleId, sourceName, file, line, message);
    }

    /// <summary>
    ///     Matches the start of the message of an absent-source note to read the name of the source.
    /// </summary>
    /// <returns>The pattern.</returns>
    [GeneratedRegex("^The source '(?<name>[^']*)'", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex AbsentSourcePattern();
}
