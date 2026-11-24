namespace DemaConsulting.HelpCards.Presentation;

/// <summary>
///     How a link to a card or an example is written in a body and in the lists of cards.
/// </summary>
public enum HelpLinkStyle
{
    /// <summary>
    ///     Words that name the target, such as <c>the card `a/b`</c>. This is the default.
    /// </summary>
    Plain,

    /// <summary>
    ///     The command that prints the target, such as <c>`tool help a b`</c>.
    /// </summary>
    Command,

    /// <summary>
    ///     A Markdown link, such as <c>[Title](a/b)</c>.
    /// </summary>
    Markdown
}
