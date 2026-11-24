namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     Ready-made front ends for a catalog: <c>HelpCli</c>, which adds a <c>help</c> command to a command-line tool
///     for people, and <c>HelpAgentTool</c> (created with <c>HelpAgentTools.Create</c>), a provider-neutral knowledge
///     tool that lets an AI agent read the cards. Start at <c>HelpCli.Create</c> for a tool and at
///     <c>HelpAgentTools.Create</c> for an agent host.
/// </summary>
/// <remarks>
///     <para>
///         Wiring help into a command-line tool takes four steps:
///     </para>
///     <list type="number">
///         <item><description>
///             Create one <c>HelpCli</c> with <c>HelpCli.Create(new HelpCliOptions { ... })</c>. <c>ToolName</c> and
///             <c>ConfigureCatalog</c> are required: the action receives the catalog builder, on which the helper
///             has already set the tool name and the <c>Version</c>, and the culture the user asked for (or
///             <c>null</c>); it adds the sources. The older <c>CatalogFactory</c>, which builds the whole catalog and
///             receives the culture, is accepted instead of <c>ConfigureCatalog</c>, never both.
///         </description></item>
///         <item><description>
///             Call <c>TryRun(args, out int exitCode)</c> before the tool's own argument parser, and return
///             <c>exitCode</c> when it returns <c>true</c>.
///         </description></item>
///         <item><description>
///             End the tool's usage text with <c>Footer</c>, a constant sentence that points to <c>tool help</c>.
///         </description></item>
///         <item><description>
///             Give an agent the same cards: <c>HelpAgentTools.Create(catalog, options)</c> returns a
///             <c>HelpAgentTool</c> with a <c>Name</c>, a <c>Description</c>, a JSON-schema
///             <c>ParametersSchema</c> and <c>Invoke(argumentsJson)</c>, which returns text. Register the first three
///             with your agent framework and answer each call with <c>Invoke</c>.
///         </description></item>
///     </list>
///     <para>
///         <b>What the command prints.</b> The command is <c>tool help [PATH...] [OPTIONS]</c>. <c>tool help</c> prints
///         the index of topics (one line per root card; <c>--all</c> adds every card), <c>tool help a b</c> or
///         <c>tool help a/b</c> prints the card <c>a/b</c>, <c>tool help a --examples</c> prints all examples of the
///         card and <c>tool help a --example name</c> one example alone (both need a card path), <c>--search words</c>
///         ranks cards, <c>--tag t</c> lists the cards with a tag, and <c>--json</c> (or <c>--format json</c>) writes
///         a versioned JSON document of the same answer. The text of every command is shown with its exit code in the
///         remarks of <c>HelpCli</c>.
///     </para>
///     <para>
///         <b>Which link style the helper uses.</b> The helper renders every link in the <em>command</em> style
///         (<c>HelpLinkStyle.Command</c>), not the plain style that <c>HelpRenderOptions</c> defaults to. A card link
///         <c>[[getting-started/install]]</c> reads <c>`tool help getting-started install`</c> (the command that prints
///         that card, from <c>HelpCliOptions.CommandTemplate</c>), a link to an example of the same card
///         (<c>[[#load]]</c>) reads the full command <c>`tool help this-card --example load`</c>, like a link to an
///         example of another card, <c>`tool help other-card --example load`</c>, so every link can be run as written. Markdown output
///         (<c>--format markdown</c>) keeps the command style. In contrast, <c>HelpAgentTool</c> and
///         <c>HelpCatalog.Render</c> without options write <c>the card `id`</c> and <c>the example `name`</c>, and a Markdown link style exists only through
///         <c>HelpRenderOptions</c>.
///     </para>
///     <para>
///         <b>Agent answers.</b> <c>Invoke</c> returns Markdown or plain text: an index grouped by source, a card with
///         its examples, one example, ranked search results, or a one-sentence message that tells the agent what to
///         send. When the catalog mounts several sources (<c>AddMountedSource</c>), card ids are global ids with the
///         mount in front (<c>core/getting-started</c>) and the <c>topic</c> argument takes that global id; a bare local
///         id works only when exactly one card has it, otherwise the answer lists the global ids to choose from. The
///         sample answers are in the remarks of <c>HelpAgentTool.Invoke</c> and in the three-library example below.
///     </para>
///     <para>
///         <b>One setting for the name and the version, and the tests.</b> <c>ToolName</c> and <c>Version</c> of the
///         options are the tool name and the version of the catalog as well, so you set each once. The same
///         configuration serves the tests of the tool: <c>options.GetCatalogConfiguration()</c> returns the action
///         that <c>HelpCardValidator.AssertValid</c> and <c>Validate</c> take, so the cards are validated in exactly
///         the composition that users get. <c>Output</c> and <c>Error</c> are fixed writers; <c>OutputProvider</c> and
///         <c>ErrorProvider</c> are functions that are called each time the helper writes, so the writer can depend on
///         the state of the tool at that moment (for example <c>OutputProvider = () => silent ? TextWriter.Null : null</c>;
///         <c>null</c> means the console). A writer and its provider are not combined.
///     </para>
///     <para>
///         Key rules: <c>TryRun</c> answers only when the first argument equals the verb (<c>help</c> by default,
///         ignoring case) and otherwise returns <c>false</c> at once without building the catalog, so a normal run of
///         the tool pays nothing. The helper prints a card, the index, examples, search results or tagged cards and
///         writes usage errors to the error writer; if building the catalog throws a <c>HelpCatalogException</c>, it
///         prints <c>Help is unavailable</c> and exits with 70 (the usual cause is a card defect, for example a source
///         without a name: every source needs a <c>name:</c> in its <c>_source.md</c>, or <c>HelpSource.WithName</c>, or the
///         rule <c>source-invalid</c> fails the catalog). Exit codes: 0 success, 2 wrong arguments or a name that
///         matches nothing, 70 defective catalog. <c>HelpAgentTool.Invoke</c> never throws for bad
///         arguments or a defective catalog: every failure is a plain-text answer that says what to send instead, and
///         answers are cut to <c>MaxOutputChars</c>. An option outside its range is an <c>ArgumentException</c> when
///         the tool is created. The arguments of an agent are untrusted input; the tool performs no file input or
///         output once the catalog exists and runs no code. The catalog comes from
///         <c>DemaConsulting.HelpCards.Catalog</c>, and <c>DemaConsulting.HelpCards.Composition</c> shows how to
///         combine several libraries into one catalog that both front ends serve. How to write the cards is on the page
///         of <c>DemaConsulting.HelpCards.Loading</c>.
///     </para>
/// </remarks>
/// <example>
///     A command-line tool that answers <c>tool help</c>, <c>tool help getting-started</c> and
///     <c>tool help --search started</c> before its own commands run. The sample builds its cards from in-memory text so
///     that it is self-contained; a real tool keeps its cards in a <c>Help</c> folder and uses
///     <c>HelpSource.FromAssembly(typeof(Program).Assembly)</c> instead (see <c>DemaConsulting.HelpCards.Loading</c>):
///     a <c>PackageReference</c> to <c>DemaConsulting.HelpCards</c> is all the project needs to embed the folder.
///     <code language="csharp">
/// using System;
/// using System.Collections.Generic;
/// using DemaConsulting.HelpCards.Integration;
/// using DemaConsulting.HelpCards.Loading;
///
/// internal static class Program
/// {
///     private static readonly HelpSource Cards = HelpSource.FromFiles(new Dictionary&lt;string, string&gt;
///     {
///         ["getting-started.md"] = "---\ntitle: Getting started\nsummary: Install the tool and run it.\n---\nRun {{tool}} load data.csv.",
///     }, name: "tool");
///
///     // Created once, before the tool's own argument parser. Nothing is read until the first 'help' command.
///     // ToolName and Version are set once: the helper gives them to the catalog builder, so they are also the
///     // {{tool}} and {{version}} of the cards and the version of the JSON documents.
///     private static readonly HelpCli Help = HelpCli.Create(new HelpCliOptions
///     {
///         ToolName = "tool",
///         Version = "1.4.0",
///         ConfigureCatalog = (builder, culture) => builder.AddSource(Cards),
///     });
///
///     public static int Main(string[] args)
///     {
///         // Answers 'tool help', 'tool help getting-started', 'tool help --search started', and so on.
///         if (Help.TryRun(args, out int exitCode))
///         {
///             return exitCode;
///         }
///
///         // The tool's own commands go here; end the usage text with the one-line pointer to help.
///         Console.WriteLine("Usage: tool &lt;command&gt;");
///         Console.WriteLine(Help.Footer);
///         return 0;
///     }
/// }
/// </code>
///     An application that exposes the same cards to an AI agent as one tool:
///     <code language="csharp">
/// using System;
/// using System.Collections.Generic;
/// using DemaConsulting.HelpCards.Catalog;
/// using DemaConsulting.HelpCards.Integration;
/// using DemaConsulting.HelpCards.Loading;
///
/// HelpSource app = HelpSource.FromFiles(new Dictionary&lt;string, string&gt;
/// {
///     ["install.md"] = "---\ntitle: Install\nsummary: Install the application.\n---\nRun the installer.",
/// }, name: "myapp");
///
/// HelpCatalog catalog = HelpCatalog.Create(builder => builder.AddSource(app).ToolName("myapp"));
///
/// // One tool for the whole catalog: register Name, Description, and ParametersSchema with your agent framework.
/// HelpAgentTool tool = HelpAgentTools.Create(catalog, new HelpAgentToolOptions { Name = "myapp_help" });
/// Console.WriteLine(tool.Name);
///
/// // Answer each call with the text Invoke returns; it never throws for bad arguments.
/// Console.WriteLine(tool.Invoke(null));
/// Console.WriteLine(tool.Invoke("""{"topic":"install"}"""));
/// Console.WriteLine(tool.Invoke("""{"query":"install"}"""));
/// </code>
///     An application that uses three libraries, each with its own cards, and serves them all as one agent tool. Each
///     source is mounted below its own root, so the ids that the agent sees are global ids: <c>core/reports</c>, and
///     <c>b/reports</c> for the source that was mounted as <c>b</c>. The <c>topic</c> argument takes these global ids; a
///     bare <c>reports</c> matches the cards of all three libraries and gets a message that lists them. The <c>source</c>
///     argument, in contrast, is the declared source name (<c>plot</c>, the <c>name</c> of its <c>_source.md</c>), not the
///     mount (<c>b</c>). (Real libraries
///     expose a <c>HelpSource</c> made with <c>HelpSource.FromAssembly</c>; the sample builds them from text.)
///     <code language="csharp">
/// using System;
/// using System.Collections.Generic;
/// using DemaConsulting.HelpCards.Catalog;
/// using DemaConsulting.HelpCards.Integration;
/// using DemaConsulting.HelpCards.Loading;
///
/// HelpCatalog catalog = HelpCatalog.Create(builder => builder
///     .AddMountedSource(Library("core", "Core"))        // mounted as 'core' (the declared name)
///     .AddMountedSource(Library("plot", "Plot"), "b")   // mounted as 'b'
///     .AddMountedSource(Library("text", "Text"))
///     .ToolName("myapp")
///     .ToolVersion("1.0.0"));
///
/// HelpAgentTool tool = HelpAgentTools.Create(catalog, new HelpAgentToolOptions { Name = "myapp_help" });
///
/// string[] calls =
/// [
///     """{}""",                                          // the index of all three libraries
///     """{"topic":"b/reports"}""",                       // a card, by global id
///     """{"topic":"reports"}""",                         // ambiguous: the answer lists the global ids
///     """{"query":"install","source":"plot"}""",         // a search limited to one source
/// ];
///
/// foreach (string call in calls)
/// {
///     Console.WriteLine($"-- {call}");
///     Console.WriteLine(tool.Invoke(call));
/// }
///
/// static HelpSource Library(string name, string title) => HelpSource.FromFiles(new Dictionary&lt;string, string&gt;
/// {
///     ["_source.md"] = $"---\nname: {name}\ntitle: {title}\nsummary: Help cards of {title}.\n---\n{title} reads data.",
///     ["getting-started.md"] = $"---\ntitle: Getting started\nsummary: Install {title} and run it.\n---\nInstall {title}, then see [[reports]].",
///     ["reports.md"] = $"---\ntitle: Reports\nsummary: Write a report with {title}.\n---\nReports are Markdown files.",
/// }, name: name);
/// </code>
///     The output (the index is grouped by source; the ids carry the mount):
///     <code language="text">
/// -- {}
/// Help index. Read a card with the topic argument, get an example with the example argument, or search with the query argument.
///
/// ## Core
///
/// Help cards of Core.
///
/// core                    Help cards of Core.
///   core/getting-started  Install Core and run it.
///   core/reports          Write a report with Core.
///
/// ## Plot
///
/// Help cards of Plot.
///
/// b                    Help cards of Plot.
///   b/getting-started  Install Plot and run it.
///   b/reports          Write a report with Plot.
///
/// ## Text
///
/// Help cards of Text.
///
/// text                    Help cards of Text.
///   text/getting-started  Install Text and run it.
///   text/reports          Write a report with Text.
/// -- {"topic":"b/reports"}
/// # Reports
///
/// Write a report with Plot.
///
/// Reports are Markdown files.
/// -- {"topic":"reports"}
/// The topic 'reports' matches several cards: core/reports, b/reports, text/reports. Ask for one of these ids.
/// -- {"query":"install","source":"plot"}
/// Cards matching 'install':
///
/// b/getting-started  Install Plot and run it.
///
/// Read one with the topic argument.
/// </code>
/// </example>
internal static class NamespaceDoc
{
}
