using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Presentation;

namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     Runs one help command: reads its arguments, creates the catalog when it is needed, selects the answer, and writes
///     it with the exit code of the contract.
/// </summary>
/// <remarks>
///     <para>
///         The order of work is fixed. The arguments are read first, and a usage error is reported without creating the
///         catalog. A card command is then offered to the fallback resolver of the tool. Only after that is the catalog
///         factory called, once for the command. A defective catalog ends the command with exit code 70; arguments that
///         name nothing end it with exit code 2.
///     </para>
///     <para>
///         The dispatcher holds only the options it was created with, so one dispatcher serves any number of commands,
///         including concurrent ones. The writers are read from the options on each command, so a tool may change the
///         console writers between commands, and a writer provider is called each time the dispatcher writes. This part holds the flow of a command; the other parts select the answer of
///         each mode and look up cards and examples.
///     </para>
/// </remarks>
internal sealed partial class CliDispatcher
{
    /// <summary>
    ///     The least width of a wrapped line when the width of the console is used.
    /// </summary>
    private const int AutoMinimumWidth = 40;

    /// <summary>
    ///     The most width of a wrapped line when the width of the console is used.
    /// </summary>
    private const int AutoMaximumWidth = 120;

    /// <summary>
    ///     The width of a wrapped line when the width of the console is asked for but not available.
    /// </summary>
    private const int FallbackWidth = HelpRenderOptions.DefaultWidth;

    /// <summary>
    ///     The options of the helper.
    /// </summary>
    private readonly HelpCliOptions _options;

    /// <summary>
    ///     The template of the command that prints a card.
    /// </summary>
    private readonly string _commandTemplate;

    /// <summary>
    ///     Creates a dispatcher.
    /// </summary>
    /// <param name="options">The options, already checked. Must not be null.</param>
    public CliDispatcher(HelpCliOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;

        // The tool name of the options is the one users type, whatever name the catalog has for its tool
        _commandTemplate = options.CommandTemplate ?? $"{options.ToolName} {options.Verb} {{path}}";
    }

    /// <summary>
    ///     Gets the sentence that points a user at the help command.
    /// </summary>
    public string Footer => $"Run '{_options.ToolName} {_options.Verb}' for topics and examples.";

    /// <summary>
    ///     Runs a help command.
    /// </summary>
    /// <param name="arguments">The arguments that follow the verb. Must not be null or contain null.</param>
    /// <returns>The exit code: 0 for success, 2 for wrong arguments or unknown names, 70 for a defective catalog.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="arguments"/> is null or contains null.</exception>
    public int Run(IReadOnlyList<string> arguments)
    {
        var parsed = CliArgumentParser.Parse(arguments, _options.EnableLanguageOption);
        if (parsed.Request is not { } request)
        {
            var usage = new CliFailure(JsonDocumentWriter.UsageErrorCode, parsed.Message!, parsed.Hint, null);
            return Report(usage, parsed.JsonRequested ? HelpFormat.Json : HelpFormat.Text);
        }

        var render = CreateRenderOptions(request);

        // A subject that the tool owns is answered before the catalog is even created
        if (request.Mode == CliMode.Card && _options.FallbackResolver?.Invoke(request.Path) is { } answer)
        {
            Write(Output, answer.Text);
            return answer.ExitCode;
        }

        var catalog = CreateCatalog(request, out var failure);
        var outcome = catalog is null ? CliOutcome.Fail(failure!) : Execute(catalog, request, render);
        if (outcome.Failure is not null)
        {
            return Report(outcome.Failure, request.Format);
        }

        Write(Output, outcome.Text!);
        return 0;
    }

    /// <summary>
    ///     Gets the writer for output: the fixed writer, else the result of the provider now, else the console.
    /// </summary>
    private TextWriter Output => _options.Output ?? _options.OutputProvider?.Invoke() ?? Console.Out;

    /// <summary>
    ///     Gets the writer for errors: the fixed writer, else the result of the provider now, else the console.
    /// </summary>
    private TextWriter Error => _options.Error ?? _options.ErrorProvider?.Invoke() ?? Console.Error;

    /// <summary>
    ///     Creates the catalog with the configuration or the factory of the tool.
    /// </summary>
    /// <param name="request">The request, which holds the culture.</param>
    /// <param name="failure">Receives the failure when no catalog could be created.</param>
    /// <returns>The catalog, or <see langword="null"/> when the factory failed with a catalog exception.</returns>
    private HelpCatalog? CreateCatalog(CliRequest request, out CliFailure? failure)
    {
        failure = null;
        try
        {
            var catalog = _options.ConfigureCatalog is not null
                ? HelpCatalog.Create(_options.GetCatalogConfiguration(request.Culture))
                : _options.CatalogFactory!(request.Culture);
            if (catalog is not null)
            {
                return catalog;
            }

            failure = new CliFailure(JsonDocumentWriter.CatalogErrorCode, "Help is unavailable: the tool created no catalog.", null, null);
        }
        catch (HelpCatalogException exception)
        {
            failure = new CliFailure(
                JsonDocumentWriter.CatalogErrorCode,
                $"Help is unavailable: {exception.Problems[0].ToDisplayText()}",
                null,
                null);
        }

        return null;
    }

    /// <summary>
    ///     Writes a failure and gives its exit code.
    /// </summary>
    /// <param name="failure">The failure.</param>
    /// <param name="format">The output format; JSON writes the error document to the output.</param>
    /// <returns>The exit code.</returns>
    private int Report(CliFailure failure, HelpFormat format)
    {
        if (format == HelpFormat.Json)
        {
            Write(
                Output,
                JsonDocumentWriter.WriteError(null, failure.Code, failure.Message, failure.Hint, failure.Candidates));
        }
        else
        {
            Write(Error, $"Error: {failure.Message}" + (failure.Hint is null ? string.Empty : $"\n{failure.Hint}"));
        }

        return failure.ExitCode;
    }

    /// <summary>
    ///     Writes text and one line feed, whatever the line ending of the platform is.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="text">The text.</param>
    private static void Write(TextWriter writer, string text)
    {
        writer.Write(text + "\n");
        writer.Flush();
    }

    /// <summary>
    ///     Creates the render options of a request: commands for links, the template of the tool, and the width.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>The options.</returns>
    private HelpRenderOptions CreateRenderOptions(CliRequest request)
    {
        return new HelpRenderOptions
        {
            Format = request.Format,
            LinkStyle = HelpLinkStyle.Command,
            CommandTemplate = _commandTemplate,
            Width = request.AutoWidth ? ResolveAutoWidth() : request.Width ?? _options.DefaultWidth,
        };
    }

    /// <summary>
    ///     Gets the width for <c>--width auto</c>: the width of the console between 40 and 120, or 78 without a console.
    /// </summary>
    /// <returns>The width.</returns>
    private int ResolveAutoWidth()
    {
        var width = _options.OutputWidthProvider is { } provider ? provider() : ConsoleWidth();
        return width is { } value and > 0 ? Math.Clamp(value, AutoMinimumWidth, AutoMaximumWidth) : FallbackWidth;
    }

    /// <summary>
    ///     Gets the width of the console when the helper writes to it and the output is not redirected.
    /// </summary>
    /// <returns>The width, or <see langword="null"/> when there is none.</returns>
    private int? ConsoleWidth()
    {
        if (_options.Output is not null || _options.OutputProvider is not null || Console.IsOutputRedirected)
        {
            return null;
        }

        try
        {
            return Console.WindowWidth;
        }
        catch (IOException)
        {
            return null;
        }
        catch (PlatformNotSupportedException)
        {
            return null;
        }
    }
}
