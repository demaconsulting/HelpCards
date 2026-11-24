using System.Globalization;
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Composition;

namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     The settings of a command-line help helper.
/// </summary>
/// <remarks>
///     <para>
///         Only <see cref="ToolName"/> and one of <see cref="ConfigureCatalog"/> and <see cref="CatalogFactory"/> are
///         required. <see cref="ConfigureCatalog"/> is the recommended way to say how the catalog is built: the helper
///         applies <see cref="ToolName"/> and <see cref="Version"/> to the catalog builder itself, so each is set once,
///         and <see cref="GetCatalogConfiguration"/> gives the same configuration to the tests of the tool. The older
///         <see cref="CatalogFactory"/> still works and builds the whole catalog by itself.
///     </para>
///     <para>
///         The options object is kept by the helper, not copied: the checks of the required values and the verb are
///         made when the helper is created, and the writers, usage lines, sections, fallback, and the other settings
///         are read from the object each time a command runs. Do not change an options object after you created a
///         helper from it. The catalog is not built when the helper is created.
///     </para>
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
    ///     Gets the version of the tool, or <see langword="null"/> when it has none.
    /// </summary>
    /// <remarks>
    ///     With <see cref="ConfigureCatalog"/> the helper sets it as <c>HelpCatalogBuilder.ToolVersion</c>, so it is the
    ///     <c>{{version}}</c> token of the cards and the <c>version</c> field of the JSON documents, and you set it
    ///     only here. With <see cref="CatalogFactory"/> the helper does not print it: the version that the help shows
    ///     comes from <c>ToolVersion</c> of the catalog that the factory creates, and you set both to the same value.
    /// </remarks>
    public string? Version { get; init; }

    /// <summary>
    ///     Gets the word that starts help on the command line; <c>help</c> unless set.
    /// </summary>
    public string Verb { get; init; } = "help";

    /// <summary>
    ///     Gets the action that configures the catalog builder, or <see langword="null"/> when <see cref="CatalogFactory"/>
    ///     is used. Set exactly one of the two.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The helper builds the catalog with <c>HelpCatalog.Create</c> only after the verb was recognized, once for
    ///         each help command that needs a catalog. It first sets <c>ToolName</c> from <see cref="ToolName"/>,
    ///         <c>ToolVersion</c> from <see cref="Version"/> when that is set, and <c>Culture</c> from the culture of the
    ///         <c>--lang</c> option when there is one, and then calls this action with the builder and that culture
    ///         (<see langword="null"/> when there is none), so the action adds the sources and may override anything
    ///         that the helper set.
    ///     </para>
    ///     <para>
    ///         The tests of the tool use <see cref="GetCatalogConfiguration"/> to validate exactly this configuration.
    ///     </para>
    /// </remarks>
    public Action<HelpCatalogBuilder, CultureInfo?>? ConfigureCatalog { get; init; }

    /// <summary>
    ///     Gets the function that creates the catalog, or <see langword="null"/> when <see cref="ConfigureCatalog"/> is
    ///     used. Set exactly one of the two.
    /// </summary>
    /// <remarks>
    ///     The function receives the culture of the <c>--lang</c> option, or <see langword="null"/> when there is none.
    ///     It is called only after the verb was recognized, once for each help command that needs a catalog. It builds
    ///     the whole catalog, so the tool name and the version of the builder are set by the function and not taken
    ///     from <see cref="ToolName"/> and <see cref="Version"/>; prefer <see cref="ConfigureCatalog"/>.
    /// </remarks>
    public Func<CultureInfo?, HelpCatalog>? CatalogFactory { get; init; }

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
    ///     Not combined with <see cref="OutputProvider"/>.
    /// </summary>
    public TextWriter? Output { get; init; }

    /// <summary>
    ///     Gets the function that returns the writer for output when a command writes, or <see langword="null"/>.
    ///     Not combined with <see cref="Output"/>.
    /// </summary>
    /// <remarks>
    ///     The function is called each time the helper writes (once for an answer), so the writer can depend on the
    ///     state of the tool at that moment, for example <c>OutputProvider = () => silent ? TextWriter.Null : null</c>.
    ///     A <see langword="null"/> result means the console output at that time. A provider counts as a writer that is
    ///     not the console for <c>--width auto</c>; give <see cref="OutputWidthProvider"/> to report a width.
    /// </remarks>
    public Func<TextWriter?>? OutputProvider { get; init; }

    /// <summary>
    ///     Gets the writer for text and Markdown errors, or <see langword="null"/> for the console error at the time of
    ///     the command. Not combined with <see cref="ErrorProvider"/>.
    /// </summary>
    public TextWriter? Error { get; init; }

    /// <summary>
    ///     Gets the function that returns the writer for errors when a command writes, or <see langword="null"/>.
    ///     Not combined with <see cref="Error"/>.
    /// </summary>
    /// <remarks>
    ///     The function is called each time the helper writes (once for an error); a <see langword="null"/> result means
    ///     the console error at that time. JSON errors are documents and go to the output writer.
    /// </remarks>
    public Func<TextWriter?>? ErrorProvider { get; init; }

    /// <summary>
    ///     Gets the template of the command that prints a card, such as <c>{tool} help {path}</c>, or
    ///     <see langword="null"/> for the tool name, the verb, and the path.
    /// </summary>
    /// <remarks>
    ///     The helper writes every link in the command style, so the template decides how card links read in the
    ///     printed help: with the default, <c>[[getting-started/install]]</c> reads <c>`tool help getting-started install`</c>
    ///     (<c>{path}</c> is the card id with each <c>/</c> replaced by a space), and a link to an example reads the full
    ///     command, <c>`tool help getting-started --example load`</c>, also for an example of the card that holds the
    ///     link. The plain style (<c>the card `id`</c>) is not used by the helper.
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
    ///     2) and the catalog is built without a culture from the option. When it is <see langword="true"/>,
    ///     <c>--lang de-DE</c> passes that culture to <see cref="ConfigureCatalog"/> or <see cref="CatalogFactory"/>.
    /// </remarks>
    public bool EnableLanguageOption { get; init; }

    /// <summary>
    ///     Gets the function that reports the width of the console for <c>--width auto</c>, or <see langword="null"/>
    ///     for the console when the helper writes to it (no <see cref="Output"/> and no <see cref="OutputProvider"/>) and
    ///     no width otherwise. It returns <see langword="null"/> when
    ///     there is no console width, for example when the output is redirected.
    /// </summary>
    public Func<int?>? OutputWidthProvider { get; init; }

    /// <summary>
    ///     Gets the action that configures a catalog builder exactly as the helper does, for the tests of the tool.
    /// </summary>
    /// <remarks>
    ///     The action sets <c>ToolName</c> from <see cref="ToolName"/>, <c>ToolVersion</c> from <see cref="Version"/> when
    ///     that is set, and <c>Culture</c> when a culture is given, and then calls <see cref="ConfigureCatalog"/>. Pass it
    ///     to <c>HelpCardValidator.Validate</c> or <c>AssertValid</c> so that the cards are validated in the same
    ///     composition that users get:
    ///     <c>HelpCardValidator.AssertValid(options.GetCatalogConfiguration())</c>.
    /// </remarks>
    /// <param name="culture">The culture of the <c>--lang</c> option to build the catalog for, or <see langword="null"/>.</param>
    /// <returns>The action that configures a builder.</returns>
    /// <exception cref="ArgumentException">Thrown when <see cref="ConfigureCatalog"/> is not set.</exception>
    public Action<HelpCatalogBuilder> GetCatalogConfiguration(CultureInfo? culture = null)
    {
        var configure = ConfigureCatalog
            ?? throw new ArgumentException("ConfigureCatalog is not set; use CatalogFactory for the catalog or set ConfigureCatalog.");
        var toolName = ToolName;
        var version = Version;
        return builder =>
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.ToolName(toolName);
            if (version is not null)
            {
                builder.ToolVersion(version);
            }

            if (culture is not null)
            {
                builder.Culture(culture);
            }

            configure(builder, culture);
        };
    }
}
