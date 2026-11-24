namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     What a help command asks for.
/// </summary>
internal enum CliMode
{
    /// <summary>
    ///     The list of topics: the roots, or the whole tree with <c>--all</c>.
    /// </summary>
    Index,

    /// <summary>
    ///     One card.
    /// </summary>
    Card,

    /// <summary>
    ///     The examples of a card in full, and a list of the cards below it that have examples.
    /// </summary>
    Examples,

    /// <summary>
    ///     One example, printed as exactly its content.
    /// </summary>
    Example,

    /// <summary>
    ///     A search over the cards.
    /// </summary>
    Search,

    /// <summary>
    ///     The cards that have a tag.
    /// </summary>
    Tags,
}
