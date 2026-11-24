namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     The outcome of resolving a path such as <c>requirements tags</c> to a card, with what went wrong if it failed.
/// </summary>
/// <remarks>
///     A successful resolution has a <see cref="Card"/> and nothing else. A failed one names the card whose
///     children were searched (<see cref="ParentId"/>, <see langword="null"/> at the top level), the segment that
///     matched nothing, and the names that would have been valid there, so a caller can write a helpful message.
///     <see cref="Suggestion"/> is the closest name or alias at that level by the rules of
///     <see cref="HelpCatalog.Suggest"/>, or <see langword="null"/> when none is close. The type is immutable and
///     therefore safe for concurrent use.
/// </remarks>
public sealed class HelpPathResolution
{
    /// <summary>
    ///     Creates a resolution.
    /// </summary>
    /// <param name="card">The card, or <see langword="null"/> when the path did not resolve.</param>
    /// <param name="parentId">The global id of the card whose children were searched, or <see langword="null"/>.</param>
    /// <param name="unknownSegment">The segment that matched nothing, or <see langword="null"/>.</param>
    /// <param name="validNames">The names that are valid at the level that failed.</param>
    /// <param name="suggestion">The suggested name, or <see langword="null"/>.</param>
    internal HelpPathResolution(
        HelpCard? card,
        string? parentId,
        string? unknownSegment,
        IReadOnlyList<string> validNames,
        string? suggestion)
    {
        Card = card;
        ParentId = parentId;
        UnknownSegment = unknownSegment;
        ValidNames = validNames;
        Suggestion = suggestion;
    }

    /// <summary>
    ///     Gets the card the path resolved to, or <see langword="null"/> when it did not resolve.
    /// </summary>
    public HelpCard? Card { get; }

    /// <summary>
    ///     Gets the global id of the card whose children were searched when a segment failed, or
    ///     <see langword="null"/> when the first segment failed or the path resolved.
    /// </summary>
    public string? ParentId { get; }

    /// <summary>
    ///     Gets the first segment that matched no card, or <see langword="null"/> when the path resolved.
    /// </summary>
    public string? UnknownSegment { get; }

    /// <summary>
    ///     Gets the names of the visible cards at the level where the path failed; empty when it resolved.
    /// </summary>
    public IReadOnlyList<string> ValidNames { get; }

    /// <summary>
    ///     Gets the closest name or alias to <see cref="UnknownSegment"/> at the level that failed, or
    ///     <see langword="null"/> when the path resolved or no name is close.
    /// </summary>
    public string? Suggestion { get; }
}
