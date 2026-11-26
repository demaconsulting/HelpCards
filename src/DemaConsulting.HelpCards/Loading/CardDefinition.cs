namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     A card as read from a source: its own text with the selected culture variant already applied.
/// </summary>
/// <remarks>
///     A definition is the loader's finished product for one card file. It still uses local ids and
///     unresolved links; composition turns definitions into the immutable catalog. Tokens are replaced (except
///     in the content of an example that sets <c>tokens: false</c>) and
///     the body is split into text and link parts.
/// </remarks>
/// <param name="SourceName">The name of the source the card belongs to.</param>
/// <param name="LocalId">The path of the card file without extension, such as <c>mermaid/flow</c>.</param>
/// <param name="File">The path of the neutral card file inside the source.</param>
/// <param name="Title">The title.</param>
/// <param name="Summary">The one-line summary.</param>
/// <param name="Aliases">The aliases, neutral first, then those of the selected translation.</param>
/// <param name="Tags">The tags.</param>
/// <param name="Order">The sort order among siblings, or <see langword="null"/>.</param>
/// <param name="Children">The <c>children</c> entries, which only order the direct children.</param>
/// <param name="SeeAlso">The see-also references.</param>
/// <param name="Examples">The examples.</param>
/// <param name="Extensions">The <c>x-</c> keys and their values.</param>
/// <param name="Culture">The culture of the file that supplied the body.</param>
/// <param name="Body">The body as text and link parts.</param>
/// <param name="BodyLine">The one-based line of the body in the file that supplied it.</param>
/// <param name="BodyFile">The path of the file that supplied the body, so a problem in a link names that file.</param>
internal sealed record CardDefinition(
    string SourceName,
    string LocalId,
    string File,
    string Title,
    string Summary,
    IReadOnlyList<LocatedValue> Aliases,
    IReadOnlyList<LocatedValue> Tags,
    int? Order,
    IReadOnlyList<LocatedValue> Children,
    IReadOnlyList<ReferenceDefinition> SeeAlso,
    IReadOnlyList<ExampleDefinition> Examples,
    IReadOnlyDictionary<string, string> Extensions,
    string Culture,
    IReadOnlyList<BodyPart> Body,
    int BodyLine,
    string BodyFile);
