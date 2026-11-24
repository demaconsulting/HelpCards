namespace DemaConsulting.HelpCards.Presentation;

/// <summary>
///     How a card or a list of cards is written out: the output format (<c>HelpFormat</c>), how links read
///     (<c>HelpLinkStyle</c>), and the wrapping width, command template and hooks (<c>HelpRenderOptions</c>). The
///     rendering itself is done by the <c>Render...</c> methods of <c>HelpCatalog</c>; start at
///     <c>HelpRenderOptions</c> when the defaults are not what you need.
/// </summary>
/// <remarks>
///     <para>
///         The common pattern is three steps:
///     </para>
///     <list type="number">
///         <item><description>
///             Build a catalog (<c>DemaConsulting.HelpCards.Catalog</c>) and find the card to show.
///         </description></item>
///         <item><description>
///             Choose the output with <c>new HelpRenderOptions { ... }</c>. Every property has a default, so
///             <c>new HelpRenderOptions()</c> (or passing <c>null</c>) gives plain text wrapped to 78 columns with
///             plain links. <c>Format</c> is <c>Text</c>, <c>Markdown</c> or <c>Json</c>; <c>LinkStyle</c> is
///             <c>Plain</c> (words such as the card <c>a/b</c>), <c>Command</c> (the command that prints the target)
///             or <c>Markdown</c> (a Markdown link); <c>Width</c> is 0 for no wrapping or 20 to 500.
///         </description></item>
///         <item><description>
///             Pass the options to <c>HelpCatalog.Render</c>, <c>RenderIndex</c>, <c>RenderExamples</c>,
///             <c>RenderExample</c>, <c>RenderSearch</c> or <c>RenderCardList</c>, and use the returned string.
///         </description></item>
///     </list>
///     <para>
///         Key rules: the options object is immutable (every property is <c>init</c>-only) and each value is checked
///         when it is set, so a value that is not defined, a width outside the range or a white-space command
///         template throws immediately, not at render time. The JSON format is one document on one line with the
///         versioned contract <c>helpSchemaVersion</c> 1, so a program can parse it. The two callbacks
///         (<c>LinkFormatter</c> and <c>JsonEnvelope</c>) run on the calling thread and may be called concurrently
///         when the catalog is shared, so they must be thread-safe and free of side effects. This namespace holds
///         only the options and the formatting enums; the rendering methods live on <c>HelpCatalog</c>
///         (<c>DemaConsulting.HelpCards.Catalog</c>).
///     </para>
/// </remarks>
/// <example>
///     Render the same card as wrapped text, as Markdown with command links, and as JSON.
///     <code language="csharp">
/// using System;
/// using System.Collections.Generic;
/// using DemaConsulting.HelpCards.Catalog;
/// using DemaConsulting.HelpCards.Loading;
/// using DemaConsulting.HelpCards.Presentation;
///
/// HelpSource source = HelpSource.FromFiles(new Dictionary&lt;string, string&gt;
/// {
///     ["build.md"] = "---\ntitle: Build\nsummary: Build a project.\n---\nFirst [[build/compile]], then test.",
///     ["build/compile.md"] = "---\ntitle: Compile\nsummary: Compile the sources.\n---\nRun the compiler.",
/// }, name: "demo");
///
/// HelpCatalog catalog = HelpCatalog.Create(builder => builder.AddSource(source).ToolName("demo"));
/// HelpCard card = catalog.Find("build")!;
///
/// // Plain text wrapped to 60 columns; links read as words, for example: the card `build/compile`.
/// Console.WriteLine(catalog.Render(card, new HelpRenderOptions { Width = 60 }));
///
/// // Markdown, with links written as the command that prints the target, for example: `demo help build compile`.
/// Console.WriteLine(catalog.Render(card, new HelpRenderOptions
/// {
///     Format = HelpFormat.Markdown,
///     LinkStyle = HelpLinkStyle.Command,
/// }));
///
/// // One JSON document on one line, as defined by the versioned JSON contract.
/// Console.WriteLine(catalog.Render(card, new HelpRenderOptions { Format = HelpFormat.Json }));
/// </code>
/// </example>
internal static class NamespaceDoc
{
}
