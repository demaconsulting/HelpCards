namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     One finding about a help source: an error, a warning, or an informational note.
/// </summary>
/// <remarks>
///     Problems are plain immutable values so a caller can collect, sort, filter, and print them without
///     catching exceptions. Every problem names where it was found (source, file, and line) and which rule
///     it breaks, so an author can fix the card without reading library code. The record is immutable and
///     therefore safe for concurrent use.
/// </remarks>
/// <param name="Severity">How serious the problem is.</param>
/// <param name="RuleId">The identifier of the rule that was broken, such as <c>syntax</c> or <c>link-target</c>.</param>
/// <param name="SourceName">The name of the source the problem was found in.</param>
/// <param name="File">The path of the file inside the source, or an empty string when no file applies.</param>
/// <param name="Line">The one-based line in the file, or 0 when no line applies.</param>
/// <param name="Message">A sentence that says what is wrong and, where possible, how to fix it.</param>
public sealed record HelpProblem(
    HelpSeverity Severity,
    string RuleId,
    string SourceName,
    string File,
    int Line,
    string Message)
{
    /// <summary>
    ///     Creates an <see cref="HelpSeverity.Error"/> problem.
    /// </summary>
    /// <remarks>
    ///     A named factory keeps the many raise sites short and makes it obvious at each site that the
    ///     problem fails catalog creation.
    /// </remarks>
    /// <param name="ruleId">The rule that was broken.</param>
    /// <param name="sourceName">The name of the source the problem was found in.</param>
    /// <param name="file">The path of the file inside the source, or an empty string.</param>
    /// <param name="line">The one-based line, or 0 when no line applies.</param>
    /// <param name="message">The message that says what is wrong and how to fix it.</param>
    /// <returns>The new problem.</returns>
    internal static HelpProblem CreateError(string ruleId, string sourceName, string file, int line, string message)
    {
        return new HelpProblem(HelpSeverity.Error, ruleId, sourceName, file, line, message);
    }
}
