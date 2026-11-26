using DemaConsulting.HelpCards.Integration;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Integration;

/// <summary>
///     Unit tests for the contract settings of <see cref="HelpCliOptions"/>: the defaults, the checks of the exit
///     codes that a tool with a JSON contract of its own sets, and what happens when its callbacks fail.
/// </summary>
public partial class HelpCliTests
{
    /// <summary>
    ///     Proves that an exception of the error body callback of the tool is not caught: it propagates out of
    ///     <see cref="HelpCli.Run"/> and <see cref="HelpCli.TryRun"/> and nothing is written.
    /// </summary>
    [Fact]
    public void HelpCli_Run_JsonErrorThrows_PropagatesTheException()
    {
        // Arrange
        var harness = new CliHarness(
            CatalogBuilding.Simple(),
            jsonError: (_, _) => throw new InvalidOperationException("The tool failed."));

        // Act / Assert
        var exception = Assert.Throws<InvalidOperationException>(() => harness.Cli.Run(["qqqqqqqq", "--json"]));
        Assert.Equal("The tool failed.", exception.Message);
        Assert.Throws<InvalidOperationException>(() => harness.Cli.TryRun(["help", "qqqqqqqq", "--json"], out _));
        Assert.Empty(harness.Output.ToString());
    }

    /// <summary>
    ///     Proves that an exception of the envelope callback of the tool propagates, for a document and for an error,
    ///     and that an envelope that returns null gives an argument exception.
    /// </summary>
    [Fact]
    public void HelpCli_Run_JsonEnvelopeThrowsOrReturnsNull_Propagates()
    {
        // Arrange
        var throwing = new CliHarness(
            CatalogBuilding.Simple(),
            jsonEnvelope: (_, _) => throw new InvalidOperationException("The envelope failed."));
        var returningNull = new CliHarness(CatalogBuilding.Simple(), jsonEnvelope: (_, _) => null!);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => throwing.Cli.Run(["guide", "--json"]));
        Assert.Throws<InvalidOperationException>(() => throwing.Cli.Run(["qqqqqqqq", "--json"]));
        Assert.Throws<ArgumentException>(() => returningNull.Cli.Run(["guide", "--json"]));
        Assert.Throws<ArgumentException>(() => returningNull.Cli.Run(["qqqqqqqq", "--json"]));
    }

    /// <summary>
    ///     Proves that the exit codes are checked when they are set: from 0 to 255, and the mapping is never null.
    /// </summary>
    [Fact]
    public void HelpCliExitCodes_Init_OutOfRangeOrNull_Throws()
    {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new HelpCliExitCodes { Success = -1 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new HelpCliExitCodes { UsageError = 256 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new HelpCliExitCodes { InternalError = 1000 });
        Assert.Equal((0, 255), (new HelpCliExitCodes { UsageError = 0 }.UsageError, new HelpCliExitCodes { InternalError = 255 }.InternalError));
        Assert.Throws<ArgumentNullException>(() => new HelpCliOptions { ToolName = "t", ExitCodes = null! });
    }

    /// <summary>
    ///     Proves that the envelope callback, the error body callback, and the exit codes are returned as they were set.
    /// </summary>
    [Fact]
    public void HelpCliOptions_ContractSettings_AreReturnedAsSet()
    {
        // Arrange
        var codes = new HelpCliExitCodes { Success = 10, UsageError = 20, InternalError = 30 };
        Func<string, Action<System.Text.Json.Utf8JsonWriter>, string> envelope = ToolContract.Envelope;
        Action<System.Text.Json.Utf8JsonWriter, HelpCliError> body = ToolContract.ErrorBody;

        // Act
        var options = new HelpCliOptions
        {
            ToolName = "t",
            JsonEnvelope = envelope,
            JsonError = body,
            ExitCodes = codes,
        };

        // Assert
        Assert.Same(codes, options.ExitCodes);
        Assert.Same(envelope, options.JsonEnvelope);
        Assert.Same(body, options.JsonError);
    }

    /// <summary>
    ///     Proves that the exit codes default to 0, 2, and 70 and that the options default to no envelope and no error
    ///     body.
    /// </summary>
    [Fact]
    public void HelpCliOptions_Defaults_AreTheContractOfTheHelper()
    {
        // Act
        var options = new HelpCliOptions { ToolName = "t" };

        // Assert
        Assert.Equal((0, 2, 70), (options.ExitCodes.Success, options.ExitCodes.UsageError, options.ExitCodes.InternalError));
        Assert.Null(options.JsonEnvelope);
        Assert.Null(options.JsonError);
    }
}
