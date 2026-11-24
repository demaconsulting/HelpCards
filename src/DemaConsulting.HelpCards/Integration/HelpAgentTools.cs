using System.Text.RegularExpressions;
using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     Creates the knowledge tool for a catalog.
/// </summary>
/// <remarks>
///     One tool serves the whole catalog; a catalog that combines several libraries is still one tool, and its sources
///     are told apart by the <c>source</c> argument. Per-source tools are not part of this release. To serve several
///     libraries, add each <c>HelpSource</c> to one catalog with <c>AddMountedSource</c>; the agent then uses global
///     ids (<c>core/getting-started</c>) as <c>topic</c>. The complete three-library sample, with its output, is on
///     the page of <c>DemaConsulting.HelpCards.Integration</c>; the sample answers of each call shape are in the
///     remarks of <see cref="HelpAgentTool.Invoke"/>.
/// </remarks>
public static partial class HelpAgentTools
{
    /// <summary>
    ///     The least and most values of the numeric options.
    /// </summary>
    private const int MinResults = 1;
    private const int MaxResults = 20;
    private const int MinChars = 1000;
    private const int MaxChars = 200000;
    private const int MaxDepth = 8;

    /// <summary>
    ///     Creates the tool for a catalog.
    /// </summary>
    /// <remarks>
    ///     The catalog is read when the tool is called and again when its description is generated, never changed. The
    ///     description is generated here, from the sources of the catalog, when the options give none.
    /// </remarks>
    /// <param name="catalog">The catalog. Must not be null.</param>
    /// <param name="options">The options, or <see langword="null"/> for the defaults.</param>
    /// <returns>The tool.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="catalog"/> is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when the name does not match <c>^[a-z][a-z0-9_]{0,63}$</c>, or the description or the tool name of the
    ///     options is white space.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the maximum number of results is not 1 to 20, the output limit not 1000 to 200000, or the index
    ///     depth not 0 to 8.
    /// </exception>
    public static HelpAgentTool Create(HelpCatalog catalog, HelpAgentToolOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        options ??= new HelpAgentToolOptions();
        if (options.Name is null || !NamePattern().IsMatch(options.Name))
        {
            throw new ArgumentException("The tool name must match ^[a-z][a-z0-9_]{0,63}$.", nameof(options));
        }

        if (options.Description is not null && string.IsNullOrWhiteSpace(options.Description))
        {
            throw new ArgumentException("The description must not be white space.", nameof(options));
        }

        if (options.ToolName is not null && string.IsNullOrWhiteSpace(options.ToolName))
        {
            throw new ArgumentException("The tool name must not be white space.", nameof(options));
        }

        CheckRange(options.MaxResults, MinResults, MaxResults, "MaxResults");
        CheckRange(options.MaxOutputChars, MinChars, MaxChars, "MaxOutputChars");
        CheckRange(options.IndexDepth, 0, MaxDepth, "IndexDepth");

        var description = options.Description ?? HelpAgentTool.Describe(catalog, options.ToolName);
        var dispatcher = new AgentDispatcher(catalog, options);
        return new HelpAgentTool(options.Name, description, dispatcher.Answer, options.MaxOutputChars, AgentDispatcher.IgnoredNotice);
    }

    /// <summary>
    ///     Gets the pattern of a valid tool name.
    /// </summary>
    /// <returns>The pattern.</returns>
    [GeneratedRegex("^[a-z][a-z0-9_]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex NamePattern();

    /// <summary>
    ///     Checks that an option is within its range.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="minimum">The least value.</param>
    /// <param name="maximum">The most value.</param>
    /// <param name="option">The name of the option, for the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is outside the range.</exception>
    private static void CheckRange(int value, int minimum, int maximum, string option)
    {
        if (value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(option, value, $"The option must be from {minimum} to {maximum}.");
        }
    }
}
