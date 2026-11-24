using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Integration;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Testing;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Testing;

/// <summary>
///     Unit tests for <see cref="HelpValidationOptions"/>, which belongs to the validator unit.
/// </summary>
public partial class HelpCardValidatorTests
{
    /// <summary>
    ///     A rule of a test that reports nothing.
    /// </summary>
    private sealed class SilentRule : IHelpCardRule
    {
        /// <inheritdoc />
        public IEnumerable<HelpProblem> Check(HelpCard card)
        {
            return [];
        }
    }

    /// <summary>
    ///     Proves that a root summary at the default limit is accepted and takes at most two lines of the topic index at
    ///     the default width beside the command label of the longest topic of the adopter.
    /// </summary>
    [Fact]
    public void HelpValidationOptions_RootSummaryAtDefaultLimit_FitsTwoIndexLines()
    {
        // Arrange: a root card named like the adopter's longest topic and a summary of exactly 80 characters
        const string toolName = "reqstream";
        const string topic = "requirements-file";
        var label = $"{toolName} help {topic}";
        var summary = string.Join(" ", Enumerable.Repeat("abcdefg", 9)) + " abcdefgh";
        var output = new StringWriter();
        var cli = HelpCli.Create(new HelpCliOptions
        {
            ToolName = toolName,
            ConfigureCatalog = (builder, culture) => builder.AddSource(HelpSource.FromFiles(
                SourceReading.Files(($"{topic}.md", SourceReading.Card(title: "Requirements file", summary: summary))),
                "test")),
            Output = output,
        });
        var source = HelpSource.FromFiles(
            SourceReading.Files(($"{topic}.md", SourceReading.Card(title: "Requirements file", summary: summary))),
            "test");

        // Act
        cli.Run([]);
        var warnings = HelpCardValidator.Validate(source);
        var lines = output.ToString().Split('\n');
        var first = Array.FindIndex(lines, line => line.StartsWith(label, StringComparison.Ordinal));
        var entry = lines.Skip(first).TakeWhile(line => line.Length > 0).ToList();

        // Assert
        Assert.Equal(new HelpValidationOptions().MaxRootSummaryChars, summary.Length);
        Assert.Empty(warnings);
        Assert.True(first >= 0);
        Assert.Equal(2, entry.Count);
        Assert.All(entry, line => Assert.True(line.Length <= 78));
    }

    /// <summary>
    ///     Proves that the options have the documented defaults.
    /// </summary>
    [Fact]
    public void HelpValidationOptions_Defaults_AreTheDocumentedValues()
    {
        // Act
        var options = new HelpValidationOptions();

        // Assert
        Assert.Equal(6000, options.MaxCardChars);
        Assert.Equal(3500, options.MaxRootIndexChars);
        Assert.Equal(100, options.MaxSummaryChars);
        Assert.Equal(80, options.MaxRootSummaryChars);
        Assert.Null(options.MaxLineLength);
        Assert.Empty(options.Rules);
        Assert.False(options.RequireExternalSources);
        Assert.False(options.WarningsAreErrors);
    }

    /// <summary>
    ///     Proves that every option returns the value it was given and that the rules are copied.
    /// </summary>
    [Fact]
    public void HelpValidationOptions_Initialization_StoresEverySetting()
    {
        // Arrange
        var rules = new List<IHelpCardRule> { new SilentRule() };

        // Act
        var options = new HelpValidationOptions
        {
            MaxCardChars = 1,
            MaxRootIndexChars = 2,
            MaxSummaryChars = 3,
            MaxRootSummaryChars = 4,
            MaxLineLength = 5,
            Rules = rules,
            RequireExternalSources = true,
            WarningsAreErrors = true
        };
        rules.Clear();

        // Assert
        Assert.Equal((1, 2, 3, 4, 5), (options.MaxCardChars, options.MaxRootIndexChars, options.MaxSummaryChars, options.MaxRootSummaryChars, options.MaxLineLength));
        Assert.Single(options.Rules);
        Assert.True(options.RequireExternalSources);
        Assert.True(options.WarningsAreErrors);
    }

    /// <summary>
    ///     Proves that a limit that is zero or negative is an argument error where it is set.
    /// </summary>
    [Fact]
    public void HelpValidationOptions_Limits_NotPositive_ThrowArgumentOutOfRangeException()
    {
        // Act and Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new HelpValidationOptions { MaxCardChars = 0 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new HelpValidationOptions { MaxRootIndexChars = -1 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new HelpValidationOptions { MaxSummaryChars = 0 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new HelpValidationOptions { MaxRootSummaryChars = 0 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new HelpValidationOptions { MaxLineLength = 0 });
        Assert.Null(new HelpValidationOptions { MaxLineLength = null }.MaxLineLength);
    }

    /// <summary>
    ///     Proves that null rules, and a null entry among the rules, are argument errors where they are set.
    /// </summary>
    [Fact]
    public void HelpValidationOptions_Rules_Null_ThrowArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => new HelpValidationOptions { Rules = null! });
        Assert.Throws<ArgumentNullException>(() => new HelpValidationOptions { Rules = [new SilentRule(), null!] });
    }
}
