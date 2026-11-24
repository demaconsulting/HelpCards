namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     Where the help cards of one library or tool come from, and the format of a card: <c>HelpSource</c>, the single
///     public type of this namespace, and the complete summary of the card file format (layout, ids, front-matter keys,
///     links, tokens, examples, cultures, and the MSBuild embedding). A library exposes one static <c>HelpSource</c>
///     (made with <c>HelpSource.FromAssembly</c>) so an application can compose its cards without knowing where the
///     files are. Start at <c>HelpSource</c>; read the remarks of this page to write cards.
/// </summary>
/// <remarks>
///     <para>
///         This page is the authoritative summary of how to write cards, so a card author needs no other document. It
///         holds, in order: the three steps for a library, the layout of the files and the ids, the MSBuild embedding,
///         the front-matter keys, the value syntax (including the one multi-line construct), examples, links, tokens,
///         cultures, the key rules, and a complete minimal card with a <c>_source.md</c>. The findings that
///         <c>HelpCatalog.Create</c> and the validator report are listed with their rule ids in the remarks of
///         <see cref="DemaConsulting.HelpCards.Catalog.HelpProblem"/>.
///     </para>
///     <para>
///         The common pattern for a library that ships help is three steps:
///     </para>
///     <list type="number">
///         <item><description>
///             Write the cards. Put one Markdown file per topic, with a small front-matter block, in the
///             <c>Help</c> folder of the project; an optional <c>_source.md</c> names the source and describes it.
///             A <c>PackageReference</c> to <c>DemaConsulting.HelpCards</c> is all the project needs: its build files
///             embed the folder in the assembly, so the project file needs no <c>EmbeddedResource</c> item (see
///             <em>Embedding in the assembly</em> below).
///         </description></item>
///         <item><description>
///             Expose one source. Keep a single static property that returns
///             <c>HelpSource.FromAssembly(typeof(MyLibHelp).Assembly)</c>. Use <c>FromFiles</c> to build a source
///             from in-memory text in tests, and <c>WithName</c>, <c>WithTokens</c> and <c>Imports</c> to declare
///             a name, <c>{{token}}</c> values and companion sources.
///         </description></item>
///         <item><description>
///             Hand the source to a catalog. <c>HelpCatalog.Create</c> (in <c>DemaConsulting.HelpCards.Catalog</c>)
///             reads and validates the files; <c>DemaConsulting.HelpCards.Composition</c> describes how several
///             sources are combined.
///         </description></item>
///     </list>
///     <para>
///         <b>Layout and ids.</b> The files of a source live below one folder, by default <c>Help</c>. The path of a
///         file below that folder, without the extension, is the <em>local id</em> of the card. A name is a
///         <em>segment</em>: lower-case ASCII letters and digits with single hyphens between them, 1 to 48
///         characters (<c>compile</c>, <c>flow-chart</c> and <c>step2</c> are valid; <c>Compile</c>, <c>my_card</c> and
///         <c>a--b</c> are not; a bad file name is the error <c>file-name</c>). A local id has 1 to 8 segments.
///         Names are compared without regard to case.
///     </para>
///     <list type="table">
///         <listheader><term>File</term><description>Meaning</description></listheader>
///         <item><term><c>Help/build.md</c></term><description>The card <c>build</c>, a root card.</description></item>
///         <item><term><c>Help/build/compile.md</c></term><description>The card <c>build/compile</c>, a child of <c>build</c>.</description></item>
///         <item><term><c>Help/_source.md</c></term><description>Describes the source as a whole; not a card.</description></item>
///         <item><term><c>Help/build/compile.de.md</c></term><description>The German translation of <c>build/compile</c> (see <em>Cultures</em>).</description></item>
///         <item><term><c>Help/build/sample.yaml</c></term><description>A data file (any extension except <c>.md</c>) that an example can name with its <c>file</c> key.</description></item>
///     </list>
///     <para>
///         <b>The hierarchy rule.</b> A card <c>a/b</c> requires the card <c>a</c> (the file <c>a.md</c>). A folder
///         alone is not enough: without <c>a.md</c> the card <c>a/b</c> is the error <c>parent-missing</c>. A root
///         card has no <c>/</c> in its id; its children are the cards one level below it, and they are listed on the
///         parent card.
///     </para>
///     <para>
///         <b>Embedding in the assembly.</b> The package ships MSBuild files that embed every file below the cards
///         folder as an embedded resource, named <c>{HelpCardsPrefix}/{path below the folder}</c> (for the default
///         settings and an assembly <c>MyLib</c>, <c>MyLib.Help/build/compile.md</c>). Data files are embedded too.
///         Three properties tune it; set them in the project file:
///     </para>
///     <list type="table">
///         <listheader><term>Property</term><description>Default and meaning</description></listheader>
///         <item><term><c>HelpCardsEnabled</c></term><description><c>true</c>. Set <c>false</c> to switch embedding off (anything other than <c>true</c> disables it).</description></item>
///         <item><term><c>HelpCardsDirectory</c></term><description><c>Help</c>. The folder below the project directory that holds the cards. If it does not exist nothing is embedded, without a warning.</description></item>
///         <item><term><c>HelpCardsPrefix</c></term><description><c>$(AssemblyName).Help</c>. The prefix of the resource names. If you change it, pass the same text to <c>HelpSource.FromAssembly(assembly, prefix)</c>.</description></item>
///     </list>
///     <para>
///         Direct versus transitive references: the build files are packed under the package's <c>build</c> folder and
///         not under <c>buildTransitive</c>, so only a project that has its own <c>PackageReference</c> to
///         <c>DemaConsulting.HelpCards</c> embeds cards. A library that ships cards must reference the package
///         itself. An application that merely references that library (or its NuGet package) embeds nothing and
///         needs no <c>Help</c> folder; if the application has cards of its own it needs its own
///         <c>PackageReference</c> too. The package still flows to the consumers of the library as an ordinary
///         dependency, so its types (<c>HelpSource</c>, <c>HelpCatalog</c>) compile there; do not set
///         <c>PrivateAssets="all"</c> on this reference in a library whose public API exposes a <c>HelpSource</c>.
///         A test project that only uses <c>DemaConsulting.HelpCards.Testing</c> may reference the package as well; it
///         embeds nothing unless it has a <c>Help</c> folder, and <c>HelpCardsEnabled</c> set to <c>false</c> removes
///         even that.
///     </para>
///     <para>
///         <b>The card file.</b> A card is a line of three hyphens, the front matter, another line of three hyphens, an
///         optional blank line, and the Markdown body. Line endings may be LF or CRLF; a byte order mark is ignored.
///         The front matter is a small, strict subset of YAML (the file is read by HelpCards' own parser, not a YAML
///         library). A file is an <em>overlay</em> when it has the key <c>extends</c>, a <em>translation</em> when its
///         name has a culture suffix such as <c>.de</c>, and a <em>card</em> otherwise. In the table R means
///         required, O optional, and a hyphen not allowed (a key that is not allowed is the error <c>key-unknown</c>,
///         and its message lists the allowed keys; a missing required key is <c>key-missing</c>; a value of the wrong
///         shape is <c>value-invalid</c>).
///     </para>
///     <list type="table">
///         <listheader><term>Key</term><description>Value, and meaning (Card / Overlay / Translation / <c>_source.md</c>)</description></listheader>
///         <item><term><c>title</c></term><description>One-line text. R / O / O / O. The heading of the card.</description></item>
///         <item><term><c>summary</c></term><description>One-line text. R / O / O / O. Shown in lists and search results; keep it to 100 characters (60 for a root card), or the validator warns <c>summary-length</c>.</description></item>
///         <item><term><c>aliases</c></term><description>List of alternative names, each a segment. O / - / O (added) / O. Another way to type the card name; unique among siblings. In <c>_source.md</c> they name the mount root.</description></item>
///         <item><term><c>tags</c></term><description>List of unique segments. O / - / - / -. Group cards across the tree; filter with <c>--tag</c> or <c>Filter</c>.</description></item>
///         <item><term><c>order</c></term><description>Integer of at most 7 digits (a leading minus is allowed). O / - / - / -. Sorts siblings; a card without <c>order</c> sorts after every card that has one, ties by name.</description></item>
///         <item><term><c>children</c></term><description>List of child names, each an existing direct child, no repeats. O / - / - / O. Puts those children first, in the listed order (<c>children-unknown</c>, <c>children-duplicate</c>). In <c>_source.md</c> it orders the roots of the source.</description></item>
///         <item><term><c>see-also</c></term><description>List of references, <c>localId</c> or <c>source:localId</c>. O / O / - / -. Listed under <em>See also</em>; one that resolves to nothing is <c>see-also-target</c>.</description></item>
///         <item><term><c>examples</c></term><description>List of example items (see <em>Examples</em>). O / O / O (only <c>name</c> and <c>description</c> per item) / -.</description></item>
///         <item><term><c>extends</c></term><description>One reference. Overlay only (R). The card the overlay adds to; see <c>DemaConsulting.HelpCards.Composition</c>.</description></item>
///         <item><term><c>optional</c></term><description><c>true</c> or <c>false</c>. Overlay only (O). With <c>true</c> the overlay is skipped when its target source is absent.</description></item>
///         <item><term><c>name</c></term><description>A segment. <c>_source.md</c> only (O). The declared name of the source: public contract, because external links use it. Required unless <c>HelpSource.WithName</c> supplies it (<c>source-invalid</c>).</description></item>
///         <item><term><c>language</c></term><description>Culture name, default <c>en</c>. <c>_source.md</c> only (O). The culture of the neutral files.</description></item>
///         <item><term><c>schema</c></term><description>Integer, default 1. <c>_source.md</c> only (O). A schema newer than the library supports is <c>schema-unsupported</c>.</description></item>
///         <item><term><c>x-</c> followed by a name</term><description>One-line text. O / O / - / O. Your own data; kept in <c>HelpCard.Extensions</c> and otherwise ignored.</description></item>
///     </list>
///     <para>
///         <c>_source.md</c> has the same layout as a card: front matter with the keys marked in its column (it needs
///         no <c>title</c> or <c>summary</c> but should have them; they describe the source in the index), and a body
///         that is the landing text of the source. Its translations are <c>_source.de.md</c> and so on.
///     </para>
///     <para>
///         <b>Value syntax and multiline rules.</b> A value is a plain, a single-quoted, or a double-quoted scalar. A
///         single-quoted scalar writes a quote as two quotes; a double-quoted one knows only the escapes backslash and
///         quote. A plain scalar must not start with <c>[ { | > &amp; * ! # % @</c> or a backtick, must not contain a
///         colon followed by a space or a space followed by <c>#</c>, and must not end with a colon: quote such a
///         value (<c>title: 'Setup: first steps'</c>). Lists are lines that start with <c>- </c>, indented or at
///         the first column; an item of <c>examples</c> is a block of <c>key: value</c> lines. Full-line comments that
///         start with <c>#</c> are allowed between entries. Not accepted (all are the error <c>syntax</c>): flow
///         collections such as <c>[a, b]</c>, anchors, aliases, tags, trailing comments, tabs in the indentation,
///         duplicate keys, and nesting deeper than a list of maps. <c>title</c> and <c>summary</c> are always one
///         line. The only multi-line value is the <c>content</c> of an example, written as a literal block with
///         <c>|</c> or <c>|-</c> on the key line and the text indented on the following lines (the indentation of the
///         first non-blank line, deeper than <c>content</c>); the block ends at the first non-blank line with less
///         indentation, blank lines and <c>#</c> lines inside are content, trailing line feeds are dropped, so
///         <c>|</c> and <c>|-</c> are the same. <c>|+</c>, <c>|2</c> and the folded <c>&gt;</c> are errors, and a block
///         anywhere else is <c>block-scalar-not-allowed</c>.
///     </para>
///     <para>
///         <b>The body.</b> The body is Markdown. Do not start a line with <c># </c> (the title is the heading) and do
///         not use the generated headings <c>## Cards</c>, <c>## Examples</c> and <c>## See also</c> (both are the
///         validator warning <c>heading-reserved</c>). Give every fenced block a language
///         (<c>fence-language</c>).
///     </para>
///     <para>
///         <b>Examples.</b> Each item of <c>examples</c> is a named, ready-to-use piece of text. Its keys are
///         <c>name</c> (a segment, unique in the scope: the source and the first segment of the card id),
///         <c>kind</c> (<c>command</c>, <c>source</c> or <c>snippet</c>), <c>language</c> (one lower-case word such as
///         <c>shell</c>, <c>yaml</c> or <c>csharp</c>), <c>description</c> (one line), <c>file</c> and <c>content</c>;
///         <c>name</c>, <c>kind</c>, <c>language</c> and <c>description</c> are always required.
///     </para>
///     <list type="table">
///         <listheader><term>Kind</term><description><c>file</c> and <c>content</c></description></listheader>
///         <item><term><c>command</c></term><description><c>file</c> not allowed; <c>content</c> required, one line that starts with the tool name, written <c>{{tool}} ...</c>. The test helper <c>ExampleCommandLine</c> checks that it parses.</description></item>
///         <item><term><c>source</c></term><description><c>file</c> required: the name to save the text as, and the data file to read when there is no <c>content</c>. <c>content</c> optional, a complete file.</description></item>
///         <item><term><c>snippet</c></term><description>As <c>source</c>, but a fragment that is never claimed to work alone.</description></item>
///     </list>
///     <para>
///         The data file of a <c>source</c> or <c>snippet</c> without <c>content</c> is read from the folder of the card
///         file, so a card <c>getting-started</c> (<c>Help/getting-started.md</c>) names <c>getting-started/settings.yaml</c>
///         and must find it (<c>example-file-missing</c>). File names use letters, digits, dots, hyphens and
///         underscores, with <c>/</c> between folders; <c>..</c>, a leading <c>/</c> and a drive are not allowed.
///         Duplicate example names in one scope are <c>duplicate-example</c>; empty or blank-first content, or a
///         <c>command</c> on several lines, is <c>example-invalid</c>.
///     </para>
///     <para>
///         <b>Links.</b> Links are checked when the catalog is built, forward references are fine, and they are
///         recognized in the body outside fenced blocks and inline code spans (<c>`[[x]]`</c> is plain text). Text
///         that starts like a link but is not one is <c>link-syntax</c>. Always write local ids: they keep working
///         when an application mounts your source under a prefix.
///     </para>
///     <list type="table">
///         <listheader><term>Link</term><description>Meaning</description></listheader>
///         <item><term><c>[[build/compile]]</c></term><description>The card with the local id <c>build/compile</c> in this source (or a source it imports). A bare id that exists in both is <c>ambiguous-reference</c>.</description></item>
///         <item><term><c>[[build/compile#settings]]</c></term><description>The example <c>settings</c> of that card or of a card below it.</description></item>
///         <item><term><c>[[#settings]]</c></term><description>The example <c>settings</c> of the card that holds the link.</description></item>
///         <item><term><c>[[other:build/compile|the compile card]]</c></term><description>A cross-source link: the card of the declared source <c>other</c> (the <c>name</c> in its <c>_source.md</c>), shown as the text after the bar (1 to 120 characters).</description></item>
///         <item><term><c>[[other:build/compile#settings|the settings]]</c></term><description>An example of a card of the source <c>other</c>.</description></item>
///     </list>
///     <para>
///         A cross-source link to a source that is not part of the application degrades to its text (an informational
///         note <c>external-source-absent</c>; <c>HelpValidationOptions.RequireExternalSources</c> turns it into an
///         error); when the source is present but has no such card it is the error <c>link-target</c> (or
///         <c>example-target</c> for an example). How a link reads in the output depends on the renderer: the
///         command-line helper writes a card link as the command that prints the card and a local example link as
///         <c>--example name</c>; the agent tool and <c>Render</c> write <c>the card `id`</c>; Markdown output writes a
///         Markdown link (see <c>HelpLinkStyle</c> in <c>DemaConsulting.HelpCards.Presentation</c> and
///         <c>HelpCliOptions</c>).
///     </para>
///     <para>
///         <b>Tokens.</b> A token is a name in double braces. HelpCards replaces tokens when it reads a file, in the
///         title, summary, examples and body (code blocks and code spans included). The names are letters and digits,
///         case-sensitive. <c>{{tool}}</c> and <c>{{version}}</c> always exist; they come from the builder
///         (<c>ToolName</c>, <c>ToolVersion</c>). More tokens belong to one source and are set with
///         <c>HelpSource.WithTokens</c>; a source sees its own tokens, those of the sources it imports, and the two
///         built-in ones. An unknown token is <c>token-unknown</c>; a name defined twice, or a built-in name
///         redefined, is <c>token-redefined</c>.
///     </para>
///     <para>
///         <b>Cultures.</b> A translation is a file with a culture suffix before the extension (<c>compile.de.md</c>,
///         <c>compile.de-DE.md</c>). The application chooses the culture with <c>HelpCatalogBuilder.Culture</c>; the
///         default is the invariant culture, which reads only the neutral files, and the current thread culture is
///         never used. For each card the loader tries <c>de-DE</c>, then <c>de</c>, then the neutral file, and the
///         first one that exists supplies the body. A translation may hold only <c>title</c>, <c>summary</c>,
///         <c>aliases</c>, examples with just <c>name</c> and <c>description</c>, and a body (<c>translation-key</c>
///         otherwise); missing texts fall back to the neutral ones one by one; a translation without a neutral file is
///         <c>translation-orphan</c>. Generated headings such as <em>Cards</em> are English.
///     </para>
///     <para>
///         <b>Key rules.</b> Creating a source performs no file I/O, so a static field costs nothing at start-up; the
///         files are read only when a catalog is built. A source is immutable (every <c>With...</c> and
///         <c>Imports</c> call returns a new source) and safe for concurrent use, and its public signatures use only
///         base class library types, so it can appear in the API of any library. Wrong arguments throw
///         <c>ArgumentException</c> or <c>ArgumentNullException</c>; defects in the cards are never reported here,
///         but by <c>HelpCatalog.Create</c> as a <c>HelpCatalogException</c>. A source cannot be created from a
///         folder on disk; ship the cards embedded. To check in a test that the embedded files match the folder, use
///         <c>HelpCardValidator.CompareToDirectory</c> (see <c>DemaConsulting.HelpCards.Testing</c>).
///     </para>
/// </remarks>
/// <example>
///     A library <c>MyLib</c> with a minimal card (<c>reports</c>: only <c>title</c> and <c>summary</c> are required),
///     a card that uses most keys with a child and a data file, and a <c>_source.md</c>. The project layout is:
///     <code language="text">
/// MyLib/
/// ├── MyLib.csproj           (PackageReference to DemaConsulting.HelpCards; nothing else is needed)
/// └── Help/
///     ├── _source.md
///     ├── reports.md
///     ├── getting-started.md
///     └── getting-started/
///         ├── install.md
///         └── settings.yaml
/// </code>
///     <c>Help/_source.md</c> declares the source:
///     <code language="markdown">
/// ---
/// name: mylib
/// title: MyLib
/// summary: Help cards of MyLib.
/// ---
/// MyLib reads data files and writes reports.
/// </code>
///     <c>Help/reports.md</c> is the smallest valid card:
///     <code language="markdown">
/// ---
/// title: Reports
/// summary: Write a report from loaded data.
/// ---
/// Reports are Markdown files.
/// </code>
///     <c>Help/getting-started.md</c> uses the other keys and every kind of link:
///     <code language="markdown">
/// ---
/// title: Getting started
/// summary: Install MyLib and run it for the first time.
/// aliases:
///   - start
/// tags:
///   - tutorial
/// order: 10
/// children:
///   - install
/// see-also:
///   - reports
/// examples:
///   - name: load
///     kind: command
///     language: shell
///     content: '{{tool}} load data.csv'
///     description: Load a data file.
///   - name: settings
///     kind: source
///     language: yaml
///     file: getting-started/settings.yaml
///     description: A settings file for the load command.
/// x-audience: beginner
/// ---
/// Install the package first, see [[getting-started/install]], then run [[#load]] with the
/// settings of [[#settings]]. Reports are described in [[reports]]; the graph library has
/// [[graph:layout|a layout card]].
/// </code>
///     <c>Help/getting-started/install.md</c> is a child of <c>getting-started</c>, so it needs the file above:
///     <code language="markdown">
/// ---
/// title: Install
/// summary: Install MyLib with the dotnet CLI.
/// ---
/// Run `dotnet add package MyLib`, then go back to [[getting-started]].
/// </code>
///     <c>Help/getting-started/settings.yaml</c> is the data file that the example <c>settings</c> names:
///     <code language="yaml">
/// delimiter: ","
/// header: true
/// </code>
///     The program that exposes the source and prints the card (the printed text is the card as
///     <c>HelpCatalog.Render</c> writes it):
///     <code language="csharp">
/// using System;
/// using DemaConsulting.HelpCards.Catalog;
/// using DemaConsulting.HelpCards.Loading;
///
/// // Creating the source reads nothing; the catalog reads the embedded cards when it is built.
/// HelpCatalog catalog = HelpCatalog.Create(builder => builder
///     .AddSource(MyLibHelp.Source)
///     .ToolName("mytool"));
///
/// HelpCard card = catalog.Find("getting-started")!;
/// Console.WriteLine(catalog.Render(card));
///
/// public static class MyLibHelp
/// {
///     public static HelpSource Source { get; } = HelpSource.FromAssembly(typeof(MyLibHelp).Assembly);
/// }
/// </code>
///     The output (a link reads as words that name the target, and the cross-source link to <c>graph</c>, a source
///     that this application does not include, shows only its text):
///     <code language="text">
/// # Getting started
///
/// Install MyLib and run it for the first time.
/// Also known as: start
/// Tags: tutorial
///
/// Install the package first, see the card `getting-started/install`, then run the example `load` with the
/// settings of the example `settings`. Reports are described in the card `reports`; the graph library has
/// a layout card.
///
/// ## Cards
///
/// - getting-started/install
///   Install MyLib with the dotnet CLI.
///
/// ## Examples
///
/// - load [command]: Load a data file.
/// - settings [source] getting-started/settings.yaml: A settings file for the
///   load command.
///
/// ## See also
///
/// - reports
///   Write a report from loaded data.
/// </code>
/// </example>
internal static class NamespaceDoc
{
}
