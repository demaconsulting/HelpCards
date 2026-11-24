namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     One help card as a catalog view shows it.
/// </summary>
/// <remarks>
///     <para>
///         A card is immutable and safe for concurrent use. It belongs to the catalog (or view) that created
///         it: <see cref="ParentId"/>, <see cref="ChildIds"/>, <see cref="SeeAlsoIds"/>, and <see cref="Body"/>
///         reflect what that view shows. In a filtered view a hidden parent does not hide its children, so a
///         card's parent and children are the nearest visible ancestor and the nearest visible descendants.
///         See-also entries never point at hidden cards.
///     </para>
///     <para>
///         <see cref="Body"/> is the Markdown body with every link written in the plain style with global ids.
///         It is computed on first access, once, thread-safely. No rendered output of any other format or style
///         is cached; the other renderings are made from the parsed parts every time. Instances are created by
///         the catalog and cannot be created by callers.
///     </para>
/// </remarks>
public sealed partial class HelpCard
{
    /// <summary>
    ///     The frozen data of the card.
    /// </summary>
    private readonly CardNode _node;

    /// <summary>
    ///     The view the card belongs to.
    /// </summary>
    private readonly CatalogScope _scope;

    /// <summary>
    ///     The body pieces of the card in this view, computed on first use.
    /// </summary>
    private readonly Lazy<IReadOnlyList<BodyPiece>> _pieces;

    /// <summary>
    ///     The plain-style body, computed on first use.
    /// </summary>
    private readonly Lazy<string> _body;

    /// <summary>
    ///     Creates a card for a view.
    /// </summary>
    /// <param name="node">The frozen data of the card.</param>
    /// <param name="scope">The view the card belongs to.</param>
    internal HelpCard(CardNode node, CatalogScope scope)
    {
        _node = node;
        _scope = scope;
        ParentId = scope.GetParentId(node);
        ChildIds = CardNode.Freeze(scope.GetChildren(node).Select(child => child.Id));
        SeeAlsoIds = CardNode.Freeze(node.SeeAlsoIds.Where(scope.IsVisible));
        _pieces = new Lazy<IReadOnlyList<BodyPiece>>(CreatePieces, LazyThreadSafetyMode.ExecutionAndPublication);
        _body = new Lazy<string>(CreateBody, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    ///     Gets the global id of the card, such as <c>mermaid/flow</c>; this is the id in every public result.
    /// </summary>
    public string Id => _node.Id;

    /// <summary>
    ///     Gets the local id: the path of the card file inside its source, without extension.
    /// </summary>
    public string LocalId => _node.LocalId;

    /// <summary>
    ///     Gets the name of the card: the last segment of its id.
    /// </summary>
    public string Name => _node.Name;

    /// <summary>
    ///     Gets the global id of the nearest visible ancestor, or <see langword="null"/> for a root.
    /// </summary>
    public string? ParentId { get; }

    /// <summary>
    ///     Gets the name of the source the card comes from.
    /// </summary>
    public string SourceName => _node.SourceName;

    /// <summary>
    ///     Gets the title, in the culture of the catalog where translated.
    /// </summary>
    public string Title => _node.Title;

    /// <summary>
    ///     Gets the one-line summary, in the culture of the catalog where translated.
    /// </summary>
    public string Summary => _node.Summary;

    /// <summary>
    ///     Gets the tags of the card.
    /// </summary>
    public IReadOnlyList<string> Tags => _node.Tags;

    /// <summary>
    ///     Gets the aliases of the card, including those of the selected translation.
    /// </summary>
    public IReadOnlyList<string> Aliases => _node.Aliases;

    /// <summary>
    ///     Gets the culture name of the file that supplied the body; the source language for a neutral file.
    /// </summary>
    public string Culture => _node.Culture;

    /// <summary>
    ///     Gets the global ids of the visible children in sibling order.
    /// </summary>
    public IReadOnlyList<string> ChildIds { get; }

    /// <summary>
    ///     Gets the global ids of the visible see-also cards.
    /// </summary>
    public IReadOnlyList<string> SeeAlsoIds { get; }

    /// <summary>
    ///     Gets the examples of the card.
    /// </summary>
    public IReadOnlyList<HelpExample> Examples => _node.Examples;

    /// <summary>
    ///     Gets the <c>x-</c> keys of the card and their values.
    /// </summary>
    public IReadOnlyDictionary<string, string> Extensions => _node.Extensions;

    /// <summary>
    ///     Gets the body as Markdown with every link written in the plain style using global ids.
    /// </summary>
    public string Body => _body.Value;

    /// <summary>
    ///     Gets the body as text and links to visible targets, for renderers.
    /// </summary>
    internal IReadOnlyList<BodyPiece> BodyPieces => _pieces.Value;

    /// <summary>
    ///     Gets the path of the neutral card file inside its source, for validators that name the file of a finding.
    /// </summary>
    internal string File => _node.File;

    /// <summary>
    ///     Gets the path of the file that supplied the body, which is a translation file when one was selected.
    /// </summary>
    internal string BodyFile => _node.BodyFile;

    /// <summary>
    ///     Gets the one-based line of the first body line in <see cref="BodyFile"/>.
    /// </summary>
    internal int BodyLine => _node.BodyLine;
}
