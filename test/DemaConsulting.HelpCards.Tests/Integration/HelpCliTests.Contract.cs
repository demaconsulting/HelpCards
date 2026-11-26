using DemaConsulting.HelpCards.Integration;

namespace DemaConsulting.HelpCards.Tests.Integration;

/// <summary>
///     Unit tests for the contract settings of <see cref="HelpCliOptions"/>: the defaults and the checks of the exit
///     codes that a tool with a JSON contract of its own sets.
/// </summary>
public partial class HelpCliTests
{
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
