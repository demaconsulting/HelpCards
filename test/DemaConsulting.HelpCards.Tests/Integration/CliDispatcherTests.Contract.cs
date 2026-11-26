using System.Text.Json;
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Integration;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Integration;

/// <summary>
///     Unit tests for the contract settings of <see cref="CliDispatcher"/>: the JSON envelope, the body of a JSON error, and
///     the exit codes that a tool with a JSON contract of its own sets in the options.
/// </summary>
public partial class CliDispatcherTests
{
    /// <summary>
    ///     Creates a harness over the simple catalog that follows the contract of the tool.
    /// </summary>
    /// <returns>The harness.</returns>
    private static CliHarness ToolHarness()
    {
        return new CliHarness(
            CatalogBuilding.Simple(),
            jsonEnvelope: ToolContract.Envelope,
            jsonError: ToolContract.ErrorBody,
            exitCodes: ToolContract.ExitCodes);
    }

    /// <summary>
    ///     Proves that the envelope of the tool wraps every kind of JSON document of the helper, each with <c>ok</c> true
    ///     and the contract of the tool and not the default envelope.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_JsonEnvelope_WrapsEveryKindOfDocument()
    {
        // Arrange
        var big = HelpCatalog.Create(builder => builder.AddSource(CatalogBuilding.Fixture("big")));
        (HelpCatalog Catalog, string[] Arguments, string Kind)[] commands =
        [
            (CatalogBuilding.Simple(), ["--json"], "index"),
            (CatalogBuilding.Simple(), ["guide", "--json"], "card"),
            (CatalogBuilding.Simple(), ["guide", "--examples", "--json"], "examples"),
            (CatalogBuilding.Simple(), ["guide", "--example", "run", "--json"], "example"),
            (CatalogBuilding.Simple(), ["--search", "guide", "--json"], "search"),
            (big, ["--tag", "quality", "--json"], "cards"),
        ];

        foreach (var (catalog, arguments, kind) in commands)
        {
            var harness = new CliHarness(catalog, jsonEnvelope: ToolContract.Envelope);

            // Act
            var code = harness.Run(arguments);

            // Assert
            Assert.Equal(0, code);
            var output = harness.Output.ToString();
            Assert.StartsWith($"{{\"schemaVersion\":2,\"ok\":true,\"command\":\"help\",\"kind\":\"{kind}\",", output, StringComparison.Ordinal);
            Assert.DoesNotContain("helpSchemaVersion", output, StringComparison.Ordinal);
            using var document = JsonDocument.Parse(output);
            Assert.Equal(kind, document.RootElement.GetProperty("kind").GetString());
        }
    }

    /// <summary>
    ///     Proves that a usage error is written in the error shape of the tool inside its envelope, to the output, and
    ///     exits with the exit code of the tool.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_UsageErrorInJson_IsTheErrorShapeOfTheTool()
    {
        // Arrange
        var harness = ToolHarness();

        // Act
        var code = harness.Run("--json", "--search", "a", "--examples");

        // Assert
        Assert.Equal(1, code);
        Assert.Equal(
            "{\"schemaVersion\":2,\"ok\":false,\"command\":\"help\",\"kind\":\"error\","
                + "\"error\":{\"code\":\"usage-error\",\"message\":\"options '--examples' and '--search' cannot be combined.\"}}\n",
            harness.Output.ToString());
        Assert.Empty(harness.Error.ToString());
    }

    /// <summary>
    ///     Proves that a name that is not found is written with its hint in the error shape of the tool, and that the
    ///     error body receives the candidates of the failure.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_NotFoundInJson_WritesTheHintAndGivesTheCandidatesToTheBody()
    {
        // Arrange
        HelpCliError? received = null;
        var harness = new CliHarness(
            CatalogBuilding.Simple(),
            jsonEnvelope: ToolContract.Envelope,
            jsonError: (writer, error) =>
            {
                received = error;
                ToolContract.ErrorBody(writer, error);
            },
            exitCodes: ToolContract.ExitCodes);

        // Act
        var code = harness.Run("guide", "--example", "nope", "--json");

        // Assert
        Assert.Equal(1, code);
        using var document = JsonDocument.Parse(harness.Output.ToString());
        var error = document.RootElement.GetProperty("error");
        Assert.False(document.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("not-found", error.GetProperty("code").GetString());
        Assert.Contains("no example 'nope'", error.GetProperty("message").GetString(), StringComparison.Ordinal);
        Assert.Contains("tool help guide --examples", error.GetProperty("hint").GetString(), StringComparison.Ordinal);
        Assert.False(error.TryGetProperty("candidates", out _));
        Assert.NotNull(received);
        Assert.Equal("not-found", received.Code);
        Assert.Equal(["guide#run", "guide#settings", "guide#fragment", "guide/setup#check"], received.Candidates);
    }

    /// <summary>
    ///     Proves that the error body receives the code, the message, and the hint of every kind of failure, and an
    ///     empty list of candidates for the codes that have none, including a catalog that was not created.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_JsonError_ReceivesTheDataOfEveryFailure()
    {
        // Arrange
        var received = new List<HelpCliError>();
        void Body(Utf8JsonWriter writer, HelpCliError error)
        {
            received.Add(error);
            ToolContract.ErrorBody(writer, error);
        }

        var usage = new CliHarness(CatalogBuilding.Simple(), jsonError: Body);
        var missing = new CliHarness(CatalogBuilding.Simple(), jsonError: Body);
        var broken = new CliHarness(factory: _ => null!, jsonError: Body);

        // Act
        usage.Run("--bogus", "--json");
        missing.Run("qqqqqqqq", "--json");
        broken.Run("--json");

        // Assert
        Assert.Equal(["usage-error", "not-found", "catalog-error"], received.Select(error => error.Code));
        Assert.Equal("Unknown option '--bogus'.", received[0].Message);
        Assert.NotNull(received[0].Hint);
        Assert.Empty(received[0].Candidates);
        Assert.Equal(["guide", "reference"], received[1].Candidates);
        Assert.Equal("Help is unavailable: the tool created no catalog.", received[2].Message);
        Assert.Null(received[2].Hint);
        Assert.Empty(received[2].Candidates);
        Assert.StartsWith("{\"helpSchemaVersion\":1,\"ok\":false,\"kind\":\"error\",\"error\":{\"code\":\"usage-error\"", usage.Output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the error body of the tool and the envelope of the tool are independent settings: each works
    ///     without the other, and the default body stays inside a custom envelope.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_EnvelopeAndErrorBody_AreIndependentSettings()
    {
        // Arrange
        var envelopeOnly = new CliHarness(CatalogBuilding.Simple(), jsonEnvelope: ToolContract.Envelope);
        var bodyOnly = new CliHarness(CatalogBuilding.Simple(), jsonError: ToolContract.ErrorBody);

        // Act
        var envelopeCode = envelopeOnly.Run("qqqqqqqq", "--json");
        var bodyCode = bodyOnly.Run("--json", "--search", "a", "--examples");

        // Assert: the default body in the envelope of the tool, and the body of the tool in the default envelope
        Assert.Equal(2, envelopeCode);
        Assert.StartsWith(
            "{\"schemaVersion\":2,\"ok\":false,\"command\":\"help\",\"kind\":\"error\",\"code\":\"not-found\",\"message\":\"Unknown help card 'qqqqqqqq'.\",",
            envelopeOnly.Output.ToString(),
            StringComparison.Ordinal);
        Assert.EndsWith("\"candidates\":[\"guide\",\"reference\"]}\n", envelopeOnly.Output.ToString(), StringComparison.Ordinal);
        Assert.Equal(2, bodyCode);
        Assert.Equal(
            "{\"helpSchemaVersion\":1,\"ok\":false,\"kind\":\"error\","
                + "\"error\":{\"code\":\"usage-error\",\"message\":\"options '--examples' and '--search' cannot be combined.\"}}\n",
            bodyOnly.Output.ToString());
    }

    /// <summary>
    ///     Proves that the error body does not change the text and Markdown errors, which are not JSON.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_JsonErrorBody_DoesNotChangeTheTextError()
    {
        // Arrange
        var harness = ToolHarness();

        // Act
        var code = harness.Run("qqqqqqqq");

        // Assert
        Assert.Equal(1, code);
        Assert.StartsWith("Error: Unknown help card 'qqqqqqqq'.\n", harness.Error.ToString(), StringComparison.Ordinal);
        Assert.Empty(harness.Output.ToString());
    }

    /// <summary>
    ///     Proves that a tool that exits with 1 for every failure gets 1 for a usage error, a name that is not found, and
    ///     a defective catalog, in text and in JSON, and 0 for a success.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_ExitCodesOfTheTool_AreUsedForEveryFailure()
    {
        // Arrange
        var broken = HelpSource.FromFiles(
            SourceReading.Files(("a.md", SourceReading.Card(body: "See [[missing]]."))),
            "test");
        var harness = ToolHarness();
        var defective = new CliHarness(
            factory: _ => CatalogBuilding.Create(broken),
            jsonEnvelope: ToolContract.Envelope,
            jsonError: ToolContract.ErrorBody,
            exitCodes: ToolContract.ExitCodes);
        var nullCatalog = new CliHarness(factory: _ => null!, exitCodes: ToolContract.ExitCodes);

        // Act
        var success = harness.Run("guide");
        var usage = harness.Run("--bogus");
        var notFound = harness.Run("nothing");
        var exampleMissing = harness.Run("guide", "--example", "nope", "--json");
        var defectiveText = defective.Run();
        var defectiveJson = defective.Run("--json");
        var withoutCatalog = nullCatalog.Run();

        // Assert
        Assert.Equal(0, success);
        Assert.Equal([1, 1, 1, 1, 1, 1], new[] { usage, notFound, exampleMissing, defectiveText, defectiveJson, withoutCatalog });
        Assert.Contains("\"error\":{\"code\":\"catalog-error\",\"message\":\"Help is unavailable: Help card 'a.md'", defective.Output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the three exit codes are independent: a tool can give success, usage errors, and defective
    ///     catalogs different numbers, and the answer of a fallback resolver keeps the exit code it chose.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_ExitCodes_AreMappedIndependentlyAndNotForTheFallback()
    {
        // Arrange
        var codes = new HelpCliExitCodes { Success = 10, UsageError = 20, InternalError = 30 };
        var ok = new CliHarness(CatalogBuilding.Simple(), exitCodes: codes, fallback: words => words[0] == "run" ? new HelpCliAnswer("Help of run.", 5) : null);
        var broken = new CliHarness(factory: _ => null!, exitCodes: codes);

        // Act
        var success = ok.Run("guide");
        var usage = ok.Run("--bogus");
        var fallback = ok.Run("run");
        var internalError = broken.Run();

        // Assert
        Assert.Equal([10, 20, 5, 30], new[] { success, usage, fallback, internalError });
    }

    /// <summary>
    ///     Proves that with none of the contract settings the documents and the exit codes are those of the helper, and
    ///     that an explicit default mapping changes nothing.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_ContractSettingsUnset_KeepDocumentsAndExitCodes()
    {
        // Arrange
        var plain = new CliHarness(CatalogBuilding.Simple());
        var explicitDefaults = new CliHarness(CatalogBuilding.Simple(), exitCodes: new HelpCliExitCodes { Success = 0, UsageError = 2, InternalError = 70 });
        string[][] commands = [["guide", "--json"], ["--json"], ["nothing", "--json"], ["--bogus", "--json"], ["nothing"], ["--bogus"]];

        // Act
        var plainCodes = commands.Select(plain.Run).ToArray();
        var explicitCodes = commands.Select(explicitDefaults.Run).ToArray();

        // Assert
        Assert.Equal([0, 0, 2, 2, 2, 2], plainCodes);
        Assert.Equal(plainCodes, explicitCodes);
        Assert.Equal(plain.Output.ToString(), explicitDefaults.Output.ToString());
        Assert.Equal(plain.Error.ToString(), explicitDefaults.Error.ToString());
        Assert.StartsWith("{\"helpSchemaVersion\":1,\"ok\":true,\"kind\":\"card\",", plain.Output.ToString(), StringComparison.Ordinal);
    }
}
