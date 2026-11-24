namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     One item of a front-matter list: a scalar or a map of scalars.
/// </summary>
/// <remarks>
///     The subset allows lists of scalars (aliases, tags) and lists of maps (examples), and nothing deeper.
///     A map item holds entries whose values are scalars; the decoder rejects any deeper nesting. Exactly
///     one of <paramref name="Scalar"/> and <paramref name="Map"/> is not null.
/// </remarks>
/// <param name="Line">The one-based line of the file the item starts on.</param>
/// <param name="Scalar">The scalar value, or <see langword="null"/> when the item is a map.</param>
/// <param name="Map">The entries of the map, or <see langword="null"/> when the item is a scalar.</param>
internal sealed record FrontMatterItem(
    int Line,
    FrontMatterScalar? Scalar,
    IReadOnlyList<FrontMatterEntry>? Map);
