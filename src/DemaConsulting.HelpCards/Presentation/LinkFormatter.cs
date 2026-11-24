using System.Text;
using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Presentation;

/// <summary>
///     Writes links, card labels, and commands in the plain, command, and Markdown styles, or through the callback
///     of the caller.
/// </summary>
/// <remarks>
///     <para>
///         This is the one place where the three link styles are defined. A body is formatted from its parsed pieces
///         every time, so nothing is cached and every style sees the same targets. Only links to visible targets
///         arrive here; a link to a hidden target is already text. The callback of the caller, when set, replaces
///         the style for every link and every card label.
///     </para>
///     <para>
///         The formatter is stateless. A link to an example of the card that contains the link (<c>[[#x]]</c>) is
///         written in its short form; every other example link names its card.
///     </para>
/// </remarks>
internal static class LinkFormatter
{
    /// <summary>
    ///     Writes the body of a card with every link formatted.
    /// </summary>
    /// <param name="context">The settings of the call. Must not be null.</param>
    /// <param name="card">The card, as the catalog view of the call shows it. Must not be null.</param>
    /// <returns>The Markdown body with the links written; text pieces are copied unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static string FormatBody(RenderContext context, HelpCard card)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(card);

        // Walk the pieces in body order: text is copied, each visible link is replaced by its formatted form
        var builder = new StringBuilder();
        foreach (var piece in card.BodyPieces)
        {
            builder.Append(piece.Text ?? FormatLink(context, piece.Link!, card.Id));
        }

        return builder.ToString();
    }

    /// <summary>
    ///     Writes one link of a body.
    /// </summary>
    /// <param name="context">The settings of the call. Must not be null.</param>
    /// <param name="link">The link to a visible target. Must not be null.</param>
    /// <param name="containingCardId">The global id of the card whose body holds the link.</param>
    /// <returns>The text that replaces the link.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static string FormatLink(RenderContext context, HelpLink link, string containingCardId)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(link);

        // The caller's callback wins over every style
        var formatter = context.Options.LinkFormatter;
        if (formatter is not null)
        {
            return formatter(link) ?? string.Empty;
        }

        var isLocal = link.CardId is null && string.Equals(link.ExampleCardId, containingCardId, StringComparison.Ordinal);
        return context.Options.LinkStyle switch
        {
            HelpLinkStyle.Command => FormatCommandLink(context, link, isLocal),
            HelpLinkStyle.Markdown => FormatMarkdownLink(link, isLocal),
            _ => FormatPlainLink(link, isLocal)
        };
    }

    /// <summary>
    ///     Writes the label of a card in a list: the id, the command, a Markdown link, or what the callback writes.
    /// </summary>
    /// <param name="context">The settings of the call. Must not be null.</param>
    /// <param name="id">The global id of the card.</param>
    /// <param name="title">The title of the card.</param>
    /// <param name="sourceName">The name of the source of the card.</param>
    /// <returns>The label.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static string FormatLabel(RenderContext context, string id, string title, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(context);

        // The callback receives the same kind of link as a body link to the card
        var formatter = context.Options.LinkFormatter;
        if (formatter is not null)
        {
            return formatter(new HelpLink(id, null, null, title, null, sourceName)) ?? string.Empty;
        }

        return context.Options.LinkStyle switch
        {
            HelpLinkStyle.Command => Command(context, id),
            HelpLinkStyle.Markdown => $"[{title}]({id})",
            _ => id
        };
    }

    /// <summary>
    ///     Writes the command that prints a card by filling in the command template.
    /// </summary>
    /// <param name="context">The settings of the call. Must not be null.</param>
    /// <param name="id">The global id of the card.</param>
    /// <returns>The command, such as <c>tool help a b</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static string Command(RenderContext context, string id)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(id);

        // The path is the id with slashes replaced by spaces, so the command is what a person would type
        var template = context.Options.CommandTemplate ?? HelpRenderOptions.DefaultCommandTemplate;
        return template
            .Replace("{tool}", context.ToolName, StringComparison.Ordinal)
            .Replace("{path}", id.Replace('/', ' '), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Writes a link in the plain style.
    /// </summary>
    /// <param name="link">The link.</param>
    /// <param name="isLocal">Whether the link is the short form for an example of the containing card.</param>
    /// <returns>The words that name the target.</returns>
    private static string FormatPlainLink(HelpLink link, bool isLocal)
    {
        if (link.ExampleName is null)
        {
            return $"the card `{link.CardId}`";
        }

        return isLocal
            ? $"the example `{link.ExampleName}`"
            : $"the example `{link.ExampleName}` of the card `{link.ExampleCardId}`";
    }

    /// <summary>
    ///     Writes a link in the command style.
    /// </summary>
    /// <param name="context">The settings of the call.</param>
    /// <param name="link">The link.</param>
    /// <param name="isLocal">Whether the link is the short form for an example of the containing card.</param>
    /// <returns>The command in a code span.</returns>
    private static string FormatCommandLink(RenderContext context, HelpLink link, bool isLocal)
    {
        if (link.ExampleName is null)
        {
            return $"`{Command(context, link.CardId!)}`";
        }

        return isLocal
            ? $"`--example {link.ExampleName}`"
            : $"`{Command(context, link.ExampleCardId!)} --example {link.ExampleName}`";
    }

    /// <summary>
    ///     Writes a link in the Markdown style.
    /// </summary>
    /// <param name="link">The link.</param>
    /// <param name="isLocal">Whether the link is the short form for an example of the containing card.</param>
    /// <returns>The Markdown link.</returns>
    private static string FormatMarkdownLink(HelpLink link, bool isLocal)
    {
        // An external link shows the text its author wrote; an internal one shows the title of the card
        if (link.ExampleName is null)
        {
            return $"[{link.Text ?? link.Title ?? link.CardId}]({link.CardId})";
        }

        if (isLocal)
        {
            return $"[{link.ExampleName}](#{link.ExampleName})";
        }

        var shown = link.Text ?? $"{link.ExampleName} of {link.Title ?? link.ExampleCardId}";
        return $"[{shown}]({link.ExampleCardId}#{link.ExampleName})";
    }
}
