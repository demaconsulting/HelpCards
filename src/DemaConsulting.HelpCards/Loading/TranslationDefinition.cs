namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     A translation file of a card, overlay, or <c>_source.md</c>, as read before it is merged.
/// </summary>
/// <remarks>
///     A translation may carry only a title, a summary, additional aliases, example descriptions, and a body.
///     Missing values fall back individually to the neutral file. A translation without a body is not used at
///     all: the neutral file then supplies everything.
/// </remarks>
/// <param name="Culture">The culture suffix of the file name as written, such as <c>de-DE</c>.</param>
/// <param name="File">The path of the translation file inside the source.</param>
/// <param name="Title">The translated title, or <see langword="null"/>.</param>
/// <param name="Summary">The translated summary, or <see langword="null"/>.</param>
/// <param name="Aliases">The aliases to add to the neutral aliases.</param>
/// <param name="ExampleDescriptions">The translated example descriptions by example name.</param>
/// <param name="HasBody">Whether the file has a body that is not empty or white space.</param>
/// <param name="Body">The body as text and link parts.</param>
/// <param name="BodyLine">The one-based line of the body in the file.</param>
internal sealed record TranslationDefinition(
    string Culture,
    string File,
    string? Title,
    string? Summary,
    IReadOnlyList<LocatedValue> Aliases,
    IReadOnlyDictionary<string, string> ExampleDescriptions,
    bool HasBody,
    IReadOnlyList<BodyPart> Body,
    int BodyLine);
