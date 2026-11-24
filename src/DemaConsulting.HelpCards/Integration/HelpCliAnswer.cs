namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     The answer of a tool to a help subject that the catalog does not own, such as the help of one command.
/// </summary>
/// <remarks>
///     The helper writes the text as given, followed by one line feed, to its output and exits with the exit code.
/// </remarks>
/// <param name="Text">The text to write.</param>
/// <param name="ExitCode">The exit code of the command; 0 unless given.</param>
public sealed record HelpCliAnswer(string Text, int ExitCode = 0);
