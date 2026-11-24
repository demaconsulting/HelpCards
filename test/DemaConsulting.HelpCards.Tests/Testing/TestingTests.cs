using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Testing;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Testing;

/// <summary>
///     Subsystem tests for Testing: the validator, the example helpers, and the Markdown helpers working together
///     on catalogs that the Composition subsystem creates.
/// </summary>
/// <remarks>
///     The tests use the embedded <c>simple</c> fixture and in-memory sources, and only the public face of the
///     Testing subsystem. Nothing is mocked and no test framework is referenced by the subsystem itself.
/// </remarks>
public class TestingTests
{
    /// <summary>
    ///     The tokens of the simple fixture.
    /// </summary>
    private static readonly Dictionary<string, string> SimpleTokens = new() { ["limit"] = "42" };

    /// <summary>
    ///     A card with a body that breaks every rule of the body and has a bad command, unused tokens aside.
    /// </summary>
    private const string DefectiveCard =
        "---\n"
        + "title: Defective\n"
        + "summary: A summary that is much too long for a root card because it keeps going and going.\n"
        + "examples:\n"
        + "  - name: bad\n"
        + "    kind: command\n"
        + "    language: shell\n"
        + "    content: 'demo run > out.txt'\n"
        + "    description: Redirects.\n"
        + "---\n"
        + "# Heading\n"
        + "```\n"
        + "code\n"
        + "```\n"
        + "```text\n"
        + "never closed\n";

    /// <summary>
    ///     Proves that a valid source passes, a defective source reports its errors without an exception, and the
    ///     assertion throws one exception for the errors.
    /// </summary>
    [Fact]
    public void Testing_Validation_ValidAndDefectiveSources_ReportAndAssert()
    {
        // Arrange
        var valid = CatalogBuilding.Fixture("simple", SimpleTokens);
        var defective = HelpSource.FromFiles(SourceReading.Files(("a.md", SourceReading.Card(body: "See [[missing]]."))), "test");

        // Act
        var none = HelpCardValidator.Validate(valid);
        var errors = HelpCardValidator.Validate(defective);
        var exception = Assert.Throws<HelpCatalogException>(() => HelpCardValidator.AssertValid(defective));
        HelpCardValidator.AssertValid(valid);

        // Assert
        Assert.Empty(none);
        var error = Assert.Single(errors);
        Assert.Equal((HelpSeverity.Error, RuleIds.LinkTarget, "a.md"), (error.Severity, error.RuleId, error.File));
        Assert.Equal(errors, exception.Problems);
    }

    /// <summary>
    ///     Proves that one defective card reports each quality rule once, each with its file and line where it has one.
    /// </summary>
    [Fact]
    public void Testing_QualityRules_DefectiveCard_ReportsEveryRule()
    {
        // Arrange
        var source = HelpSource.FromFiles(SourceReading.Files(("defective.md", DefectiveCard)), "demo")
            .WithTokens(new Dictionary<string, string> { ["spare"] = "x" });
        var options = new HelpValidationOptions { MaxCardChars = 20, MaxRootIndexChars = 20, MaxLineLength = 8 };

        // Act
        var problems = HelpCardValidator.Validate(source, options);

        // Assert
        var rules = problems.Select(problem => problem.RuleId).Distinct().Order(StringComparer.Ordinal).ToList();
        Assert.Equal(
            [
                RuleIds.CardSize,
                RuleIds.CommandParse,
                RuleIds.FenceLanguage,
                RuleIds.FenceUnclosed,
                RuleIds.HeadingReserved,
                RuleIds.LineLength,
                RuleIds.SummaryLength,
                RuleIds.UnusedToken
            ],
            rules);
        Assert.All(problems, problem => Assert.Equal(HelpSeverity.Warning, problem.Severity));
        var heading = Assert.Single(problems, problem => problem.RuleId == RuleIds.HeadingReserved);
        Assert.Equal(("defective.md", 11), (heading.File, heading.Line));
        var unclosed = Assert.Single(problems, problem => problem.RuleId == RuleIds.FenceUnclosed);
        Assert.Equal(15, unclosed.Line);
    }

    /// <summary>
    ///     Proves that a product rule, the option for external sources, and warnings that fail the assertion each
    ///     change the result of the validation.
    /// </summary>
    [Fact]
    public void Testing_Options_RulesExternalSourcesAndWarnings_ChangeTheResult()
    {
        // Arrange
        var source = CatalogBuilding.Fixture("simple", SimpleTokens);
        var ruleOptions = new HelpValidationOptions { Rules = [new GuideRule()] };
        var strictRuleOptions = new HelpValidationOptions { Rules = [new GuideRule()], WarningsAreErrors = true };
        var externalOptions = new HelpValidationOptions { RequireExternalSources = true };

        // Act
        var lenient = HelpCardValidator.Validate(source);
        var withRule = HelpCardValidator.Validate(source, ruleOptions);
        var external = HelpCardValidator.Validate(source, externalOptions);
        HelpCardValidator.AssertValid(source, ruleOptions);
        var exception = Assert.Throws<HelpCatalogException>(() => HelpCardValidator.AssertValid(source, strictRuleOptions));

        // Assert
        Assert.Empty(lenient);
        var ruleProblem = Assert.Single(withRule);
        Assert.Equal((HelpSeverity.Warning, "guide-rule"), (ruleProblem.Severity, ruleProblem.RuleId));
        Assert.NotEmpty(external);
        Assert.All(external, problem => Assert.Equal(RuleIds.ExternalSourceMissing, problem.RuleId));
        Assert.Equal(withRule, exception.Problems);
    }

    /// <summary>
    ///     A rule of a product that flags the guide card.
    /// </summary>
    private sealed class GuideRule : IHelpCardRule
    {
        /// <inheritdoc />
        public IEnumerable<HelpProblem> Check(HelpCard card)
        {
            return card.Id == "guide"
                ? [new HelpProblem(HelpSeverity.Warning, "guide-rule", card.SourceName, card.File, 1, "The guide is flagged.")]
                : [];
        }
    }
    /// <summary>
    ///     Proves that the commands of the simple fixture are enumerated and parse into the arguments after the tool name.
    /// </summary>
    [Fact]
    public void Testing_Examples_SimpleFixtureCommands_AreEnumeratedAndParsed()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act
        var commands = HelpExamples.Enumerate(catalog, [HelpExample.CommandKind]).ToList();
        var parsed = commands
            .Select(pair => ExampleCommandLine.TryParse(pair.Example.Content, "simple", out var arguments, out var error) ? string.Join(' ', arguments) : error)
            .ToList();

        // Assert
        Assert.Equal("guide", commands[0].Card.Id);
        Assert.Equal("run", commands[0].Example.Name);
        Assert.Equal("run input.txt", parsed[0]);
        Assert.All(HelpExamples.Enumerate(catalog), pair => Assert.Contains(pair.Example, pair.Card.Examples));
    }

    /// <summary>
    ///     Proves that the Markdown helpers read the headings, sections, and bullets of a card body from a catalog.
    /// </summary>
    [Fact]
    public void Testing_Markdown_CardBody_ReadsHeadingsSectionsAndBullets()
    {
        // Arrange
        var body = "Intro.\n\n## Options\n\n- `--a`: One.\n- `--b`: Two.\n\n## Notes\n\nText.";
        var catalog = CatalogBuilding.FromFiles(("a.md", SourceReading.Card(body: body)));

        // Act
        var card = catalog.Find("a")!;
        var headings = HelpCardMarkdown.Headings(card.Body);
        var section = HelpCardMarkdown.GetSection(card.Body, "Notes");
        var bullets = HelpCardMarkdown.Bullets(card.Body, "Options");

        // Assert
        Assert.Equal(["Options", "Notes"], headings);
        Assert.Equal("Text.", section);
        Assert.Equal(["`--a`: One.", "`--b`: Two."], bullets);
    }

    /// <summary>
    ///     Proves that the comparison of resources with a directory finds the files that are on one side only.
    /// </summary>
    [Fact]
    public void Testing_Resources_CompareToDirectory_FindsFilesOnOneSideOnly()
    {
        // Arrange
        var source = CatalogBuilding.Fixture("simple");
        using var directory = new TemporaryDirectory();
        foreach (var path in source.Provider.ListFiles())
        {
            directory.Write(path, source.Provider.ReadText(path));
        }

        directory.Write("forgotten.md", "text");

        // Act
        var problems = HelpCardValidator.CompareToDirectory(source, directory.FullPath);

        // Assert
        var problem = Assert.Single(problems);
        Assert.Equal((RuleIds.ResourcesMismatch, "forgotten.md"), (problem.RuleId, problem.File));
    }
}
