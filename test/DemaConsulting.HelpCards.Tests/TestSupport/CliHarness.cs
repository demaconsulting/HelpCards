using System.Globalization;
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Integration;

namespace DemaConsulting.HelpCards.Tests.TestSupport;

/// <summary>
///     A command-line helper wired to in-memory writers and a counting catalog factory, shared by the Integration tests.
/// </summary>
/// <remarks>
///     The harness records how often the factory was called and with which culture, and collects everything the helper
///     writes, so a test can assert on the output, the error output, the exit code, and whether any catalog was created.
/// </remarks>
internal sealed class CliHarness
{
    /// <summary>
    ///     Creates a harness over a catalog.
    /// </summary>
    /// <param name="catalog">The catalog the factory returns, or <see langword="null"/> to supply <paramref name="factory"/>.</param>
    /// <param name="factory">The factory to count, or <see langword="null"/> to return <paramref name="catalog"/>.</param>
    /// <param name="verb">The verb, or <see langword="null"/> for <c>help</c>.</param>
    /// <param name="language">Whether the language option is enabled.</param>
    /// <param name="usage">The usage lines, or <see langword="null"/>.</param>
    /// <param name="preamble">The index preamble, or <see langword="null"/>.</param>
    /// <param name="sections">The index sections, or <see langword="null"/>.</param>
    /// <param name="fallback">The fallback resolver, or <see langword="null"/>.</param>
    /// <param name="width">The console width the provider reports, or <see langword="null"/> for no provider.</param>
    /// <param name="defaultWidth">The default width, or <see langword="null"/> for the default of the options.</param>
    /// <param name="template">The command template, or <see langword="null"/>.</param>
    public CliHarness(
        HelpCatalog? catalog = null,
        Func<CultureInfo?, HelpCatalog>? factory = null,
        string? verb = null,
        bool language = false,
        IReadOnlyList<string>? usage = null,
        string? preamble = null,
        IReadOnlyList<HelpCliSection>? sections = null,
        Func<IReadOnlyList<string>, HelpCliAnswer?>? fallback = null,
        Func<int?>? width = null,
        int? defaultWidth = null,
        string? template = null)
    {
        Func<CultureInfo?, HelpCatalog> inner = factory ?? (_ => catalog!);
        var options = new HelpCliOptions
        {
            ToolName = "tool",
            Version = "1.0",
            Verb = verb ?? "help",
            CatalogFactory = culture =>
            {
                FactoryCalls++;
                LastCulture = culture;
                return inner(culture);
            },
            UsageLines = usage,
            IndexPreamble = preamble,
            IndexSections = sections,
            FallbackResolver = fallback,
            Output = Output,
            Error = Error,
            EnableLanguageOption = language,
            OutputWidthProvider = width,
            CommandTemplate = template,
            DefaultWidth = defaultWidth ?? 78,
        };
        Cli = HelpCli.Create(options);
    }

    /// <summary>
    ///     Gets the helper.
    /// </summary>
    public HelpCli Cli { get; }

    /// <summary>
    ///     Gets what the helper wrote to its output.
    /// </summary>
    public StringWriter Output { get; } = new() { NewLine = "\r\n" };

    /// <summary>
    ///     Gets what the helper wrote to its error output.
    /// </summary>
    public StringWriter Error { get; } = new() { NewLine = "\r\n" };

    /// <summary>
    ///     Gets how many times the catalog factory was called.
    /// </summary>
    public int FactoryCalls { get; private set; }

    /// <summary>
    ///     Gets the culture the factory received last.
    /// </summary>
    public CultureInfo? LastCulture { get; private set; }

    /// <summary>
    ///     Runs a help command whose verb is already removed.
    /// </summary>
    /// <param name="arguments">The arguments after the verb.</param>
    /// <returns>The exit code.</returns>
    public int Run(params string[] arguments)
    {
        return Cli.Run(arguments);
    }
}
