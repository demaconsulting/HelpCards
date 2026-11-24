namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     A string value from the front matter together with where it was written.
/// </summary>
/// <remarks>
///     Aliases, tags, and <c>children</c> entries keep their location so a later check, such as an unknown
///     tag or a missing child, can name the exact file and line instead of only the card.
/// </remarks>
/// <param name="Value">The value.</param>
/// <param name="File">The path of the file the value was written in, inside the source.</param>
/// <param name="Line">The one-based line of the file.</param>
internal sealed record LocatedValue(string Value, string File, int Line);
