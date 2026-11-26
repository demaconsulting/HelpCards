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
    ///     Proves that the tool name, the version, and the culture are set once in the options and reach the cards, the
    ///     JSON documents, and the catalog that the tests of the tool validate.
    /// </summary>
    [Fact]
    public void Integration_Cli_ConfigureCatalog_AppliesToolNameVersionAndCulture()
    {
        // Arrange
        var output = new StringWriter();
        var options = new HelpCliOptions
        {
            ToolName = "mytool",
            Version = "3.1",
            EnableLanguageOption = true,
            ConfigureCatalog = (builder, culture) => builder.AddSource(
                CatalogBuilding.Fixture("simple", new Dictionary<string, string> { ["limit"] = "42" })),
            Output = output,
        };
        var cli = HelpCli.Create(options);

        // Act
        var card = cli.Run(["guide", "--lang", "de-DE"]);
        var json = cli.Run(["--json"]);
        var validated = HelpCatalog.Create(options.GetCatalogConfiguration(System.Globalization.CultureInfo.GetCultureInfo("de-DE")));

        // Assert: the name and the version are in the cards, the commands, and the JSON documents, and in the catalog
        // of the tests
        Assert.Equal((0, 0), (card, json));
        Assert.Contains("Version 3.1 of mytool accepts", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("`mytool help guide setup`", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("\"version\":\"3.1\"", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("Version 3.1 of mytool accepts", validated.Find("guide")!.Body, StringComparison.Ordinal);
        Assert.Equal("de-DE", validated.Culture.Name);
    }

    /// <summary>
    ///     Proves that the helper, its parser, and its dispatcher apply the contract settings of the tool together: the
    ///     envelope and the error shape of the tool for a usage error, a name that is not found, and a catalog that
    ///     cannot be created, with the exit code of the tool, and the default behavior when nothing is set.
    /// </summary>
    [Fact]
    public void Integration_Cli_ToolContract_AppliesToEveryFailureOfTheHelper()
    {
        // Arrange
        var tool = new CliHarness(
            CatalogBuilding.Simple(),
            jsonEnvelope: ToolContract.Envelope,
            jsonError: ToolContract.ErrorBody,
            exitCodes: ToolContract.ExitCodes);
        var unavailable = new CliHarness(
            factory: _ => null!,
            jsonEnvelope: ToolContract.Envelope,
            jsonError: ToolContract.ErrorBody,
            exitCodes: ToolContract.ExitCodes);
        var plain = new CliHarness(CatalogBuilding.Simple());

        // Act
        var usage = tool.Run("--json", "--bogus");
        var notFound = tool.Run("nothing", "--json");
        var noCatalog = unavailable.Run("--json");
        var success = tool.Run("--search", "guide", "--json");
        var plainUsage = plain.Run("--json", "--bogus");

        // Assert
        Assert.Equal((1, 1, 1, 0, 2), (usage, notFound, noCatalog, success, plainUsage));
        var documents = tool.Output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.StartsWith("{\"schemaVersion\":2,\"ok\":false,\"command\":\"help\",\"kind\":\"error\",\"error\":{\"code\":\"usage-error\"", documents[0], StringComparison.Ordinal);
        Assert.StartsWith("{\"schemaVersion\":2,\"ok\":false,\"command\":\"help\",\"kind\":\"error\",\"error\":{\"code\":\"not-found\"", documents[1], StringComparison.Ordinal);
        Assert.StartsWith("{\"schemaVersion\":2,\"ok\":true,\"command\":\"help\",\"kind\":\"search\"", documents[2], StringComparison.Ordinal);
        Assert.Equal(
            "{\"schemaVersion\":2,\"ok\":false,\"command\":\"help\",\"kind\":\"error\","
                + "\"error\":{\"code\":\"catalog-error\",\"message\":\"Help is unavailable: the tool created no catalog.\"}}\n",
            unavailable.Output.ToString());
        Assert.StartsWith("{\"helpSchemaVersion\":1,\"ok\":false,\"kind\":\"error\",\"code\":\"usage-error\"", plain.Output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the providers choose the writers when a command writes, so a quiet mode that is read after the
    ///     helper was created silences help, and that an error still reaches the error provider.
    /// </summary>
    [Fact]
    public void Integration_Cli_Writers_ProvidersChooseTheWriterWhenACommandWrites()
    {
        // Arrange
        var output = new StringWriter();
        var errors = new StringWriter();
        var silent = false;
        var cli = HelpCli.Create(new HelpCliOptions
        {
            ToolName = "tool",
            ConfigureCatalog = (builder, culture) => builder.AddSource(
                CatalogBuilding.Fixture("simple", new Dictionary<string, string> { ["limit"] = "42" })),
            OutputProvider = () => silent ? TextWriter.Null : output,
            ErrorProvider = () => silent ? TextWriter.Null : errors,
        });

        // Act
        cli.Run(["guide"]);
        cli.Run(["nothing"]);
        var loud = (output.ToString(), errors.ToString());
        silent = true;
        cli.Run(["reference"]);
        cli.Run(["nothing"]);

        // Assert: the second pair of commands wrote nowhere
        Assert.StartsWith("# Guide\n", loud.Item1, StringComparison.Ordinal);
        Assert.StartsWith("Error: Unknown help card 'nothing'.", loud.Item2, StringComparison.Ordinal);
        Assert.Equal(loud, (output.ToString(), errors.ToString()));
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
    ///     Proves that the answers are restricted by source and that a card, example, source, or tag that does not exist
    ///     is answered with the closest name or the valid names, and that one example is printed as exactly its content.
    /// </summary>
    [Fact]
    public void Integration_Cli_RestrictionsAndMisses_AreAnsweredWithValidNames()
    {
        // Arrange
        var catalog = HelpCatalog.Create(builder => builder
            .AddSource(CatalogBuilding.Fixture("simple", new Dictionary<string, string> { ["limit"] = "42" }))
            .AddSource(CatalogBuilding.LibB()));
        var restricted = new CliHarness(catalog);
        var everything = new CliHarness(catalog);
        var source = new CliHarness(catalog);
        var tag = new CliHarness(catalog);
        var card = new CliHarness(catalog);
        var example = new CliHarness(catalog);
        var exact = new CliHarness(catalog);

        // Act
        restricted.Run("--source", "simple");
        everything.Run();
        var sourceCode = source.Run("--source", "nope");
        var tagCode = tag.Run("--tag", "nope");
        var cardCode = card.Run("guid");
        var exampleCode = example.Run("guide", "--example", "nope");
        var exactCode = exact.Run("guide", "--example", "run");

        // Assert: the restriction hides the other source
        Assert.Contains("guide", restricted.Output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("chart", restricted.Output.ToString(), StringComparison.Ordinal);
        Assert.Contains("chart", everything.Output.ToString(), StringComparison.Ordinal);

        // Assert: every miss exits with 2 and names what is valid
        Assert.Equal((2, 2, 2, 2, 0), (sourceCode, tagCode, cardCode, exampleCode, exactCode));
        Assert.StartsWith("Error: Unknown help source 'nope'.", source.Error.ToString(), StringComparison.Ordinal);
        Assert.Contains("simple", source.Error.ToString(), StringComparison.Ordinal);
        Assert.StartsWith("Error: Unknown tag 'nope'.", tag.Error.ToString(), StringComparison.Ordinal);
        Assert.Contains("tutorial", tag.Error.ToString(), StringComparison.Ordinal);
        Assert.Contains("Did you mean 'guide'?", card.Error.ToString(), StringComparison.Ordinal);
        Assert.StartsWith("Error: The help card 'guide' has no example 'nope'.", example.Error.ToString(), StringComparison.Ordinal);
        Assert.Contains("Examples: run", example.Error.ToString(), StringComparison.Ordinal);

        // Assert: one example is its content and nothing else
        Assert.DoesNotContain("\n", exact.Output.ToString().TrimEnd('\n'), StringComparison.Ordinal);
        Assert.Empty(exact.Error.ToString());
    }

    /// <summary>
    ///     Proves that an ambiguous example is a JSON error on the output with the candidates, like a card that is not
    ///     found, and that neither writes to the error channel.
    /// </summary>
    [Fact]
    public void Integration_Cli_JsonFailures_AmbiguousAndNotFound_UseTheOutputChannel()
    {
        // Arrange: two roots of one mounted source have an example of the same name
        const string example = "examples:\n  - name: x\n    kind: command\n    language: shell\n    content: tool x\n    description: An example.\n";
        var source = HelpSource.FromFiles(
            SourceReading.Files(("a.md", SourceReading.Card(extraFrontMatter: example)), ("b.md", SourceReading.Card(extraFrontMatter: example))),
            "lib");
        var catalog = HelpCatalog.Create(builder => builder.AddMountedSource(source, "m"));
        var ambiguous = new CliHarness(catalog);
        var missing = new CliHarness(catalog);

        // Act
        var ambiguousCode = ambiguous.Run("m", "--example", "x", "--json");
        var missingCode = missing.Run("zzz", "--json");

        // Assert
        Assert.Equal((2, 2), (ambiguousCode, missingCode));
        Assert.Contains("\"kind\":\"error\",\"code\":\"ambiguous\"", ambiguous.Output.ToString(), StringComparison.Ordinal);
        Assert.Contains("\"candidates\":[\"m/a#x\",\"m/b#x\"]", ambiguous.Output.ToString(), StringComparison.Ordinal);
        Assert.Contains("\"kind\":\"error\",\"code\":\"not-found\"", missing.Output.ToString(), StringComparison.Ordinal);
        Assert.Empty(ambiguous.Error.ToString());
        Assert.Empty(missing.Error.ToString());
    }

    /// <summary>
    ///     Proves that the tool is created with options that are checked at once: a name that does not match the
    ///     pattern and a limit outside its range are argument errors.
    /// </summary>
    [Fact]
    public void Integration_Agent_Creation_InvalidOptions_AreCheckedWhereTheToolIsCreated()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => HelpAgentTools.Create(catalog, new HelpAgentToolOptions { Name = "Bad Name" }));
        Assert.ThrowsAny<ArgumentException>(() => HelpAgentTools.Create(catalog, new HelpAgentToolOptions { MaxResults = 0 }));
        Assert.ThrowsAny<ArgumentException>(() => HelpAgentTools.Create(catalog, new HelpAgentToolOptions { MaxOutputChars = 10 }));
        Assert.ThrowsAny<ArgumentException>(() => HelpAgentTools.Create(catalog, new HelpAgentToolOptions { IndexDepth = 9 }));
        Assert.Throws<ArgumentNullException>(() => HelpAgentTools.Create(null!));
    }

    /// <summary>
    ///     Proves that using the tool reads no file after the catalog is created, and that text the agent sends never
    ///     makes it throw, even text that is not valid Unicode.
    /// </summary>
    [Fact]
    public void Integration_Agent_Use_ReadsNoFileAndNeverThrowsForHostileText()
    {
        // Arrange
        var files = new CountingFileProvider(SourceReading.Files(("a.md", SourceReading.Card())));
        var tool = HelpAgentTools.Create(CatalogBuilding.Create(HelpSource.FromProvider(files, "test")));
        var before = (files.ListCount, files.ReadCount);
        string[] sent = [null!, string.Empty, "{\"topic\":\"a\"}", "{\"query\":\"x\"}", "{\"topic\":\"\\uD800\"}", "\uD800", "{\"topic\":\"../../etc/passwd\"}"];

        // Act
        var answers = sent.Select(tool.Invoke).ToList();

        // Assert
        Assert.All(answers, answer => Assert.False(string.IsNullOrWhiteSpace(answer)));
        Assert.Equal(before, (files.ListCount, files.ReadCount));
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
