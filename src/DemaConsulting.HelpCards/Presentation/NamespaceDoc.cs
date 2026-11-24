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
///         <b>The JSON contract (<c>helpSchemaVersion</c> 1).</b> A JSON document is one object. With the default
///         envelope it is one line of ASCII (a character outside ASCII is written as <c>\uXXXX</c>) that starts with
///         <c>{"helpSchemaVersion":1,"ok":...,"kind":"..."}</c> and continues with the fields of its kind. <c>ok</c> is
///         <c>true</c> for every kind except <c>error</c>, whose document has <c>"ok":false</c>. The fields of a kind
///         are always in the order below, an optional value is left out (there is never a JSON <c>null</c>), and an
///         array is always present, possibly empty. The field <c>command</c> (the command that prints a card) is
///         written only when the link style is <c>Command</c>, as it is for the command-line helper. A
///         <c>JsonEnvelope</c> callback replaces the whole envelope: it receives only the kind and writes its own
///         members, so <c>helpSchemaVersion</c> and <c>ok</c> are present only if the callback writes them, the
///         library writes the fields of the kind after the members of the callback, and the one-line layout is the
///         callback's to keep (the library still escapes non-ASCII characters).
///     </para>
///     <list type="table">
///         <listheader><term>kind (made by)</term><description>Fields after the envelope</description></listheader>
///         <item><term><c>index</c> (<c>RenderIndex</c>)</term><description><c>tool</c> and <c>version</c> (only when set with <c>ToolName</c> and <c>ToolVersion</c>); <c>usage</c> (array of lines; command-line helper only); <c>sources</c>, each with <c>name</c>, <c>title</c>, <c>summary</c>; <c>cards</c>, each with <c>id</c>, <c>title</c>, <c>summary</c>, <c>aliases</c>, <c>tags</c>, <c>source</c>, <c>depth</c> (0 for a root), <c>command</c>.</description></item>
///         <item><term><c>card</c> (<c>Render</c>)</term><description><c>id</c>, <c>title</c>, <c>summary</c>, <c>aliases</c>, <c>tags</c>, <c>source</c>, <c>culture</c>, <c>body</c> (Markdown with the links written in the link style), <c>children</c> and <c>seeAlso</c> (each item <c>id</c>, <c>title</c>, <c>summary</c>, <c>command</c>), <c>examples</c> (each item <c>name</c>, <c>kind</c>, <c>language</c>, <c>fileName</c> when the example has one, <c>description</c>, <c>content</c>; the content is always included), <c>links</c> (the visible link targets of the body in body order; each item has some of <c>cardId</c>, <c>exampleName</c>, <c>exampleCardId</c>, <c>text</c>).</description></item>
///         <item><term><c>examples</c> (<c>RenderExamples</c>)</term><description><c>id</c> (the card); <c>examples</c> (as in <c>card</c>); <c>cards</c> (the cards below it that have examples; each item <c>id</c>, <c>summary</c>, <c>command</c>, <c>examples</c> as an array of example names).</description></item>
///         <item><term><c>example</c> (<c>RenderExample</c>)</term><description><c>id</c> (the card that was asked for); <c>example</c> (an object as an item of <c>examples</c> plus <c>card</c>, the id of the card that owns it).</description></item>
///         <item><term><c>search</c> (<c>RenderSearch</c>)</term><description><c>query</c>; <c>results</c>, best first, each with <c>id</c>, <c>title</c>, <c>summary</c>, <c>source</c>, <c>score</c> (a number), <c>matchedOn</c> (array of the fields that matched, in the fixed order <c>alias</c>, <c>id</c>, <c>example</c>, <c>title</c>, <c>tag</c>, <c>summary</c>), <c>command</c>.</description></item>
///         <item><term><c>cards</c> (<c>RenderCardList</c>)</term><description><c>filter</c> with <c>tags</c> and <c>sources</c> (arrays); <c>cards</c>, each with <c>id</c>, <c>title</c>, <c>summary</c>, <c>tags</c>, <c>source</c>, <c>command</c>.</description></item>
///         <item><term><c>error</c> (command-line helper)</term><description><c>ok</c> is <c>false</c>. <c>code</c> (<c>usage-error</c>, <c>not-found</c>, <c>ambiguous</c> or <c>catalog-error</c>), <c>message</c>, <c>hint</c> (when there is one), and <c>candidates</c> (array of global ids; only for <c>not-found</c> and <c>ambiguous</c>).</description></item>
///     </list>
///     <para>
///         Key rules: the options object is immutable (every property is <c>init</c>-only) and each value is checked
///         when it is set, so a value that is not defined, a width outside the range or a white-space command
///         template throws immediately, not at render time. The JSON format is one document on one line with the
///         versioned contract <c>helpSchemaVersion</c> 1 (above), so a program can parse it. The two callbacks
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
