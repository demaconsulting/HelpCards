using System.Text.RegularExpressions;
using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     Replaces <c>{{name}}</c> tokens in the text of one source with the values that are in scope for it.
/// </summary>
/// <remarks>
///     <para>
///         The scope of a source is its own tokens, the tokens of every source it imports (transitively), and
///         the built-ins <c>tool</c> and <c>version</c>. A name that is defined twice in one scope, or that
///         redefines a built-in, is reported as <c>token-redefined</c> when the resolver is created; the
///         first definition wins so reading can continue and report further defects. A source never sees the
///         tokens of a source it does not import. Names are case-sensitive and use ASCII letters and digits.
///     </para>
///     <para>
///         An unknown token is reported as <c>token-unknown</c> at its line and left in the text. Text that
///         only looks like a token (<c>{{ x }}</c>, <c>{{a-b}}</c>) is not a token and is left alone. There is
///         no escape for a literal token. A resolver is immutable after creation and safe for concurrent use.
///     </para>
/// </remarks>
internal sealed partial class TokenResolver
{
    /// <summary>
    ///     The name of the built-in token that holds the name of the tool.
    /// </summary>
    public const string ToolToken = "tool";

    /// <summary>
    ///     The name of the built-in token that holds the version of the tool.
    /// </summary>
    public const string VersionToken = "version";

    /// <summary>
    ///     The values by token name for the source.
    /// </summary>
    private readonly Dictionary<string, string> _values;

    /// <summary>
    ///     The name of the source the resolver belongs to, for problems.
    /// </summary>
    private readonly string _sourceName;

    /// <summary>
    ///     Creates a resolver.
    /// </summary>
    /// <param name="sourceName">The name of the source.</param>
    /// <param name="values">The values in scope.</param>
    /// <param name="scopeProblems">The problems found while building the scope.</param>
    private TokenResolver(string sourceName, Dictionary<string, string> values, IReadOnlyList<HelpProblem> scopeProblems)
    {
        _sourceName = sourceName;
        _values = values;
        ScopeProblems = scopeProblems;
    }

    /// <summary>
    ///     Gets the <c>token-redefined</c> problems found while the scope was built.
    /// </summary>
    public IReadOnlyList<HelpProblem> ScopeProblems { get; }

    /// <summary>
    ///     Determines whether a name is a valid token name.
    /// </summary>
    /// <param name="name">The name to test.</param>
    /// <returns><see langword="true"/> when the name is one or more ASCII letters and digits.</returns>
    public static bool IsValidTokenName(string? name)
    {
        return !string.IsNullOrEmpty(name) && name.All(char.IsAsciiLetterOrDigit);
    }

    /// <summary>
    ///     Builds the token scope of a source.
    /// </summary>
    /// <param name="sourceName">The name of the source the scope is for.</param>
    /// <param name="ownTokens">The tokens of the source itself.</param>
    /// <param name="importedTokens">
    ///     The tokens of every imported source (transitively) in a stable order, each with the imported source's name.
    /// </param>
    /// <param name="tool">The value of the built-in <c>tool</c> token.</param>
    /// <param name="version">The value of the built-in <c>version</c> token; empty when unknown.</param>
    /// <returns>The resolver; its <see cref="ScopeProblems"/> lists the redefinitions.</returns>
    public static TokenResolver Create(
        string sourceName,
        IReadOnlyDictionary<string, string> ownTokens,
        IEnumerable<KeyValuePair<string, IReadOnlyDictionary<string, string>>> importedTokens,
        string tool,
        string version)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ToolToken] = tool,
            [VersionToken] = version
        };
        var owners = new Dictionary<string, string>(StringComparer.Ordinal);
        var problems = new List<HelpProblem>();

        // Own tokens first, then imports in order; names are visited in ordinal order so messages are stable
        Add(sourceName, ownTokens);
        foreach (var (importedName, tokens) in importedTokens)
        {
            Add(importedName, tokens);
        }

        return new TokenResolver(sourceName, values, problems);

        void Add(string owner, IReadOnlyDictionary<string, string> tokens)
        {
            foreach (var name in tokens.Keys.Order(StringComparer.Ordinal))
            {
                if (name is ToolToken or VersionToken)
                {
                    problems.Add(Redefined(sourceName, $"The token '{name}' of source '{owner}' is built in and cannot be redefined."));
                }
                else if (owners.TryGetValue(name, out var first))
                {
                    problems.Add(Redefined(sourceName, $"The token '{name}' is defined by both source '{first}' and source '{owner}'."));
                }
                else
                {
                    owners[name] = owner;
                    values[name] = tokens[name];
                }
            }
        }
    }

    /// <summary>
    ///     Replaces every known token in a text and reports every unknown one.
    /// </summary>
    /// <param name="text">The text, such as a title, a summary, an example, or a body.</param>
    /// <param name="file">The path of the file the text came from, for problems.</param>
    /// <param name="firstLine">The one-based line of the file the text starts on.</param>
    /// <param name="problems">Receives a <c>token-unknown</c> problem for each unknown token.</param>
    /// <returns>The text with known tokens replaced; unknown tokens stay as written.</returns>
    public string Substitute(string text, string file, int firstLine, ICollection<HelpProblem> problems)
    {
        // Most texts contain no token, so skip the scan for them
        if (!text.Contains("{{", StringComparison.Ordinal))
        {
            return text;
        }

        return TokenPattern().Replace(text, match =>
        {
            var name = match.Groups["name"].Value;
            if (_values.TryGetValue(name, out var value))
            {
                return value;
            }

            // The line is the first line of the text plus the line feeds before the token
            var line = firstLine + text.AsSpan(0, match.Index).Count('\n');
            problems.Add(HelpProblem.CreateError(
                RuleIds.TokenUnknown,
                _sourceName,
                file,
                line,
                $"The token '{match.Value}' is not defined; define '{name}' with HelpSource.WithTokens or correct the name."));
            return match.Value;
        });
    }

    /// <summary>
    ///     Lists the names of the well-formed tokens that a text uses.
    /// </summary>
    /// <remarks>
    ///     The validator uses this to find tokens that a source defines but no file uses. Built-in and unknown
    ///     names are listed like any other.
    /// </remarks>
    /// <param name="text">The text to scan.</param>
    /// <returns>The distinct token names in the text.</returns>
    public static IEnumerable<string> FindNames(string text)
    {
        return text.Contains("{{", StringComparison.Ordinal)
            ? TokenPattern().Matches(text).Select(match => match.Groups["name"].Value).Distinct(StringComparer.Ordinal)
            : [];
    }

    /// <summary>
    ///     Matches a well-formed token.
    /// </summary>
    /// <returns>The pattern.</returns>
    [GeneratedRegex(@"\{\{(?<name>[A-Za-z0-9]+)\}\}", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex TokenPattern();

    /// <summary>
    ///     Creates a <c>token-redefined</c> problem for a source.
    /// </summary>
    /// <param name="sourceName">The name of the source whose scope is wrong.</param>
    /// <param name="message">The message.</param>
    /// <returns>The problem.</returns>
    private static HelpProblem Redefined(string sourceName, string message)
    {
        return HelpProblem.CreateError(RuleIds.TokenRedefined, sourceName, string.Empty, 0, message);
    }
}
