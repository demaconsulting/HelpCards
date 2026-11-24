using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Integration;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Integration;

/// <summary>
///     Unit tests for <see cref="HelpCli"/>: creation, the verb gate, the footer, and the factory that is never called
///     for anything that is not a help command.
/// </summary>
public class HelpCliTests
{
    /// <summary>
    ///     Creates a harness whose factory fails the test when it is called.
    /// </summary>
    /// <returns>The harness.</returns>
    private static CliHarness ThrowingHarness()
    {
        return new CliHarness(factory: _ => throw new InvalidOperationException("The catalog factory must not be called."));
    }

    /// <summary>
    ///     Proves that creating a helper and reading the footer never call the catalog factory.
    /// </summary>
    [Fact]
    public void HelpCli_Create_AndFooter_NeverCallTheFactory()
    {
        // Arrange / Act
        var harness = ThrowingHarness();
        var footer = harness.Cli.Footer;

        // Assert
        Assert.Equal("Run 'tool help' for topics and examples.", footer);
        Assert.Equal(0, harness.FactoryCalls);
    }

    /// <summary>
    ///     Gets the arguments of commands that are not help commands.
    /// </summary>
    public static TheoryData<string[]> NonHelpArguments => new()
    {
        Array.Empty<string>(),
        new[] { "--help" },
        new[] { "build" },
        new[] { "helper" },
        new[] { "-h" },
        new[] { "x", "help" },
        new[] { string.Empty },
    };

    /// <summary>
    ///     Proves that arguments that are not a help command return false at once, with exit code 0 and no catalog,
    ///     output, or error.
    /// </summary>
    [Theory]
    [MemberData(nameof(NonHelpArguments))]
    public void HelpCli_TryRun_NotTheVerb_ReturnsFalseWithoutCallingTheFactory(string[] args)
    {
        // Arrange
        var harness = ThrowingHarness();

        // Act
        var handled = harness.Cli.TryRun(args, out var exitCode);

        // Assert
        Assert.False(handled);
        Assert.Equal(0, exitCode);
        Assert.Equal(0, harness.FactoryCalls);
        Assert.Empty(harness.Output.ToString());
        Assert.Empty(harness.Error.ToString());
    }

    /// <summary>
    ///     Proves that the verb is compared ignoring case and that the rest of the arguments are the help command.
    /// </summary>
    [Theory]
    [InlineData("help")]
    [InlineData("HELP")]
    [InlineData("Help")]
    public void HelpCli_TryRun_Verb_RunsTheCommandIgnoringCase(string verb)
    {
        // Arrange
        var harness = new CliHarness(CatalogBuilding.Simple());

        // Act
        var handled = harness.Cli.TryRun([verb, "guide", "setup"], out var exitCode);

        // Assert
        Assert.True(handled);
        Assert.Equal(0, exitCode);
        Assert.StartsWith("# Setup\n", harness.Output.ToString(), StringComparison.Ordinal);
        Assert.Equal(1, harness.FactoryCalls);
    }

    /// <summary>
    ///     Proves that another verb is used when one is set, and that the footer names it.
    /// </summary>
    [Fact]
    public void HelpCli_Create_CustomVerb_ReplacesHelp()
    {
        // Arrange
        var harness = new CliHarness(CatalogBuilding.Simple(), verb: "man");

        // Act
        var man = harness.Cli.TryRun(["man"], out var manCode);
        var help = harness.Cli.TryRun(["help"], out _);

        // Assert
        Assert.True(man);
        Assert.Equal(0, manCode);
        Assert.False(help);
        Assert.Equal("Run 'tool man' for topics and examples.", harness.Cli.Footer);
        Assert.Contains("tool man guide", harness.Output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the verb alone is a help command that lists the topics.
    /// </summary>
    [Fact]
    public void HelpCli_TryRun_VerbAlone_WritesTheIndex()
    {
        // Arrange
        var harness = new CliHarness(CatalogBuilding.Simple());

        // Act
        var handled = harness.Cli.TryRun(["help"], out var exitCode);

        // Assert
        Assert.True(handled);
        Assert.Equal(0, exitCode);
        Assert.Contains("Help topics:\n", harness.Output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that <see cref="HelpCli.Run"/> takes the arguments after the verb and rejects null.
    /// </summary>
    [Fact]
    public void HelpCli_Run_Arguments_AreTheWordsAfterTheVerb()
    {
        // Arrange
        var harness = new CliHarness(CatalogBuilding.Simple());

        // Act
        var code = harness.Cli.Run(["guide"]);

        // Assert
        Assert.Equal(0, code);
        Assert.StartsWith("# Guide\n", harness.Output.ToString(), StringComparison.Ordinal);
        Assert.Throws<ArgumentNullException>(() => harness.Cli.Run(null!));
        Assert.Throws<ArgumentNullException>(() => harness.Cli.TryRun(null!, out _));
        Assert.Throws<ArgumentNullException>(() => harness.Cli.Run(["a", null!]));
    }

    /// <summary>
    ///     Proves that invalid options are rejected where the helper is created.
    /// </summary>
    [Fact]
    public void HelpCli_Create_InvalidOptions_Throw()
    {
        // Arrange
        static HelpCatalog Factory(System.Globalization.CultureInfo? culture) => CatalogBuilding.Simple();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => HelpCli.Create(null!));
        Assert.Throws<ArgumentNullException>(() => HelpCli.Create(new HelpCliOptions { ToolName = "t", CatalogFactory = null! }));
        Assert.Throws<ArgumentException>(() => HelpCli.Create(new HelpCliOptions { ToolName = " ", CatalogFactory = Factory }));
        Assert.Throws<ArgumentException>(() => HelpCli.Create(new HelpCliOptions { ToolName = "a{b", CatalogFactory = Factory }));
        Assert.Throws<ArgumentException>(() => HelpCli.Create(new HelpCliOptions { ToolName = "t", Verb = "", CatalogFactory = Factory }));
        Assert.Throws<ArgumentException>(() => HelpCli.Create(new HelpCliOptions { ToolName = "t", Verb = "a b", CatalogFactory = Factory }));
        Assert.Throws<ArgumentException>(() => HelpCli.Create(new HelpCliOptions { ToolName = "t", Verb = "-h", CatalogFactory = Factory }));
        Assert.Throws<ArgumentException>(() => HelpCli.Create(new HelpCliOptions { ToolName = "t", CommandTemplate = " ", CatalogFactory = Factory }));
        Assert.Throws<ArgumentOutOfRangeException>(() => HelpCli.Create(new HelpCliOptions { ToolName = "t", DefaultWidth = 10, CatalogFactory = Factory }));
        Assert.NotNull(HelpCli.Create(new HelpCliOptions { ToolName = "t", DefaultWidth = 0, CatalogFactory = Factory }));
    }
}
