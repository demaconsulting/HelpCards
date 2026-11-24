namespace DemaConsulting.HelpCards.Presentation;

/// <summary>
///     How a link to a card or an example is written in a body and in the lists of cards.
/// </summary>
public enum HelpLinkStyle
{
    /// <summary>
    ///     Words that name the target, such as <c>the card `a/b`</c> or <c>the example `load`</c>. This is the default of
    ///     <see cref="HelpRenderOptions"/>, and what <c>HelpAgentTool</c> writes.
    /// </summary>
    Plain,

    /// <summary>
    ///     The command that prints the target, such as <c>`tool help a b`</c>; a link to an example of the same card is
    ///     <c>`--example load`</c>. This is the style that <c>HelpCli</c> always uses for its text and Markdown output.
    /// </summary>
    Command,

    /// <summary>
    ///     A Markdown link, such as <c>[Title](a/b)</c>.
    /// </summary>
    Markdown
}
