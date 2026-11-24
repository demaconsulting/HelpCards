namespace DemaConsulting.HelpCards.Testing;

/// <summary>
///     Helpers that prove, in your own tests, that your help cards are valid, that your command examples parse and
///     that the embedded files match what you ship: <c>HelpCardValidator</c> (with <c>HelpValidationOptions</c>
///     and your own <c>IHelpCardRule</c> rules), <c>HelpExamples</c>, <c>ExampleCommandLine</c>, <c>HelpExampleRunner</c>
///     (which runs your command examples through your own delegate and compares the result they state) and
///     <c>HelpCardMarkdown</c>. Start at <c>HelpCardValidator.AssertValid</c>. The helpers do not depend on a test
///     framework.
/// </summary>
/// <remarks>
///     <para>
///         The common pattern in a test project that references your library is four steps:
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
///             <c>ExampleCommandLine.TryParse</c> proves that a <c>command</c> example splits into arguments.
///         </description></item>
///         <item><description>
///             Run the examples. A <c>command</c> example can state the exit code (<c>exit</c>) and the output text
///             (<c>expect</c>) it gives. <c>HelpExampleRunner.Run</c> saves the <c>source</c> examples of each card to a
///             scratch folder, splits every command, calls your delegate to run it (HelpCards never starts a process or a
///             shell), and compares the exit code and the output; <c>AssertSucceeded</c> fails the test with every
///             difference. This is the supported way to prove that the help of a product is true.
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
///         <b>Tests that build a catalog need valid cards.</b> A test that builds a catalog itself, such as the
///         <c>CommandExamples_Parse</c> test below, calls <c>HelpCatalog.Create</c>, which throws a
///         <c>HelpCatalogException</c> (not an assertion failure) for an invalid card. A broken card therefore fails
///         every test that builds a catalog, not only the one that calls <c>AssertValid</c>; read the first failure.
///         <c>HelpCardValidator.CompareToDirectory</c> compares the embedded files with a folder on disk. It is not part
///         of the pattern above: a test that reads the checked-in files of the repository is not a unit test of the
///         product, and the packaged targets embed every file below <c>Help</c>, so <c>AssertValid</c> and the runner
///         already prove what ships. Keep <c>CompareToDirectory</c> for a packaging or release check outside the unit
///         tests; its remarks give the reason.
///     </para>
/// </remarks>
/// <example>
///     An xUnit test class for a library <c>MyLib</c> that exposes <c>MyLibHelp.Source</c> (the static class of the
///     example on the page of <c>DemaConsulting.HelpCards.Loading</c>) and a class <c>MyTool</c> whose method
///     <c>Run(string[] arguments, string workingDirectory, TextWriter output)</c> is the entry point of the tool and
///     returns its exit code: one test asserts the cards are valid, with warnings as errors, one proves every command
///     example parses, and one runs every command example and compares the result that the example states.
///     <code language="csharp">
/// using System.Collections.Generic;
/// using System.IO;
/// using System.Linq;
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
///
///     [Fact]
///     public void CommandExamples_GiveTheStatedResult()
///     {
///         HelpCatalog catalog = HelpCatalog.Create(builder => builder
///             .AddSource(MyLibHelp.Source)
///             .ToolName("mytool"));
///
///         // The delegate runs one command the way a user would; the runner has saved the source examples of the
///         // card to invocation.WorkingDirectory, so the files the command names exist.
///         HelpExampleRunReport report = HelpExampleRunner.Run(catalog, invocation =>
///         {
///             var output = new StringWriter();
///             int exitCode = MyTool.Run(invocation.Arguments.ToArray(), invocation.WorkingDirectory, output);
///             return new HelpExampleOutcome(exitCode, output.ToString());
///         });
///
///         Assert.True(report.CommandsRun > 0);
///         report.AssertSucceeded();
///     }
/// }
/// </code>
/// </example>
internal static class NamespaceDoc
{
}
