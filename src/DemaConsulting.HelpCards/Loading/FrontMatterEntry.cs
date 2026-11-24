namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     One <c>key: value</c> entry of the front matter.
/// </summary>
/// <remarks>
///     The value is either a scalar or a list, never both: exactly one of <paramref name="Scalar"/> and
///     <paramref name="Items"/> is not null. An empty value (a key with nothing after the colon and no list
///     below it) is a scalar with empty text.
/// </remarks>
/// <param name="Key">The key as written.</param>
/// <param name="Line">The one-based line of the file the key is on.</param>
/// <param name="Scalar">The scalar value, or <see langword="null"/> when the value is a list.</param>
/// <param name="Items">The list items, or <see langword="null"/> when the value is a scalar.</param>
internal sealed record FrontMatterEntry(
    string Key,
    int Line,
    FrontMatterScalar? Scalar,
    IReadOnlyList<FrontMatterItem>? Items);
