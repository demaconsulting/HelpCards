using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Integration;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Integration;

/// <summary>
///     Subsystem tests for the Integration subsystem: the command-line helper and its parser and dispatcher working
///     together over real catalogs.
/// </summary>
public class IntegrationTests
{
    /// <summary>
    ///     Proves that commands that are not help commands never create the catalog, touch a resource, or write, while
    ///     the help verb does create it, once.
    /// </summary>
    [Fact]
    public void Integration_Cli_NonHelpArguments_NeverCreateTheCatalog()
    {
        // Arrange
        var files = new CountingFileProvider(SourceReading.Files(("a.md", SourceReading.Card())));
        var harness = new CliHarness(factory: _ => CatalogBuilding.Create(HelpSource.FromProvider(files, "test")));
        string[][] others = [[], ["--help"], ["build"], ["--version"], ["helper"]];

        // Act
        var handled = others.Select(args => harness.Cli.TryRun(args, out _)).ToList();
        var footer = harness.Cli.Footer;
        var before = (harness.FactoryCalls, files.ListCount, files.ReadCount);
        var help = harness.Cli.TryRun(["help"], out var exitCode);

        // Assert
        Assert.All(handled, Assert.False);
        Assert.Equal("Run 'tool help' for topics and examples.", footer);
        Assert.Equal((0, 0, 0), before);
        Assert.True(help);
        Assert.Equal(0, exitCode);
        Assert.Equal(1, harness.FactoryCalls);
    }

    /// <summary>
    ///     Proves that every mode of the option table answers over the simple fixture and that the arguments choose
    ///     the format.
    /// </summary>
    [Fact]
    public void Integration_Cli_OptionTable_SelectsModesAndFormats()
    {
        // Arrange
        var harness = new CliHarness(CatalogBuilding.Simple());
        string[][] commands =
        [
            [],
            ["--all"],
            ["guide"],
            ["guide", "--examples"],
            ["guide", "--example", "run"],
            ["--search", "setup"],
            ["--tag", "tutorial"],
            ["--source", "simple"],
        ];

        // Act
        var results = commands
            .Select(command =>
            {
                var text = new CliHarness(CatalogBuilding.Simple());
                var json = new CliHarness(CatalogBuilding.Simple());
                var markdown = new CliHarness(CatalogBuilding.Simple());
                return (
                    text.Run(command),
                    json.Run([.. command, "--json"]),
                    markdown.Run([.. command, "--format", "markdown"]),
                    json.Output.ToString());
            })
            .ToList();

        // Assert
        Assert.All(results, result => Assert.Equal((0, 0, 0), (result.Item1, result.Item2, result.Item3)));
        Assert.All(results, result => Assert.StartsWith("{\"helpSchemaVersion\":1,\"ok\":true,\"kind\":", result.Item4, StringComparison.Ordinal));
        Assert.Equal(["index", "index", "card", "examples", "example", "search", "cards", "index"], results.Select(result => Kind(result.Item4)));
        Assert.Equal(0, harness.FactoryCalls);
    }

    /// <summary>
    ///     Proves that the three exit codes and the two error channels follow the contract.
    /// </summary>
    [Fact]
    public void Integration_Cli_Failures_UseExitCodesAndChannels()
    {
        // Arrange
        var broken = HelpSource.FromFiles(SourceReading.Files(("a.md", SourceReading.Card(body: "See [[missing]]."))), "test");
        var usage = new CliHarness(CatalogBuilding.Simple());
        var unknown = new CliHarness(CatalogBuilding.Simple());
        var defective = new CliHarness(factory: _ => CatalogBuilding.Create(broken));
        var jsonUnknown = new CliHarness(CatalogBuilding.Simple());

        // Act
        var usageCode = usage.Run("--tag");
        var unknownCode = unknown.Run("nope");
        var defectiveCode = defective.Run();
        var jsonCode = jsonUnknown.Run("nope", "--json");

        // Assert
        Assert.Equal((2, 2, 70, 2), (usageCode, unknownCode, defectiveCode, jsonCode));
        Assert.StartsWith("Error: option '--tag' needs a value.", usage.Error.ToString(), StringComparison.Ordinal);
        Assert.StartsWith("Error: Unknown help card 'nope'.", unknown.Error.ToString(), StringComparison.Ordinal);
        Assert.StartsWith("Error: Help is unavailable: ", defective.Error.ToString(), StringComparison.Ordinal);
        Assert.All(new[] { usage, unknown, defective }, harness => Assert.Empty(harness.Output.ToString()));
        Assert.Empty(jsonUnknown.Error.ToString());
        Assert.Contains("\"kind\":\"error\",\"code\":\"not-found\"", jsonUnknown.Output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the tool can extend the index and the lookup: sections, the fallback resolver, the width, the
    ///     language, and the command template.
    /// </summary>
    [Fact]
    public void Integration_Cli_Extensions_SectionsFallbackWidthLanguageAndTemplate()
    {
        // Arrange
        var harness = new CliHarness(
            CatalogBuilding.Create(CatalogBuilding.Fixture("big")),
            language: true,
            preamble: "Hello.",
            sections: [new HelpCliSection("Commands:", ["go  Run"])],
            fallback: words => words[0] == "go" ? new HelpCliAnswer("Go help.") : null,
            width: () => 60,
            template: "{tool} man {path}");

        // Act
        var index = harness.Run("--width", "auto", "--lang", "de");
        var answer = harness.Run("go");

        // Assert
        Assert.Equal((0, 0), (index, answer));
        Assert.Contains("Hello.\n\nCommands:\n  go  Run\n\nHelp topics:\nbig man build", harness.Output.ToString(), StringComparison.Ordinal);
        Assert.EndsWith("Go help.\n", harness.Output.ToString(), StringComparison.Ordinal);
        Assert.Equal("de", harness.LastCulture!.Name);
    }

    /// <summary>
    ///     Reads the kind of a JSON document.
    /// </summary>
    /// <param name="json">The document.</param>
    /// <returns>The value of the <c>kind</c> property.</returns>
    private static string Kind(string json)
    {
        using var document = System.Text.Json.JsonDocument.Parse(json);
        return document.RootElement.GetProperty("kind").GetString()!;
    }
}
