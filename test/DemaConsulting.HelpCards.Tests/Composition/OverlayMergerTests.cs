using DemaConsulting.HelpCards.Composition;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Composition;

/// <summary>
///     Unit tests for <see cref="OverlayMerger"/>: how overlays are resolved and merged into the cards they extend.
/// </summary>
/// <remarks>
///     The merger works on the placed cards of a registry, so these tests compose small in-memory sources through
///     the builder and look at the merged data and the problems.
/// </remarks>
public class OverlayMergerTests
{
    /// <summary>
    ///     Writes an overlay file.
    /// </summary>
    /// <param name="extends">The value of <c>extends</c>.</param>
    /// <param name="extra">Further front-matter lines, each ending with a line feed.</param>
    /// <param name="body">The body.</param>
    /// <returns>The text of the file.</returns>
    private static string Overlay(string extends, string extra = "", string body = "")
    {
        return $"---\nextends: {extends}\n{extra}---\n{body}";
    }

    /// <summary>
    ///     Proves that title, summary, body, see-also entries, examples, and extension keys of an overlay are merged.
    /// </summary>
    [Fact]
    public void OverlayMerger_Apply_AllKeys_MergeIntoTarget()
    {
        // Arrange
        var example = "examples:\n  - name: extra\n    kind: command\n    language: shell\n    content: run\n    description: Run.\n";
        var result = CatalogBuilding.Compose(
            ("a.md", SourceReading.Card(extraFrontMatter: "x-one: base\n", body: "Base.")),
            ("b.md", SourceReading.Card()),
            ("o.md", Overlay("a", $"title: New\nsummary: New summary.\nsee-also:\n  - b\nx-one: over\n{example}", "More.")));

        // Act
        var node = result.Data!.Find("a")!;

        // Assert
        Assert.Empty(result.Errors);
        Assert.Equal(("New", "New summary."), (node.Title, node.Summary));
        Assert.Equal("Base.\n\nMore.", string.Concat(node.Body.Select(part => part.Text)));
        Assert.Equal(["b"], node.SeeAlsoIds);
        Assert.Equal(["extra"], node.Examples.Select(item => item.Name));
        Assert.Equal("over", node.Extensions["x-one"]);
    }

    /// <summary>
    ///     Proves that an overlay without a body adds no blank line and an overlay body to a card without one has no separator.
    /// </summary>
    [Fact]
    public void OverlayMerger_Apply_EmptyBodies_AddNoSeparator()
    {
        // Arrange
        var result = CatalogBuilding.Compose(
            ("a.md", SourceReading.Card(body: "Base.")),
            ("b.md", "---\ntitle: B\nsummary: Empty card.\n---\n"),
            ("o1.md", Overlay("a")),
            ("o2.md", Overlay("b", body: "Only overlay.")));

        // Assert
        Assert.Equal("Base.", string.Concat(result.Data!.Find("a")!.Body.Select(part => part.Text)));
        Assert.Equal("Only overlay.", string.Concat(result.Data.Find("b")!.Body.Select(part => part.Text)));
    }

    /// <summary>
    ///     Proves that an overlay whose target does not exist, or is a generated mount root, is an overlay-target error.
    /// </summary>
    [Fact]
    public void OverlayMerger_Apply_MissingTargetAndMountRoot_ReportOverlayTarget()
    {
        // Arrange
        var lib = CatalogBuilding.Named("lib", ("a.md", SourceReading.Card()));
        var app = CatalogBuilding.Named("app", ("o1.md", Overlay("lib:nope")), ("o2.md", Overlay("lib:lib")), ("o3.md", Overlay("missing"))).Imports(lib);

        // Act
        var result = CatalogBuilding.Compose(builder => builder.AddMountedSource(lib).AddSource(app));

        // Assert: reported at the line of extends
        Assert.Equal(
            [("o1.md", "overlay-target", 2), ("o2.md", "overlay-target", 2), ("o3.md", "overlay-target", 2)],
            result.Errors.Select(problem => (problem.File, problem.RuleId, problem.Line)));
    }

    /// <summary>
    ///     Proves that a source that is not registered is an error unless the overlay is optional, and a later one is an error.
    /// </summary>
    [Fact]
    public void OverlayMerger_Apply_AbsentAndLaterSource_ReportExtendsTargetUnlessOptionalAndAbsent()
    {
        // Arrange
        var later = CatalogBuilding.Named("later", ("a.md", SourceReading.Card()));
        var app = CatalogBuilding.Named(
            "app",
            ("o1.md", Overlay("absent:a")),
            ("o2.md", Overlay("absent:a", "optional: true\n")),
            ("o3.md", Overlay("later:a", "optional: true\n")));

        // Act
        var result = CatalogBuilding.Compose(builder => builder.AddSource(app).AddSource(later));

        // Assert: o1 and o3 are errors; o2 is skipped with an informational problem
        Assert.Equal(
            [("o1.md", "extends-target"), ("o3.md", "extends-target")],
            result.Errors.Select(problem => (problem.File, problem.RuleId)));
        Assert.Equal(
            [("o2.md", HelpCards.Catalog.HelpSeverity.Info, "overlay-optional-skipped")],
            result.Problems.Where(problem => problem.Severity == HelpCards.Catalog.HelpSeverity.Info).Select(problem => (problem.File, problem.Severity, problem.RuleId)));
    }

    /// <summary>
    ///     Proves that an optional overlay with a bare target that does not exist is still an error.
    /// </summary>
    [Fact]
    public void OverlayMerger_Apply_OptionalWithBareMissingTarget_IsStillAnError()
    {
        // Arrange / Act
        var result = CatalogBuilding.Compose(("a.md", SourceReading.Card()), ("o.md", Overlay("missing", "optional: true\n")));

        // Assert
        Assert.Equal(["overlay-target"], result.Errors.Select(problem => problem.RuleId));
    }

    /// <summary>
    ///     Proves that a bare overlay target that two sources of one product both have is ambiguous.
    /// </summary>
    [Fact]
    public void OverlayMerger_Apply_BareTargetInSourceAndImport_IsAmbiguousUnlessQualified()
    {
        // Arrange
        var lib = CatalogBuilding.Named("lib", ("x.md", SourceReading.Card()));
        var app = CatalogBuilding.Named("app", ("x.md", SourceReading.Card()), ("o1.md", Overlay("x")), ("o2.md", Overlay("lib:x", body: "Lib."))).Imports(lib);

        // Act
        var result = CatalogBuilding.Compose(builder => builder.AddSource(lib).AddMountedSource(app));

        // Assert: only the bare overlay is reported
        var problem = Assert.Single(result.Errors);
        Assert.Equal(("o1.md", "ambiguous-reference"), (problem.File, problem.RuleId));
    }

    /// <summary>
    ///     Proves that the body links of an overlay resolve in the scope of the overlay's source and that
    ///     <c>[[#name]]</c> refers to the subtree of the target card.
    /// </summary>
    [Fact]
    public void OverlayMerger_Apply_BodyLinks_ResolveInOverlaySourceWithTargetSubtree()
    {
        // Arrange: the example lives below the target; the card it links to only exists in the overlay's source
        var example = "examples:\n  - name: deep\n    kind: command\n    language: shell\n    content: run\n    description: Run.\n";
        var lib = CatalogBuilding.Named("lib", ("t.md", SourceReading.Card()), ("t/child.md", SourceReading.Card(extraFrontMatter: example)));
        var app = CatalogBuilding.Named("app", ("mine.md", SourceReading.Card()), ("o.md", Overlay("lib:t", body: "See [[mine]] and [[#deep]]."))).Imports(lib);

        // Act
        var result = CatalogBuilding.Compose(builder => builder.AddSource(lib).AddSource(app));

        // Assert
        Assert.Empty(result.Errors);
        var links = result.Data!.Find("t")!.Body.Where(part => part.Link is not null).Select(part => part.Link!).ToList();
        Assert.Equal(("mine", null), (links[0].CardId, links[0].ExampleName));
        Assert.Equal((null, "deep", "t/child"), (links[1].CardId, links[1].ExampleName, links[1].ExampleCardId));
    }

    /// <summary>
    ///     Proves that an overlay link problem is reported in the file of the overlay.
    /// </summary>
    [Fact]
    public void OverlayMerger_Apply_BrokenLinkInOverlayBody_IsReportedInOverlayFile()
    {
        // Arrange / Act
        var result = CatalogBuilding.Compose(("a.md", SourceReading.Card()), ("o.md", Overlay("a", body: "See [[missing]].")));

        // Assert
        var problem = Assert.Single(result.Errors);
        Assert.Equal(("o.md", "link-target", 4), (problem.File, problem.RuleId, problem.Line));
    }

    /// <summary>
    ///     Proves that null arguments are argument errors.
    /// </summary>
    [Fact]
    public void OverlayMerger_Apply_NullArguments_ThrowArgumentNullException()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => OverlayMerger.Apply(null!, new ProblemCollector()));
    }

    /// <summary>
    ///     Proves that the overlays of one source apply in ordinal file order, not in the order the files were listed.
    /// </summary>
    [Fact]
    public void OverlayMerger_Apply_OverlaysOfOneSource_ApplyInOrdinalFileOrder()
    {
        // Arrange
        var result = CatalogBuilding.Compose(
            ("a.md", SourceReading.Card(title: "Base", body: "Base.")),
            ("o-b.md", Overlay("a", "title: B\n", "B.")),
            ("o-a.md", Overlay("a", "title: A\n", "A.")));

        // Act
        var node = result.Data!.Find("a")!;

        // Assert: the last overlay in file order wins the title and its body comes last
        Assert.Equal("B", node.Title);
        Assert.Equal("Base.\n\nA.\n\nB.", string.Concat(node.Body.Select(part => part.Text)));
    }
}
