using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Integration;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Integration;

/// <summary>
///     Unit tests for <see cref="CliDispatcher"/>: the flow of a command, the exit codes, the error output, the width,
///     the language, and the fallback of the tool. The answers of the modes are tested in the other part of the class.
/// </summary>
public partial class CliDispatcherTests
{
    /// <summary>
    ///     Proves that a usage error is written to the error writer with its hint, exits with 2, and never creates the
    ///     catalog.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_UsageError_WritesErrorAndHintWithoutCreatingTheCatalog()
    {
        // Arrange
        var harness = new CliHarness(CatalogBuilding.Simple());

        // Act
        var code = harness.Run("--bogus");

        // Assert
        Assert.Equal(2, code);
        Assert.StartsWith("Error: Unknown option '--bogus'.\nValid options: --json,", harness.Error.ToString(), StringComparison.Ordinal);
        Assert.Empty(harness.Output.ToString());
        Assert.Equal(0, harness.FactoryCalls);
    }

    /// <summary>
    ///     Proves that a usage error is the error document on the output when the arguments ask for JSON.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_UsageErrorInJson_WritesTheErrorDocumentToOutput()
    {
        // Arrange
        var harness = new CliHarness(CatalogBuilding.Simple());

        // Act
        var code = harness.Run("--json", "--search", "a", "--examples");

        // Assert
        Assert.Equal(2, code);
        Assert.Equal(
            "{\"helpSchemaVersion\":1,\"ok\":false,\"kind\":\"error\",\"code\":\"usage-error\",\"message\":\"options '--examples' and '--search' cannot be combined.\"}\n",
            harness.Output.ToString());
        Assert.Empty(harness.Error.ToString());
    }

    /// <summary>
    ///     Proves that everything is written with LF only, whatever the line ending of the writer is.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_Output_UsesLineFeedsOnly()
    {
        // Arrange
        var harness = new CliHarness(CatalogBuilding.Simple());

        // Act
        harness.Run("guide");
        harness.Run("--bogus");

        // Assert
        Assert.DoesNotContain('\r', harness.Output.ToString());
        Assert.DoesNotContain('\r', harness.Error.ToString());
        Assert.EndsWith("\n", harness.Output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("\n\n\n", harness.Output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a defective catalog exits with 70 and says that help is unavailable, in text on the error writer
    ///     and in JSON as the <c>catalog-error</c> document.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_DefectiveCatalog_ExitsWith70()
    {
        // Arrange
        var broken = HelpSource.FromFiles(SourceReading.Files(("a.md", SourceReading.Card(body: "See [[missing]]."))), "test");
        var text = new CliHarness(factory: _ => CatalogBuilding.Create(broken));
        var json = new CliHarness(factory: _ => CatalogBuilding.Create(broken));

        // Act
        var textCode = text.Run();
        var jsonCode = json.Run("--json");

        // Assert
        Assert.Equal(70, textCode);
        Assert.StartsWith("Error: Help is unavailable: Help card 'a.md' of source 'test'", text.Error.ToString(), StringComparison.Ordinal);
        Assert.Empty(text.Output.ToString());
        Assert.Equal(70, jsonCode);
        Assert.StartsWith(
            "{\"helpSchemaVersion\":1,\"ok\":false,\"kind\":\"error\",\"code\":\"catalog-error\",\"message\":\"Help is unavailable: Help card 'a.md'",
            json.Output.ToString(),
            StringComparison.Ordinal);
        Assert.DoesNotContain("candidates", json.Output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a factory that returns no catalog is reported as unavailable help, not as an exception.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_FactoryReturnsNull_ExitsWith70()
    {
        // Arrange
        var harness = new CliHarness(factory: _ => null!);

        // Act
        var code = harness.Run();

        // Assert
        Assert.Equal(70, code);
        Assert.Contains("Help is unavailable", harness.Error.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the fallback resolver is offered the words of a card command before the catalog exists, and that
    ///     its answer is written as given with its exit code.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_FallbackAnswer_IsWrittenAsGivenBeforeTheCatalogIsCreated()
    {
        // Arrange
        IReadOnlyList<string>? offered = null;
        var harness = new CliHarness(
            factory: _ => throw new InvalidOperationException("The catalog must not be created."),
            fallback: words =>
            {
                offered = words;
                return words[0] == "build" ? new HelpCliAnswer("Help of the build command.", 3) : null;
            });

        // Act
        var code = harness.Run("build", "--json");

        // Assert
        Assert.Equal(3, code);
        Assert.Equal("Help of the build command.\n", harness.Output.ToString());
        Assert.Equal(["build"], offered);
        Assert.Equal(0, harness.FactoryCalls);
    }

    /// <summary>
    ///     Proves that a fallback that has no answer lets the catalog answer, and that it is offered card commands only.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_FallbackWithoutAnswer_LetsTheCatalogAnswerAndIsNotOfferedOtherModes()
    {
        // Arrange
        var offers = 0;
        var harness = new CliHarness(
            CatalogBuilding.Simple(),
            fallback: _ =>
            {
                offers++;
                return null;
            });

        // Act
        var card = harness.Run("guide");
        var examples = harness.Run("guide", "--examples");
        var index = harness.Run();
        var search = harness.Run("--search", "guide");

        // Assert
        Assert.Equal([0, 0, 0, 0], new[] { card, examples, index, search });
        Assert.Equal(1, offers);
    }

    /// <summary>
    ///     Proves that the language option passes its culture to the factory when it is enabled, and that the factory
    ///     receives no culture otherwise.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_Language_PassesTheCultureToTheFactory()
    {
        // Arrange
        var enabled = new CliHarness(CatalogBuilding.Simple(), language: true);
        var disabled = new CliHarness(CatalogBuilding.Simple());

        // Act
        enabled.Run("--lang", "de-DE");
        var given = enabled.LastCulture;
        enabled.Run();
        var noneGiven = enabled.LastCulture;
        var rejected = disabled.Run("--lang", "de");

        // Assert
        Assert.Equal("de-DE", given!.Name);
        Assert.Null(noneGiven);
        Assert.Equal(2, rejected);
        Assert.Equal(0, disabled.FactoryCalls);
    }

    /// <summary>
    ///     Proves that the width option wraps the generated lines of a listing and that JSON ignores it.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_Width_WrapsGeneratedLines()
    {
        // Arrange
        var catalog = CatalogBuilding.Create(CatalogBuilding.Fixture("big"));
        var narrow = new CliHarness(catalog);
        var wide = new CliHarness(catalog);
        var json = new CliHarness(catalog);
        var jsonNarrow = new CliHarness(catalog);

        // Act
        narrow.Run("--all", "--width", "40");
        wide.Run("--all", "--width", "120");
        json.Run("--all", "--json");
        jsonNarrow.Run("--all", "--json", "--width", "20");

        // Assert
        Assert.All(narrow.Output.ToString().Split('\n'), line => Assert.True(line.Length <= 40, line));
        Assert.Contains(wide.Output.ToString().Split('\n'), line => line.Length > 40);
        Assert.Equal(json.Output.ToString(), jsonNarrow.Output.ToString());
    }

    /// <summary>
    ///     Proves that the default width of the options applies when the command line gives none.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_DefaultWidth_AppliesWithoutTheOption()
    {
        // Arrange
        var catalog = CatalogBuilding.Create(CatalogBuilding.Fixture("big"));
        var byDefault = new CliHarness(catalog, defaultWidth: 40);
        var explicitWidth = new CliHarness(catalog);
        var unwrapped = new CliHarness(catalog, defaultWidth: 0);

        // Act
        byDefault.Run("--all");
        explicitWidth.Run("--all", "--width", "40");
        unwrapped.Run("--all");

        // Assert
        Assert.Equal(explicitWidth.Output.ToString(), byDefault.Output.ToString());
        Assert.Contains(unwrapped.Output.ToString().Split('\n'), line => line.Length > 60);
    }
    /// <summary>
    ///     Proves that <c>--width auto</c> takes the width of the console, clamped to 40 to 120, and 78 without one.
    /// </summary>
    [Theory]
    [InlineData(100, 100)]
    [InlineData(10, 40)]
    [InlineData(500, 120)]
    [InlineData(null, 78)]
    [InlineData(0, 78)]
    public void CliDispatcher_Run_AutoWidth_ClampsTheConsoleWidth(int? console, int expected)
    {
        // Arrange
        var catalog = CatalogBuilding.Create(CatalogBuilding.Fixture("big"));
        var auto = new CliHarness(catalog, width: () => console);
        var fixedWidth = new CliHarness(catalog);

        // Act
        auto.Run("--all", "--width", "auto");
        fixedWidth.Run("--all", "--width", expected.ToString(System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        Assert.Equal(fixedWidth.Output.ToString(), auto.Output.ToString());
    }

    /// <summary>
    ///     Proves that without a width provider and with a writer that is not the console, <c>auto</c> is 78.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_AutoWidthWithoutProvider_Is78ForAnyWriter()
    {
        // Arrange
        var catalog = CatalogBuilding.Create(CatalogBuilding.Fixture("big"));
        var auto = new CliHarness(catalog);
        var fixedWidth = new CliHarness(catalog);

        // Act
        auto.Run("--all", "--width", "auto");
        fixedWidth.Run("--all", "--width", "78");

        // Assert
        Assert.Equal(fixedWidth.Output.ToString(), auto.Output.ToString());
    }

    /// <summary>
    ///     Proves that the command template of the options builds the commands of the output, and that the tool name of
    ///     the options is used by default, whatever name the catalog has for its tool.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_CommandTemplate_BuildsTheCommands()
    {
        // Arrange
        var custom = new CliHarness(CatalogBuilding.Simple(), template: "{tool} manual {path}");
        var standard = new CliHarness(CatalogBuilding.Simple());

        // Act
        custom.Run("guide");
        standard.Run("guide");

        // Assert: the catalog calls its tool "simple"; the helper writes the name the user types
        Assert.Contains("`simple manual guide setup`", custom.Output.ToString(), StringComparison.Ordinal);
        Assert.Contains("`tool help guide setup`", standard.Output.ToString(), StringComparison.Ordinal);
    }
}
