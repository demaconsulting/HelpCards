using System.Text;

namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     Resolves a path such as <c>requirements tags</c> to a card by walking names and aliases.
/// </summary>
/// <remarks>
///     <para>
///         A path is split on <c>/</c> and white space. The first segment must match the name or an alias of a
///         root, and each later segment the name or an alias of a child of the card found so far. Matching ignores
///         case and is strict: the unqualified local id of a card is not a path unless it is also the global id.
///     </para>
///     <para>
///         The walk goes through hidden cards, because a hidden parent does not hide its children, but a hidden
///         target is reported like an unknown one so a view never reveals a card it hides. The class is static and
///         stateless and therefore safe for concurrent use.
///     </para>
/// </remarks>
internal static class PathResolver
{
    /// <summary>
    ///     Finds the visible card a path names.
    /// </summary>
    /// <param name="scope">The scope that decides what is visible.</param>
    /// <param name="path">The path. Must not be null.</param>
    /// <returns>The card, or <see langword="null"/> when the path names nothing or a hidden card.</returns>
    public static CardNode? Find(CatalogScope scope, string path)
    {
        return Resolve(scope, path).Card;
    }

    /// <summary>
    ///     Resolves a path and, when it fails, says where and what would have been valid.
    /// </summary>
    /// <param name="scope">The scope that decides what is visible.</param>
    /// <param name="path">The path. Must not be null.</param>
    /// <returns>The outcome.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static PathMatch Resolve(CatalogScope scope, string path)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(path);

        var data = scope.Data;
        var segments = path.Split(['/', ' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var level = data.RootIds.Select(id => data.Find(id)!).ToList();
        CardNode? parent = null;
        CardNode? found = null;
        foreach (var segment in segments)
        {
            found = Match(level, segment);
            if (found is null)
            {
                return Failure(scope, parent, segment, level);
            }

            parent = found;
            level = found.ChildIds.Select(id => data.Find(id)!).ToList();
        }

        // An empty path names nothing; a hidden target is reported like an unknown segment
        if (found is null)
        {
            return Failure(scope, null, null, data.RootIds.Select(id => data.Find(id)!).ToList());
        }

        if (!scope.IsVisible(found.Id))
        {
            var siblings = found.ParentId is null
                ? data.RootIds.Select(id => data.Find(id)!).ToList()
                : data.Find(found.ParentId)!.ChildIds.Select(id => data.Find(id)!).ToList();
            return Failure(scope, found.ParentId is null ? null : data.Find(found.ParentId), segments[^1], siblings);
        }

        return new PathMatch(found, null, null, []);
    }

    /// <summary>
    ///     Builds a failed outcome.
    /// </summary>
    /// <param name="scope">The scope that decides what is visible.</param>
    /// <param name="parent">The card whose children were searched, or <see langword="null"/> at the top level.</param>
    /// <param name="segment">The segment that matched nothing, or <see langword="null"/>.</param>
    /// <param name="level">The cards that were searched.</param>
    /// <returns>The outcome with the visible names of the searched level.</returns>
    private static PathMatch Failure(CatalogScope scope, CardNode? parent, string? segment, IReadOnlyList<CardNode> level)
    {
        // A hidden card is never pointed at: report the nearest visible ancestor of the failing parent
        var visibleParentId = parent;
        while (visibleParentId is not null && !scope.IsVisible(visibleParentId.Id))
        {
            visibleParentId = visibleParentId.ParentId is null ? null : scope.Data.Find(visibleParentId.ParentId);
        }

        var visible = level.Where(node => scope.IsVisible(node.Id)).ToList();
        var names = visible.Select(node => node.Name).ToList();

        // The closest name or alias at the level that failed, if any is close enough to be a likely slip
        var suggestion = segment is null
            ? null
            : SearchIndex.Suggest(segment, visible.SelectMany(node => node.Aliases.Prepend(node.Name)), 1).FirstOrDefault();
        return new PathMatch(null, visibleParentId?.Id, segment, names, suggestion);
    }

    /// <summary>
    ///     Finds the card at one level whose name or alias equals a segment.
    /// </summary>
    /// <param name="level">The cards to search.</param>
    /// <param name="segment">The segment.</param>
    /// <returns>The card, or <see langword="null"/>.</returns>
    private static CardNode? Match(IReadOnlyList<CardNode> level, string segment)
    {
        var normalized = Normalize(segment);
        return level.FirstOrDefault(node =>
            string.Equals(node.Name, normalized, StringComparison.OrdinalIgnoreCase)
            || node.Aliases.Any(alias => string.Equals(alias, normalized, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    ///     Normalizes text to NFC, the form in which aliases are stored.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The normalized text, or the text itself when it is not valid Unicode.</returns>
    private static string Normalize(string text)
    {
        try
        {
            return text.Normalize(NormalizationForm.FormC);
        }
        catch (ArgumentException)
        {
            return text;
        }
    }
}
