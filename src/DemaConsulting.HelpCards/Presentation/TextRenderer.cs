using System.Text;
using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Presentation;

/// <summary>
///     Renders cards, indexes, example documents, search results, and card lists as text or Markdown.
/// </summary>
/// <remarks>
///     <para>
///         The Markdown format has exactly the layout of the text format except that nothing is wrapped and that
///         the labels <c>Also known as</c> and <c>Tags</c> are written in bold. Output has LF line endings, no
///         trailing line feed, no ANSI sequences, and does not depend on the culture of the machine.
///     </para>
///     <para>
///         The renderer is stateless and reads only the immutable catalog view named in the context, so concurrent
///         calls are safe. Every call formats the parsed body pieces again; no rendered output is cached. This file
///         holds the card document; the lists and the example documents are in the other parts of the class.
///     </para>
/// </remarks>
internal static partial class TextRenderer
{
    /// <summary>
    ///     The indent of the summary under a list entry and of the hanging lines of a list.
    /// </summary>
    private const string EntryIndent = "  ";

    /// <summary>
    ///     The marker that starts a list entry.
    /// </summary>
    private const string BulletPrefix = "- ";

    /// <summary>
    ///     Renders one card.
    /// </summary>
    /// <param name="context">The settings of the call. Must not be null.</param>
    /// <param name="card">The card, as the catalog view of the call shows it. Must not be null.</param>
    /// <returns>The document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static string RenderCard(RenderContext context, HelpCard card)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(card);

        // The header: title, summary, then the optional aliases and tags
        var builder = new StringBuilder();
        builder.Append("# ").Append(card.Title).Append("\n\n");
        TextWrapper.Append(builder, card.Summary, context.WrapWidth, string.Empty, string.Empty);
        AppendLabeledLine(context, builder, HelpStrings.AliasesLabel, card.Aliases);
        if (context.Options.IncludeTags)
        {
            AppendLabeledLine(context, builder, HelpStrings.TagsLabel, card.Tags);
        }

        // The authored body is copied as it is, with its links written in the chosen style
        var body = LinkFormatter.FormatBody(context, card).Trim('\n', '\r');
        if (body.Length > 0)
        {
            builder.Append('\n').Append(body).Append('\n');
        }

        // The generated sections follow in a fixed order and are left out when empty
        AppendCardList(context, builder, HelpStrings.CardsHeading, context.Catalog.GetChildren(card));
        AppendExamplesSection(context, builder, card);
        AppendCardList(context, builder, HelpStrings.SeeAlsoHeading, context.Catalog.GetSeeAlso(card));
        return Finish(builder);
    }

    /// <summary>
    ///     Formats a document that was built with a line feed after every line.
    /// </summary>
    /// <param name="builder">The builder that holds the document.</param>
    /// <returns>The document without a trailing line feed.</returns>
    private static string Finish(StringBuilder builder)
    {
        return builder.ToString().TrimEnd('\n');
    }

    /// <summary>
    ///     Appends a line such as <c>Tags: a, b</c> when there are values, wrapped with a hanging indent.
    /// </summary>
    /// <param name="context">The settings of the call.</param>
    /// <param name="builder">The builder to append to.</param>
    /// <param name="label">The label without a colon.</param>
    /// <param name="values">The values, joined by a comma and a space.</param>
    private static void AppendLabeledLine(
        RenderContext context,
        StringBuilder builder,
        string label,
        IReadOnlyList<string> values)
    {
        if (values.Count == 0)
        {
            return;
        }

        var text = $"{HelpStrings.Label(label, context.IsMarkdown)} {string.Join(", ", values)}";
        TextWrapper.Append(builder, text, context.WrapWidth, string.Empty, EntryIndent);
    }

    /// <summary>
    ///     Appends a section that lists cards, each with its label and its summary below, when there are cards.
    /// </summary>
    /// <param name="context">The settings of the call.</param>
    /// <param name="builder">The builder to append to.</param>
    /// <param name="heading">The heading text of the section.</param>
    /// <param name="cards">The cards to list.</param>
    private static void AppendCardList(
        RenderContext context,
        StringBuilder builder,
        string heading,
        IReadOnlyList<HelpCard> cards)
    {
        if (cards.Count == 0)
        {
            return;
        }

        AppendHeading(builder, heading);
        foreach (var card in cards)
        {
            AppendBullet(context, builder, card.Id, card.Title, card.SourceName, card.Summary);
        }
    }

    /// <summary>
    ///     Appends a blank line, a second-level heading, and a blank line.
    /// </summary>
    /// <param name="builder">The builder to append to.</param>
    /// <param name="heading">The heading text.</param>
    private static void AppendHeading(StringBuilder builder, string heading)
    {
        builder.Append('\n').Append(HelpStrings.Heading(heading)).Append("\n\n");
    }

    /// <summary>
    ///     Appends a list entry: a bullet with the label of a card and, below it, the wrapped summary.
    /// </summary>
    /// <param name="context">The settings of the call.</param>
    /// <param name="builder">The builder to append to.</param>
    /// <param name="id">The global id of the card.</param>
    /// <param name="title">The title of the card.</param>
    /// <param name="sourceName">The name of the source of the card.</param>
    /// <param name="summary">The summary of the card.</param>
    private static void AppendBullet(
        RenderContext context,
        StringBuilder builder,
        string id,
        string title,
        string sourceName,
        string summary)
    {
        builder.Append(BulletPrefix).Append(LinkFormatter.FormatLabel(context, id, title, sourceName)).Append('\n');
        TextWrapper.Append(builder, summary, context.WrapWidth, EntryIndent, EntryIndent);
    }

    /// <summary>
    ///     Appends the line of one entry of an index or a list: the padded label, two spaces, and the summary wrapped
    ///     at the hanging indent.
    /// </summary>
    /// <param name="context">The settings of the call.</param>
    /// <param name="builder">The builder to append to.</param>
    /// <param name="prefix">The padded label followed by two spaces.</param>
    /// <param name="summary">The summary.</param>
    private static void AppendEntry(RenderContext context, StringBuilder builder, string prefix, string summary)
    {
        // A blank summary leaves the label alone, without trailing spaces
        if (string.IsNullOrWhiteSpace(summary))
        {
            builder.Append(prefix.TrimEnd()).Append('\n');
            return;
        }

        TextWrapper.AppendHanging(builder, summary, context.WrapWidth, prefix);
    }
}
