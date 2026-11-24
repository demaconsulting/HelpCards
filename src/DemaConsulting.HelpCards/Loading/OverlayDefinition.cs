namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     An overlay as read from a source: text that extends a card instead of defining one.
/// </summary>
/// <remarks>
///     An overlay is a file whose front matter has <c>extends</c>. Its path only needs to be a valid id that
///     is unique in the source. Applying overlays to cards is the job of composition; the loader only reads
///     and validates the file.
/// </remarks>
/// <param name="SourceName">The name of the source the overlay belongs to.</param>
/// <param name="LocalId">The path of the overlay file without extension.</param>
/// <param name="File">The path of the neutral overlay file inside the source.</param>
/// <param name="Extends">The card the overlay extends.</param>
/// <param name="Optional">Whether the overlay is skipped when the source it targets is not present.</param>
/// <param name="Title">The replacement title, or <see langword="null"/>.</param>
/// <param name="Summary">The replacement summary, or <see langword="null"/>.</param>
/// <param name="SeeAlso">The see-also references to append.</param>
/// <param name="Examples">The examples to append.</param>
/// <param name="Extensions">The <c>x-</c> keys and their values.</param>
/// <param name="Culture">The culture of the file that supplied the body.</param>
/// <param name="Body">The body to append, as text and link parts; empty when the overlay has no body.</param>
/// <param name="BodyLine">The one-based line of the body in the file that supplied it.</param>
/// <param name="BodyFile">The path of the file that supplied the body, so a problem in a link names that file.</param>
internal sealed record OverlayDefinition(
    string SourceName,
    string LocalId,
    string File,
    ReferenceDefinition Extends,
    bool Optional,
    string? Title,
    string? Summary,
    IReadOnlyList<ReferenceDefinition> SeeAlso,
    IReadOnlyList<ExampleDefinition> Examples,
    IReadOnlyDictionary<string, string> Extensions,
    string Culture,
    IReadOnlyList<BodyPart> Body,
    int BodyLine,
    string BodyFile);
