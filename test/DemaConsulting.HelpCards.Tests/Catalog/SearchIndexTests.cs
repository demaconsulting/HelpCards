using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Catalog;

/// <summary>
///     Unit tests for <see cref="SearchIndex"/>: the scoring of search and the suggestions for unknown names.
/// </summary>
public class SearchIndexTests
{
    /// <summary>
    ///     Searches a catalog through its index, over every visible card.
    /// </summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="query">The query.</param>
    /// <returns>All results, best first.</returns>
    private static IReadOnlyList<HelpSearchResult> SearchAll(HelpCatalog catalog, string query)
    {
        return catalog.Scope.Data.Search.Search(catalog.Scope.Cards, query);
    }

    /// <summary>
    ///     Creates the catalog of the twelve-card fixture.
    /// </summary>
    /// <returns>The catalog.</returns>
    private static HelpCatalog Big()
    {
        return CatalogBuilding.Create(CatalogBuilding.Fixture("big"));
    }

    /// <summary>
    ///     Proves that the ranking of the queries of the report equals the golden file, which pins every weight, bonus,
    ///     matched field, and tie-break.
    /// </summary>
    [Fact]
    public void SearchIndex_Search_BigFixtureQueries_MatchRankingGolden()
    {
        // Arrange
        var catalog = Big();

        // Act
        var report = RankingReport.Describe(catalog);

        // Assert
        GoldenFiles.AssertEqual("cli", "ranking.expected.txt", report);
    }

    /// <summary>
    ///     Proves that an exact word scores by field: alias, id, example, title, tag, and summary, and that the bonus for
    ///     all words is added once.
    /// </summary>
    [Fact]
    public void SearchIndex_Search_ExactMatchPerField_ScoresTheWeightOfTheField()
    {
        // Arrange: one card for each field, so that only the tested field can match the word
        var catalog = CatalogBuilding.FromFiles(
            ("a.md", SourceReading.Card(title: "Plain", summary: "Plain words.", extraFrontMatter: "aliases:\n  - zeta\n")),
            ("b.md", SourceReading.Card(title: "Plain", summary: "Plain words.", extraFrontMatter: "tags:\n  - zeta\n")),
            ("c.md", SourceReading.Card(title: "Zeta", summary: "Plain words.")),
            ("d.md", SourceReading.Card(title: "Plain", summary: "A zeta here.")),
            ("e.md", SourceReading.Card(
                title: "Plain",
                summary: "Plain words.",
                extraFrontMatter: "examples:\n  - name: zeta\n    kind: command\n    language: shell\n    content: test zeta\n    description: An example.\n")));

        // Act
        var scores = SearchAll(catalog, "zeta").ToDictionary(result => result.Id, result => result.Score);

        // Assert: the best field of one word plus the 50 for all words matched; the alias also equals the whole query, +100
        Assert.Equal(40 + 50 + 100, scores["a"]);
        Assert.Equal(20 + 50, scores["b"]);
        Assert.Equal(30 + 50, scores["c"]);
        Assert.Equal(10 + 50, scores["d"]);
        Assert.Equal(25 + 50, scores["e"]);
    }

    /// <summary>
    ///     Proves that the start of a word scores less than the whole word, only from three letters, and never for tags.
    /// </summary>
    [Fact]
    public void SearchIndex_Search_Prefixes_ScoreLessNeedThreeLettersAndSkipTags()
    {
        // Arrange
        var catalog = CatalogBuilding.FromFiles(
            ("alpha.md", SourceReading.Card(title: "Different", summary: "Plain words.", extraFrontMatter: "aliases:\n  - gamma\ntags:\n  - gammatag\n")),
            ("gammaray.md", SourceReading.Card(title: "Gammarays", summary: "Gamma rays are photons.")));

        // Act
        var gam = SearchAll(catalog, "gam").ToDictionary(result => result.Id, result => result);
        var ga = SearchAll(catalog, "ga");
        var tag = SearchAll(catalog, "gammat");

        // Assert: the best prefix of "gam" is the alias or the id (20); the other fields prefix-match too and are reported
        Assert.Equal(20 + 50, gam["alpha"].Score);
        Assert.Equal(["alias"], gam["alpha"].MatchedOn);
        Assert.Equal(20 + 50, gam["gammaray"].Score);
        Assert.Equal(["id", "title", "summary"], gam["gammaray"].MatchedOn); Assert.Empty(ga);
        Assert.Empty(tag);
    }

    /// <summary>
    ///     Proves that a plural <c>s</c> is removed from long words for an English or neutral catalog and from no other.
    /// </summary>
    [Fact]
    public void SearchIndex_Search_Plurals_AreStemmedOnlyForEnglishAndNeutralCatalogs()
    {
        // Arrange
        var files = new[] { ("test.md", SourceReading.Card(title: "Test", summary: "One check.")) };
        var source = HelpSource.FromFiles(SourceReading.Files(files), "test");
        var neutral = CatalogBuilding.Create(source);
        var english = CatalogBuilding.Create(source, builder => builder.Culture(CatalogBuilding.Culture("en-US")));
        var german = CatalogBuilding.Create(source, builder => builder.Culture(CatalogBuilding.Culture("de-DE")));

        // Act
        var neutralScore = SearchAll(neutral, "tests").Single().Score;
        var englishScore = SearchAll(english, "tests").Single().Score;
        var germanResults = SearchAll(german, "tests");

        // Assert: the word "tests" becomes "test" and matches id and title exactly
        Assert.Equal(40 + 50, neutralScore);
        Assert.Equal(40 + 50, englishScore);
        Assert.Empty(germanResults);
    }

    /// <summary>
    ///     Proves that a word of three letters or fewer keeps its final <c>s</c>.
    /// </summary>
    [Fact]
    public void SearchIndex_Search_ShortWordEndingInS_IsNotStemmed()
    {
        // Arrange
        var catalog = CatalogBuilding.FromFiles(("bus.md", SourceReading.Card(title: "Bus", summary: "A vehicle.")));

        // Act
        var result = SearchAll(catalog, "bus").Single();

        // Assert
        Assert.Equal(40 + 50 + 100, result.Score);
    }

    /// <summary>
    ///     Proves that a repeated word does not count twice and that case, punctuation, and extra spaces do not matter.
    /// </summary>
    [Fact]
    public void SearchIndex_Search_RepeatedWordsCaseAndPunctuation_AreNormalized()
    {
        // Arrange
        var catalog = Big();

        // Act
        var once = SearchAll(catalog, "build");
        var messy = SearchAll(catalog, "  BUILD, build!  ");

        // Assert
        // The whole-query bonus belongs to the plain query only, so compare a card that does not get it
        Assert.Equal(once.Select(result => result.Id), messy.Select(result => result.Id));
        Assert.Equal(90, messy.Single(result => result.Id == "build/clean").Score);
    }

    /// <summary>
    ///     Proves that a query without a word gives no result, and that a null query throws.
    /// </summary>
    [Fact]
    public void SearchIndex_Search_QueryWithoutWord_GivesNothingAndNullThrows()
    {
        // Arrange
        var catalog = Big();
        var index = catalog.Scope.Data.Search;

        // Act
        var empty = SearchAll(catalog, string.Empty);
        var punctuation = SearchAll(catalog, " / - ");

        // Assert
        Assert.Empty(empty);
        Assert.Empty(punctuation);
        Assert.Throws<ArgumentNullException>(() => index.Search(catalog.Scope.Cards, null!));
        Assert.Throws<ArgumentNullException>(() => index.Search(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => SearchIndex.Build(null!));
    }

    /// <summary>
    ///     Proves that the name of a mount does not boost the cards below it, because the local id is indexed.
    /// </summary>
    [Fact]
    public void SearchIndex_Search_MountedSource_DoesNotBoostChildrenByTheMountName()
    {
        // Arrange
        var catalog = HelpCatalog.Create(builder => builder.AddMountedSource(CatalogBuilding.Fixture("big"), "zebra"));

        // Act
        var byMount = SearchAll(catalog, "zebra");
        var byLocalId = SearchAll(catalog, "clean");

        // Assert
        Assert.DoesNotContain(byMount, result => result.Id.StartsWith("zebra/", StringComparison.Ordinal));
        Assert.Contains(byLocalId, result => result.Id == "zebra/build/clean");
    }

    /// <summary>
    ///     Proves that results are ordered by score, then by depth within the source, then by global id.
    /// </summary>
    [Fact]
    public void SearchIndex_Search_EqualScores_OrderByDepthThenId()
    {
        // Arrange: three cards that all score the same for the word "word"
        var catalog = CatalogBuilding.FromFiles(
            ("z.md", SourceReading.Card(title: "Other", summary: "A word.")),
            ("a/child.md", SourceReading.Card(title: "Other", summary: "A word.")),
            ("a.md", SourceReading.Card(title: "Other", summary: "A word.")),
            ("b.md", SourceReading.Card(title: "Other", summary: "A word.")));

        // Act
        var ids = SearchAll(catalog, "word").Select(result => result.Id).ToList();

        // Assert
        Assert.Equal(["a", "b", "z", "a/child"], ids);
    }

    /// <summary>
    ///     Proves that suggestions are the closest names within the distance rule, ordered by distance and then ordinally.
    /// </summary>
    [Fact]
    public void SearchIndex_Suggest_Candidates_AreFilteredByDistanceAndOrdered()
    {
        // Arrange
        string[] candidates = ["tags", "tests", "tag", "build", "Tags", "stage"];

        // Act
        var close = SearchIndex.Suggest("tagz", candidates, 3);
        var limited = SearchIndex.Suggest("tagz", candidates, 1);
        var none = SearchIndex.Suggest("zzzzzz", candidates, 3);

        // Assert: "tags" and "Tags" are distance 1 and "tag" is distance 1; "tests" and "stage" are 3 and 2 away
        Assert.Equal(["Tags", "tag", "tags"], close);
        Assert.Equal(["Tags"], limited);
        Assert.Empty(none);
    }

    /// <summary>
    ///     Proves that the accepted distance grows with the length of the word: two for short words, a third of the length
    ///     for longer ones, ignoring case.
    /// </summary>
    [Fact]
    public void SearchIndex_Suggest_LongWords_AcceptAThirdOfTheLength()
    {
        // Arrange
        string[] candidates = ["configuration"];

        // Act: "configuration" has 13 letters, so 4 changes are accepted for a 13-letter word, 2 for a short one
        var four = SearchIndex.Suggest("CONFIGURATXXX", candidates, 3);
        var five = SearchIndex.Suggest("configuraXXXXX", candidates, 3);
        var shortWord = SearchIndex.Suggest("tagzz", ["tags"], 3);
        var tooFar = SearchIndex.Suggest("tagzzz", ["tags"], 3);

        // Assert
        Assert.Equal(["configuration"], four);
        Assert.Empty(five);
        Assert.Equal(["tags"], shortWord);
        Assert.Empty(tooFar);
    }

    /// <summary>
    ///     Proves that invalid arguments are rejected.
    /// </summary>
    [Fact]
    public void SearchIndex_Suggest_InvalidArguments_Throw()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => SearchIndex.Suggest(null!, [], 3));
        Assert.Throws<ArgumentNullException>(() => SearchIndex.Suggest("a", null!, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => SearchIndex.Suggest("a", [], 0));
    }
}
