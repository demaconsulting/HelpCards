using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Composition;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Composition;

/// <summary>
///     Unit tests for <see cref="HelpCatalogBuilder"/>: configuration and the composition pipeline.
/// </summary>
public class HelpCatalogBuilderTests
{
    /// <summary>
    ///     A source with one valid card, for tests that only configure the builder.
    /// </summary>
    private static HelpSource OneCard()
    {
        return HelpSource.FromFiles(SourceReading.Files(("a.md", SourceReading.Card())), "test");
    }

    /// <summary>
    ///     Proves that a null source is an argument error.
    /// </summary>
    [Fact]
    public void HelpCatalogBuilder_AddSource_NullSource_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new HelpCatalogBuilder();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => builder.AddSource(null!));
    }

    /// <summary>
    ///     Proves that a second source, flat or mounted, is accepted and both compose into one catalog.
    /// </summary>
    [Fact]
    public void HelpCatalogBuilder_AddSource_SecondSource_ComposesBothSources()
    {
        // Arrange
        var other = HelpSource.FromFiles(SourceReading.Files(("b.md", SourceReading.Card())), "other");

        // Act
        var result = new HelpCatalogBuilder().AddSource(OneCard()).AddMountedSource(other).Compose();

        // Assert
        Assert.Empty(result.Errors);
        Assert.Equal(["a", "other", "other/b"], result.Data!.Cards.Select(node => node.Id));
    }

    /// <summary>
    ///     Proves that a source with imports is accepted and its imports are checked when the catalog is composed.
    /// </summary>
    [Fact]
    public void HelpCatalogBuilder_AddSource_SourceWithImports_IsCheckedAtComposition()
    {
        // Arrange
        var imported = OneCard();
        var importing = HelpSource.FromFiles(SourceReading.Files(("b.md", SourceReading.Card())), "importer").Imports(imported);

        // Act
        var good = new HelpCatalogBuilder().AddSource(imported).AddSource(importing).Compose();
        var missing = new HelpCatalogBuilder().AddSource(importing).Compose();

        // Assert
        Assert.Empty(good.Errors);
        Assert.Equal(["import-missing"], missing.Errors.Select(problem => problem.RuleId));
    }

    /// <summary>
    ///     Proves that a null source and an empty mount are argument errors, and that a mount may be omitted.
    /// </summary>
    [Fact]
    public void HelpCatalogBuilder_AddMountedSource_InvalidArguments_ThrowAndValidOnesChain()
    {
        // Arrange
        var builder = new HelpCatalogBuilder();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => builder.AddMountedSource(null!));
        Assert.Throws<ArgumentException>(() => builder.AddMountedSource(OneCard(), " "));
        Assert.Same(builder, builder.AddMountedSource(OneCard()));
        Assert.Same(builder, builder.AddMountedSource(OneCard(), "m"));
    }
    /// <summary>
    ///     Proves that invalid settings are argument errors at once and valid ones return the builder for chaining.
    /// </summary>
    [Fact]
    public void HelpCatalogBuilder_Settings_InvalidArguments_ThrowAndValidOnesChain()
    {
        // Arrange
        var builder = new HelpCatalogBuilder();

        // Act / Assert: invalid
        Assert.Throws<ArgumentNullException>(() => builder.Culture(null!));
        Assert.Throws<ArgumentException>(() => builder.ToolName(" "));
        Assert.Throws<ArgumentNullException>(() => builder.ToolVersion(null!));
        Assert.Throws<ArgumentNullException>(() => builder.TagVocabulary(null!));
        Assert.Throws<ArgumentException>(() => builder.TagVocabulary(["Bad Tag"]));
        Assert.Throws<ArgumentException>(() => builder.TagVocabulary(["a"], ["b"]));
        Assert.Throws<ArgumentNullException>(() => builder.ExpectedRoots(null!));
        Assert.Throws<ArgumentException>(() => builder.ExpectedRoots(["a/b"]));

        // Assert: valid calls return the same builder
        Assert.Same(builder, builder.Culture(CatalogBuilding.Culture("de")));
        Assert.Same(builder, builder.ToolName("tool"));
        Assert.Same(builder, builder.ToolVersion(string.Empty));
        Assert.Same(builder, builder.TagVocabulary(["a"], ["a"]));
        Assert.Same(builder, builder.ExpectedRoots(["a"]));
        Assert.Same(builder, builder.AddSource(OneCard()));
        Assert.Same(builder, builder.AddMountedSource(OneCard(), "mount"));
    }

    /// <summary>
    ///     Proves that composing without a source is an argument error and not a catalog problem.
    /// </summary>
    [Fact]
    public void HelpCatalogBuilder_Compose_NoSource_ThrowsArgumentException()
    {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => new HelpCatalogBuilder().Compose());
    }

    /// <summary>
    ///     Proves that a valid source composes to data and no error.
    /// </summary>
    [Fact]
    public void HelpCatalogBuilder_Compose_ValidSource_HasDataAndNoErrors()
    {
        // Arrange / Act
        var result = CatalogBuilding.Compose(("a.md", SourceReading.Card()), ("a/b.md", SourceReading.Card()));

        // Assert
        Assert.NotNull(result.Data);
        Assert.Empty(result.Errors);
        Assert.Equal(["a", "a/b"], result.Data.Cards.Select(node => node.Id));
        Assert.Equal(new HelpSourceInfo("test", null, "test", "Help cards of test.", "en", 1), Assert.Single(result.Data.Sources));
    }

    /// <summary>
    ///     Proves that the built-in tokens come from the builder, and that the tool name defaults to the source name.
    /// </summary>
    [Fact]
    public void HelpCatalogBuilder_Compose_ToolAndVersion_ReplaceBuiltInTokens()
    {
        // Arrange
        var files = ("a.md", SourceReading.Card(body: "{{tool}} {{version}}."));

        // Act
        var configured = CatalogBuilding.Compose(HelpSource.FromFiles(SourceReading.Files(files), "test"), b => b.ToolName("builder").ToolVersion("3.1"));
        var defaults = CatalogBuilding.Compose(files);

        // Assert
        Assert.Equal("builder 3.1.", PlainBody(configured.Data!, "a"));
        Assert.Equal("test .", PlainBody(defaults.Data!, "a"));
    }

    /// <summary>
    ///     Proves that a card without its parent card is reported once, and its own children are not reported.
    /// </summary>
    [Fact]
    public void HelpCatalogBuilder_Compose_MissingParent_ReportsParentMissingOnce()
    {
        // Arrange: a/b has no a, and a/b/c has the parent a/b
        var errors = CatalogBuilding.Errors(("a/b.md", SourceReading.Card()), ("a/b/c.md", SourceReading.Card()));

        // Act / Assert
        var problem = Assert.Single(errors);
        Assert.Equal(("parent-missing", "a/b.md", 0), (problem.RuleId, problem.File, problem.Line));
    }

    /// <summary>
    ///     Proves that children of a card that failed to load are dropped without a second problem.
    /// </summary>
    [Fact]
    public void HelpCatalogBuilder_Compose_FailedParent_ReportsOnlyTheFailure()
    {
        // Arrange: the parent has a flow sequence, which is a syntax error
        var errors = CatalogBuilding.Errors(
            ("a.md", "---\ntitle: [x]\nsummary: S.\n---\nBody."),
            ("a/b.md", SourceReading.Card()),
            ("c.md", SourceReading.Card(body: "See [[a]] and [[a/b]].")));

        // Act / Assert: one problem for one broken file
        var problem = Assert.Single(errors);
        Assert.Equal(("syntax", "a.md"), (problem.RuleId, problem.File));
    }

    /// <summary>
    ///     Proves that a card under a card of another source is reported once as tree-foreign and left out.
    /// </summary>
    [Fact]
    public void HelpCatalogBuilder_Compose_CardUnderForeignCard_ReportsTreeForeign()
    {
        // Arrange: the second source has a/b.md but a.md belongs to the first source
        var first = HelpSource.FromFiles(SourceReading.Files(("a.md", SourceReading.Card())), "first");
        var second = HelpSource.FromFiles(SourceReading.Files(("a/b.md", SourceReading.Card()), ("a/b/c.md", SourceReading.Card())), "second");

        // Act
        var result = new HelpCatalogBuilder().AddSource(first).AddSource(second).Compose();

        // Assert
        var problem = Assert.Single(result.Errors);
        Assert.Equal(("tree-foreign", "second", "a/b.md"), (problem.RuleId, problem.SourceName, problem.File));
        Assert.Contains("'first'", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a card with a tag outside the vocabulary is reported at the line of the tag.
    /// </summary>
    [Fact]
    public void HelpCatalogBuilder_Compose_UnknownTag_ReportsTagUnknownAtItsLine()
    {
        // Arrange
        var source = HelpSource.FromFiles(
            SourceReading.Files(("a.md", SourceReading.Card(extraFrontMatter: "tags:\n  - good\n  - bad\n"))),
            "test");

        // Act
        var result = CatalogBuilding.Compose(source, builder => builder.TagVocabulary(["good"]));

        // Assert
        var problem = Assert.Single(result.Errors);
        Assert.Equal(("tag-unknown", "a.md", 6), (problem.RuleId, problem.File, problem.Line));
        Assert.Contains("good", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a card without any required tag is reported and a card with one is accepted.
    /// </summary>
    [Fact]
    public void HelpCatalogBuilder_Compose_RequiredTag_ReportsTagRequiredForCardsWithoutOne()
    {
        // Arrange
        var source = HelpSource.FromFiles(
            SourceReading.Files(
                ("a.md", SourceReading.Card(extraFrontMatter: "tags:\n  - kind\n")),
                ("b.md", SourceReading.Card(extraFrontMatter: "tags:\n  - extra\n")),
                ("c.md", SourceReading.Card())),
            "test");

        // Act
        var result = CatalogBuilding.Compose(source, builder => builder.TagVocabulary(["kind", "extra"], ["kind"]));

        // Assert
        Assert.Equal([("tag-required", "b.md"), ("tag-required", "c.md")], result.Errors.Select(problem => (problem.RuleId, problem.File)));
    }

    /// <summary>
    ///     Proves that expected roots must equal the actual roots, naming what is missing and unexpected.
    /// </summary>
    [Fact]
    public void HelpCatalogBuilder_Compose_ExpectedRootsDiffer_ReportsMissingAndUnexpected()
    {
        // Arrange
        var source = HelpSource.FromFiles(SourceReading.Files(("a.md", SourceReading.Card()), ("b.md", SourceReading.Card()), ("a/c.md", SourceReading.Card())), "test");

        // Act
        var mismatch = CatalogBuilding.Compose(source, builder => builder.ExpectedRoots(["a", "z"]));
        var match = CatalogBuilding.Compose(source, builder => builder.ExpectedRoots(["b", "a"]));

        // Assert
        var problem = Assert.Single(mismatch.Errors);
        Assert.Equal("roots-mismatch", problem.RuleId);
        Assert.Contains("missing: z", problem.Message, StringComparison.Ordinal);
        Assert.Contains("unexpected: b", problem.Message, StringComparison.Ordinal);
        Assert.Empty(match.Errors);
    }

    /// <summary>
    ///     Proves that a root that failed to load still counts as present, so only the failure is reported.
    /// </summary>
    [Fact]
    public void HelpCatalogBuilder_Compose_ExpectedRootFailedToLoad_ReportsOnlyTheFailure()
    {
        // Arrange
        var source = HelpSource.FromFiles(SourceReading.Files(("a.md", "---\ntitle: [x]\n---\nBody."), ("b.md", SourceReading.Card())), "test");

        // Act
        var result = CatalogBuilding.Compose(source, builder => builder.ExpectedRoots(["a", "b"]));

        // Assert
        Assert.Equal(["syntax"], result.Errors.Select(problem => problem.RuleId));
    }

    /// <summary>
    ///     Proves that an unreadable source does not add a roots mismatch to its own problem.
    /// </summary>
    [Fact]
    public void HelpCatalogBuilder_Compose_UnsupportedSchema_DoesNotReportRootsMismatch()
    {
        // Arrange
        var source = HelpSource.FromFiles(SourceReading.Files(("_source.md", "---\nname: test\nschema: 9\n---\nBody."), ("a.md", SourceReading.Card())), "test");

        // Act
        var result = CatalogBuilding.Compose(source, builder => builder.ExpectedRoots(["a"]));

        // Assert
        Assert.Equal(["schema-unsupported"], result.Errors.Select(problem => problem.RuleId));
    }

    /// <summary>
    ///     Proves that all defects of several files are reported together in the order file, line, and rule.
    /// </summary>
    [Fact]
    public void HelpCatalogBuilder_Compose_SeveralDefects_AreAllReportedInOrder()
    {
        // Arrange: a bad link in b, an unknown key in a, a missing example link target in c
        var result = CatalogBuilding.Compose(
            ("c.md", SourceReading.Card(body: "See [[#nothing]].")),
            ("b.md", SourceReading.Card(body: "See [[missing]].")),
            ("a.md", SourceReading.Card(extraFrontMatter: "tag: x\n")));

        // Assert
        Assert.Null(result.Data);
        Assert.Equal(
            [("a.md", "key-unknown"), ("b.md", "link-target"), ("c.md", "example-target")],
            result.Errors.Select(problem => (problem.File, problem.RuleId)));
        Assert.Equal(result.Errors, result.Problems);
    }

    /// <summary>
    ///     Proves that an overlay replaces the title and appends its body to the card it extends.
    /// </summary>
    [Fact]
    public void HelpCatalogBuilder_Compose_Overlay_ReplacesTitleAndAppendsBody()
    {
        // Arrange
        var result = CatalogBuilding.Compose(
            ("a.md", SourceReading.Card(title: "Original", body: "Text.")),
            ("o.md", "---\nextends: a\ntitle: Replaced\n---\nMore text."));

        // Act
        var node = result.Data!.Find("a")!;

        // Assert: the overlay is not a card of its own
        Assert.Empty(result.Errors);
        Assert.Equal(["a"], result.Data.Cards.Select(card => card.Id));
        Assert.Equal("Replaced", node.Title);
        Assert.Equal("Text.\n\nMore text.", PlainBody(result.Data, "a"));
    }
    /// <summary>
    ///     Proves that informational problems are kept in the result and in the data when there is no error.
    /// </summary>
    [Fact]
    public void HelpCatalogBuilder_Compose_AbsentExternalSource_IsInfoAndDataKeepsIt()
    {
        // Arrange / Act
        var result = CatalogBuilding.Compose(("a.md", SourceReading.Card(body: "See [[other:x|the text]].")));

        // Assert
        Assert.Empty(result.Errors);
        var info = Assert.Single(result.Problems);
        Assert.Equal((HelpSeverity.Info, "external-source-absent", "a.md", 5), (info.Severity, info.RuleId, info.File, info.Line));
        Assert.Equal(result.Problems, result.Data!.Diagnostics);
        Assert.Equal("See the text.", PlainBody(result.Data, "a"));
    }

    /// <summary>
    ///     Proves that the culture of the builder selects the translation.
    /// </summary>
    [Fact]
    public void HelpCatalogBuilder_Compose_Culture_SelectsTranslation()
    {
        // Arrange
        var source = HelpSource.FromFiles(
            SourceReading.Files(
                ("a.md", SourceReading.Card(title: "Neutral")),
                ("a.de.md", SourceReading.Card(title: "German", body: "Translated."))),
            "test");

        // Act
        var neutral = CatalogBuilding.Compose(source).Data!;
        var german = CatalogBuilding.Compose(source, builder => builder.Culture(CatalogBuilding.Culture("de-DE"))).Data!;

        // Assert
        Assert.Equal(("Neutral", "en"), (neutral.Find("a")!.Title, neutral.Find("a")!.Culture));
        Assert.Equal(("German", "de"), (german.Find("a")!.Title, german.Find("a")!.Culture));
        Assert.Equal("de-DE", german.Culture.Name);
    }

    /// <summary>
    ///     Proves that the builder lists the sources that were added, in order, for the validator.
    /// </summary>
    [Fact]
    public void HelpCatalogBuilder_Sources_AfterAddSource_ListsTheSource()
    {
        // Arrange
        var builder = new HelpCatalogBuilder();
        var source = HelpSource.FromFiles(SourceReading.Files(("a.md", SourceReading.Card())), "test");

        // Act
        var before = builder.Sources.Count;
        builder.AddSource(source);

        // Assert
        Assert.Equal(0, before);
        Assert.Same(source, Assert.Single(builder.Sources));
    }

    /// <summary>
    ///     Writes the text parts of a node body, with links left out.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="id">The id of the card.</param>
    /// <returns>The text parts joined.</returns>
    private static string PlainBody(CatalogData data, string id)
    {
        return string.Concat(data.Find(id)!.Body.Select(part => part.Text ?? "<link>"));
    }
}
