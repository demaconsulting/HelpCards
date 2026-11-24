using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Presentation;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Catalog;

/// <summary>
///     Unit tests for <see cref="HelpCatalog"/>: creation, properties, lookup, and example search.
/// </summary>
public partial class HelpCatalogTests
{
    /// <summary>
    ///     Proves that a null configuration callback is an argument error.
    /// </summary>
    [Fact]
    public void HelpCatalog_Create_NullConfigure_ThrowsArgumentNullException()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => HelpCatalog.Create(null!));
    }

    /// <summary>
    ///     Proves that a builder without a source is an argument error, never a catalog exception.
    /// </summary>
    [Fact]
    public void HelpCatalog_Create_NoSource_ThrowsArgumentException()
    {
        // Act
        var exception = Assert.Throws<ArgumentException>(() => HelpCatalog.Create(_ => { }));

        // Assert
        Assert.IsNotType<HelpCatalogException>(exception);
    }

    /// <summary>
    ///     Proves that a defective source throws one exception that carries every error.
    /// </summary>
    [Fact]
    public void HelpCatalog_Create_DefectiveSource_ThrowsExceptionWithAllErrors()
    {
        // Arrange: two defective cards, a bad link and an unknown key
        var source = HelpSource.FromFiles(
            SourceReading.Files(
                ("a.md", SourceReading.Card(body: "See [[missing]].")),
                ("b.md", SourceReading.Card(extraFrontMatter: "tag: x\n"))),
            "test");

        // Act
        var exception = Assert.Throws<HelpCatalogException>(() => CatalogBuilding.Create(source));

        // Assert: both errors in order, and the message names the first plus the count of the rest
        Assert.Equal(["link-target", "key-unknown"], exception.Problems.Select(problem => problem.RuleId));
        Assert.StartsWith("Help card 'a.md' of source 'test', line 5:", exception.Message, StringComparison.Ordinal);
        Assert.EndsWith("(and 1 more)", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that creating a catalog from the simple fixture exposes its roots, cards in depth-first order,
    ///     source, tags, and culture.
    /// </summary>
    [Fact]
    public void HelpCatalog_Properties_SimpleFixture_ExposeTreeSourceTagsAndCulture()
    {
        // Arrange / Act
        var catalog = CatalogBuilding.Simple();

        // Assert
        Assert.Equal(["guide", "reference"], catalog.Roots.Select(card => card.Id));
        Assert.Equal(
            ["guide", "guide/setup", "guide/usage", "reference", "reference/limits"],
            catalog.Cards.Select(card => card.Id));
        Assert.Equal(["authoring", "reference", "tutorial"], catalog.TagNames);
        Assert.Equal(new HelpSourceInfo("simple", null, "Simple fixture", "A small source that exercises every kind of card feature.", "en", 1), Assert.Single(catalog.Sources));
        Assert.Same(System.Globalization.CultureInfo.InvariantCulture, catalog.Culture);
    }

    /// <summary>
    ///     Proves that the diagnostics of a successful catalog hold only informational problems.
    /// </summary>
    [Fact]
    public void HelpCatalog_Diagnostics_AbsentExternalSource_HoldsInfoProblemsOnly()
    {
        // Arrange / Act
        var catalog = CatalogBuilding.Simple();

        // Assert: the see-also entry and the link to the absent source 'other' each leave a note
        Assert.All(catalog.Diagnostics, problem => Assert.Equal(HelpSeverity.Info, problem.Severity));
        Assert.Equal(
            [("guide.md", 12), ("guide.md", 36)],
            catalog.Diagnostics.Select(problem => (problem.File, problem.Line)));
        Assert.All(catalog.Diagnostics, problem => Assert.Equal("external-source-absent", problem.RuleId));
    }

    /// <summary>
    ///     Proves that a catalog never reads a file after creation.
    /// </summary>
    [Fact]
    public void HelpCatalog_Create_ThenUse_ReadsEachFileOnceAndNeverAgain()
    {
        // Arrange
        var provider = new CountingFileProvider(SourceReading.Files(("a.md", SourceReading.Card()), ("a/b.md", SourceReading.Card())));
        var catalog = CatalogBuilding.Create(HelpSource.FromProvider(provider, "test"));
        var listed = provider.ListCount;
        var read = provider.ReadCount;

        // Act: use the catalog in every way
        _ = catalog.Find("a/b");
        _ = catalog.Resolve("a x");
        _ = catalog.Where(_ => true).Filter(["x"]).Cards.Select(card => card.Body).ToList();
        _ = catalog.Cards.Select(card => card.Body).ToList();
        _ = catalog.Search("a");
        _ = catalog.Suggest("b");
        _ = catalog.FindExamples("run");
        _ = catalog.RenderIndex();
        _ = catalog.RenderCardList(catalog.Cards);
        _ = catalog.RenderSearch("a", catalog.Search("a"));
        foreach (var format in new[] { HelpFormat.Text, HelpFormat.Markdown, HelpFormat.Json })
        {
            var options = new HelpRenderOptions { Format = format };
            _ = catalog.Cards.Select(card => catalog.Render(card, options)).ToList();
            _ = catalog.Cards.Select(card => catalog.RenderExamples(card, options)).ToList();
            _ = catalog.Cards.Select(card => catalog.FormatBody(card, options)).ToList();
        }

        // Assert
        Assert.Equal(1, listed);
        Assert.Equal(2, read);
        Assert.Equal(listed, provider.ListCount);
        Assert.Equal(read, provider.ReadCount);
    }

    /// <summary>
    ///     Proves that a path finds a card by name, alias, and case-insensitive segments separated by slashes or spaces.
    /// </summary>
    [Fact]
    public void HelpCatalog_Find_PathForms_FindsCard()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        (string Path, string ExpectedId)[] cases =
        [
            ("guide", "guide"), ("GUIDE", "guide"), ("start", "guide"), ("guide/setup", "guide/setup"),
            ("guide setup", "guide/setup"), ("  start   Usage ", "guide/usage"), ("ref limits", "reference/limits")
        ];

        // Act / Assert
        foreach (var (path, expectedId) in cases)
        {
            Assert.Equal(expectedId, catalog.Find(path)?.Id);
        }
    }

    /// <summary>
    ///     Proves that unknown, empty, and unqualified paths find nothing and a null path is an argument error.
    /// </summary>
    [Fact]
    public void HelpCatalog_Find_UnknownOrEmptyPath_ReturnsNull()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act / Assert
        foreach (var path in new[] { "unknown", "guide/unknown", "setup", string.Empty, "   " })
        {
            Assert.Null(catalog.Find(path));
        }

        Assert.Throws<ArgumentNullException>(() => catalog.Find(null!));
    }

    /// <summary>
    ///     Proves that resolving a good path gives the card and nothing else.
    /// </summary>
    [Fact]
    public void HelpCatalog_Resolve_ValidPath_ReturnsCardOnly()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act
        var resolution = catalog.Resolve("guide setup");

        // Assert
        Assert.Equal("guide/setup", resolution.Card?.Id);
        Assert.Null(resolution.ParentId);
        Assert.Null(resolution.UnknownSegment);
        Assert.Empty(resolution.ValidNames);
        Assert.Null(resolution.Suggestion);
    }

    /// <summary>
    ///     Proves that a failing segment names its parent, itself, and the valid names at that level.
    /// </summary>
    [Fact]
    public void HelpCatalog_Resolve_UnknownChild_ReportsParentSegmentAndValidNames()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act
        var resolution = catalog.Resolve("guide deploy");

        // Assert
        Assert.Null(resolution.Card);
        Assert.Equal("guide", resolution.ParentId);
        Assert.Equal("deploy", resolution.UnknownSegment);
        Assert.Equal(["setup", "usage"], resolution.ValidNames);
        Assert.Null(resolution.Suggestion);
    }

    /// <summary>
    ///     Proves that a failing first segment has no parent and lists the root names.
    /// </summary>
    [Fact]
    public void HelpCatalog_Resolve_UnknownRoot_HasNoParentAndListsRoots()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act
        var resolution = catalog.Resolve("guid");

        // Assert
        Assert.Null(resolution.ParentId);
        Assert.Equal("guid", resolution.UnknownSegment);
        Assert.Equal(["guide", "reference"], resolution.ValidNames);
        Assert.Throws<ArgumentNullException>(() => catalog.Resolve(null!));
    }

    /// <summary>
    ///     Proves that children and see-also cards follow sibling order and the order written.
    /// </summary>
    [Fact]
    public void HelpCatalog_GetChildren_AndGetSeeAlso_FollowOrder()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var guide = catalog.Find("guide")!;

        // Act / Assert: children by order value, see-also in written order (the absent source is dropped)
        Assert.Equal(["guide/setup", "guide/usage"], catalog.GetChildren(guide).Select(card => card.Id));
        Assert.Equal(["reference/limits"], catalog.GetSeeAlso(guide).Select(card => card.Id));
        Assert.Equal(guide.ChildIds, catalog.GetChildren(guide).Select(card => card.Id));
        Assert.Empty(catalog.GetChildren(catalog.Find("guide/setup")!));
        Assert.Throws<ArgumentNullException>(() => catalog.GetChildren(null!));
        Assert.Throws<ArgumentNullException>(() => catalog.GetSeeAlso(null!));
    }

    /// <summary>
    ///     Proves that all examples of a name are found, with the cards that own them.
    /// </summary>
    [Fact]
    public void HelpCatalog_FindExamples_ByName_ReturnsEveryMatchWithOwner()
    {
        // Arrange: two cards with an example of the same name in different scopes
        var catalog = CatalogBuilding.FromFiles(
            ("a.md", SourceReading.Card(extraFrontMatter: Example("demo"))),
            ("b.md", SourceReading.Card(extraFrontMatter: Example("demo"))),
            ("b/c.md", SourceReading.Card(extraFrontMatter: Example("other"))));

        // Act
        var all = catalog.FindExamples("DEMO");

        // Assert
        Assert.Equal(["a", "b"], all.Select(reference => reference.CardId));
        Assert.All(all, reference => Assert.Equal("demo", reference.Example.Name));
        Assert.Empty(catalog.FindExamples("none"));
        Assert.Throws<ArgumentException>(() => catalog.FindExamples(" "));
    }

    /// <summary>
    ///     Proves that the <c>card#name</c> form and the scope card limit the search to a subtree.
    /// </summary>
    [Fact]
    public void HelpCatalog_FindExamples_CardFormAndScope_LimitToSubtree()
    {
        // Arrange
        var catalog = CatalogBuilding.FromFiles(
            ("a.md", SourceReading.Card(extraFrontMatter: Example("one"))),
            ("a/b.md", SourceReading.Card(extraFrontMatter: Example("two"))),
            ("c.md", SourceReading.Card(extraFrontMatter: Example("two"))));

        // Act / Assert
        Assert.Equal(["a/b"], catalog.FindExamples("a#two").Select(reference => reference.CardId));
        Assert.Equal(["a/b"], catalog.FindExamples("two", "a").Select(reference => reference.CardId));
        Assert.Equal(["a/b", "c"], catalog.FindExamples("two").Select(reference => reference.CardId));
        Assert.Empty(catalog.FindExamples("unknown#two"));
        Assert.Empty(catalog.FindExamples("two", "unknown"));
        Assert.Empty(catalog.FindExamples("c#one"));
    }

    /// <summary>
    ///     Proves that a single example search starts at the card and goes depth first.
    /// </summary>
    [Fact]
    public void HelpCatalog_FindExample_SearchesCardThenSubtreeDepthFirst()
    {
        // Arrange: the example name exists in the card, in a child, and in a grandchild
        var catalog = CatalogBuilding.FromFiles(
            ("a.md", SourceReading.Card()),
            ("a/b.md", SourceReading.Card(extraFrontMatter: Example("same"))),
            ("a/b/c.md", SourceReading.Card(extraFrontMatter: Example("deep"))));
        var root = catalog.Find("a")!;

        // Act / Assert
        Assert.Equal("a/b", catalog.FindExample(root, "same")?.CardId);
        Assert.Equal("a/b/c", catalog.FindExample(root, "deep")?.CardId);
        Assert.Null(catalog.FindExample(catalog.Find("a/b/c")!, "same"));
        Assert.Null(catalog.FindExample(root, "none"));
        Assert.Throws<ArgumentNullException>(() => catalog.FindExample(null!, "same"));
        Assert.Throws<ArgumentException>(() => catalog.FindExample(root, ""));
    }

    /// <summary>
    ///     Proves that every public list of a catalog is read-only, so a cast cannot change the catalog.
    /// </summary>
    [Fact]
    public void HelpCatalog_Lists_AreReadOnlyCollections()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var guide = catalog.Find("guide")!;

        // Act / Assert: none of the lists can be written through the collection interfaces
        Assert.True(((ICollection<HelpCard>)catalog.Cards).IsReadOnly);
        Assert.True(((ICollection<HelpCard>)catalog.Roots).IsReadOnly);
        Assert.True(((ICollection<string>)catalog.TagNames).IsReadOnly);
        Assert.True(((ICollection<string>)guide.Tags).IsReadOnly);
        Assert.True(((ICollection<string>)guide.ChildIds).IsReadOnly);
        Assert.True(((ICollection<HelpExample>)guide.Examples).IsReadOnly);
        Assert.True(((IDictionary<string, string>)guide.Extensions).IsReadOnly);
        Assert.True(((ICollection<HelpProblem>)catalog.Diagnostics).IsReadOnly);
    }

    /// <summary>
    ///     Builds the front matter of a card with one command example.
    /// </summary>
    /// <param name="name">The name of the example.</param>
    /// <returns>The front-matter lines, each ending with a line feed.</returns>
    private static string Example(string name)
    {
        return $"examples:\n  - name: {name}\n    kind: command\n    language: shell\n    content: run {name}\n    description: Example {name}.\n";
    }

    /// <summary>
    ///     Proves that data that was composed beforehand gives a catalog with the cards of a created catalog.
    /// </summary>
    [Fact]
    public void HelpCatalog_FromData_ComposedData_ShowsTheCardsOfCreate()
    {
        // Arrange
        var tokens = new Dictionary<string, string> { ["limit"] = "42" };
        var created = CatalogBuilding.Create(CatalogBuilding.Fixture("simple", tokens));
        var data = CatalogBuilding.Compose(CatalogBuilding.Fixture("simple", tokens)).Data;

        // Act
        var catalog = HelpCatalog.FromData(data!);

        // Assert
        Assert.Equal(created.Cards.Select(card => card.Id), catalog.Cards.Select(card => card.Id));
        Assert.Equal(created.Cards.Select(card => card.Body), catalog.Cards.Select(card => card.Body));
        Assert.Equal(created.Roots.Select(card => card.Id), catalog.Roots.Select(card => card.Id));
    }
}
