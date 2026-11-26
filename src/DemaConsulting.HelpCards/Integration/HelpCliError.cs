namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     The data of a failed help command, handed to <see cref="HelpCliOptions.JsonError"/> so that a tool can write the
///     error in the shape of its own JSON contract.
/// </summary>
/// <remarks>
///     The record is immutable. It carries what the default error document carries: the same code, message, hint, and
///     candidates, so a tool that writes its own error body loses no information.
/// </remarks>
/// <param name="Code">
///     One of <c>usage-error</c> (wrong arguments), <c>not-found</c> (a card, example, tag, or source that does not
///     exist), <c>ambiguous</c> (a name that matches several), or <c>catalog-error</c> (a defective or missing catalog).
/// </param>
/// <param name="Message">The message for a reader.</param>
/// <param name="Hint">The hint for the reader, or <see langword="null"/> when there is none.</param>
/// <param name="Candidates">
///     The global ids or names that could have been meant, for <c>not-found</c> and <c>ambiguous</c>; empty for every
///     other code and when there is none.
/// </param>
public sealed record HelpCliError(string Code, string Message, string? Hint, IReadOnlyList<string> Candidates);
