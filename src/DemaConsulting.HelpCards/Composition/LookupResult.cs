namespace DemaConsulting.HelpCards.Composition;

/// <summary>
///     The result of looking up one reference.
/// </summary>
/// <param name="Kind">What the lookup found.</param>
/// <param name="Build">The card when <paramref name="Kind"/> is <see cref="LookupKind.Found"/>; otherwise <see langword="null"/>.</param>
/// <param name="Detail">A description of the clash when <paramref name="Kind"/> is <see cref="LookupKind.Ambiguous"/>.</param>
internal sealed record LookupResult(LookupKind Kind, CardBuild? Build, string? Detail = null);
