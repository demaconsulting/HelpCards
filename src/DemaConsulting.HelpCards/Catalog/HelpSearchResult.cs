namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     One hit of a search over a catalog.
/// </summary>
/// <remarks>
///     One hit of a search over a catalog. The record is immutable and compares by value, including the fields that
///     <see cref="MatchedOn"/> lists (two results are equal when their fields and the listed names are equal). The ranking and the
///     matched field names are defined by <see cref="HelpCatalog.Search"/>; a caller may also build results itself
///     to have them rendered.
/// </remarks>
/// <param name="Id">The global id of the card that matched.</param>
/// <param name="Title">The title of the card.</param>
/// <param name="Summary">The summary of the card.</param>
/// <param name="SourceName">The name of the source of the card.</param>
/// <param name="Score">The relevance score; higher is better.</param>
/// <param name="MatchedOn">The names of the fields that matched, such as <c>id</c> or <c>title</c>.</param>
public sealed record HelpSearchResult(
    string Id,
    string Title,
    string Summary,
    string SourceName,
    int Score,
    IReadOnlyList<string> MatchedOn)
{
    /// <summary>
    ///     Determines whether another result has the same values, comparing <see cref="MatchedOn"/> by its items.
    /// </summary>
    /// <param name="other">The other result, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when every field is equal and the matched field names are the same in order.</returns>
    public bool Equals(HelpSearchResult? other)
    {
        return other is not null
            && Id == other.Id
            && Title == other.Title
            && Summary == other.Summary
            && SourceName == other.SourceName
            && Score == other.Score
            && MatchedOn.SequenceEqual(other.MatchedOn);
    }

    /// <summary>
    ///     Gets a hash code that is consistent with <see cref="Equals(HelpSearchResult?)"/>.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id);
        hash.Add(Title);
        hash.Add(Summary);
        hash.Add(SourceName);
        hash.Add(Score);
        foreach (var name in MatchedOn)
        {
            hash.Add(name);
        }

        return hash.ToHashCode();
    }
}
