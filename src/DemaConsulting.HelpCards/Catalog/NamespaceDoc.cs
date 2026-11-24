namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     The immutable catalog of help cards and everything you read from it: <c>HelpCatalog</c> with its lookups,
///     search, filtered views and renderings, the <c>HelpCard</c> and <c>HelpExample</c> it holds, and the
///     <c>HelpProblem</c>, <c>HelpSearchResult</c> and <c>HelpPathResolution</c> results. Start at
///     <c>HelpCatalog.Create</c>, then call <c>Find</c>, <c>Search</c> or <c>Render</c>. To write cards (front-matter
///     keys, links, examples, embedding), read the page of <c>DemaConsulting.HelpCards.Loading</c>; the table of every
///     rule id that a problem can carry is in the remarks of <c>HelpProblem</c>.
/// </summary>
/// <remarks>
///     <para>
///         The common pattern is four steps:
///     </para>
///     <list type="number">
///         <item><description>
///             Build once. <c>HelpCatalog.Create(builder =&gt; ...)</c> takes a configuration callback (see
///             <c>DemaConsulting.HelpCards.Composition</c>), reads every source, validates the whole composition and
///             returns the catalog.
///         </description></item>
///         <item><description>
///             Look up. <c>Find(path)</c> returns the card for a path such as <c>build compile</c> or
///             <c>build/compile</c> (names and aliases work, case does not matter) or <c>null</c>. <c>Resolve(path)</c>
///             does the same walk and explains a failure: the unknown segment, the valid names and a suggestion.
///             <c>Roots</c>, <c>Cards</c>, <c>GetChildren</c> and <c>GetSeeAlso</c> navigate the tree.
///         </description></item>
///         <item><description>
///             Search. <c>Search(query, options)</c> returns ranked <c>HelpSearchResult</c> values, best first;
///             <c>Suggest</c> returns spelling suggestions.
///         </description></item>
///         <item><description>
///             Show. <c>Render</c>, <c>RenderIndex</c>, <c>RenderExamples</c>, <c>RenderExample</c> and
///             <c>RenderSearch</c> return text, Markdown or JSON; the optional <c>HelpRenderOptions</c> are in
///             <c>DemaConsulting.HelpCards.Presentation</c>.
///         </description></item>
///     </list>
///     <para>
///         Key rules: the catalog and every object it returns are immutable and safe to use from several threads.
///         A defective source is an error at creation, never later: <c>Create</c> throws one
///         <c>HelpCatalogException</c> whose <c>Problems</c> list holds every error, while the informational notes of a
///         successful build are in <c>Diagnostics</c>. After creation, lookups return <c>null</c> or a
///         <c>HelpPathResolution</c> instead of throwing. A lookup is strict: the local id of a mounted source does
///         not resolve, only the global id does. A view (<c>Filter</c>, <c>FilterSource</c>, <c>Where</c>,
///         <c>Without</c>) is a new catalog over the same data; a hidden card is not found, and its visible
///         descendants move up. An unknown source, tag or id given to a view is an <c>ArgumentException</c>.
///         Callbacks such as the <c>Where</c> predicate run on the calling thread and must be side-effect free.
///     </para>
/// </remarks>
/// <example>
///     Build a catalog from one in-memory source, look up a card by name and by alias, explain a failed lookup,
///     search, and narrow the catalog to the cards with a tag.
///     <code language="csharp">
/// using System;
/// using System.Collections.Generic;
/// using System.Linq;
/// using DemaConsulting.HelpCards.Catalog;
/// using DemaConsulting.HelpCards.Loading;
///
/// HelpSource source = HelpSource.FromFiles(new Dictionary&lt;string, string&gt;
/// {
///     ["build.md"] = "---\ntitle: Build\nsummary: Build a project.\ntags:\n  - tutorial\n---\nSee [[build/compile]].",
///     ["build/compile.md"] = "---\ntitle: Compile\nsummary: Compile the sources.\naliases:\n  - make\n---\nRun the compiler.",
///     ["install.md"] = "---\ntitle: Install\nsummary: Install the tool.\n---\nCopy the files.",
/// }, name: "demo");
///
/// HelpCatalog catalog = HelpCatalog.Create(builder => builder.AddSource(source).ToolName("demo"));
///
/// // Lookup: a path is split on slashes and white space; names and aliases work and case does not matter.
/// HelpCard? compile = catalog.Find("build make");
/// Console.WriteLine(compile?.Id);
///
/// // A failed lookup says what went wrong instead of throwing.
/// HelpPathResolution miss = catalog.Resolve("build compiler");
/// Console.WriteLine($"{miss.UnknownSegment}: did you mean '{miss.Suggestion}'?");
///
/// // Search: ranked results, best first.
/// foreach (HelpSearchResult hit in catalog.Search("install"))
/// {
///     Console.WriteLine($"{hit.Id} ({hit.Score})");
/// }
///
/// // Views are new catalogs over the same data.
/// HelpCatalog tutorials = catalog.Filter(includeTags: ["tutorial"]);
/// Console.WriteLine(string.Join(", ", tutorials.Cards.Select(card => card.Id)));
/// </code>
/// </example>
internal static class NamespaceDoc
{
}
