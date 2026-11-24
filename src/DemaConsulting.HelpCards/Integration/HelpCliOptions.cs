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
    public required string ToolName { get; init; }

    /// <summary>
    ///     Gets the version of the tool for information, or <see langword="null"/> when it has none. The helper does not
    ///     print it: the version that the help shows comes from <c>HelpCatalogBuilder.ToolVersion</c> of the catalog
    ///     that <see cref="CatalogFactory"/> creates.
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
    public string? CommandTemplate { get; init; }

    /// <summary>
    ///     Gets the width at which text is wrapped when the command line does not say; 78 unless set, and 0 for no
    ///     wrapping, otherwise 20 to 500.
    /// </summary>
    public int DefaultWidth { get; init; } = 78;

    /// <summary>
    ///     Gets a value indicating whether the <c>--lang</c> option is accepted; <see langword="false"/> unless set.
    /// </summary>
    public bool EnableLanguageOption { get; init; }

    /// <summary>
    ///     Gets the function that reports the width of the console for <c>--width auto</c>, or <see langword="null"/>
    ///     for the console when the helper writes to it and no width otherwise. It returns <see langword="null"/> when
    ///     there is no console width, for example when the output is redirected.
    /// </summary>
    public Func<int?>? OutputWidthProvider { get; init; }
}
