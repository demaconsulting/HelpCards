using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Composition;

/// <summary>
///     A card while the pipeline builds it: its definition plus everything that overlays add.
/// </summary>
/// <remarks>
///     <para>
///         A build starts as the card the loader read. Overlay merging then replaces the title and summary and
///         appends body pieces, see-also entries, and examples; each addition remembers the source it came from,
///         because its references resolve in the scope of that source while its text belongs to this card. When
///         merging is done the pipeline creates the immutable <see cref="CardNode"/> from the build.
///     </para>
///     <para>
///         A build belongs to one catalog build and is not safe for concurrent use.
///     </para>
/// </remarks>
internal sealed class CardBuild
{
    /// <summary>
    ///     Creates a build from a card definition.
    /// </summary>
    /// <param name="entry">The source the card belongs to.</param>
    /// <param name="definition">The card as the loader read it.</param>
    /// <param name="globalId">The global id of the card.</param>
    public CardBuild(SourceEntry entry, CardDefinition definition, string globalId)
    {
        Entry = entry;
        Definition = definition;
        GlobalId = globalId;
        Title = definition.Title;
        Summary = definition.Summary;
        Bodies.Add(new BodySegment(definition.Body, definition.BodyFile, entry));
        SeeAlso.AddRange(definition.SeeAlso.Select(reference => new SeeAlsoEntry(reference, entry)));
        Examples.AddRange(definition.Examples.Select(example => new ExampleEntry(example, entry)));
        foreach (var (key, value) in definition.Extensions)
        {
            Extensions[key] = value;
        }
    }

    /// <summary>
    ///     Gets the source the card belongs to.
    /// </summary>
    public SourceEntry Entry { get; }

    /// <summary>
    ///     Gets the card as the loader read it.
    /// </summary>
    public CardDefinition Definition { get; }

    /// <summary>
    ///     Gets the global id of the card.
    /// </summary>
    public string GlobalId { get; }

    /// <summary>
    ///     Gets or sets the title; an overlay replaces it.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    ///     Gets or sets the summary; an overlay replaces it.
    /// </summary>
    public string Summary { get; set; }

    /// <summary>
    ///     Gets the body pieces in order: the card's own body, then those of its overlays.
    /// </summary>
    public List<BodySegment> Bodies { get; } = [];

    /// <summary>
    ///     Gets the see-also entries in order: the card's own, then those of its overlays.
    /// </summary>
    public List<SeeAlsoEntry> SeeAlso { get; } = [];

    /// <summary>
    ///     Gets the examples in order: the card's own, then those of its overlays.
    /// </summary>
    public List<ExampleEntry> Examples { get; } = [];

    /// <summary>
    ///     Gets the <c>x-</c> keys; an overlay replaces the value of a key it repeats.
    /// </summary>
    public Dictionary<string, string> Extensions { get; } = new(StringComparer.Ordinal);

    /// <summary>
    ///     Gets or sets the node created from this build, once the nodes exist.
    /// </summary>
    public CardNode? Node { get; set; }

    /// <summary>
    ///     One body of a card or of an overlay, with the file and source it was read from.
    /// </summary>
    /// <param name="Parts">The body as text and link parts.</param>
    /// <param name="File">The file the body came from, for problems.</param>
    /// <param name="Entry">The source whose scope resolves the links.</param>
    internal sealed record BodySegment(IReadOnlyList<BodyPart> Parts, string File, SourceEntry Entry);

    /// <summary>
    ///     One see-also reference with the source whose scope resolves it.
    /// </summary>
    /// <param name="Reference">The reference.</param>
    /// <param name="Entry">The source whose scope resolves it.</param>
    internal sealed record SeeAlsoEntry(ReferenceDefinition Reference, SourceEntry Entry);

    /// <summary>
    ///     One example with the source it was written in.
    /// </summary>
    /// <param name="Definition">The example.</param>
    /// <param name="Entry">The source that wrote it.</param>
    internal sealed record ExampleEntry(ExampleDefinition Definition, SourceEntry Entry);
}
