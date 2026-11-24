namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     The filtered views of a catalog.
/// </summary>
public sealed partial class HelpCatalog
{
    /// <summary>
    ///     Creates a view that shows cards by tag.
    /// </summary>
    /// <remarks>
    ///     A card stays visible when it has at least one of the included tags (or no tag is included) and none
    ///     of the excluded tags. Tags are compared ignoring case. A hidden parent does not hide its children;
    ///     they move up to the nearest visible ancestor. A view cannot show more than the catalog it is made from.
    /// </remarks>
    /// <param name="includeTags">The tags of which a card needs one; <see langword="null"/> or empty for no restriction.</param>
    /// <param name="excludeTags">The tags that hide a card; <see langword="null"/> or empty for none.</param>
    /// <returns>The view; this catalog is unchanged.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when a tag is empty or white space, or is not in the closed tag vocabulary of the catalog.
    /// </exception>
    public HelpCatalog Filter(IEnumerable<string>? includeTags, IEnumerable<string>? excludeTags = null)
    {
        var include = ToTagSet(includeTags, nameof(includeTags));
        var exclude = ToTagSet(excludeTags, nameof(excludeTags));
        var scope = Scope.Restrict(node =>
            (include.Count == 0 || node.Tags.Any(include.Contains)) && !node.Tags.Any(exclude.Contains));
        return new HelpCatalog(scope);
    }

    /// <summary>
    ///     Creates a view that shows cards by the source they come from.
    /// </summary>
    /// <remarks>
    ///     A card stays visible when its source is included (or no source is included) and not excluded. Source
    ///     names are compared ignoring case.
    /// </remarks>
    /// <param name="includeSources">The sources to show; <see langword="null"/> or empty for all.</param>
    /// <param name="excludeSources">The sources to hide; <see langword="null"/> or empty for none.</param>
    /// <returns>The view; this catalog is unchanged.</returns>
    /// <exception cref="ArgumentException">Thrown when a name is empty or is not the name of a source of the catalog.</exception>
    public HelpCatalog FilterSource(IEnumerable<string>? includeSources, IEnumerable<string>? excludeSources = null)
    {
        var include = ToSourceSet(includeSources, nameof(includeSources));
        var exclude = ToSourceSet(excludeSources, nameof(excludeSources));
        var scope = Scope.Restrict(node =>
            (include.Count == 0 || include.Contains(node.SourceName)) && !exclude.Contains(node.SourceName));
        return new HelpCatalog(scope);
    }

    /// <summary>
    ///     Creates a view that shows the cards for which a predicate is true.
    /// </summary>
    /// <remarks>
    ///     The predicate is called once for each card of this catalog, immediately, in <see cref="Cards"/> order,
    ///     on the calling thread. Only its results are kept; the predicate itself is not retained, so a view never
    ///     calls back later.
    /// </remarks>
    /// <param name="predicate">Decides whether a card stays visible. Must not be null.</param>
    /// <returns>The view; this catalog is unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
    public HelpCatalog Where(Func<HelpCard, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        var keep = Cards.Where(predicate).Select(card => card.Id).ToHashSet(StringComparer.Ordinal);
        return new HelpCatalog(Scope.Restrict(node => keep.Contains(node.Id)));
    }

    /// <summary>
    ///     Creates a view that hides the cards with the given ids.
    /// </summary>
    /// <remarks>
    ///     Only the listed cards are hidden; their children stay visible and move up. Ids are compared ignoring
    ///     case.
    /// </remarks>
    /// <param name="ids">The global ids of the cards to hide. Must not be null.</param>
    /// <returns>The view; this catalog is unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ids"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an id is empty or is not the id of a card of the catalog.</exception>
    public HelpCatalog Without(IEnumerable<string> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var hidden = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in ids)
        {
            RequireText(id, nameof(ids));
            var node = Scope.Data.Find(id) ?? throw new ArgumentException($"The catalog has no card with the id '{id}'.", nameof(ids));
            hidden.Add(node.Id);
        }

        return new HelpCatalog(Scope.Restrict(node => !hidden.Contains(node.Id)));
    }

    /// <summary>
    ///     Copies tags into a set after checking them against the closed vocabulary, if there is one.
    /// </summary>
    /// <param name="tags">The tags, or <see langword="null"/>.</param>
    /// <param name="parameterName">The name of the parameter, for the exception.</param>
    /// <returns>The set of tags, ignoring case.</returns>
    private HashSet<string> ToTagSet(IEnumerable<string>? tags, string parameterName)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in tags ?? [])
        {
            RequireText(tag, parameterName);
            var allowed = Scope.Data.AllowedTags;
            if (allowed is not null && !allowed.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"The tag '{tag}' is not in the tag vocabulary of the catalog.", parameterName);
            }

            set.Add(tag);
        }

        return set;
    }

    /// <summary>
    ///     Copies source names into a set after checking that each is a source of the catalog.
    /// </summary>
    /// <param name="names">The names, or <see langword="null"/>.</param>
    /// <param name="parameterName">The name of the parameter, for the exception.</param>
    /// <returns>The set of source names, ignoring case, spelled as the catalog spells them.</returns>
    private HashSet<string> ToSourceSet(IEnumerable<string>? names, string parameterName)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in names ?? [])
        {
            RequireText(name, parameterName);
            var source = Scope.Data.Sources.FirstOrDefault(info => string.Equals(info.Name, name, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"The catalog has no source named '{name}'.", parameterName);
            set.Add(source.Name);
        }

        return set;
    }

    /// <summary>
    ///     Checks that a value is not null, empty, or white space.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="parameterName">The parameter the value came from, for the exception.</param>
    /// <exception cref="ArgumentException">Thrown when the value is null, empty, or white space.</exception>
    private static void RequireText(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A value must not be null, empty, or white space.", parameterName);
        }
    }
}
