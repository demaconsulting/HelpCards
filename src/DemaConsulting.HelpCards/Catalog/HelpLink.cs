namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     A link in a card body whose target is visible, with everything a renderer needs to write it.
/// </summary>
/// <remarks>
///     <para>
///         A link names a card, an example, or both. Ids are global ids. For an example link
///         <see cref="ExampleCardId"/> is the card that owns the example, which is the card the link names or a
///         card below it, and <see cref="Title"/> is the title of that owner. A link written <c>[[#x]]</c> names no
///         card, so <see cref="CardId"/> is <see langword="null"/>. Only an external link, written
///         <c>[[source:id|text]]</c>, has <see cref="Text"/> and <see cref="SourceName"/>.
///     </para>
///     <para>
///         Links to hidden targets and to sources that are not present are never given to callers as links;
///         they appear as plain text. The type is immutable and therefore safe for concurrent use. Instances are
///         created by the catalog and cannot be created by callers.
///     </para>
/// </remarks>
public sealed class HelpLink
{
    /// <summary>
    ///     Creates a link.
    /// </summary>
    /// <param name="cardId">The global id of the card the link names, or <see langword="null"/>.</param>
    /// <param name="exampleName">The example name, or <see langword="null"/> for a card link.</param>
    /// <param name="exampleCardId">The global id of the card that owns the example, or <see langword="null"/>.</param>
    /// <param name="title">The title of the card that the link displays.</param>
    /// <param name="text">The display text of an external link, or <see langword="null"/>.</param>
    /// <param name="sourceName">The source name of an external link, or <see langword="null"/>.</param>
    internal HelpLink(
        string? cardId,
        string? exampleName,
        string? exampleCardId,
        string? title,
        string? text,
        string? sourceName)
    {
        CardId = cardId;
        ExampleName = exampleName;
        ExampleCardId = exampleCardId;
        Title = title;
        Text = text;
        SourceName = sourceName;
    }

    /// <summary>
    ///     Gets the global id of the card the link names, or <see langword="null"/> for <c>[[#example]]</c>.
    /// </summary>
    public string? CardId { get; }

    /// <summary>
    ///     Gets the name of the example the link names, or <see langword="null"/> for a card link.
    /// </summary>
    public string? ExampleName { get; }

    /// <summary>
    ///     Gets the global id of the card that owns the example, or <see langword="null"/> for a card link.
    /// </summary>
    public string? ExampleCardId { get; }

    /// <summary>
    ///     Gets the title of the card the link displays: the target card, or the owner of the example.
    /// </summary>
    public string? Title { get; }

    /// <summary>
    ///     Gets the display text of an external link, or <see langword="null"/> for a local link.
    /// </summary>
    public string? Text { get; }

    /// <summary>
    ///     Gets the declared source name of an external link, or <see langword="null"/> for a local link.
    /// </summary>
    public string? SourceName { get; }
}
