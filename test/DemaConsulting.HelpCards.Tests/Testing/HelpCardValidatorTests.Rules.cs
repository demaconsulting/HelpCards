using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Testing;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Testing;

/// <summary>
///     Unit tests for the quality rules of <see cref="HelpCardValidator"/>.
/// </summary>
public partial class HelpCardValidatorTests
{
    /// <summary>
    ///     The line of a card file on which a body starts when the card has no extra front matter.
    /// </summary>
    private const int FirstBodyLine = 5;

    /// <summary>
    ///     A card rule of a test that reports one warning for the card with a given identifier.
    /// </summary>
    /// <param name="cardId">The identifier of the card to report.</param>
    private sealed class ReportingRule(string cardId) : IHelpCardRule
    {
        /// <inheritdoc />
        public IEnumerable<HelpProblem> Check(HelpCard card)
        {
            return card.Id == cardId
                ? [new HelpProblem(HelpSeverity.Warning, "no-commands", card.SourceName, card.File, 3, "The card has a command.")]
                : [];
        }
    }

    /// <summary>
    ///     Proves that two source examples of one card with the same save-as name are a warning that names both, at the card
    ///     file, and that different names and a snippet with the same name are not.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_SameSaveAsTwice_WarnsSaveAsDuplicate()
    {
        // Arrange: two sources saved as data.yaml, a snippet with the same name, and two different names in another card
        var duplicated = SourceReading.Card(extraFrontMatter: "examples:\n"
            + "  - name: one\n    kind: source\n    language: yaml\n    file: data.yaml\n    content: 'a: 1'\n    description: D.\n"
            + "  - name: two\n    kind: source\n    language: yaml\n    file: data.yaml\n    content: 'a: 2'\n    description: D.\n"
            + "  - name: three\n    kind: snippet\n    language: yaml\n    file: data.yaml\n    content: 'a: 3'\n    description: D.\n");
        var distinct = SourceReading.Card(extraFrontMatter: "examples:\n"
            + "  - name: one\n    kind: source\n    language: yaml\n    file: one.yaml\n    content: 'a: 1'\n    description: D.\n"
            + "  - name: two\n    kind: source\n    language: yaml\n    file: two.yaml\n    content: 'a: 2'\n    description: D.\n");

        // Act
        var problems = ValidateFiles(null, ("a.md", duplicated), ("b.md", distinct));

        // Assert
        var problem = Assert.Single(problems);
        Assert.Equal((HelpSeverity.Warning, RuleIds.SaveAsDuplicate, "a.md", 0), (problem.Severity, problem.RuleId, problem.File, problem.Line));
        Assert.Contains("'one', 'two'", problem.Message, StringComparison.Ordinal);
        Assert.Contains("'data.yaml'", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a summary over the limit for roots or for other cards is reported, and one at the limit is not.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_LongSummary_ReportsSummaryLength()
    {
        // Act
        var problems = ValidateFiles(
            null,
            ("a.md", SourceReading.Card(summary: new string('r', 61))),
            ("a/b.md", SourceReading.Card(summary: new string('c', 101))),
            ("a/c.md", SourceReading.Card(summary: new string('c', 100))),
            ("d.md", SourceReading.Card(summary: new string('r', 60))));

        // Assert
        Assert.Equal(2, problems.Count);
        Assert.All(problems, problem =>
        {
            Assert.Equal(RuleIds.SummaryLength, problem.RuleId);
            Assert.Equal(HelpSeverity.Warning, problem.Severity);
            Assert.Equal("test", problem.SourceName);
            Assert.Equal(0, problem.Line);
        });
        Assert.Equal("a.md", problems[0].File);
        Assert.Contains("61 characters", problems[0].Message);
        Assert.Contains("at most 60", problems[0].Message);
        Assert.Equal("a/b.md", problems[1].File);
        Assert.Contains("at most 100", problems[1].Message);
    }

    /// <summary>
    ///     Proves that a card whose rendered text is over the limit is reported and that the limit for a root
    ///     with children is separate.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_LargeCard_ReportsCardSize()
    {
        // Arrange
        var files = new[]
        {
            ("a.md", SourceReading.Card(body: new string('x', 300))),
            ("a/b.md", SourceReading.Card(body: new string('y', 300))),
            ("c.md", SourceReading.Card(body: "Small."))
        };

        // Act
        var cardLimit = ValidateFiles(new HelpValidationOptions { MaxCardChars = 200, MaxRootIndexChars = 10000 }, files);
        var rootLimit = ValidateFiles(new HelpValidationOptions { MaxCardChars = 10000, MaxRootIndexChars = 200 }, files);
        var noLimit = ValidateFiles(null, files);

        // Assert
        Assert.Equal(["a/b.md"], cardLimit.Select(problem => problem.File));
        Assert.Equal(["a.md"], rootLimit.Select(problem => problem.File));
        Assert.All(cardLimit.Concat(rootLimit), problem => Assert.Equal(RuleIds.CardSize, problem.RuleId));
        Assert.Contains("renders as", cardLimit[0].Message);
        Assert.Empty(noLimit);
    }

    /// <summary>
    ///     Proves that a level-one heading and the headings of the generated sections are reported with the line of
    ///     the file, and that the same text inside a fence is not reported.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_ReservedHeadings_ReportHeadingReservedWithLine()
    {
        // Arrange
        var body = "Intro.\n# Title\n## Cards\n## Examples\n## See also\n## Other\n```text\n# In a fence\n## Cards\n```";

        // Act
        var problems = ValidateFiles(null, ("a.md", SourceReading.Card(body: body)));

        // Assert
        Assert.All(problems, problem => Assert.Equal(RuleIds.HeadingReserved, problem.RuleId));
        Assert.Equal(
            [FirstBodyLine + 1, FirstBodyLine + 2, FirstBodyLine + 3, FirstBodyLine + 4],
            problems.Select(problem => problem.Line));
        Assert.All(problems, problem => Assert.Equal("a.md", problem.File));
        Assert.Contains("level-one", problems[0].Message);
        Assert.Contains("'## Cards'", problems[1].Message);
    }

    /// <summary>
    ///     Proves that a fenced block without a language and one that is never closed are reported at their opening lines.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_Fences_ReportLanguageAndUnclosed()
    {
        // Arrange
        var body = "Text.\n```\nno language\n```\n\n```text\nfine\n```\n\n~~~yaml\nnever closed";

        // Act
        var problems = ValidateFiles(null, ("a.md", SourceReading.Card(body: body)));

        // Assert
        Assert.Equal(2, problems.Count);
        Assert.Equal((RuleIds.FenceLanguage, FirstBodyLine + 1), (problems[0].RuleId, problems[0].Line));
        Assert.Equal((RuleIds.FenceUnclosed, FirstBodyLine + 9), (problems[1].RuleId, problems[1].Line));
        Assert.All(problems, problem => Assert.Equal(HelpSeverity.Warning, problem.Severity));
    }

    /// <summary>
    ///     Proves that a token that a source defines but no file uses is reported without a file, and that a used
    ///     token and the built-in tokens are not.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_UnusedToken_ReportsUnusedToken()
    {
        // Arrange
        var source = HelpSource.FromFiles(
                SourceReading.Files(("a.md", SourceReading.Card(body: "Uses {{used}} and {{tool}} but not the other.")), ("b.md", SourceReading.Card(body: "More."))),
                "test")
            .WithTokens(new Dictionary<string, string> { ["used"] = "1", ["spare"] = "2", ["alsoSpare"] = "3" });

        // Act
        var problems = HelpCardValidator.Validate(source);

        // Assert
        Assert.Equal(2, problems.Count);
        Assert.All(problems, problem =>
        {
            Assert.Equal(RuleIds.UnusedToken, problem.RuleId);
            Assert.Equal(HelpSeverity.Warning, problem.Severity);
            Assert.Equal("test", problem.SourceName);
            Assert.Equal(string.Empty, problem.File);
            Assert.Equal(0, problem.Line);
        });
        Assert.Contains("'alsoSpare'", problems[0].Message);
        Assert.Contains("'spare'", problems[1].Message);
    }

    /// <summary>
    ///     Proves that a file that is not valid UTF-8 is reported with its location instead of aborting the scan.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_UnreadableDataFile_ReportsSourceReadInsteadOfThrowing()
    {
        // Arrange: a valid card plus a data file that no card references and that holds invalid UTF-8
        using var directory = new TemporaryDirectory();
        directory.Write("a.md", SourceReading.Card(body: "Uses {{used}}."));
        File.WriteAllBytes(Path.Combine(directory.FullPath, "data.bin"), [0x61, 0xFF, 0x62]);
        var source = HelpSource.FromProvider(new DirectoryFileProvider(directory.FullPath))
            .WithName("test")
            .WithTokens(new Dictionary<string, string> { ["used"] = "1", ["spare"] = "2" });

        // Act
        var problems = HelpCardValidator.Validate(source);

        // Assert: the unreadable file is reported at its path and no token is guessed to be unused
        var problem = Assert.Single(problems, p => p.RuleId == RuleIds.SourceRead);
        Assert.Equal(HelpSeverity.Error, problem.Severity);
        Assert.Equal("data.bin", problem.File);
        Assert.DoesNotContain(problems, p => p.RuleId == RuleIds.UnusedToken);
    }

    /// <summary>
    ///     Proves that a token used only in an example or a title counts as used.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_TokenUsedInTitle_IsNotReported()
    {
        // Arrange
        var source = HelpSource.FromFiles(
                SourceReading.Files(("a.md", SourceReading.Card(title: "The {{name}} card"))),
                "test")
            .WithTokens(new Dictionary<string, string> { ["name"] = "demo" });

        // Act
        var problems = HelpCardValidator.Validate(source);

        // Assert
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that lines longer than the limit are reported when a limit is set, and not otherwise.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_LongLines_ReportLineLengthOnlyWithLimit()
    {
        // Arrange
        var files = new[] { ("a.md", SourceReading.Card(body: $"Short.\n{new string('w', 31)}\n```text\n{new string('c', 32)}\n```")) };

        // Act
        var off = ValidateFiles(null, files);
        var on = ValidateFiles(new HelpValidationOptions { MaxLineLength = 30 }, files);

        // Assert
        Assert.Empty(off);
        Assert.Equal([FirstBodyLine + 1, FirstBodyLine + 3], on.Select(problem => problem.Line));
        Assert.All(on, problem => Assert.Equal(RuleIds.LineLength, problem.RuleId));
        Assert.Contains("31 characters", on[0].Message);
    }

    /// <summary>
    ///     Proves that a command example that cannot be parsed, or does not start with the tool name, is reported.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_CommandExamples_ReportCommandParse()
    {
        // Arrange
        var examples = "examples:\n"
            + "  - name: good\n    kind: command\n    language: shell\n    content: 'helper run \"a b\"'\n    description: Fine.\n"
            + "  - name: piped\n    kind: command\n    language: shell\n    content: 'helper run | more'\n    description: Piped.\n"
            + "  - name: other\n    kind: command\n    language: shell\n    content: 'other run'\n    description: Other tool.\n"
            + "  - name: text\n    kind: snippet\n    language: text\n    file: t.txt\n    content: 'a | b'\n    description: Not a command.\n";
        var source = HelpSource.FromFiles(SourceReading.Files(("a.md", SourceReading.Card(extraFrontMatter: examples))), "test");

        // Act
        var problems = HelpCardValidator.Validate(builder => builder.AddSource(source).ToolName("helper"));

        // Assert
        Assert.Equal(2, problems.Count);
        Assert.All(problems, problem =>
        {
            Assert.Equal(RuleIds.CommandParse, problem.RuleId);
            Assert.Equal("a.md", problem.File);
        });
        Assert.Contains("'piped'", problems[0].Message);
        Assert.Contains("shell operator '|'", problems[0].Message);
        Assert.Contains("'other'", problems[1].Message);
    }

    /// <summary>
    ///     Proves that the command check uses the name of the first source when no tool name is set.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_CommandWithoutToolName_UsesSourceName()
    {
        // Arrange
        var example = "examples:\n  - name: run\n    kind: command\n    language: shell\n    content: 'test run'\n    description: Run.\n";

        // Act
        var problems = ValidateFiles(null, ("a.md", SourceReading.Card(extraFrontMatter: example)));

        // Assert
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that the rules of the options see every card and that their problems are part of the result.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_CustomRule_AddsItsProblems()
    {
        // Arrange
        var options = new HelpValidationOptions { Rules = [new ReportingRule("b")] };

        // Act
        var problems = ValidateFiles(options, ("a.md", SourceReading.Card()), ("b.md", SourceReading.Card()));

        // Assert
        var problem = Assert.Single(problems);
        Assert.Equal(("no-commands", "b.md", 3), (problem.RuleId, problem.File, problem.Line));
    }

    /// <summary>
    ///     Proves that a custom error fails the assertion and that a rule that returns nothing is accepted.
    /// </summary>
    [Fact]
    public void HelpCardValidator_AssertValid_CustomRuleError_Fails()
    {
        // Arrange
        var rule = new ErrorRule();
        var source = HelpSource.FromFiles(SourceReading.Files(("a.md", SourceReading.Card())), "test");

        // Act
        var exception = Assert.Throws<HelpCatalogException>(
            () => HelpCardValidator.AssertValid(source, new HelpValidationOptions { Rules = [rule, new NullRule()] }));

        // Assert
        Assert.Equal("domain", Assert.Single(exception.Problems).RuleId);
    }

    /// <summary>
    ///     Proves that a link to an absent external source is only an error when the options require sources.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_ExternalSources_AreErrorsOnlyWhenRequired()
    {
        // Arrange
        var source = CatalogBuilding.Fixture("simple", SimpleTokens);

        // Act
        var lenient = HelpCardValidator.Validate(source);
        var strict = HelpCardValidator.Validate(source, new HelpValidationOptions { RequireExternalSources = true });
        var catalog = HelpCardValidator.Validate(CatalogBuilding.Create(source), new HelpValidationOptions { RequireExternalSources = true });

        // Assert
        Assert.Empty(lenient);
        Assert.NotEmpty(strict);
        Assert.All(strict, problem =>
        {
            Assert.Equal(RuleIds.ExternalSourceMissing, problem.RuleId);
            Assert.Equal(HelpSeverity.Error, problem.Severity);
            Assert.Equal("guide.md", problem.File);
            Assert.Contains("'other'", problem.Message);
        });
        Assert.Equal(strict, catalog);
    }

    /// <summary>
    ///     A rule of a test that reports an error.
    /// </summary>
    private sealed class ErrorRule : IHelpCardRule
    {
        /// <inheritdoc />
        public IEnumerable<HelpProblem> Check(HelpCard card)
        {
            return [new HelpProblem(HelpSeverity.Error, "domain", card.SourceName, card.File, 0, "Not allowed.")];
        }
    }

    /// <summary>
    ///     A rule of a test that returns no sequence at all.
    /// </summary>
    private sealed class NullRule : IHelpCardRule
    {
        /// <inheritdoc />
        public IEnumerable<HelpProblem> Check(HelpCard card)
        {
            return null!;
        }
    }
}
