using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Composition;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Testing;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Testing;

/// <summary>
///     Unit tests for <see cref="HelpCardValidator"/>: the entry points, ordering, and assertion.
/// </summary>
/// <remarks>
///     The quality rules are tested in <c>HelpCardValidatorTests.Rules.cs</c>, the options in
///     <c>HelpCardValidatorTests.Options.cs</c>, and the directory comparison in
///     <c>HelpCardValidatorTests.Resources.cs</c>. Defective sources live in memory, never as files.
/// </remarks>
public partial class HelpCardValidatorTests
{
    /// <summary>
    ///     The tokens of the simple fixture.
    /// </summary>
    private static readonly Dictionary<string, string> SimpleTokens = new() { ["limit"] = "42" };

    /// <summary>
    ///     Validates in-memory files as a source named <c>test</c>.
    /// </summary>
    /// <param name="options">The options, or <see langword="null"/>.</param>
    /// <param name="files">The path and text of each file.</param>
    /// <returns>The problems.</returns>
    private static IReadOnlyList<HelpProblem> ValidateFiles(HelpValidationOptions? options, params (string Path, string Text)[] files)
    {
        return HelpCardValidator.Validate(HelpSource.FromFiles(SourceReading.Files(files), "test"), options);
    }

    /// <summary>
    ///     Proves that a valid source has no problems at all.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_SimpleFixtureSource_ReturnsNoProblems()
    {
        // Act
        var problems = HelpCardValidator.Validate(CatalogBuilding.Fixture("simple", SimpleTokens));

        // Assert
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that the three entry points give the same answer for the same cards.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_SourceCallbackAndCatalog_AgreeOnQualityProblems()
    {
        // Arrange
        var source = HelpSource.FromFiles(
            SourceReading.Files(("a.md", SourceReading.Card(summary: new string('x', 70)))),
            "test");
        var options = new HelpValidationOptions { MaxRootSummaryChars = 60 };

        // Act
        var fromSource = HelpCardValidator.Validate(source, options);
        var fromCallback = HelpCardValidator.Validate(builder => builder.AddSource(source), options);
        var fromCatalog = HelpCardValidator.Validate(HelpCatalog.Create(builder => builder.AddSource(source)), options);

        // Assert
        var problem = Assert.Single(fromSource);
        Assert.Equal(RuleIds.SummaryLength, problem.RuleId);
        Assert.Equal(fromSource, fromCallback);
        Assert.Equal(fromSource, fromCatalog);
    }

    /// <summary>
    ///     Proves that a defective source returns its errors in display order without throwing and without warnings.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_DefectiveSource_ReturnsErrorsWithoutThrowing()
    {
        // Arrange
        var files = new[]
        {
            ("b.md", SourceReading.Card(extraFrontMatter: "tag: x\n")),
            ("a.md", SourceReading.Card(summary: new string('x', 70), body: "See [[missing]].")),
            ("c.md", "no front matter")
        };

        // Act
        var problems = ValidateFiles(null, files);

        // Assert
        Assert.NotEmpty(problems);
        Assert.All(problems, problem => Assert.Equal(HelpSeverity.Error, problem.Severity));
        Assert.Equal(
            problems.OrderBy(problem => problem.File, StringComparer.Ordinal).ThenBy(problem => problem.Line).ToList(),
            problems);
        Assert.Contains(problems, problem => problem.RuleId == RuleIds.KeyUnknown && problem.File == "b.md");
        Assert.Contains(problems, problem => problem.RuleId == RuleIds.LinkTarget && problem.File == "a.md");
    }

    /// <summary>
    ///     Proves that a summary that tries to span lines is an error of the build, so no summary can be longer than one line.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_SummaryWithLineBreak_IsAnErrorOfTheBuild()
    {
        // Act
        var problems = ValidateFiles(null, ("a.md", SourceReading.Card(summary: "\"one\\ntwo\"")));

        // Assert
        var problem = Assert.Single(problems);
        Assert.Equal(HelpSeverity.Error, problem.Severity);
        Assert.Equal("a.md", problem.File);
    }

    /// <summary>
    ///     Proves that errors come before warnings and that each group is ordered by file, line, and rule.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_ErrorsAndWarnings_ErrorsFirstThenFileLineRule()
    {
        // Arrange
        var source = CatalogBuilding.Fixture("simple", SimpleTokens);
        var options = new HelpValidationOptions { RequireExternalSources = true, MaxCardChars = 100, MaxRootIndexChars = 100 };

        // Act
        var problems = HelpCardValidator.Validate(source, options);

        // Assert
        var severities = problems.Select(problem => problem.Severity).ToList();
        Assert.Equal(HelpSeverity.Error, severities[0]);
        Assert.Equal(severities.OrderByDescending(severity => severity), severities);
        foreach (var group in problems.GroupBy(problem => problem.Severity))
        {
            Assert.Equal(
                group.OrderBy(problem => problem.File, StringComparer.Ordinal).ThenBy(problem => problem.Line).ThenBy(problem => problem.RuleId, StringComparer.Ordinal),
                group);
        }
    }

    /// <summary>
    ///     Proves that the builder callback of a product is validated with its tag vocabulary and tool name.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_Callback_AppliesBuilderSettings()
    {
        // Arrange
        var source = CatalogBuilding.Fixture("simple", SimpleTokens);

        // Act
        var problems = HelpCardValidator.Validate(builder => builder.AddSource(source).TagVocabulary(["tutorial"]));

        // Assert
        Assert.NotEmpty(problems);
        Assert.All(problems, problem => Assert.Equal(RuleIds.TagUnknown, problem.RuleId));
    }

    /// <summary>
    ///     Proves that a callback that adds no source and a source that imports another source are argument errors.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Validate_InvalidBuilder_ThrowsArgumentException()
    {
        // Arrange
        var inner = HelpSource.FromFiles(SourceReading.Files(("a.md", SourceReading.Card())), "inner");
        var outer = HelpSource.FromFiles(SourceReading.Files(("b.md", SourceReading.Card())), "outer").Imports(inner);

        // Act and Assert
        Assert.Throws<ArgumentException>(() => HelpCardValidator.Validate(_ => { }));
        Assert.Throws<ArgumentException>(() => HelpCardValidator.Validate(outer));
    }

    /// <summary>
    ///     Proves that null arguments are argument errors for every entry point.
    /// </summary>
    [Fact]
    public void HelpCardValidator_Members_NullArguments_ThrowArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => HelpCardValidator.Validate((HelpSource)null!));
        Assert.Throws<ArgumentNullException>(() => HelpCardValidator.Validate((Action<HelpCatalogBuilder>)null!));
        Assert.Throws<ArgumentNullException>(() => HelpCardValidator.Validate((HelpCatalog)null!));
        Assert.Throws<ArgumentNullException>(() => HelpCardValidator.AssertValid((HelpSource)null!));
        Assert.Throws<ArgumentNullException>(() => HelpCardValidator.AssertValid((Action<HelpCatalogBuilder>)null!));
        Assert.Throws<ArgumentNullException>(() => HelpCardValidator.AssertValid((HelpCatalog)null!));
    }

    /// <summary>
    ///     Proves that a valid source passes the assertion for every kind of argument.
    /// </summary>
    [Fact]
    public void HelpCardValidator_AssertValid_ValidSource_DoesNotThrow()
    {
        // Arrange
        var source = CatalogBuilding.Fixture("simple", SimpleTokens);

        // Act and Assert
        HelpCardValidator.AssertValid(source);
        HelpCardValidator.AssertValid(builder => builder.AddSource(source));
        HelpCardValidator.AssertValid(CatalogBuilding.Create(source));
    }

    /// <summary>
    ///     Proves that an error fails the assertion with an exception that carries every error.
    /// </summary>
    [Fact]
    public void HelpCardValidator_AssertValid_Errors_ThrowCatalogExceptionWithAllErrors()
    {
        // Arrange
        var source = HelpSource.FromFiles(
            SourceReading.Files(("a.md", SourceReading.Card(body: "See [[x]].")), ("b.md", SourceReading.Card(body: "See [[y]]."))),
            "test");

        // Act
        var exception = Assert.Throws<HelpCatalogException>(() => HelpCardValidator.AssertValid(source));

        // Assert
        Assert.Equal(2, exception.Problems.Count);
        Assert.All(exception.Problems, problem => Assert.Equal(RuleIds.LinkTarget, problem.RuleId));
        Assert.Contains("(and 1 more)", exception.Message);
    }

    /// <summary>
    ///     Proves that warnings fail the assertion only when the options say so.
    /// </summary>
    [Fact]
    public void HelpCardValidator_AssertValid_Warnings_FailOnlyWithWarningsAreErrors()
    {
        // Arrange
        var source = HelpSource.FromFiles(SourceReading.Files(("a.md", SourceReading.Card(summary: new string('x', 70)))), "test");

        // Act
        HelpCardValidator.AssertValid(source);
        var exception = Assert.Throws<HelpCatalogException>(
            () => HelpCardValidator.AssertValid(source, new HelpValidationOptions { WarningsAreErrors = true }));

        // Assert
        var problem = Assert.Single(exception.Problems);
        Assert.Equal(HelpSeverity.Warning, problem.Severity);
        Assert.Equal(RuleIds.SummaryLength, problem.RuleId);
    }
}
