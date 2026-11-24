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
    ///     Proves that the tool is created with the name a host chooses, with the schema of the contract, and with a
    ///     description that lists the sources.
    /// </summary>
    [Fact]
    public void Integration_Agent_Creation_NameSchemaAndDescription()
    {
        // Arrange / Act
        var tool = HelpAgentTools.Create(CatalogBuilding.Simple());
        var named = HelpAgentTools.Create(CatalogBuilding.Simple(), new HelpAgentToolOptions { Name = "canvasnet_help" });

        // Assert
        Assert.Equal("help", tool.Name);
        Assert.Equal("canvasnet_help", named.Name);
        Assert.StartsWith("{\"type\":\"object\",\"properties\":{\"topic\":{\"type\":\"string\"", tool.ParametersSchema, StringComparison.Ordinal);
        Assert.EndsWith("\"additionalProperties\":false}", tool.ParametersSchema, StringComparison.Ordinal);
        Assert.Contains("- simple: A small source that exercises every kind of card feature.", tool.Description, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that one tool answers the index, a card, an example, and a search over a catalog of several sources.
    /// </summary>
    [Fact]
    public void Integration_Agent_Calls_AnswerIndexCardExampleAndSearch()
    {
        // Arrange
        var simple = CatalogBuilding.Fixture("simple", new Dictionary<string, string> { ["limit"] = "42" });
        var catalog = HelpCatalog.Create(builder => builder.AddSource(simple).AddSource(CatalogBuilding.LibB()));
        var tool = HelpAgentTools.Create(catalog);

        // Act
        var index = tool.Invoke("{}");
        var card = tool.Invoke("{\"topic\":\"guide\"}");
        var example = tool.Invoke("{\"example\":\"run\"}");
        var search = tool.Invoke("{\"query\":\"chart\",\"maxResults\":1}");

        // Assert
        Assert.Contains("## Simple fixture", index, StringComparison.Ordinal);
        Assert.Contains("## Library B", index, StringComparison.Ordinal);
        Assert.StartsWith("# Guide\n", card, StringComparison.Ordinal);
        Assert.StartsWith("Example `run` of `guide` (command, shell)", example, StringComparison.Ordinal);
        Assert.StartsWith("Cards matching 'chart':\n\nchart ", search, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that wrong arguments and misses are answered with text that says what to send, and that nothing the
    ///     agent sends makes the tool throw.
    /// </summary>
    [Fact]
    public void Integration_Agent_Arguments_AreAnsweredAsTextAndNeverThrow()
    {
        // Arrange
        var tool = HelpAgentTools.Create(CatalogBuilding.Simple());
        string[] sent = ["{", "[]", "{\"x\":1}", "{\"topic\":1}", "{\"maxResults\":99}", "{\"topic\":\"nope\"}", "{\"example\":\"nope\"}", "{\"query\":\"qqq\"}", "\uD800", "{\"source\":\"nope\"}"];

        // Act
        var answers = sent.Select(tool.Invoke).ToList();

        // Assert
        Assert.All(answers, answer => Assert.False(string.IsNullOrWhiteSpace(answer)));
        Assert.Equal("The arguments must be a JSON object.", answers[1]);
        Assert.StartsWith("Unknown argument 'x'.", answers[2], StringComparison.Ordinal);
        Assert.StartsWith("No help card matches 'nope'.", answers[5], StringComparison.Ordinal);
        Assert.StartsWith("No example named 'nope'", answers[6], StringComparison.Ordinal);
        Assert.Equal("No cards match 'qqq'. Call with no arguments for the index.", answers[7]);
    }

    /// <summary>
    ///     Proves that an answer over the output limit is cut at a line end with the number of omitted characters.
    /// </summary>
    [Fact]
    public void Integration_Agent_Budget_CutsLongAnswersAtALineEnd()
    {
        // Arrange
        var body = string.Join("\n", Enumerable.Range(1, 300).Select(number => $"Line {number} of a long card body."));
        var catalog = CatalogBuilding.FromFiles(("long.md", SourceReading.Card(title: "Long", body: body)));
        var tool = HelpAgentTools.Create(catalog, new HelpAgentToolOptions { MaxOutputChars = 1000 });

        // Act
        var answer = tool.Invoke("{\"topic\":\"long\"}");

        // Assert
        Assert.Matches(@"\n\n\[Truncated: \d+ characters omitted\. Ask for a narrower topic or an example\.\]$", answer);
        Assert.True(answer.IndexOf("\n\n[Truncated", StringComparison.Ordinal) <= 1000);
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
