namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     The result of reading the arguments of a call: the arguments, or the message that tells the agent what to send
///     instead.
/// </summary>
/// <remarks>
///     Exactly one of the two properties is set. The record is immutable.
/// </remarks>
/// <param name="Arguments">The arguments, or <see langword="null"/> when they are wrong.</param>
/// <param name="Error">The message for the agent, or <see langword="null"/> when the arguments are right.</param>
internal sealed record AgentReadResult(AgentArguments? Arguments, string? Error);
