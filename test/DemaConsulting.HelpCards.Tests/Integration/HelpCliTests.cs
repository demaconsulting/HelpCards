using System.Globalization;
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Composition;
using DemaConsulting.HelpCards.Integration;
using DemaConsulting.HelpCards.Loading;
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
        Assert.Throws<ArgumentException>(() => HelpCli.Create(new HelpCliOptions { ToolName = "t" }));
        Assert.Throws<ArgumentException>(() => HelpCli.Create(new HelpCliOptions { ToolName = " ", CatalogFactory = Factory }));
        Assert.Throws<ArgumentException>(() => HelpCli.Create(new HelpCliOptions { ToolName = "a{b", CatalogFactory = Factory }));
        Assert.Throws<ArgumentException>(() => HelpCli.Create(new HelpCliOptions { ToolName = "t", Verb = "", CatalogFactory = Factory }));
        Assert.Throws<ArgumentException>(() => HelpCli.Create(new HelpCliOptions { ToolName = "t", Verb = "a b", CatalogFactory = Factory }));
        Assert.Throws<ArgumentException>(() => HelpCli.Create(new HelpCliOptions { ToolName = "t", Verb = "-h", CatalogFactory = Factory }));
        Assert.Throws<ArgumentException>(() => HelpCli.Create(new HelpCliOptions { ToolName = "t", CommandTemplate = " ", CatalogFactory = Factory }));
        Assert.Throws<ArgumentOutOfRangeException>(() => HelpCli.Create(new HelpCliOptions { ToolName = "t", DefaultWidth = 10, CatalogFactory = Factory }));
        Assert.NotNull(HelpCli.Create(new HelpCliOptions { ToolName = "t", DefaultWidth = 0, CatalogFactory = Factory }));
    }

    /// <summary>
    ///     Creates the source of the simple fixture with the token its cards need.
    /// </summary>
    /// <returns>The source.</returns>
    private static HelpSource LimitedSimple()
    {
        return CatalogBuilding.Fixture("simple", new Dictionary<string, string> { ["limit"] = "42" });
    }

    /// <summary>
    ///     Creates options that build the catalog from the simple fixture through <c>ConfigureCatalog</c>.
    /// </summary>
    /// <param name="configure">Receives the call of the action, or <see langword="null"/>.</param>
    /// <returns>The options, whose writers are not set.</returns>
    private static HelpCliOptions ConfigureOptions(Action<HelpCatalogBuilder, CultureInfo?>? configure = null)
    {
        return new HelpCliOptions
        {
            ToolName = "mytool",
            Version = "3.1",
            EnableLanguageOption = true,
            ConfigureCatalog = (builder, culture) =>
            {
                builder.AddSource(LimitedSimple());
                configure?.Invoke(builder, culture);
            },
        };
    }

    /// <summary>
    ///     Proves that a helper needs exactly one of the catalog configuration and the catalog factory.
    /// </summary>
    [Fact]
    public void HelpCli_Create_BothFactoryAndConfigure_Throws()
    {
        // Arrange
        static HelpCatalog Factory(CultureInfo? culture) => CatalogBuilding.Simple();

        // Act
        var exception = Assert.Throws<ArgumentException>(() => HelpCli.Create(new HelpCliOptions
        {
            ToolName = "t",
            CatalogFactory = Factory,
            ConfigureCatalog = (builder, culture) => builder.AddSource(LimitedSimple()),
        }));

        // Assert
        Assert.Contains("exactly one of ConfigureCatalog and CatalogFactory", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a helper without a catalog configuration and without a factory is rejected when it is created.
    /// </summary>
    [Fact]
    public void HelpCli_Create_NeitherFactoryNorConfigure_Throws()
    {
        // Arrange / Act
        var exception = Assert.Throws<ArgumentException>(() => HelpCli.Create(new HelpCliOptions { ToolName = "t" }));

        // Assert
        Assert.Contains("exactly one of ConfigureCatalog and CatalogFactory", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a fixed writer and the provider for the same stream are not combined.
    /// </summary>
    [Fact]
    public void HelpCli_Create_OutputAndProvider_Throws()
    {
        // Arrange
        var writer = new StringWriter();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => HelpCli.Create(new HelpCliOptions
        {
            ToolName = "t",
            ConfigureCatalog = (builder, culture) => { },
            Output = writer,
            OutputProvider = () => writer,
        }));
        Assert.Throws<ArgumentException>(() => HelpCli.Create(new HelpCliOptions
        {
            ToolName = "t",
            ConfigureCatalog = (builder, culture) => { },
            Error = writer,
            ErrorProvider = () => writer,
        }));
        Assert.NotNull(HelpCli.Create(new HelpCliOptions
        {
            ToolName = "t",
            ConfigureCatalog = (builder, culture) => { },
            Output = writer,
            ErrorProvider = () => writer,
        }));
    }

    /// <summary>
    ///     Proves that the catalog is configured only after the verb is seen, once for each command that needs it.
    /// </summary>
    [Fact]
    public void HelpCli_TryRun_ConfigureCatalog_BuildsOnlyAfterVerb()
    {
        // Arrange
        var calls = 0;
        var output = new StringWriter();
        var cli = HelpCli.Create(ConfigureOptionsWith(() => calls++, output));

        // Act
        var footer = cli.Footer;
        var other = cli.TryRun(["build"], out _);
        var afterGate = calls;
        var handled = cli.TryRun(["help", "guide"], out var exitCode);

        // Assert
        Assert.Equal("Run 'tool help' for topics and examples.", footer.Replace("mytool", "tool", StringComparison.Ordinal));
        Assert.False(other);
        Assert.Equal(0, afterGate);
        Assert.True(handled);
        Assert.Equal(0, exitCode);
        Assert.Equal(1, calls);
        Assert.StartsWith("# Guide\n", output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Creates options like <see cref="ConfigureOptions"/> that call back on every configuration and write to a
    ///     writer.
    /// </summary>
    /// <param name="onConfigure">Called when the catalog is configured.</param>
    /// <param name="output">The writer for output and errors.</param>
    /// <returns>The options.</returns>
    private static HelpCliOptions ConfigureOptionsWith(Action onConfigure, StringWriter output)
    {
        var inner = ConfigureOptions((builder, culture) => onConfigure());
        return new HelpCliOptions
        {
            ToolName = inner.ToolName,
            Version = inner.Version,
            EnableLanguageOption = true,
            ConfigureCatalog = inner.ConfigureCatalog,
            Output = output,
            Error = output,
        };
    }

    /// <summary>
    ///     Proves that the helper sets the tool name, the version, and the culture on the builder before the action runs
    ///     and that the action may override them.
    /// </summary>
    [Fact]
    public void HelpCli_TryRun_ConfigureCatalog_AppliesToolNameVersionAndCulture()
    {
        // Arrange
        CultureInfo? received = null;
        CultureInfo? builderCulture = null;
        var plain = new StringWriter();
        var overridden = new StringWriter();
        var cli = HelpCli.Create(new HelpCliOptions
        {
            ToolName = "mytool",
            Version = "3.1",
            EnableLanguageOption = true,
            ConfigureCatalog = (builder, culture) =>
            {
                builder.AddSource(LimitedSimple());
                received = culture;
                builderCulture = culture;
            },
            Output = plain,
        });
        var overriding = HelpCli.Create(new HelpCliOptions
        {
            ToolName = "mytool",
            Version = "3.1",
            ConfigureCatalog = (builder, culture) => builder.AddSource(LimitedSimple()).ToolVersion("9.9"),
            Output = overridden,
        });

        // Act
        var code = cli.Run(["guide", "--lang", "de-DE"]);
        overriding.Run(["guide"]);

        // Assert: the tokens of the card come from the options, and the culture reaches the action
        Assert.Equal(0, code);
        Assert.Contains("Version 3.1 of mytool accepts", plain.ToString(), StringComparison.Ordinal);
        Assert.Contains("`mytool help guide setup`", plain.ToString(), StringComparison.Ordinal);
        Assert.Equal("de-DE", received?.Name);
        Assert.Equal(received, builderCulture);
        Assert.Contains("Version 9.9 of mytool accepts", overridden.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the configuration for tests applies the same settings as the helper and needs a configuration.
    /// </summary>
    [Fact]
    public void HelpCliOptions_GetCatalogConfiguration_AppliesTheSettingsOfTheHelper()
    {
        // Arrange
        CultureInfo? received = null;
        var options = ConfigureOptions((builder, culture) => received = culture);
        var german = CultureInfo.GetCultureInfo("de-DE");

        // Act
        var catalog = HelpCatalog.Create(options.GetCatalogConfiguration(german));
        var invariant = HelpCatalog.Create(options.GetCatalogConfiguration());

        // Assert
        Assert.Equal("de-DE", catalog.Culture.Name);
        Assert.Equal(CultureInfo.InvariantCulture.Name, invariant.Culture.Name);
        Assert.Null(received);
        Assert.Contains("Version 3.1 of mytool accepts", catalog.Find("guide")!.Body, StringComparison.Ordinal);
        Assert.Throws<ArgumentNullException>(() => options.GetCatalogConfiguration()(null!));
    }

    /// <summary>
    ///     Proves that the configuration for tests cannot be taken from options that use a catalog factory.
    /// </summary>
    [Fact]
    public void HelpCliOptions_GetCatalogConfiguration_WithoutConfigure_Throws()
    {
        // Arrange
        var options = new HelpCliOptions { ToolName = "t", CatalogFactory = _ => CatalogBuilding.Simple() };

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => options.GetCatalogConfiguration());
    }

    /// <summary>
    ///     Proves that the writer providers are called when the helper writes, so the writer can change between commands.
    /// </summary>
    [Fact]
    public void HelpCli_TryRun_OutputProvider_IsCalledAtCommandTime()
    {
        // Arrange: the provider hands out another writer for each command
        var first = new StringWriter();
        var second = new StringWriter();
        var errors = new StringWriter();
        var current = first;
        var outputCalls = 0;
        var errorCalls = 0;
        var options = ConfigureOptions();
        var cli = HelpCli.Create(new HelpCliOptions
        {
            ToolName = options.ToolName,
            ConfigureCatalog = options.ConfigureCatalog,
            OutputProvider = () =>
            {
                outputCalls++;
                return current;
            },
            ErrorProvider = () =>
            {
                errorCalls++;
                return errors;
            },
        });

        // Act
        var creationCalls = outputCalls + errorCalls;
        cli.Run(["guide"]);
        current = second;
        cli.Run(["reference"]);
        var bad = cli.Run(["nothing"]);

        // Assert
        Assert.Equal(0, creationCalls);
        Assert.StartsWith("# Guide\n", first.ToString(), StringComparison.Ordinal);
        Assert.StartsWith("# Reference\n", second.ToString(), StringComparison.Ordinal);
        Assert.Equal(2, bad);
        Assert.StartsWith("Error: Unknown help card 'nothing'.", errors.ToString(), StringComparison.Ordinal);
        Assert.Equal((2, 1), (outputCalls, errorCalls));
    }

    /// <summary>
    ///     Proves that a provider that returns null makes the helper write to the console, and that a silent writer can
    ///     be chosen by the provider.
    /// </summary>
    [Fact]
    public void HelpCli_TryRun_ProviderReturnsNull_UsesConsole()
    {
        // Arrange
        var silent = false;
        var options = ConfigureOptions();
        var cli = HelpCli.Create(new HelpCliOptions
        {
            ToolName = options.ToolName,
            ConfigureCatalog = options.ConfigureCatalog,
            OutputProvider = () => silent ? TextWriter.Null : null,
            ErrorProvider = () => silent ? TextWriter.Null : null,
        });
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var console = new StringWriter();
        using var consoleError = new StringWriter();

        // Act
        Console.SetOut(console);
        Console.SetError(consoleError);
        try
        {
            cli.Run(["guide"]);
            cli.Run(["nothing"]);
            silent = true;
            cli.Run(["reference"]);
            cli.Run(["nothing"]);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }

        // Assert: only the commands before the provider chose the null writer reached the console
        Assert.StartsWith("# Guide\n", console.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("# Reference", console.ToString(), StringComparison.Ordinal);
        Assert.Equal(1, consoleError.ToString().Split("Error:").Length - 1);
    }

    /// <summary>
    ///     Proves that a provider counts as a writer that is not the console for <c>--width auto</c>.
    /// </summary>
    [Fact]
    public void HelpCli_TryRun_ProviderAndAutoWidth_IsNotTheConsoleWidth()
    {
        // Arrange
        var fixedWidth = new StringWriter();
        var auto = new StringWriter();
        var options = ConfigureOptions();
        var withProvider = HelpCli.Create(new HelpCliOptions
        {
            ToolName = options.ToolName,
            ConfigureCatalog = options.ConfigureCatalog,
            OutputProvider = () => auto,
        });
        var plain = HelpCli.Create(new HelpCliOptions
        {
            ToolName = options.ToolName,
            ConfigureCatalog = options.ConfigureCatalog,
            Output = fixedWidth,
        });

        // Act
        withProvider.Run(["--all", "--width", "auto"]);
        plain.Run(["--all", "--width", "78"]);

        // Assert: no console width is used, so the width is the fallback of 78
        Assert.Equal(fixedWidth.ToString(), auto.ToString());
    }
}
