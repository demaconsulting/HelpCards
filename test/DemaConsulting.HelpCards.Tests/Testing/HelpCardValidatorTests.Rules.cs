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
            ("a.md", SourceReading.Card(summary: new string('r', 81))),
            ("a/b.md", SourceReading.Card(summary: new string('c', 101))),
            ("a/c.md", SourceReading.Card(summary: new string('c', 100))),
            ("d.md", SourceReading.Card(summary: new string('r', 80))));

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
        Assert.Contains("81 characters", problems[0].Message);
        Assert.Contains("at most 80", problems[0].Message);
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
    ///     Proves that a fence limit replaces the line limit inside fenced blocks only, with the fence delimited by
    ///     the same scan as the other fence rules.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_FenceLineLimit_UsesSeparateLimitInsideFences()
    {
        // Arrange
        var body = string.Join(
            "\n",
            new string('p', 80),
            new string('p', 81),
            "```text",
            new string('c', 100),
            new string('c', 120),
            new string('c', 121),
            "```",
            new string('p', 81));
        var options = new HelpValidationOptions { MaxLineLength = 80, MaxFenceLineLength = 120 };

        // Act
        var problems = ValidateFiles(options, ("a.md", SourceReading.Card(body: body)));

        // Assert: the prose limit applies before and after the fence and the fence limit applies inside it
        Assert.Equal([FirstBodyLine + 1, FirstBodyLine + 5, FirstBodyLine + 7], problems.Select(problem => problem.Line));
        Assert.All(problems, problem => Assert.Equal(RuleIds.LineLength, problem.RuleId));
        Assert.Contains("81 characters", problems[0].Message);
        Assert.Contains("wrap it at 80", problems[0].Message);
        Assert.DoesNotContain("fenced code block", problems[0].Message);
        Assert.Contains("121 characters", problems[1].Message);
        Assert.Contains("The line in a fenced code block", problems[1].Message);
        Assert.Contains("wrap it at 120", problems[1].Message);
        Assert.Contains("81 characters", problems[2].Message);
        Assert.DoesNotContain("fenced code block", problems[2].Message);
    }

    /// <summary>
    ///     Proves that setting only the fence limit checks the lines of fenced blocks, including a block that is
    ///     never closed, and does not check prose.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_OnlyFenceLineLimit_ChecksFenceLinesAndNotProse()
    {
        // Arrange
        var body = string.Join(
            "\n",
            new string('p', 200),
            "```text",
            new string('c', 30),
            new string('c', 31),
            "```",
            "~~~text",
            new string('u', 31));

        // Act
        var problems = ValidateFiles(
            new HelpValidationOptions { MaxFenceLineLength = 30 },
            ("a.md", SourceReading.Card(body: body)));

        // Assert: only the two fence lines of 31 characters are reported
        var lengths = problems.Where(problem => problem.RuleId == RuleIds.LineLength).ToList();
        Assert.Equal([FirstBodyLine + 3, FirstBodyLine + 6], lengths.Select(problem => problem.Line));
        Assert.All(lengths, problem => Assert.Contains("The line in a fenced code block has 31 characters; wrap it at 30.", problem.Message));
    }

    /// <summary>
    ///     Gets the bodies for the fence delimiter test, each with the body lines (one-based) that a fence limit of
    ///     20 must report. Prose is never reported because no line limit is set.
    /// </summary>
    public static TheoryData<string, int[]> FenceDelimiterLines => new()
    {
        // An over-limit opening line, through its info string, with a closing line at the limit
        { string.Join("\n", "```text " + new string('i', 20), "ok", "```"), new[] { 1 } },

        // An over-limit closing line: a longer closing fence is still a closing fence
        { string.Join("\n", "```text", "ok", new string('`', 21)), new[] { 3 } },

        // A longer closing fence within the limit is not reported
        { string.Join("\n", "```text", "ok", new string('`', 20)), Array.Empty<int>() },

        // Tilde fences: opening, body, and closing lines are all checked
        { string.Join("\n", "~~~text " + new string('i', 20), new string('c', 21), new string('~', 21)), new[] { 1, 2, 3 } },
        { string.Join("\n", "~~~text", new string('c', 20), "~~~"), Array.Empty<int>() },

        // The indent of a fence counts toward the length of every line of it
        { string.Join("\n", "  ```text", "  " + new string('c', 19), "  ```"), new[] { 2 } },
        { string.Join("\n", "  ```text", "  " + new string('c', 18), "  ```"), Array.Empty<int>() },

        // A fence that is never closed has its opening line and its lines checked
        { string.Join("\n", "```text " + new string('i', 20), new string('c', 21)), new[] { 1, 2 } },

        // A shorter fence inside a longer one is body, so it is checked as body and does not close the block
        { string.Join("\n", "````text", "```" + new string('x', 18), "````"), new[] { 2 } },
    };

    /// <summary>
    ///     Proves that the fence limit applies to the opening and closing fence lines as well as to the lines between
    ///     them, for backtick and tilde fences, for a closing fence that is longer than the opening one, for an
    ///     indented fence, for an info string, and for a fence that is never closed.
    /// </summary>
    /// <param name="body">The body of the card.</param>
    /// <param name="expectedLines">The one-based body lines that must be reported.</param>
    [Theory]
    [MemberData(nameof(FenceDelimiterLines))]
    public void HelpCardValidator_Validate_FenceLimit_AppliesToDelimiterLinesOfEveryFenceKind(string body, int[] expectedLines)
    {
        // Arrange: only the fence limit is set, so prose lines are never checked
        var options = new HelpValidationOptions { MaxFenceLineLength = 20 };

        // Act
        var problems = ValidateFiles(options, ("a.md", SourceReading.Card(body: body)));

        // Assert
        var lengths = problems.Where(problem => problem.RuleId == RuleIds.LineLength).ToList();
        Assert.Equal(expectedLines.Select(line => FirstBodyLine + line - 1), lengths.Select(problem => problem.Line));
        Assert.All(lengths, problem => Assert.Contains("wrap it at 20", problem.Message, StringComparison.Ordinal));
    }

    /// <summary>
    ///     Proves that a command example with an unquoted shell character is reported with the character and its column.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_CommandWithUnquotedBrace_NamesCharacterAndColumn()
    {
        // Arrange
        var examples = "examples:\n"
            + "  - name: braced\n    kind: command\n    language: shell\n    content: 'helper run {a}'\n    description: Braced.\n";
        var source = HelpSource.FromFiles(SourceReading.Files(("a.md", SourceReading.Card(extraFrontMatter: examples))), "test");

        // Act
        var problems = HelpCardValidator.Validate(builder => builder.AddSource(source).ToolName("helper"));

        // Assert
        var problem = Assert.Single(problems);
        Assert.Equal(RuleIds.CommandParse, problem.RuleId);
        Assert.Equal(HelpSeverity.Warning, problem.Severity);
        Assert.Equal("a.md", problem.File);
        Assert.Equal(0, problem.Line);
        Assert.Contains("'braced'", problem.Message);
        Assert.Contains("shell expansion character '{'", problem.Message);
        Assert.Contains("(column 12)", problem.Message);
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
