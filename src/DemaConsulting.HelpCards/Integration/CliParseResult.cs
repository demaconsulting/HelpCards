namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     The result of reading the arguments of a help command: a request, or the first usage error.
/// </summary>
/// <remarks>
///     Exactly one of <paramref name="Request"/> and <paramref name="Message"/> is set. The record is immutable.
/// </remarks>
/// <param name="Request">The request, or <see langword="null"/> when the arguments are wrong.</param>
/// <param name="Message">The message of the first usage error, or <see langword="null"/>.</param>
/// <param name="Hint">A hint for the user, or <see langword="null"/>.</param>
/// <param name="JsonRequested">Whether the arguments ask for JSON, so that an error is written as JSON too.</param>
internal sealed record CliParseResult(CliRequest? Request, string? Message, string? Hint, bool JsonRequested);
