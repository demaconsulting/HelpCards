namespace DemaConsulting.HelpCards.Testing;

/// <summary>
///     The result of running one command example: its exit code and the text it wrote.
/// </summary>
/// <remarks>
///     A test returns it from the delegate that <see cref="HelpExampleRunner"/> calls for every command example. The
///     output is the text the command wrote to the standard output and the standard error, in the order a person
///     would read it; <see cref="HelpExampleRunner"/> compares it with the <c>expect</c> fragments of the example after
///     it replaces CRLF with LF. The record is immutable and therefore safe for concurrent use.
/// </remarks>
/// <param name="ExitCode">The exit code of the command.</param>
/// <param name="Output">The text the command wrote; <see langword="null"/> is treated like an empty text.</param>
public sealed record HelpExampleOutcome(int ExitCode, string? Output);
