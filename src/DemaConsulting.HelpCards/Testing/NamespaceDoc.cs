namespace DemaConsulting.HelpCards.Testing;

/// <summary>
///     Helpers that prove, in your own tests, that your help cards are valid, that your command examples parse and
///     that the embedded files match the files on disk: <c>HelpCardValidator</c> (with <c>HelpValidationOptions</c>
///     and your own <c>IHelpCardRule</c> rules), <c>HelpExamples</c>, <c>ExampleCommandLine</c> and
///     <c>HelpCardMarkdown</c>. Start at <c>HelpCardValidator.AssertValid</c>. The helpers do not depend on a test
///     framework.
/// </summary>
/// <remarks>
///     <para>
///         The common pattern in a test project that references your library is three steps:
///     </para>
///     <list type="number">
///         <item><description>
///             Assert the cards. <c>HelpCardValidator.AssertValid(MyLibHelp.Source)</c> builds a flat catalog of the
///             source (and the sources it imports) and throws a <c>HelpCatalogException</c> that lists the defects it
///             finds. The checks run in stages (a front-matter error hides the link checks of its file), so after a
///             fix the test can report more; see <c>HelpProblem</c>.
///             Pass an <c>Action&lt;HelpCatalogBuilder&gt;</c> to check exactly the composition of an application, or a
///             <c>HelpCatalog</c> that already exists.
///         </description></item>
///         <item><description>
///             Tighten the quality rules with <c>HelpValidationOptions</c>: limits for card, summary and line length,
///             <c>RequireExternalSources</c>, your own <c>Rules</c>, and <c>WarningsAreErrors</c> so warnings fail
///             <c>AssertValid</c> too.
///         </description></item>
///         <item><description>
///             Check the examples. <c>HelpExamples.Enumerate</c> lists the examples of a catalog by kind, and
///             <c>ExampleCommandLine.TryParse</c> proves that a <c>command</c> example splits into arguments. Use
///             <c>HelpCardValidator.CompareToDirectory</c> to prove the embedded files match the folder that holds
///             your cards.
///         </description></item>
///     </list>
///     <para>
///         Key rules: <c>Validate</c> returns every problem (the errors of building the catalog first, then the
///         warnings of the quality rules) and never throws for a defect of a card; <c>AssertValid</c> throws on
///         errors, and on warnings only when <c>WarningsAreErrors</c> is set. The rule ids, with severity and
///         meaning, are in one table in the remarks of <c>HelpProblem</c> (<c>DemaConsulting.HelpCards.Catalog</c>);
///         a problem has line 0 when no single line applies. Wrong arguments throw
///         <c>ArgumentException</c> or <c>ArgumentNullException</c>, and <c>TryParse</c> reports a command it cannot
///         parse through its <c>error</c> output instead of throwing. Custom rules and callbacks run on the calling
///         thread and should be free of side effects. The helpers work on the types of
///         <c>DemaConsulting.HelpCards.Loading</c> and <c>DemaConsulting.HelpCards.Catalog</c>; ship them only in
///         test projects, never in the library that carries the cards. A test project may reference the package
///         for these helpers without embedding anything (it embeds only a <c>Help</c> folder of its own).
///     </para>
///     <para>
///         <b>Two tests, both need valid cards.</b> A test that builds a catalog itself, such as the
///         <c>CommandExamples_Parse</c> test below, calls <c>HelpCatalog.Create</c>, which throws a
///         <c>HelpCatalogException</c> (not an assertion failure) for an invalid card. A broken card therefore fails
///         every test that builds a catalog, not only the one that calls <c>AssertValid</c>; read the first failure.
///         To prove the embedded files match the folder on disk, add a test with <c>CompareToDirectory</c>; its
///         remarks show how to find the <c>Help</c> folder from a test.
///     </para>
/// </remarks>
/// <example>
///     An xUnit test class for a library <c>MyLib</c> that exposes <c>MyLibHelp.Source</c> (the static class of the
///     example on the page of <c>DemaConsulting.HelpCards.Loading</c>): one test asserts the cards
///     are valid, with warnings as errors, and one proves every command example parses.
///     <code language="csharp">
/// using System.Collections.Generic;
/// using DemaConsulting.HelpCards.Catalog;
/// using DemaConsulting.HelpCards.Testing;
/// using Xunit;
///
/// public class HelpTests
/// {
///     [Fact]
///     public void Cards_AreValid()
///     {
///         // Throws a HelpCatalogException that lists the defects found; warnings fail the test too.
///         HelpCardValidator.AssertValid(MyLibHelp.Source, new HelpValidationOptions { WarningsAreErrors = true });
///     }
///
///     [Fact]
///     public void CommandExamples_Parse()
///     {
///         // HelpCatalog.Create throws a HelpCatalogException for an invalid card, so a broken card fails this
///         // test as well as Cards_AreValid; fix the cards first.
///         HelpCatalog catalog = HelpCatalog.Create(builder => builder
///             .AddSource(MyLibHelp.Source)
///             .ToolName("mytool"));
///
///         foreach ((HelpCard card, HelpExample example) in HelpExamples.Enumerate(catalog, [HelpExample.CommandKind]))
///         {
///             bool parsed = ExampleCommandLine.TryParse(example.Content, "mytool", out IReadOnlyList&lt;string&gt; arguments, out string? error);
///
///             Assert.True(parsed, $"{card.Id}#{example.Name}: {error}");
///             Assert.NotEmpty(arguments);
///         }
///     }
/// }
/// </code>
/// </example>
internal static class NamespaceDoc
{
}
