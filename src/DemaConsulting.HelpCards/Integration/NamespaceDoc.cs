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
///             <c>CatalogFactory</c> are required; the factory receives the culture the user asked for, or
///             <c>null</c>, and returns a <c>HelpCatalog</c>.
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
///         Key rules: <c>TryRun</c> answers only when the first argument equals the verb (<c>help</c> by default,
///         ignoring case) and otherwise returns <c>false</c> at once without building the catalog, so a normal run of
///         the tool pays nothing. The helper prints a card, the index, examples, search results or tagged cards and
///         writes usage errors to the error writer; if the catalog factory throws a <c>HelpCatalogException</c>, it
///         prints <c>Help is unavailable</c> and exits with 70. <c>HelpAgentTool.Invoke</c> never throws for bad
///         arguments or a defective catalog: every failure is a plain-text answer that says what to send instead, and
///         answers are cut to <c>MaxOutputChars</c>. An option outside its range is an <c>ArgumentException</c> when
///         the tool is created. The arguments of an agent are untrusted input; the tool performs no file input or
///         output once the catalog exists and runs no code. The catalog comes from
///         <c>DemaConsulting.HelpCards.Catalog</c>, and <c>DemaConsulting.HelpCards.Composition</c> shows how to
///         combine several libraries into one catalog that both front ends serve.
///     </para>
/// </remarks>
/// <example>
///     A command-line tool that answers <c>tool help</c>, <c>tool help getting-started</c> and
///     <c>tool help --search started</c> before its own commands run:
///     <code language="csharp">
/// using System;
/// using System.Collections.Generic;
/// using System.Globalization;
/// using DemaConsulting.HelpCards.Catalog;
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
///     private static readonly HelpCli Help = HelpCli.Create(new HelpCliOptions
///     {
///         ToolName = "tool",
///         Version = "1.4.0",
///         CatalogFactory = culture => HelpCatalog.Create(builder => builder
///             .AddSource(Cards)
///             .ToolName("tool")
///             .ToolVersion("1.4.0")
///             .Culture(culture ?? CultureInfo.InvariantCulture)),
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
/// </example>
internal static class NamespaceDoc
{
}
