namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     The arguments of one call of the knowledge tool after they were read and checked.
/// </summary>
/// <remarks>
///     Text values are trimmed, and a value that was null, empty, or white space is <see langword="null"/>. The record is
///     immutable.
/// </remarks>
/// <param name="Topic">The card id or path, or <see langword="null"/>.</param>
/// <param name="Example">The example name or <c>card#name</c>, or <see langword="null"/>.</param>
/// <param name="Query">The words to search for, or <see langword="null"/>.</param>
/// <param name="Source">The source to restrict to, or <see langword="null"/>.</param>
/// <param name="Tag">The tag to restrict to, or <see langword="null"/>.</param>
/// <param name="MaxResults">The most search results to return, from 1 to 20, or <see langword="null"/>.</param>
internal sealed record AgentArguments(
    string? Topic,
    string? Example,
    string? Query,
    string? Source,
    string? Tag,
    int? MaxResults);
