using System.Globalization;
using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     The settings of a command-line help helper.
/// </summary>
/// <remarks>
///     Only <see cref="ToolName"/> and <see cref="CatalogFactory"/> are required. The options object is kept by the
///     helper, not copied: the checks of the required values and the verb are made when the helper is created, and the
///     writers, usage lines, sections, fallback, and the other settings are read from the object each time a command
///     runs. Do not change an options object after you created a helper from it. The catalog factory is not called
///     when the helper is created.
/// </remarks>
public sealed class HelpCliOptions
{
    /// <summary>
    ///     Gets the name of the tool as the user types it; it appears in usage text, hints, and commands.
    /// </summary>
    /// <remarks>
    ///     This is the name in the printed commands (<c>tool help ...</c>). The catalog has its own tool name,
    ///     <c>HelpCatalogBuilder.ToolName</c>, which is the value of the <c>{{tool}}</c> token in the cards; set both to
    ///     the same value.
    /// </remarks>
    public required string ToolName { get; init; }

    /// <summary>
    ///     Gets the version of the tool for information, or <see langword="null"/> when it has none. The helper does not
    ///     print it: the version that the help shows (the <c>{{version}}</c> token and the <c>version</c> field of the JSON
    ///     documents) comes from <c>HelpCatalogBuilder.ToolVersion</c> of the catalog that <see cref="CatalogFactory"/>
    ///     creates. Set both to the same value.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    ///     Gets the word that starts help on the command line; <c>help</c> unless set.
    /// </summary>
    public string Verb { get; init; } = "help";

    /// <summary>
    ///     Gets the function that creates the catalog; it receives the culture of the <c>--lang</c> option, or
    ///     <see langword="null"/> when there is none. It is called only after the verb was recognized, once for each
    ///     help command that needs a catalog.
    /// </summary>
    public required Func<CultureInfo?, HelpCatalog> CatalogFactory { get; init; }

    /// <summary>
    ///     Gets the usage lines written at the top of the index, or <see langword="null"/> for none.
    /// </summary>
    public IReadOnlyList<string>? UsageLines { get; init; }

    /// <summary>
    ///     Gets the text written between the usage lines and the sections of the index, or <see langword="null"/>.
    /// </summary>
    public string? IndexPreamble { get; init; }

    /// <summary>
    ///     Gets the sections written between the preamble and the topics of the index, or <see langword="null"/>.
    /// </summary>
    public IReadOnlyList<HelpCliSection>? IndexSections { get; init; }

    /// <summary>
    ///     Gets the function that is offered the path words of a card command before the catalog is searched, or
    ///     <see langword="null"/>. It returns an answer for a subject that the tool owns, and <see langword="null"/> to
    ///     let the catalog answer.
    /// </summary>
    public Func<IReadOnlyList<string>, HelpCliAnswer?>? FallbackResolver { get; init; }

    /// <summary>
    ///     Gets the writer for output, or <see langword="null"/> for the console output at the time of the command.
    /// </summary>
    public TextWriter? Output { get; init; }

    /// <summary>
    ///     Gets the writer for text and Markdown errors, or <see langword="null"/> for the console error at the time of
    ///     the command.
    /// </summary>
    public TextWriter? Error { get; init; }

    /// <summary>
    ///     Gets the template of the command that prints a card, such as <c>{tool} help {path}</c>, or
    ///     <see langword="null"/> for the tool name, the verb, and the path.
    /// </summary>
    /// <remarks>
    ///     The helper writes every link in the command style, so the template decides how card links read in the
    ///     printed help: with the default, <c>[[getting-started/install]]</c> reads <c>`tool help getting-started install`</c>
    ///     (<c>{path}</c> is the card id with each <c>/</c> replaced by a space), and a link to an example of the same
    ///     card reads <c>`--example load`</c>. The plain style (<c>the card `id`</c>) is not used by the helper.
    /// </remarks>
    public string? CommandTemplate { get; init; }

    /// <summary>
    ///     Gets the width at which text is wrapped when the command line does not say; 78 unless set, and 0 for no
    ///     wrapping, otherwise 20 to 500.
    /// </summary>
    public int DefaultWidth { get; init; } = 78;

    /// <summary>
    ///     Gets a value indicating whether the <c>--lang</c> option is accepted; <see langword="false"/> unless set.
    /// </summary>
    /// <remarks>
    ///     When it is <see langword="false"/>, <c>--lang</c> is rejected like any unknown option (usage error, exit code
    ///     2) and the catalog factory always receives <see langword="null"/>. When it is <see langword="true"/>,
    ///     <c>--lang de-DE</c> passes that culture to <see cref="CatalogFactory"/>.
    /// </remarks>
    public bool EnableLanguageOption { get; init; }

    /// <summary>
    ///     Gets the function that reports the width of the console for <c>--width auto</c>, or <see langword="null"/>
    ///     for the console when the helper writes to it and no width otherwise. It returns <see langword="null"/> when
    ///     there is no console width, for example when the output is redirected.
    /// </summary>
    public Func<int?>? OutputWidthProvider { get; init; }
}
