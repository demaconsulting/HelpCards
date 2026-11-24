namespace DemaConsulting.HelpCards.Composition;

/// <summary>
///     How several help sources become one catalog: <c>HelpCatalogBuilder</c>, which configures the sources and
///     settings of a catalog. You never create a builder yourself; <c>HelpCatalog.Create</c> hands one to your
///     callback. Start at <c>AddSource</c> and <c>AddMountedSource</c>.
/// </summary>
/// <remarks>
///     <para>
///         The common pattern is four steps, all inside the callback of <c>HelpCatalog.Create</c>:
///     </para>
///     <list type="number">
///         <item><description>
///             Add each source. <c>AddSource</c> adds a source flat, so its cards keep their local ids.
///             <c>AddMountedSource</c> adds it below one generated root card named by the mount (the source name by
///             default), so the card <c>install</c> of a source mounted as <c>core</c> has the id
///             <c>core/install</c>.
///         </description></item>
///         <item><description>
///             Name the tool. <c>ToolName</c> and <c>ToolVersion</c> set the built-in <c>{{tool}}</c> and
///             <c>{{version}}</c> tokens; <c>Culture</c> chooses the translations.
///         </description></item>
///         <item><description>
///             Optionally constrain the result. <c>TagVocabulary</c> declares the closed set of tags, and
///             <c>ExpectedRoots</c> requires the root ids to equal a set.
///         </description></item>
///         <item><description>
///             Handle defects. <c>HelpCatalog.Create</c> throws one <c>HelpCatalogException</c> whose <c>Problems</c>
///             lists the errors found. The checks run in stages (a front-matter error hides the link checks of its
///             file), so fix the listed errors and run again.
///         </description></item>
///     </list>
///     <para>
///         Key rules: the order of the calls matters, because it decides the order of the roots, the order in which
///         overlays apply, and the order of imports (a source that is imported must be added before the source that
///         imports it). A mount is exactly one segment. Nothing shadows silently: duplicate ids, names, mounts and
///         source names are errors that name both sources. Authored cards link by local id, so a mount is the
///         application's decision and can change without editing a card. Wrong arguments throw
///         <c>ArgumentException</c> or <c>ArgumentNullException</c>. The builder only describes the composition; the
///         catalog it produces is immutable and thread-safe, and lives in <c>DemaConsulting.HelpCards.Catalog</c>.
///         The sources come from <c>DemaConsulting.HelpCards.Loading</c>.
///     </para>
/// </remarks>
/// <example>
///     An application that composes two libraries: the plugin imports the core library, so a bare link in the
///     plugin's cards resolves in the core library too, and each source is mounted under its own root.
///     <code language="csharp">
/// using System;
/// using System.Collections.Generic;
/// using DemaConsulting.HelpCards.Catalog;
/// using DemaConsulting.HelpCards.Loading;
///
/// // Two libraries as they would expose their help; FromFiles keeps this example self-contained.
/// HelpSource core = HelpSource.FromFiles(new Dictionary&lt;string, string&gt;
/// {
///     ["install.md"] = "---\ntitle: Install\nsummary: Install the core library.\n---\nRun {{tool}} install.",
/// }, name: "core");
///
/// HelpSource plugin = HelpSource.FromFiles(new Dictionary&lt;string, string&gt;
/// {
///     ["setup.md"] = "---\ntitle: Set up\nsummary: Set up the plugin.\n---\nInstall the core first, see [[install]].",
/// }, name: "plugin").Imports(core);
///
/// try
/// {
///     // The order of the calls matters: an imported source must be added before the source that imports it.
///     HelpCatalog catalog = HelpCatalog.Create(builder => builder
///         .AddMountedSource(core)
///         .AddMountedSource(plugin, "ext")
///         .ToolName("myapp")
///         .ToolVersion("1.0.0"));
///
///     foreach (HelpCard card in catalog.Cards)
///     {
///         Console.WriteLine(card.Id);
///     }
///
///     Console.WriteLine(catalog.Render(catalog.Find("ext/setup")!));
/// }
/// catch (HelpCatalogException ex)
/// {
///     // The errors found in every source are listed together (in stages: see HelpCatalog.Create).
///     foreach (HelpProblem problem in ex.Problems)
///     {
///         Console.Error.WriteLine($"{problem.RuleId} {problem.SourceName}:{problem.File}:{problem.Line} {problem.Message}");
///     }
///
///     return 1;
/// }
///
/// return 0;
/// </code>
/// </example>
internal static class NamespaceDoc
{
}
