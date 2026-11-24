using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Catalog;

/// <summary>
///     Unit tests for the views of <see cref="HelpCatalog"/>: filtering, hiding, lifting, and composition.
/// </summary>
public partial class HelpCatalogTests
{
    /// <summary>
    ///     Proves that a tag filter keeps the cards with an included tag and drops the cards with an excluded tag.
    /// </summary>
    [Fact]
    public void HelpCatalog_Filter_IncludeAndExcludeTags_KeepsMatchingCards()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act
        var reference = catalog.Filter(["Reference"]);
        var withoutAuthoring = catalog.Filter(null, ["authoring"]);
        var everything = catalog.Filter(null);

        // Assert: tags are compared ignoring case; no include list means no restriction
        Assert.Equal(["reference", "reference/limits"], reference.Cards.Select(card => card.Id));
        Assert.Equal(["guide/setup", "guide/usage", "reference", "reference/limits"], withoutAuthoring.Cards.Select(card => card.Id));
        Assert.Equal(catalog.Cards.Select(card => card.Id), everything.Cards.Select(card => card.Id));
        Assert.Equal(["reference"], reference.TagNames);
    }

    /// <summary>
    ///     Proves that an unknown tag is an argument error only when the catalog has a closed vocabulary.
    /// </summary>
    [Fact]
    public void HelpCatalog_Filter_UnknownTag_ThrowsOnlyForClosedVocabulary()
    {
        // Arrange
        var open = CatalogBuilding.Simple();
        var closed = CatalogBuilding.Create(
            CatalogBuilding.Fixture("simple", new Dictionary<string, string> { ["limit"] = "42" }),
            builder => builder.TagVocabulary(["tutorial", "authoring", "reference"]));

        // Act / Assert
        Assert.Empty(open.Filter(["nothing"]).Cards);
        Assert.Throws<ArgumentException>(() => closed.Filter(["nothing"]));
        Assert.Throws<ArgumentException>(() => closed.Filter(null, ["nothing"]));
        Assert.Throws<ArgumentException>(() => open.Filter([" "]));
        Assert.Equal(["reference", "reference/limits"], closed.Filter(["reference"]).Cards.Select(card => card.Id));
    }

    /// <summary>
    ///     Proves that a source filter includes and excludes by source name and rejects unknown names.
    /// </summary>
    [Fact]
    public void HelpCatalog_FilterSource_IncludeExcludeAndUnknown_Behaves()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act
        var included = catalog.FilterSource(["SIMPLE"]);
        var excluded = catalog.FilterSource(null, ["simple"]);

        // Assert
        Assert.Equal(catalog.Cards.Count, included.Cards.Count);
        Assert.Empty(excluded.Cards);
        Assert.Empty(excluded.Sources);
        Assert.Empty(excluded.TagNames);
        Assert.Throws<ArgumentException>(() => catalog.FilterSource(["nothing"]));
        Assert.Throws<ArgumentException>(() => catalog.FilterSource(null, ["nothing"]));
    }

    /// <summary>
    ///     Proves that a predicate runs once per card, immediately, in card order, and is not kept.
    /// </summary>
    [Fact]
    public void HelpCatalog_Where_Predicate_RunsOncePerCardEagerlyInOrderAndIsNotRetained()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var seen = new List<string>();

        // Act
        var view = catalog.Where(card =>
        {
            seen.Add(card.Id);
            return card.Tags.Contains("tutorial");
        });
        var countAfterCreation = seen.Count;
        _ = view.Cards.ToList();
        _ = view.Find("guide");
        _ = view.Where(_ => true);

        // Assert: five cards, in Cards order, and no later call
        Assert.Equal(catalog.Cards.Select(card => card.Id), seen);
        Assert.Equal(5, countAfterCreation);
        Assert.Equal(countAfterCreation, seen.Count);
        Assert.Equal(["guide", "guide/setup", "guide/usage"], view.Cards.Select(card => card.Id));
        Assert.Throws<ArgumentNullException>(() => catalog.Where(null!));
    }

    /// <summary>
    ///     Proves that hiding a card lifts its visible children to the nearest visible ancestor or the roots.
    /// </summary>
    [Fact]
    public void HelpCatalog_Without_HiddenParent_LiftsChildren()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act
        var view = catalog.Without(["guide"]);

        // Assert
        Assert.Equal(["guide/setup", "guide/usage", "reference"], view.Roots.Select(card => card.Id));
        Assert.Null(view.Find("guide/setup")!.ParentId);
        Assert.Equal(["guide/setup", "guide/usage", "reference", "reference/limits"], view.Cards.Select(card => card.Id));
        Assert.Equal(["guide", "reference"], catalog.Roots.Select(card => card.Id));
    }

    /// <summary>
    ///     Proves that a lifted card moves to its nearest visible ancestor when only a middle card is hidden.
    /// </summary>
    [Fact]
    public void HelpCatalog_Without_HiddenMiddleCard_LiftsToNearestVisibleAncestor()
    {
        // Arrange
        var catalog = CatalogBuilding.FromIds("a", "a/b", "a/b/c", "a/b/d");

        // Act
        var view = catalog.Without(["A/B"]);

        // Assert
        Assert.Equal(["a/b/c", "a/b/d"], view.GetChildren(view.Find("a")!).Select(card => card.Id));
        Assert.Equal("a", view.Find("a/b/c")!.ParentId);
        Assert.Equal(["a/b/c", "a/b/d"], view.Find("a")!.ChildIds);
    }

    /// <summary>
    ///     Proves that an unknown or empty id is an argument error.
    /// </summary>
    [Fact]
    public void HelpCatalog_Without_UnknownOrEmptyId_ThrowsArgumentException()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => catalog.Without(["nothing"]));
        Assert.Throws<ArgumentException>(() => catalog.Without([""]));
        Assert.Throws<ArgumentNullException>(() => catalog.Without(null!));
        Assert.Equal(catalog.Cards.Count, catalog.Without([]).Cards.Count);
    }

    /// <summary>
    ///     Proves that lookups walk through hidden cards but never return a hidden card.
    /// </summary>
    [Fact]
    public void HelpCatalog_Find_HiddenCard_WalksThroughButReturnsNullForHiddenTarget()
    {
        // Arrange
        var view = CatalogBuilding.Simple().Without(["guide"]);

        // Act
        var resolution = view.Resolve("guide");

        // Assert
        Assert.Null(view.Find("guide"));
        Assert.Equal("guide/setup", view.Find("guide setup")?.Id);
        Assert.Null(resolution.Card);
        Assert.Equal("guide", resolution.UnknownSegment);
        Assert.Equal(["reference"], resolution.ValidNames);
    }

    /// <summary>
    ///     Proves that see-also entries to hidden cards are dropped.
    /// </summary>
    [Fact]
    public void HelpCatalog_GetSeeAlso_HiddenTarget_IsDropped()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var view = catalog.Without(["reference/limits"]);

        // Act / Assert
        Assert.Equal(["reference/limits"], catalog.Find("guide")!.SeeAlsoIds);
        Assert.Empty(view.Find("guide")!.SeeAlsoIds);
        Assert.Empty(view.GetSeeAlso(view.Find("guide")!));
    }

    /// <summary>
    ///     Proves that examples of hidden cards are not found.
    /// </summary>
    [Fact]
    public void HelpCatalog_FindExamples_HiddenCard_IsNotFound()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var view = catalog.Without(["guide/setup"]);

        // Act / Assert
        Assert.Single(catalog.FindExamples("check"));
        Assert.Empty(view.FindExamples("check"));
        Assert.Null(view.FindExample(view.Find("guide")!, "check"));
        Assert.Equal("guide", view.FindExample(view.Find("guide")!, "run")?.CardId);
    }

    /// <summary>
    ///     Proves that views of views show the intersection and share the frozen data.
    /// </summary>
    [Fact]
    public void HelpCatalog_Views_Compose_ByIntersectionAndShareData()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act
        var view = catalog.Filter(["tutorial"]).Without(["guide/usage"]).Where(card => card.Id != "guide");

        // Assert
        Assert.Equal(["guide/setup"], view.Cards.Select(card => card.Id));
        Assert.Same(catalog.Diagnostics, view.Diagnostics);
        Assert.Same(catalog.Culture, view.Culture);
        Assert.Equal(5, catalog.Cards.Count);
    }

    /// <summary>
    ///     Proves that the cards of different views are different objects with the same id.
    /// </summary>
    [Fact]
    public void HelpCatalog_Views_CardsOfViewsAreIndependentObjects()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var view = catalog.Without(["guide/usage"]);

        // Act
        var original = catalog.Find("guide")!;
        var filtered = view.Find("guide")!;

        // Assert: the view changes ChildIds of its own card object, never of the original
        Assert.NotSame(original, filtered);
        Assert.Equal(["guide/setup", "guide/usage"], original.ChildIds);
        Assert.Equal(["guide/setup"], filtered.ChildIds);
        Assert.Equal(["guide/setup"], view.GetChildren(original).Select(card => card.Id));
    }

    /// <summary>
    ///     Proves that many threads can use one catalog and its views at the same time.
    /// </summary>
    [Fact]
    public void HelpCatalog_Concurrency_ParallelFindBodyWhereAndFilter_AreConsistent()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var expectedBody = catalog.Find("guide")!.Body;
        var predicateCalls = 0;
        var failures = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Act: each worker uses the shared catalog in several ways
        Parallel.For(0, 64, _ =>
        {
            var view = catalog.Where(card =>
            {
                Interlocked.Increment(ref predicateCalls);
                return card.Id != "reference";
            });
            if (catalog.Find("guide setup")?.Id != "guide/setup")
            {
                failures.Add("find");
            }

            if (view.Cards.Count != 4 || view.Find("guide") is null)
            {
                failures.Add("view");
            }

            if (catalog.Filter(["tutorial"]).Cards.Count != 3 || catalog.FindExamples("run").Count != 1)
            {
                failures.Add("filter");
            }

            if (catalog.Find("guide")!.Body != expectedBody)
            {
                failures.Add("body");
            }
        });

        // Assert: one predicate call per card per worker and no inconsistency
        Assert.Empty(failures);
        Assert.Equal(64 * 5, predicateCalls);
    }

    /// <summary>
    ///     Proves that creating a catalog twice from the same source gives identical results, whatever the current culture.
    /// </summary>
    [Fact]
    public void HelpCatalog_Create_Twice_IsDeterministicAndCultureIndependent()
    {
        // Arrange
        var original = System.Globalization.CultureInfo.CurrentCulture;
        string Describe(HelpCatalog catalog) => string.Join("|", catalog.Cards.Select(card => $"{card.Id}:{card.Title}:{card.Body}"));
        try
        {
            // Act
            System.Globalization.CultureInfo.CurrentCulture = CatalogBuilding.Culture("tr-TR");
            var first = Describe(CatalogBuilding.Simple());
            System.Globalization.CultureInfo.CurrentCulture = CatalogBuilding.Culture("de-DE");
            var second = Describe(CatalogBuilding.Simple());

            // Assert
            Assert.Equal(first, second);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = original;
        }
    }
}
