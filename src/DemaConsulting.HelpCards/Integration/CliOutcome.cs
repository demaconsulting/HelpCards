namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     The outcome of one mode of a help command: the text to write, or a failure.
/// </summary>
/// <remarks>
///     Exactly one of the two properties is set. The record is immutable.
/// </remarks>
/// <param name="Text">The text to write, or <see langword="null"/> when the command failed.</param>
/// <param name="Failure">The failure, or <see langword="null"/> when the command succeeded.</param>
internal sealed record CliOutcome(string? Text, CliFailure? Failure)
{
    /// <summary>
    ///     Creates the outcome of a command that succeeded.
    /// </summary>
    /// <param name="text">The text to write.</param>
    /// <returns>The outcome.</returns>
    public static CliOutcome Success(string text)
    {
        return new CliOutcome(text, null);
    }

    /// <summary>
    ///     Creates the outcome of a command that failed.
    /// </summary>
    /// <param name="failure">The failure.</param>
    /// <returns>The outcome.</returns>
    public static CliOutcome Fail(CliFailure failure)
    {
        return new CliOutcome(null, failure);
    }
}
