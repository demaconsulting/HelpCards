using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Catalog;

/// <summary>
///     Unit tests for the <c>Search</c> and <c>Suggest</c> members of <see cref="HelpCatalog"/>.
/// </summary>
public partial class HelpCatalogTests
{
    /// <summary>
    ///     Creates the catalog of the twelve-card fixture.
    /// </summary>
    /// <returns>The catalog.</returns>
    private static HelpCatalog BigCatalog()
    {
        return CatalogBuilding.Create(CatalogBuilding.Fixture("big"));
    }

    /// <summary>
    ///     Proves that a search without options returns at most eight results, best first, and that the options limit it
    ///     further.
    /// </summary>
    [Fact]
    public void HelpCatalog_Search_MaxResults_LimitsTheBestResults()
    {
        // Arrange
        var catalog = BigCatalog();

        // Act
        var defaults = catalog.Search("the");
        var three = catalog.Search("the", new HelpSearchOptions { MaxResults = 3 });
        var all = catalog.Search("the", new HelpSearchOptions { MaxResults = 20 });

        // Assert
        Assert.Equal(8, defaults.Count);
        Assert.Equal(all.Take(3).Select(result => result.Id), three.Select(result => result.Id));
        Assert.Equal(all.Take(8).Select(result => result.Id), defaults.Select(result => result.Id));
        Assert.True(all.Count > 8);
        Assert.Equal(all.OrderByDescending(result => result.Score).Select(result => result.Score), all.Select(result => result.Score));
    }

    /// <summary>
    ///     Proves that a search returns the data of the card with its source and matched fields.
    /// </summary>
    [Fact]
    public void HelpCatalog_Search_Result_CarriesCardDataAndMatchedFields()
    {
        // Arrange
        var catalog = BigCatalog();

        // Act
        var result = catalog.Search("ship").Single();

        // Assert
        Assert.Equal("deploy", result.Id);
        Assert.Equal("Deploy", result.Title);
        Assert.Equal("Put the program where people can use it.", result.Summary);
        Assert.Equal("big", result.SourceName);
        Assert.Equal(190, result.Score);
        Assert.Equal(["alias"], result.MatchedOn);
    }

    /// <summary>
    ///     Proves that the source and tag options restrict the search, and that they combine.
    /// </summary>
    [Fact]
    public void HelpCatalog_Search_SourcesAndTags_RestrictTheCardsSearched()
    {
        // Arrange
        var catalog = HelpCatalog.Create(builder => builder
            .AddSource(CatalogBuilding.Fixture("big"))
            .AddSource(CatalogBuilding.LibA()));

        // Act
        var all = catalog.Search("program", new HelpSearchOptions { MaxResults = 20 });
        var big = catalog.Search("program", new HelpSearchOptions { MaxResults = 20, Sources = ["big"] });
        var other = catalog.Search("program", new HelpSearchOptions { Sources = ["LIB-A"] });
        var quality = catalog.Search("program", new HelpSearchOptions { MaxResults = 20, Tags = ["quality"] });
        var both = catalog.Search("program", new HelpSearchOptions { MaxResults = 20, Tags = ["release"], Sources = ["big"] });

        // Assert
        Assert.Equal(all.Count, big.Count);
        Assert.Empty(other);
        Assert.NotEmpty(quality);
        Assert.All(quality, result => Assert.StartsWith("test", result.Id, StringComparison.Ordinal));
        Assert.All(both, result => Assert.Contains("release", catalog.Find(result.Id)!.Tags));
    }

    /// <summary>
    ///     Proves that a search through a view returns visible cards only, whatever the index holds.
    /// </summary>
    [Fact]
    public void HelpCatalog_Search_View_ReturnsVisibleCardsOnly()
    {
        // Arrange
        var catalog = BigCatalog();
        var view = catalog.Without(["build/clean", "deploy"]);

        // Act
        var all = catalog.Search("deploy", new HelpSearchOptions { MaxResults = 20 }).Select(result => result.Id).ToList();
        var seen = view.Search("deploy", new HelpSearchOptions { MaxResults = 20 }).Select(result => result.Id).ToList();

        // Assert
        Assert.Contains("deploy", all);
        Assert.DoesNotContain("deploy", seen);
        Assert.DoesNotContain("build/clean", view.Search("clean").Select(result => result.Id));
    }

    /// <summary>
    ///     Proves that a search rejects a null query, a maximum below one, an unknown source, and a tag outside a closed
    ///     vocabulary.
    /// </summary>
    [Fact]
    public void HelpCatalog_Search_InvalidArguments_Throw()
    {
        // Arrange
        var catalog = BigCatalog();
        var closed = CatalogBuilding.Create(CatalogBuilding.Fixture("big"), builder => builder.TagVocabulary(["build", "quality", "release"]));

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => catalog.Search(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => catalog.Search("x", new HelpSearchOptions { MaxResults = 0 }));
        Assert.Throws<ArgumentException>(() => catalog.Search("x", new HelpSearchOptions { Sources = ["nope"] }));
        Assert.Throws<ArgumentException>(() => closed.Search("x", new HelpSearchOptions { Tags = ["nope"] }));
        Assert.Empty(catalog.Search("   "));
    }

    /// <summary>
    ///     Proves that suggestions come from the names, aliases, and ids of the visible cards and respect the limit.
    /// </summary>
    [Fact]
    public void HelpCatalog_Suggest_NamesAliasesAndIds_AreCandidates()
    {
        // Arrange
        var catalog = BigCatalog();

        // Act
        var name = catalog.Suggest("built");
        var alias = catalog.Suggest("shop");
        var id = catalog.Suggest("build/lean");
        var limited = catalog.Suggest("test", 1);
        var none = catalog.Suggest("qqqqqqqq");

        // Assert
        Assert.Equal("build", name[0]);
        Assert.Equal(["ship"], alias);
        Assert.Equal(["build/clean"], id);
        Assert.Single(limited);
        Assert.Empty(none);
    }

    /// <summary>
    ///     Proves that a hidden card is never suggested.
    /// </summary>
    [Fact]
    public void HelpCatalog_Suggest_View_NeverSuggestsHiddenCards()
    {
        // Arrange
        var view = BigCatalog().Without(["build/clean"]);

        // Act
        var suggestions = view.Suggest("lean");

        // Assert
        Assert.DoesNotContain("build/clean", suggestions);
        Assert.DoesNotContain("clean", suggestions);
    }

    /// <summary>
    ///     Proves that suggestions reject invalid arguments.
    /// </summary>
    [Fact]
    public void HelpCatalog_Suggest_InvalidArguments_Throw()
    {
        // Arrange
        var catalog = BigCatalog();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => catalog.Suggest(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => catalog.Suggest("x", 0));
    }

    /// <summary>
    ///     Proves that resolving an unknown path gives the closest name or alias at the level that failed.
    /// </summary>
    [Fact]
    public void HelpCatalog_Resolve_UnknownSegment_SuggestsClosestNameOrAlias()
    {
        // Arrange
        var catalog = BigCatalog();

        // Act
        var name = catalog.Resolve("build compiled");
        var alias = catalog.Resolve("shop");
        var far = catalog.Resolve("qqqqqqqq");
        var found = catalog.Resolve("build");

        // Assert
        Assert.Equal("compile", name.Suggestion);
        Assert.Equal("ship", alias.Suggestion);
        Assert.Null(far.Suggestion);
        Assert.Null(found.Suggestion);
    }
}
