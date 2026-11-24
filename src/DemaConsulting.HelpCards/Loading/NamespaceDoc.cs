namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     Where the help cards of one library or tool come from: <c>HelpSource</c>, the single public type of this
///     namespace. A library exposes one static <c>HelpSource</c> (made with <c>HelpSource.FromAssembly</c>) so an
///     application can compose its cards without knowing where the files are. Start at <c>HelpSource</c>.
/// </summary>
/// <remarks>
///     <para>
///         The common pattern for a library that ships help is three steps:
///     </para>
///     <list type="number">
///         <item><description>
///             Write the cards. Put one Markdown file per topic, with a small front-matter block, in the
///             <c>Help</c> folder of the project; an optional <c>_source.md</c> names the source and describes it.
///             The package build files of HelpCards embed that folder in the assembly when the project references
///             the package directly (the MSBuild properties are <c>HelpCardsEnabled</c>, <c>HelpCardsDirectory</c>
///             and <c>HelpCardsPrefix</c>; the resource prefix defaults to <c>{AssemblyName}.Help</c>).
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
///         Key rules: creating a source performs no file I/O, so a static field costs nothing at start-up; the
///         files are read only when a catalog is built. A source is immutable (every <c>With...</c> and
///         <c>Imports</c> call returns a new source) and safe for concurrent use, and its public signatures use only
///         base class library types, so it can appear in the API of any library. Wrong arguments throw
///         <c>ArgumentException</c> or <c>ArgumentNullException</c>; defects in the cards are never reported here,
///         but by <c>HelpCatalog.Create</c> as a <c>HelpCatalogException</c>. A source cannot be created from a
///         folder on disk; ship the cards embedded.
///     </para>
/// </remarks>
/// <example>
///     A library <c>MyLib</c> with two cards, exposed as a source and printed through a catalog. The project layout
///     and the card files are:
///     <code language="text">
/// MyLib/
/// ├── MyLib.csproj
/// └── Help/
///     ├── _source.md
///     └── getting-started.md
/// </code>
///     <code language="markdown">
/// ---
/// name: mylib
/// title: MyLib
/// summary: Help cards of MyLib.
/// ---
/// MyLib reads data files and writes reports.
/// </code>
///     <code language="markdown">
/// ---
/// title: Getting started
/// summary: Install MyLib and run it for the first time.
/// tags:
///   - tutorial
/// examples:
///   - name: load
///     kind: command
///     language: shell
///     content: '{{tool}} load data.csv'
///     description: Load a data file.
/// ---
/// Install the package first, then run [[#load]].
/// </code>
///     The program that exposes the source and prints the card:
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
/// </example>
internal static class NamespaceDoc
{
}
