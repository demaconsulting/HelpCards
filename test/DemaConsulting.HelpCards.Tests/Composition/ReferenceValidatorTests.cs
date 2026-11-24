using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Composition;

/// <summary>
///     Unit tests for <see cref="DemaConsulting.HelpCards.Composition.ReferenceValidator"/>.
/// </summary>
/// <remarks>
///     The validator works on the definitions that the loader produces and on the nodes of the tree, so these
///     tests compose small in-memory sources and look at the problems and the resolved data.
/// </remarks>
public class ReferenceValidatorTests
{
    /// <summary>
    ///     Builds the front matter of a card with command examples of the given names.
    /// </summary>
    /// <param name="names">The example names.</param>
    /// <returns>The front-matter lines.</returns>
    private static string Examples(params string[] names)
    {
        return "examples:\n" + string.Concat(names.Select(name =>
            $"  - name: {name}\n    kind: command\n    language: shell\n    content: run {name}\n    description: Example {name}.\n"));
    }

    /// <summary>
    ///     Proves that a link to a card that does not exist is reported at the line of the link.
    /// </summary>
    [Fact]
    public void ReferenceValidator_ResolveBody_UnknownCard_ReportsLinkTargetAtLine()
    {
        // Arrange / Act
        var errors = CatalogBuilding.Errors(("a.md", SourceReading.Card(body: "One.\nSee [[nope/x]] here.")));

        // Assert
        var problem = Assert.Single(errors);
        Assert.Equal(("link-target", "a.md", 6), (problem.RuleId, problem.File, problem.Line));
        Assert.Contains("[[nope/x]]", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that forward references are as good as backward references.
    /// </summary>
    [Fact]
    public void ReferenceValidator_ResolveBody_ForwardAndBackwardLinks_AreResolved()
    {
        // Arrange / Act
        var result = CatalogBuilding.Compose(
            ("a.md", SourceReading.Card(body: "Forward [[z]].")),
            ("z.md", SourceReading.Card(body: "Backward [[a]].")));

        // Assert
        Assert.Empty(result.Errors);
        Assert.Equal("z", result.Data!.Find("a")!.Body[1].Link!.CardId);
        Assert.Equal("a", result.Data.Find("z")!.Body[1].Link!.CardId);
    }

    /// <summary>
    ///     Proves that an example link names the card whose subtree holds the example and finds it depth first.
    /// </summary>
    [Fact]
    public void ReferenceValidator_ResolveBody_ExampleLinks_FindOwnerInSubtree()
    {
        // Arrange
        var result = CatalogBuilding.Compose(
            ("a.md", SourceReading.Card(extraFrontMatter: Examples("own"), body: "[[a#own]] [[a#deep]] [[#own]] [[#deep]]")),
            ("a/b.md", SourceReading.Card(extraFrontMatter: Examples("deep"))));

        // Act
        var links = result.Data!.Find("a")!.Body.Where(part => part.Link is not null).Select(part => part.Link!).ToList();

        // Assert: card id, example name, and owner
        Assert.Empty(result.Errors);
        Assert.Equal(
            [("a", "own", "a"), ("a", "deep", "a/b"), (null, "own", "a"), (null, "deep", "a/b")],
            links.Select(link => (link.CardId, link.ExampleName, link.ExampleCardId)));
    }

    /// <summary>
    ///     Proves that an example that is not in the subtree of the card is reported as an example target problem.
    /// </summary>
    [Fact]
    public void ReferenceValidator_ResolveBody_ExampleNotInSubtree_ReportsExampleTarget()
    {
        // Arrange: the example exists, but in a sibling tree
        var errors = CatalogBuilding.Errors(
            ("a.md", SourceReading.Card(body: "[[b#thing]] and [[#thing]].")),
            ("b.md", SourceReading.Card(extraFrontMatter: Examples("thing"))),
            ("c.md", SourceReading.Card(body: "[[b#nothing]]")));

        // Act / Assert: [[b#thing]] is fine; [[#thing]] looks only below a; [[b#nothing]] is missing
        Assert.Equal(
            [("a.md", "example-target", 5), ("c.md", "example-target", 5)],
            errors.Select(problem => (problem.File, problem.RuleId, problem.Line)));
    }

    /// <summary>
    ///     Proves that an external link to the present source resolves like a local link, and a wrong card is reported.
    /// </summary>
    [Fact]
    public void ReferenceValidator_ResolveBody_ExternalLinkToOwnSource_ResolvesLikeLocal()
    {
        // Arrange
        var good = CatalogBuilding.Compose(
            ("a.md", SourceReading.Card(body: "[[test:b|the b card]]")),
            ("b.md", SourceReading.Card()));
        var bad = CatalogBuilding.Errors(("a.md", SourceReading.Card(body: "[[test:missing|the card]]")));

        // Act
        var link = good.Data!.Find("a")!.Body.Single().Link!;

        // Assert
        Assert.Equal(("b", "the b card", "test"), (link.CardId, link.Text, link.SourceName));
        Assert.Equal("link-target", Assert.Single(bad).RuleId);
    }

    /// <summary>
    ///     Proves that an external link to a source that is not present degrades to text and leaves an informational problem.
    /// </summary>
    [Fact]
    public void ReferenceValidator_ResolveBody_ExternalLinkToAbsentSource_DegradesToText()
    {
        // Arrange / Act
        var result = CatalogBuilding.Compose(("a.md", SourceReading.Card(body: "Use [[other:x#y|the other thing]] now.")));

        // Assert
        Assert.Empty(result.Errors);
        Assert.Equal(["Use ", "the other thing", " now."], result.Data!.Find("a")!.Body.Select(part => part.Text));
        Assert.Equal("external-source-absent", Assert.Single(result.Problems).RuleId);
    }

    /// <summary>
    ///     Proves that see-also entries are resolved in order without duplicates and unknown ones are reported.
    /// </summary>
    [Fact]
    public void ReferenceValidator_ResolveSeeAlso_Entries_AreResolvedAndDeduplicated()
    {
        // Arrange
        var good = CatalogBuilding.Compose(
            ("a.md", SourceReading.Card(extraFrontMatter: "see-also:\n  - c\n  - b\n  - c\n  - other:x\n")),
            ("b.md", SourceReading.Card()),
            ("c.md", SourceReading.Card()));
        var bad = CatalogBuilding.Errors(("a.md", SourceReading.Card(extraFrontMatter: "see-also:\n  - nope\n")));

        // Act / Assert
        Assert.Equal(["c", "b"], good.Data!.Find("a")!.SeeAlsoIds);
        Assert.Equal("external-source-absent", Assert.Single(good.Problems).RuleId);
        var problem = Assert.Single(bad);
        Assert.Equal(("see-also-target", "a.md", 5), (problem.RuleId, problem.File, problem.Line));
    }

    /// <summary>
    ///     Proves that references to a file that failed to load are not reported again.
    /// </summary>
    [Fact]
    public void ReferenceValidator_References_ToFailedFile_AreSuppressed()
    {
        // Arrange
        var errors = CatalogBuilding.Errors(
            ("a.md", "---\ntitle: [x]\nsummary: S.\n---\nBody."),
            ("b.md", SourceReading.Card(extraFrontMatter: "see-also:\n  - a\n", body: "See [[a]] and [[a#x]].")));

        // Act / Assert
        Assert.Equal("syntax", Assert.Single(errors).RuleId);
    }

    /// <summary>
    ///     Proves that an example name used twice in one scope is reported at the second use.
    /// </summary>
    [Fact]
    public void ReferenceValidator_CheckExampleNames_SameScope_ReportsDuplicateAtSecondUse()
    {
        // Arrange: a and a/b share the scope a
        var errors = CatalogBuilding.Errors(
            ("a.md", SourceReading.Card(extraFrontMatter: Examples("same"))),
            ("a/b.md", SourceReading.Card(extraFrontMatter: Examples("same"))));

        // Act / Assert
        var problem = Assert.Single(errors);
        Assert.Equal(("duplicate-example", "a/b.md", 5), (problem.RuleId, problem.File, problem.Line));
        Assert.Contains("a.md", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the same example name in different scopes is allowed.
    /// </summary>
    [Fact]
    public void ReferenceValidator_CheckExampleNames_DifferentScopes_AreAllowed()
    {
        // Arrange / Act
        var result = CatalogBuilding.Compose(
            ("a.md", SourceReading.Card(extraFrontMatter: Examples("same"))),
            ("b.md", SourceReading.Card(extraFrontMatter: Examples("same"))));

        // Assert
        Assert.Empty(result.Errors);
    }

    /// <summary>
    ///     Proves that an alias that equals a sibling name or alias is reported at the alias, ignoring case.
    /// </summary>
    [Fact]
    public void ReferenceValidator_CheckSiblingNames_AliasEqualsSibling_ReportsNameDuplicate()
    {
        // Arrange
        var errors = CatalogBuilding.Errors(
            ("a.md", SourceReading.Card(extraFrontMatter: "aliases:\n  - shared\n")),
            ("b.md", SourceReading.Card(extraFrontMatter: "aliases:\n  - shared\n  - a\n")));

        // Act / Assert: b repeats the alias of a (line 5) and the name of a (line 6)
        Assert.Equal(
            [("b.md", 5), ("b.md", 6)],
            errors.Select(problem => (problem.File, problem.Line)));
        Assert.All(errors, problem => Assert.Equal("name-duplicate", problem.RuleId));
    }

    /// <summary>
    ///     Proves that two aliases of one card that are equal are reported, and that the name of a card is claimed too.
    /// </summary>
    [Fact]
    public void ReferenceValidator_CheckSiblingNames_OwnAliasesAndNames_AreChecked()
    {
        // Arrange: a card whose alias repeats its own name
        var errors = CatalogBuilding.Errors(("a.md", SourceReading.Card(extraFrontMatter: "aliases:\n  - a\n")));

        // Act / Assert
        var problem = Assert.Single(errors);
        Assert.Equal(("name-duplicate", "a.md", 5), (problem.RuleId, problem.File, problem.Line));
    }

    /// <summary>
    ///     Proves that the same name or alias below different parents is allowed.
    /// </summary>
    [Fact]
    public void ReferenceValidator_CheckSiblingNames_DifferentParents_AreAllowed()
    {
        // Arrange / Act
        var result = CatalogBuilding.Compose(
            ("a.md", SourceReading.Card(extraFrontMatter: "aliases:\n  - same\n")),
            ("b.md", SourceReading.Card(extraFrontMatter: "aliases:\n  - other\n")),
            ("a/x.md", SourceReading.Card(extraFrontMatter: "aliases:\n  - same\n")),
            ("b/x.md", SourceReading.Card(extraFrontMatter: "aliases:\n  - same\n")));

        // Assert
        Assert.Empty(result.Errors);
    }

    /// <summary>
    ///     Proves that a link with a source name resolves in that source even when the importer has a card of the same
    ///     id, and that a bare link and a bare see-also entry resolve to the importer's own cards.
    /// </summary>
    [Fact]
    public void ReferenceValidator_ResolveBody_ImportedSource_ResolvesBareAndQualifiedLinks()
    {
        // Arrange
        var lib = CatalogBuilding.Named("lib", ("x.md", SourceReading.Card(title: "Lib X")));
        var app = CatalogBuilding.Named(
            "app",
            ("x.md", SourceReading.Card(title: "App X")),
            ("y.md", SourceReading.Card(body: "See [[lib:x|the lib]]. [[z]] too.", extraFrontMatter: "see-also:\n  - z\n")),
            ("z.md", SourceReading.Card())).Imports(lib);

        // Act
        var result = CatalogBuilding.Compose(builder => builder.AddSource(lib).AddMountedSource(app));

        // Assert: the qualified link names the library card; the bare ones are the importer's own
        Assert.Empty(result.Errors);
        var body = result.Data!.Find("app/y")!.Body;
        Assert.Equal(("x", "lib", "the lib"), (body[1].Link!.CardId, body[1].Link!.SourceName, body[1].Link!.Text));
        Assert.Equal("app/z", body[3].Link!.CardId);
        Assert.Equal(["app/z"], result.Data.Find("app/y")!.SeeAlsoIds);
    }

    /// <summary>
    ///     Proves that a bare see-also entry that names a card of two sources is ambiguous and a qualified one is not.
    /// </summary>
    [Fact]
    public void ReferenceValidator_ResolveSeeAlso_BareIdInTwoSources_ReportsAmbiguousReference()
    {
        // Arrange
        var lib = CatalogBuilding.Named("lib", ("x.md", SourceReading.Card()));
        var app = CatalogBuilding.Named(
            "app",
            ("x.md", SourceReading.Card()),
            ("a.md", SourceReading.Card(extraFrontMatter: "see-also:\n  - x\n  - lib:x\n"))).Imports(lib);

        // Act
        var result = CatalogBuilding.Compose(builder => builder.AddSource(lib).AddMountedSource(app));

        // Assert
        var problem = Assert.Single(result.Errors);
        Assert.Equal(("ambiguous-reference", "a.md", 5), (problem.RuleId, problem.File, problem.Line));
        Assert.Contains("source:x", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that an example name added by an overlay is checked in the scope of the card it extends and that the
    ///     problem is reported in the overlay file.
    /// </summary>
    [Fact]
    public void ReferenceValidator_CheckExampleNames_OverlayRepeatsName_IsReportedInOverlayFile()
    {
        // Arrange / Act
        var result = CatalogBuilding.Compose(
            ("a.md", SourceReading.Card(extraFrontMatter: Examples("same"))),
            ("a/b.md", SourceReading.Card()),
            ("o.md", $"---\nextends: a/b\n{Examples("same")}---\n"));

        // Assert
        var problem = Assert.Single(result.Errors);
        Assert.Equal(("duplicate-example", "o.md", 4), (problem.RuleId, problem.File, problem.Line));
    }
}
