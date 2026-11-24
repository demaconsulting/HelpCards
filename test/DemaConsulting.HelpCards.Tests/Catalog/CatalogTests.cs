using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Catalog;

/// <summary>
///     Subsystem tests for Catalog: the catalog, its cards, views, lookups, and problem model working together.
/// </summary>
/// <remarks>
///     These tests create catalogs with the builder of the Composition subsystem and a source of the Loading
///     subsystem, as the subsystem design documents, and then use only the public face of the Catalog subsystem.
/// </remarks>
public class CatalogTests
{
    /// <summary>
    ///     Proves that a catalog exposes the tree, the sources, and the tags of a source as immutable values.
    /// </summary>
    [Fact]
    public void Catalog_Model_SimpleFixture_ExposesImmutableTreeSourcesAndTags()
    {
        // Arrange / Act
        var catalog = CatalogBuilding.Simple();
        var guide = catalog.Find("guide")!;

        // Assert
        Assert.Equal(["guide", "reference"], catalog.Roots.Select(card => card.Id));
        Assert.Equal(5, catalog.Cards.Count);
        Assert.Equal("simple", Assert.Single(catalog.Sources).Name);
        Assert.Equal(["authoring", "reference", "tutorial"], catalog.TagNames);
        Assert.Equal(["run", "settings", "fragment"], guide.Examples.Select(example => example.Name));
        Assert.True(((ICollection<HelpCard>)catalog.Cards).IsReadOnly);
        Assert.Same(guide, catalog.Find("guide"));
    }

    /// <summary>
    ///     Proves that lookups find cards by path and alias, report failures, and find examples.
    /// </summary>
    [Fact]
    public void Catalog_Lookup_PathsAliasesAndExamples_FindCardsAndReportFailures()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act
        var found = catalog.Find("start usage");
        var failed = catalog.Resolve("start nothing");
        var examples = catalog.FindExamples("settings");

        // Assert
        Assert.Equal("guide/usage", found?.Id);
        Assert.Equal(("guide", "nothing"), (failed.ParentId, failed.UnknownSegment));
        Assert.Equal(["setup", "usage"], failed.ValidNames);
        Assert.Equal("guide", Assert.Single(examples).CardId);
        Assert.Equal("settings", catalog.FindExample(catalog.Find("guide")!, "settings")?.Example.Name);
    }

    /// <summary>
    ///     Proves that views filter by tag, source, predicate, and id, compose, and lift children of hidden parents.
    /// </summary>
    [Fact]
    public void Catalog_Views_FilterSourceWhereWithout_ComposeAndLiftHiddenParents()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act
        var tagged = catalog.Filter(["reference"]);
        var composed = catalog.FilterSource(["simple"]).Where(card => card.Name != "guide").Without(["reference"]);

        // Assert
        Assert.Equal(["reference", "reference/limits"], tagged.Cards.Select(card => card.Id));
        Assert.Equal(["guide/setup", "guide/usage", "reference/limits"], composed.Roots.Select(card => card.Id));
        Assert.Equal(5, catalog.Cards.Count);
    }

    /// <summary>
    ///     Proves that links to hidden targets are written as text and see-also entries to hidden cards are dropped.
    /// </summary>
    [Fact]
    public void Catalog_Views_HiddenTargets_AreWrittenAsTextAndDroppedFromSeeAlso()
    {
        // Arrange
        var view = CatalogBuilding.Simple().Without(["reference/limits", "guide/setup"]);

        // Act
        var guide = view.Find("guide")!;

        // Assert
        Assert.Contains("Start with Setup and then read the card `guide/usage`.", guide.Body, StringComparison.Ordinal);
        Assert.Contains("described in the limits card, and", guide.Body, StringComparison.Ordinal);
        Assert.Empty(guide.SeeAlsoIds);
    }

    /// <summary>
    ///     Proves that defects throw the catalog exception with every error and misuse throws argument exceptions.
    /// </summary>
    [Fact]
    public void Catalog_Failures_DefectsAndMisuse_ThrowTheDocumentedExceptions()
    {
        // Arrange
        var defective = HelpSource.FromFiles(
            SourceReading.Files(("a.md", SourceReading.Card(body: "[[x]] [[y]]")), ("b.md", SourceReading.Card(body: "[[z]]"))),
            "test");
        var catalog = CatalogBuilding.Simple();

        // Act
        var exception = Assert.Throws<HelpCatalogException>(() => CatalogBuilding.Create(defective));

        // Assert
        Assert.Equal(["link-target", "link-target", "link-target"], exception.Problems.Select(problem => problem.RuleId));
        Assert.EndsWith("(and 2 more)", exception.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentNullException>(() => HelpCatalog.Create(null!));
        Assert.Throws<ArgumentException>(() => catalog.Without(["nothing"]));
        Assert.Throws<ArgumentException>(() => catalog.FilterSource(["nothing"]));
        Assert.Throws<ArgumentNullException>(() => catalog.Find(null!));
    }

    /// <summary>
    ///     Proves that problems are plain values that can be compared and printed.
    /// </summary>
    [Fact]
    public void Catalog_Problems_Records_CompareByValueAndPrintOneSentence()
    {
        // Arrange
        var first = new HelpProblem(HelpSeverity.Error, "syntax", "demo", "a.md", 3, "Bad.");
        var second = new HelpProblem(HelpSeverity.Error, "syntax", "demo", "a.md", 3, "Bad.");

        // Act / Assert
        Assert.Equal(first, second);
        Assert.Equal("Help card 'a.md' of source 'demo', line 3: Bad.", new HelpCatalogException([first]).Message);
        Assert.True(HelpSeverity.Info < HelpSeverity.Warning && HelpSeverity.Warning < HelpSeverity.Error);
    }

    /// <summary>
    ///     Proves that one catalog serves many threads at once with consistent results.
    /// </summary>
    [Fact]
    public void Catalog_Concurrency_ParallelUseOfOneCatalog_IsConsistent()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var expected = catalog.Cards.Select(card => card.Body).ToList();
        var calls = 0;
        var wrong = 0;

        // Act
        Parallel.For(0, 100, _ =>
        {
            var view = catalog.Where(card =>
            {
                Interlocked.Increment(ref calls);
                return true;
            });
            var bodies = view.Cards.Select(card => card.Body).ToList();
            if (!bodies.SequenceEqual(expected) || catalog.Find("guide usage") is null || catalog.FindExamples("run").Count != 1)
            {
                Interlocked.Increment(ref wrong);
            }
        });

        // Assert: every call of the predicate is counted and no worker saw a different result
        Assert.Equal(0, wrong);
        Assert.Equal(500, calls);
    }

    /// <summary>
    ///     Proves that translations are selected per card and fall back to less specific cultures and neutral text.
    /// </summary>
    [Fact]
    public void Catalog_Cultures_I18nFixture_SelectsTranslationPerCardWithFallback()
    {
        // Arrange
        var source = CatalogBuilding.Fixture("i18n");

        // Act
        var neutral = CatalogBuilding.Create(source);
        var german = CatalogBuilding.Create(source, builder => builder.Culture(CatalogBuilding.Culture("de")));
        var germany = CatalogBuilding.Create(source, builder => builder.Culture(CatalogBuilding.Culture("de-DE")));

        // Assert: the specific translation, the language translation, and neutral text, with the body culture
        Assert.Equal(("Welcome", "en"), (neutral.Find("welcome")!.Title, neutral.Find("welcome")!.Culture));
        Assert.Equal(("Welcome (language de)", "de"), (german.Find("welcome")!.Title, german.Find("welcome")!.Culture));
        Assert.Equal(("Welcome (culture de-DE)", "de-DE"), (germany.Find("welcome")!.Title, germany.Find("welcome")!.Culture));
        Assert.Equal("Farewell (language de)", germany.Find("farewell")!.Title);
        Assert.Equal(("Plain", "en"), (germany.Find("plain")!.Title, germany.Find("plain")!.Culture));
        Assert.Equal(["hello", "greeting-de"], german.Find("welcome")!.Aliases);
        Assert.Equal(["hello"], germany.Find("welcome")!.Aliases);
        Assert.Equal("Greet with the text of the language de.", german.Find("welcome")!.Examples[0].Description);
        Assert.Equal("de", german.Culture.Name);
    }

    /// <summary>
    ///     Proves that the big fixture loads as three groups of three below three roots.
    /// </summary>
    [Fact]
    public void Catalog_Model_BigFixture_HasTwelveCardsInThreeGroups()
    {
        // Arrange / Act
        var catalog = CatalogBuilding.Create(CatalogBuilding.Fixture("big"));

        // Assert
        Assert.Equal(12, catalog.Cards.Count);
        Assert.Equal(["build", "deploy", "test"], catalog.Roots.Select(card => card.Id));
        Assert.All(catalog.Roots, root => Assert.Equal(3, root.ChildIds.Count));
        Assert.Equal(["build", "quality", "release"], catalog.TagNames);
        Assert.Equal("deploy", catalog.Find("ship")!.Id);
    }
}
