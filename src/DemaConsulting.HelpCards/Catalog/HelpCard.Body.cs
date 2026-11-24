using System.Text;

namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     The body of a card: applying the hiding rules of a view and writing the plain style.
/// </summary>
public sealed partial class HelpCard
{
    /// <summary>
    ///     The text written for a link to an example that the view hides.
    /// </summary>
    private const string HiddenExampleText = "an example";

    /// <summary>
    ///     Applies the hiding rules of the view to the resolved body.
    /// </summary>
    /// <remarks>
    ///     A link to a hidden card becomes the title of that card, a link to a hidden example becomes
    ///     <c>an example</c>, and an external link to a hidden target becomes its display text. Adjacent text
    ///     pieces are merged so the result does not depend on how the body was split.
    /// </remarks>
    /// <returns>The pieces in body order.</returns>
    private IReadOnlyList<BodyPiece> CreatePieces()
    {
        var pieces = new List<BodyPiece>();
        var pending = new StringBuilder();
        foreach (var part in _node.Body)
        {
            if (part.Link is null)
            {
                pending.Append(part.Text);
                continue;
            }

            var link = CreateVisibleLink(part.Link);
            if (link is null)
            {
                pending.Append(CreateHiddenText(part.Link));
                continue;
            }

            if (pending.Length > 0)
            {
                pieces.Add(new BodyPiece(pending.ToString(), null));
                pending.Clear();
            }

            pieces.Add(new BodyPiece(null, link));
        }

        if (pending.Length > 0)
        {
            pieces.Add(new BodyPiece(pending.ToString(), null));
        }

        return CardNode.Freeze(pieces);
    }

    /// <summary>
    ///     Creates the public link for a resolved link when its target is visible.
    /// </summary>
    /// <param name="link">The resolved link.</param>
    /// <returns>The link, or <see langword="null"/> when the target is hidden.</returns>
    private HelpLink? CreateVisibleLink(ResolvedLink link)
    {
        var displayedId = link.ExampleName is null ? link.CardId : link.ExampleCardId;
        if (displayedId is null || !_scope.IsVisible(displayedId))
        {
            return null;
        }

        var title = _scope.Data.Find(displayedId)!.Title;
        return new HelpLink(link.CardId, link.ExampleName, link.ExampleCardId, title, link.Text, link.SourceName);
    }

    /// <summary>
    ///     Writes the text that replaces a link to a hidden target.
    /// </summary>
    /// <param name="link">The resolved link.</param>
    /// <returns>The display text of an external link, the text <c>an example</c>, or the title of the card.</returns>
    private string CreateHiddenText(ResolvedLink link)
    {
        if (link.Text is not null)
        {
            return link.Text;
        }

        return link.ExampleName is not null ? HiddenExampleText : _scope.Data.Find(link.CardId!)!.Title;
    }

    /// <summary>
    ///     Writes the body with every link in the plain style.
    /// </summary>
    /// <remarks>
    ///     This is the one place in the Catalog subsystem that writes link text. The plain style is defined by the
    ///     presentation rules: a card link reads <c>the card `a/b`</c>, an example link reads
    ///     <c>the example `x` of the card `a/b`</c>, and a local example link of the containing card reads
    ///     <c>the example `x`</c>. The Presentation subsystem formats the same pieces in the other styles.
    /// </remarks>
    /// <returns>The Markdown body.</returns>
    private string CreateBody()
    {
        var builder = new StringBuilder();
        foreach (var piece in BodyPieces)
        {
            builder.Append(piece.Text ?? WritePlainLink(piece.Link!));
        }

        return builder.ToString();
    }

    /// <summary>
    ///     Writes one link in the plain style.
    /// </summary>
    /// <param name="link">The link to a visible target.</param>
    /// <returns>The text that replaces the link.</returns>
    private string WritePlainLink(HelpLink link)
    {
        if (link.ExampleName is null)
        {
            return $"the card `{link.CardId}`";
        }

        var ownsLocally = link.CardId is null && string.Equals(link.ExampleCardId, Id, StringComparison.Ordinal);
        return ownsLocally
            ? $"the example `{link.ExampleName}`"
            : $"the example `{link.ExampleName}` of the card `{link.ExampleCardId}`";
    }
}
