using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     Everything the loader read from one source: its description, its cards, its overlays, and its problems.
/// </summary>
/// <remarks>
///     The definition is produced even when problems exist, so one run reports every defect of the source.
///     A card or overlay file whose front matter could not be read is not in <see cref="Cards"/> or
///     <see cref="Overlays"/>; its local id is in <see cref="FailedIds"/> so that references to it can be
///     left unreported (cascade suppression). The title and summary already have their defaults applied.
/// </remarks>
/// <param name="Name">The declared name of the source.</param>
/// <param name="File">The path of <c>_source.md</c>, or an empty string when the source has none.</param>
/// <param name="Title">The title of the source; the name when none is declared.</param>
/// <param name="Summary">The summary of the source; <c>Help cards of {name}.</c> when none is declared.</param>
/// <param name="Language">The culture of the neutral files, <c>en</c> by default.</param>
/// <param name="Schema">The card format major version of the source.</param>
/// <param name="Aliases">The aliases that name the mount root of the source.</param>
/// <param name="Children">The <c>children</c> entries that order the roots of the source.</param>
/// <param name="Extensions">The <c>x-</c> keys of <c>_source.md</c> and their values.</param>
/// <param name="Culture">The culture of the file that supplied the landing body.</param>
/// <param name="Body">The landing body as text and link parts.</param>
/// <param name="BodyLine">The one-based line of the landing body in the file that supplied it.</param>
/// <param name="Cards">The cards in ordinal file order.</param>
/// <param name="Overlays">The overlays in ordinal file order.</param>
/// <param name="FailedIds">The local ids of card and overlay files that could not be read.</param>
/// <param name="Problems">Every problem found, in the order it was found.</param>
internal sealed record SourceDefinition(
    string Name,
    string File,
    string Title,
    string Summary,
    string Language,
    int Schema,
    IReadOnlyList<LocatedValue> Aliases,
    IReadOnlyList<LocatedValue> Children,
    IReadOnlyDictionary<string, string> Extensions,
    string Culture,
    IReadOnlyList<BodyPart> Body,
    int BodyLine,
    IReadOnlyList<CardDefinition> Cards,
    IReadOnlyList<OverlayDefinition> Overlays,
    IReadOnlyList<string> FailedIds,
    IReadOnlyList<HelpProblem> Problems);
