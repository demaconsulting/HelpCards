using DemaConsulting.HelpCards.Presentation;

namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     The command-line helper that gives a tool a <c>help</c> command: the topic list, cards, examples, search, and tags,
///     in text, Markdown, or JSON.
/// </summary>
/// <remarks>
///     <para>
///         The helper is independent of any command-line library. A tool calls <see cref="TryRun"/> with its arguments
///         before its own parser; when the first argument is the help verb the helper answers the command and the tool
///         exits with the returned code. Every other command returns at once, without creating the catalog or reading any
///         resource, and <see cref="Footer"/> is a constant for the tool's own usage text. A helper is typically kept in a
///         static field because creating it does no work.
///     </para>
///     <para>
///         The command is <c>{tool} {verb} [PATH...] [OPTIONS]</c> with the options <c>--json</c>, <c>--format</c>,
///         <c>--examples</c>, <c>--example</c>, <c>--search</c>, <c>--tag</c>, <c>--source</c>, <c>--all</c>,
///         <c>--width</c>, and, when enabled, <c>--lang</c>. Success exits with 0 and writes to the output; wrong
///         arguments and names that match nothing exit with 2; a defective catalog exits with 70. In text and Markdown
///         an error goes to the error writer as <c>Error: {message}</c> and an optional hint line; in JSON it is the
///         error document on the output.
///     </para>
///     <para>
///         A helper is immutable and safe for concurrent use as far as its writers and its catalog factory are.
///     </para>
///     <para>
///         Links in the printed text use the <em>command</em> style: a link to a card reads as the command that prints
///         it (<c>`tool help getting-started install`</c>, built from <see cref="HelpCliOptions.CommandTemplate"/>), and
///         a link to an example of the same card reads <c>`--example load`</c>, the option to add to the command of
///         that card. This differs from the plain style (<c>the card `id`</c>) of <c>HelpCatalog.Render</c> without options
///         and of the agent tool. <c>--examples</c> and <c>--example name</c> need the path of a card.
///     </para>
/// </remarks>
/// <example>
///     The commands of a tool with two root cards and one child, run in a loop so that the program prints exactly what
///     a user sees (the sample points both writers at the console output, so errors appear in order). The cards are
///     in-memory text; a real tool uses <c>HelpSource.FromAssembly</c>.
///     <code language="csharp">
/// using System;
/// using System.Collections.Generic;
/// using System.Globalization;
/// using DemaConsulting.HelpCards.Catalog;
/// using DemaConsulting.HelpCards.Integration;
/// using DemaConsulting.HelpCards.Loading;
///
/// HelpSource cards = HelpSource.FromFiles(new Dictionary&lt;string, string&gt;
/// {
///     ["_source.md"] = "---\nname: demo\ntitle: Demo tool\nsummary: Help cards of the demo tool.\n---\nThe demo tool loads data files.",
///     ["getting-started.md"] = "---\ntitle: Getting started\nsummary: Install the tool and run it for the first time.\ntags:\n  - tutorial\nexamples:\n  - name: load\n    kind: command\n    language: shell\n    content: '{{tool}} load data.csv'\n    description: Load a data file.\n---\nInstall the package, then run [[#load]]. Next, see [[getting-started/install]] and [[reports]].",
///     ["getting-started/install.md"] = "---\ntitle: Install\nsummary: Install the tool with the dotnet CLI.\n---\nRun `dotnet tool install {{tool}}`.",
///     ["reports.md"] = "---\ntitle: Reports\nsummary: Write a report from loaded data.\naliases:\n  - report\ntags:\n  - tutorial\n---\nReports are written as Markdown files.",
/// }, name: "demo");
///
/// HelpCli help = HelpCli.Create(new HelpCliOptions
/// {
///     ToolName = "tool",
///     Version = "1.4.0",
///     CatalogFactory = culture => HelpCatalog.Create(builder => builder
///         .AddSource(cards)
///         .ToolName("tool")
///         .ToolVersion("1.4.0")
///         .Culture(culture ?? CultureInfo.InvariantCulture)),
///     Output = Console.Out,
///     Error = Console.Out,
/// });
///
/// string[] commands =
/// [
///     "help",
///     "help getting-started",
///     "help getting-started install",
///     "help --search install",
///     "help getting-started --examples",
///     "help getting-started --example load",
///     "help --search install --json",
///     "help nothing",
/// ];
///
/// foreach (string command in commands)
/// {
///     Console.WriteLine($"$ tool {command}");
///     bool answered = help.TryRun(command.Split(' '), out int exitCode);
///     Console.WriteLine($"[answered: {answered}, exit code: {exitCode}]");
///     Console.WriteLine();
/// }
/// </code>
///     The output:
///     <code language="text">
/// $ tool help
/// Help topics:
/// tool help getting-started  Install the tool and run it for the first time.
/// tool help reports          Write a report from loaded data.
///
/// Run 'tool help' for topics and examples.
/// [answered: True, exit code: 0]
///
/// $ tool help getting-started
/// # Getting started
///
/// Install the tool and run it for the first time.
/// Tags: tutorial
///
/// Install the package, then run `--example load`. Next, see `tool help getting-started install` and `tool help reports`.
///
/// ## Cards
///
/// - tool help getting-started install
///   Install the tool with the dotnet CLI.
///
/// ## Examples
///
/// - load [command]: Load a data file.
///
/// Print them in full with: tool help getting-started --examples, or one alone
///   with: tool help getting-started --example &lt;name&gt;.
/// [answered: True, exit code: 0]
///
/// $ tool help getting-started install
/// # Install
///
/// Install the tool with the dotnet CLI.
///
/// Run `dotnet tool install tool`.
/// [answered: True, exit code: 0]
///
/// $ tool help --search install
/// tool help getting-started install  Install the tool with the dotnet CLI.
/// tool help getting-started          Install the tool and run it for the first
///                                    time.
/// [answered: True, exit code: 0]
///
/// $ tool help getting-started --examples
/// # Getting started: examples
///
/// ## load [command]
///
/// Load a data file.
///
/// ```shell
/// tool load data.csv
/// ```
///
/// Print one alone with: tool help getting-started --example &lt;name&gt;
/// [answered: True, exit code: 0]
///
/// $ tool help getting-started --example load
/// tool load data.csv
/// [answered: True, exit code: 0]
///
/// $ tool help --search install --json
/// {"helpSchemaVersion":1,"ok":true,"kind":"search","query":"install","results":[{"id":"getting-started/install","title":"Install","summary":"Install the tool with the dotnet CLI.","source":"demo","score":190,"matchedOn":["id","title","summary"],"command":"tool help getting-started install"},{"id":"getting-started","title":"Getting started","summary":"Install the tool and run it for the first time.","source":"demo","score":60,"matchedOn":["summary"],"command":"tool help getting-started"}]}
/// [answered: True, exit code: 0]
///
/// $ tool help nothing
/// Error: Unknown help card 'nothing'.
/// Help topics: getting-started, reports. Run 'tool help' for their summaries.
/// [answered: True, exit code: 2]
/// </code>
/// </example>
public sealed class HelpCli
{
    /// <summary>
    ///     The dispatcher that runs the commands.
    /// </summary>
    private readonly CliDispatcher _dispatcher;

    /// <summary>
    ///     The verb that starts help.
    /// </summary>
    private readonly string _verb;

    /// <summary>
    ///     Creates a helper from checked options.
    /// </summary>
    /// <param name="options">The options.</param>
    private HelpCli(HelpCliOptions options)
    {
        _verb = options.Verb;
        _dispatcher = new CliDispatcher(options);
    }

    /// <summary>
    ///     Gets the sentence that tells users how to start help, for the usage text of the tool.
    /// </summary>
    /// <remarks>
    ///     The sentence is <c>Run '{tool} {verb}' for topics and examples.</c> It is a constant: reading it never
    ///     creates the catalog.
    /// </remarks>
    public string Footer => _dispatcher.Footer;

    /// <summary>
    ///     Creates a helper.
    /// </summary>
    /// <remarks>
    ///     Creating a helper does not call the catalog factory and does no I/O.
    /// </remarks>
    /// <param name="options">The options. Must not be null.</param>
    /// <returns>The helper.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> or its catalog factory is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when the tool name or the verb is empty or white space, the verb contains white space or starts with
    ///     a hyphen, the tool name contains a brace, the command template is white space, or the default width is
    ///     not 0 or 20 to 500.
    /// </exception>
    public static HelpCli Create(HelpCliOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.CatalogFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ToolName);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Verb);
        if (options.ToolName.Contains('{', StringComparison.Ordinal) || options.ToolName.Contains('}', StringComparison.Ordinal))
        {
            throw new ArgumentException("The tool name must not contain a brace.");
        }

        if (options.Verb.Any(char.IsWhiteSpace) || options.Verb.StartsWith('-'))
        {
            throw new ArgumentException("The verb must not contain white space or start with a hyphen.");
        }

        // The render options check the template and the width, so their rules stay in one place
        _ = new HelpRenderOptions { CommandTemplate = options.CommandTemplate ?? "{path}", Width = options.DefaultWidth };
        return new HelpCli(options);
    }

    /// <summary>
    ///     Runs a help command when the arguments start with the help verb.
    /// </summary>
    /// <remarks>
    ///     The verb is compared ignoring case. When it does not match, or there are no arguments, the method returns
    ///     <see langword="false"/> at once, without creating the catalog or touching any resource.
    /// </remarks>
    /// <param name="args">The arguments of the tool. Must not be null.</param>
    /// <param name="exitCode">
    ///     Receives the exit code when the command was a help command (0 success, 2 wrong arguments or a name that matches
    ///     nothing, 70 defective catalog); otherwise 0.
    /// </param>
    /// <returns><see langword="true"/> when the arguments were a help command and have been answered.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> is null.</exception>
    public bool TryRun(string[] args, out int exitCode)
    {
        ArgumentNullException.ThrowIfNull(args);
        exitCode = 0;
        if (args.Length == 0 || !string.Equals(args[0], _verb, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        exitCode = Run(args.Skip(1).ToList());
        return true;
    }

    /// <summary>
    ///     Runs a help command whose verb is already removed.
    /// </summary>
    /// <param name="helpArguments">The arguments after the verb. Must not be null or contain null.</param>
    /// <returns>The exit code: 0 for success, 2 for wrong arguments or unknown names, 70 for a defective catalog.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="helpArguments"/> is null or contains null.</exception>
    public int Run(IReadOnlyList<string> helpArguments)
    {
        return _dispatcher.Run(helpArguments);
    }
}
