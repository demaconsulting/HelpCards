using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Composition;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Composition;

/// <summary>
///     Subsystem tests for Composition: the builder, the reference validator, the sibling order, and the problem
///     collector working together on whole sources.
/// </summary>
/// <remarks>
///     These tests use the Composition units through the builder, with the Loading subsystem to read sources and
///     the frozen data and problem model of the Catalog subsystem as documented in the subsystem design, and nothing
///     else. They compose the embedded fixtures and in-memory sources and assert on the data and problems.
/// </remarks>
public class CompositionTests
{
    /// <summary>
    ///     Proves that a whole source composes into a frozen, ordered tree with resolved references.
    /// </summary>
    [Fact]
    public void Composition_Pipeline_SimpleFixture_ProducesFrozenTreeWithResolvedReferences()
    {
        // Arrange / Act
        var result = CatalogBuilding.Compose(CatalogBuilding.Fixture("simple", new Dictionary<string, string> { ["limit"] = "42" }));
        var data = result.Data!;

        // Assert: tree, parents, children in sibling order, resolved see-also and examples
        Assert.Empty(result.Errors);
        Assert.Equal(["guide", "guide/setup", "guide/usage", "reference", "reference/limits"], data.Cards.Select(node => node.Id));
        Assert.Equal(["guide/setup", "guide/usage"], data.Find("guide")!.ChildIds);
        Assert.Equal("guide", data.Find("guide/usage")!.ParentId);
        Assert.Equal(["reference/limits"], data.Find("guide")!.SeeAlsoIds);
        Assert.Equal(["run", "settings", "fragment"], data.Find("guide")!.Examples.Select(example => example.Name));
        Assert.Equal(["external-source-absent", "external-source-absent"], data.Diagnostics.Select(problem => problem.RuleId));
    }

    /// <summary>
    ///     Proves that siblings follow the one comparator: the children list, then order values, then name.
    /// </summary>
    [Fact]
    public void Composition_Ordering_OrderFixture_FollowsChildrenListThenOrderThenName()
    {
        // Arrange / Act
        var data = CatalogBuilding.Compose(CatalogBuilding.Fixture("order")).Data!;

        // Assert: roots by the list of _source.md, then order five (tie by name), then unordered by name
        Assert.Equal(["zeta", "alpha", "beta", "delta", "eta", "gamma"], data.RootIds);
        Assert.Equal(["alpha/three", "alpha/two", "alpha/one", "alpha/four"], data.Find("alpha")!.ChildIds);
        Assert.Equal(
            ["zeta", "alpha", "alpha/three", "alpha/two", "alpha/one", "alpha/four", "beta", "delta", "eta", "gamma"],
            data.Cards.Select(node => node.Id));
    }

    /// <summary>
    ///     Proves that every defect of several files is collected, sorted, and free of follow-up problems.
    /// </summary>
    [Fact]
    public void Composition_Failures_SeveralDefects_AreCollectedOnceAndInOrder()
    {
        // Arrange: a syntax error that others link to, a missing parent, a bad tag, a bad link, a duplicate alias
        var source = HelpSource.FromFiles(
            SourceReading.Files(
                ("a.md", "---\ntitle: [x]\n---\nBody."),
                ("b.md", SourceReading.Card(body: "See [[a]] and [[a#x]] and [[nowhere]].", extraFrontMatter: "tags:\n  - bad\naliases:\n  - dup\n")),
                ("c.md", SourceReading.Card(extraFrontMatter: "aliases:\n  - dup\n")),
                ("x/y.md", SourceReading.Card()),
                ("x/y/z.md", SourceReading.Card())),
            "test");

        // Act
        var result = CatalogBuilding.Compose(source, builder => builder.TagVocabulary(["good"]));

        // Assert: exactly one problem per defect, in file and line order
        Assert.Null(result.Data);
        Assert.Equal(
            [("a.md", "syntax"), ("b.md", "tag-unknown"), ("b.md", "link-target"), ("c.md", "name-duplicate"), ("x/y.md", "parent-missing")],
            result.Errors.Select(problem => (problem.File, problem.RuleId)));
    }

    /// <summary>
    ///     Proves that forward and backward links, example links, external links, and see-also entries all resolve.
    /// </summary>
    [Fact]
    public void Composition_References_ForwardExampleExternalAndSeeAlso_AreResolved()
    {
        // Arrange / Act
        var result = CatalogBuilding.Compose(
            ("a.md", SourceReading.Card(extraFrontMatter: "see-also:\n  - z/y\n", body: "[[z/y#ex]] [[test:z|text]] [[#ex]] [[nowhere:q|degrades]]")),
            ("z.md", SourceReading.Card()),
            ("z/y.md", SourceReading.Card(extraFrontMatter: "examples:\n  - name: ex\n    kind: command\n    language: shell\n    content: run\n    description: Run.\n")));

        // Assert: the example link in the subtree resolves; [[#ex]] in a looks only in a, so it fails
        Assert.Equal(["example-target"], result.Errors.Select(problem => problem.RuleId));
        var fixedResult = CatalogBuilding.Compose(
            ("a.md", SourceReading.Card(extraFrontMatter: "see-also:\n  - z/y\n", body: "[[z/y#ex]] [[test:z|text]] [[nowhere:q|degrades]]")),
            ("z.md", SourceReading.Card()),
            ("z/y.md", SourceReading.Card(extraFrontMatter: "examples:\n  - name: ex\n    kind: command\n    language: shell\n    content: run\n    description: Run.\n")));
        Assert.Empty(fixedResult.Errors);
        Assert.Equal(["z/y"], fixedResult.Data!.Find("a")!.SeeAlsoIds);
    }

    /// <summary>
    ///     Proves that names, aliases, example names, and roots are checked against their rules.
    /// </summary>
    [Fact]
    public void Composition_Uniqueness_NamesAliasesExamplesAndRoots_AreChecked()
    {
        // Arrange
        var example = "examples:\n  - name: same\n    kind: command\n    language: shell\n    content: run\n    description: Run.\n";
        var source = HelpSource.FromFiles(
            SourceReading.Files(
                ("a.md", SourceReading.Card(extraFrontMatter: "aliases:\n  - b\n" + example)),
                ("b.md", SourceReading.Card()),
                ("a/c.md", SourceReading.Card(extraFrontMatter: example))),
            "test");

        // Act
        var result = CatalogBuilding.Compose(source, builder => builder.ExpectedRoots(["a"]));

        // Assert: ordinal file order puts the problem without a file first
        Assert.Equal(
            [(string.Empty, "roots-mismatch"), ("a/c.md", "duplicate-example"), ("b.md", "name-duplicate")],
            result.Errors.Select(problem => (problem.File, problem.RuleId)));
    }

    /// <summary>
    ///     Proves that this release composes one source and does not apply overlays.
    /// </summary>
    [Fact]
    public void Composition_SingleSource_SecondSourceImportsAndOverlays_AreHandledAsDocumented()
    {
        // Arrange
        var source = HelpSource.FromFiles(SourceReading.Files(("a.md", SourceReading.Card()), ("o.md", "---\nextends: a\n---\nMore.")), "test");
        var builder = new HelpCatalogBuilder().AddSource(source);

        // Act / Assert: a second source and an import are argument errors; an overlay is read and left unapplied
        Assert.Throws<ArgumentException>(() => builder.AddSource(source));
        Assert.Throws<ArgumentException>(() => new HelpCatalogBuilder().AddSource(source.Imports(source.WithName("other"))));
        var data = builder.Compose().Data!;
        Assert.Equal(["a"], data.Cards.Select(node => node.Id));
        Assert.Equal("Body text.", string.Concat(data.Find("a")!.Body.Select(part => part.Text)));
    }

    /// <summary>
    ///     Proves that composition reads each file once and selects translations by the configured culture.
    /// </summary>
    [Fact]
    public void Composition_Reading_EachFileOnce_AndCultureSelectsTranslation()
    {
        // Arrange
        var provider = new CountingFileProvider(SourceReading.Files(
            ("a.md", SourceReading.Card(title: "Neutral")),
            ("a.de.md", SourceReading.Card(title: "German", body: "German text."))));
        var source = HelpSource.FromProvider(provider, "test");

        // Act
        var neutral = CatalogBuilding.Compose(source).Data!;
        var german = CatalogBuilding.Compose(source, builder => builder.Culture(CatalogBuilding.Culture("de"))).Data!;

        // Assert: two files per composition, listed once per composition
        Assert.Equal(("Neutral", "German"), (neutral.Find("a")!.Title, german.Find("a")!.Title));
        Assert.Equal(2, provider.ListCount);
        Assert.Equal(4, provider.ReadCount);
    }
}
