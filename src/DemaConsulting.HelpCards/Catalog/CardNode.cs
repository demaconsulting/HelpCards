using System.Collections.ObjectModel;

namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     The frozen data of one card, shared by every view of a catalog.
/// </summary>
/// <remarks>
///     A node holds what composition decided about a card: its global id, its place in the tree, its text,
///     its examples, its resolved body, and its resolved see-also ids. Views never change a node; they decide
///     which nodes are visible. The lists are read-only collections so no caller can change them through a cast.
///     Composition first creates nodes without a body and see-also ids, because resolving references needs the
///     tree; the final nodes are made from these with <c>with</c> and are never changed afterwards.
/// </remarks>
/// <param name="Id">The global id.</param>
/// <param name="LocalId">The local id, the path of the card file without extension.</param>
/// <param name="Name">The last segment of the id.</param>
/// <param name="ParentId">The global id of the parent card, or <see langword="null"/> for a root.</param>
/// <param name="SourceName">The name of the source the card belongs to.</param>
/// <param name="Title">The title.</param>
/// <param name="Summary">The one-line summary.</param>
/// <param name="Tags">The tags.</param>
/// <param name="Aliases">The aliases.</param>
/// <param name="Culture">The culture of the file that supplied the body.</param>
/// <param name="ChildIds">The global ids of the children in sibling order.</param>
/// <param name="SeeAlsoIds">The global ids of the see-also cards, resolved, without duplicates.</param>
/// <param name="Examples">The examples.</param>
/// <param name="Extensions">The <c>x-</c> keys and their values.</param>
/// <param name="Body">The body as text and resolved links.</param>
internal sealed record CardNode(
    string Id,
    string LocalId,
    string Name,
    string? ParentId,
    string SourceName,
    string Title,
    string Summary,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Aliases,
    string Culture,
    IReadOnlyList<string> ChildIds,
    IReadOnlyList<string> SeeAlsoIds,
    IReadOnlyList<HelpExample> Examples,
    IReadOnlyDictionary<string, string> Extensions,
    IReadOnlyList<ResolvedPart> Body)
{
    /// <summary>
    ///     Copies a sequence into a read-only collection that cannot be changed through a cast.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="items">The items to copy.</param>
    /// <returns>The read-only collection.</returns>
    public static IReadOnlyList<T> Freeze<T>(IEnumerable<T> items)
    {
        return new ReadOnlyCollection<T>(items.ToList());
    }

    /// <summary>
    ///     Copies a dictionary into a read-only dictionary that cannot be changed through a cast.
    /// </summary>
    /// <param name="items">The pairs to copy.</param>
    /// <returns>The read-only dictionary.</returns>
    public static IReadOnlyDictionary<string, string> Freeze(IEnumerable<KeyValuePair<string, string>> items)
    {
        return new ReadOnlyDictionary<string, string>(items.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
    }
}
