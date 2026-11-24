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
    ///     The command that prints the target, such as <c>`tool help a b`</c>; a link to an example is the full
    ///     command <c>`tool help a b --example load`</c>, also when the example belongs to the card that holds the
    ///     link, so the text can be run as written. This is the style that <c>HelpCli</c> always uses for its text and
    ///     Markdown output.
    /// </summary>
    Command,

    /// <summary>
    ///     A Markdown link, such as <c>[Title](a/b)</c>; a link to an example is <c>[load of Title](a/b#load)</c> and
    ///     always names the card that owns the example in its target.
    /// </summary>
    Markdown
}
