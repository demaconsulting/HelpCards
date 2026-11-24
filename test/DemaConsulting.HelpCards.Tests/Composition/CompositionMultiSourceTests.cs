using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Composition;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Presentation;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Composition;

/// <summary>
///     Subsystem tests for the composition of several sources: mounts, imports, overlays, links between sources,
///     collisions, and ordering.
/// </summary>
/// <remarks>
///     The tests compose the embedded <c>lib-a</c>, <c>lib-b</c>, <c>app</c>, and <c>tool</c> fixtures and small
///     in-memory sources through the builder, and assert on the catalog or the problems that the Composition
///     subsystem returns.
/// </remarks>
public class CompositionMultiSourceTests
{
    /// <summary>
    ///     Proves that mounted sources get a generated root card and global ids with the mount as prefix.
    /// </summary>
    [Fact]
    public void Composition_Mounting_TwoLibraries_GivesMountRootsAndGlobalIds()
    {
        // Arrange / Act
        var catalog = HelpCatalog.Create(builder => builder
            .AddMountedSource(CatalogBuilding.LibA())
            .AddMountedSource(CatalogBuilding.LibB(), "charts"));

        // Assert: roots in registration order, children in source order, global ids everywhere
        Assert.Equal(["lib-a", "charts"], catalog.Roots.Select(card => card.Id));
        Assert.Equal(
            ["lib-a", "lib-a/shapes", "lib-a/graph", "lib-a/graph/layout", "charts", "charts/chart", "charts/chart/axis"],
            catalog.Cards.Select(card => card.Id));
        var root = catalog.Find("alpha")!;
        Assert.Equal(("lib-a", "lib-a", "Library A", null), (root.Id, root.SourceName, root.Title, root.ParentId));
        Assert.Empty(root.Tags);
        Assert.Empty(root.Examples);
        Assert.Equal(["lib-a/shapes", "lib-a/graph"], root.ChildIds);
        var layout = catalog.Find("lib-a/graph/layout")!;
        Assert.Equal(("graph/layout", "lib-a/graph", "lib-a"), (layout.LocalId, layout.ParentId, layout.SourceName));
        Assert.Null(catalog.Find("graph"));
        Assert.Equal("lib-a/graph", catalog.Find("lib-a diagram")!.Id);
        Assert.Equal(
            [("lib-a", "lib-a", "Library A"), ("lib-b", "charts", "Library B")],
            catalog.Sources.Select(info => (info.Name, info.Mount ?? string.Empty, info.Title)));
    }

    /// <summary>
    ///     Proves that the landing text of a mount root is the body of <c>_source.md</c> with its links resolved.
    /// </summary>
    [Fact]
    public void Composition_Mounting_MountRoot_UsesSourceBodyAsLandingText()
    {
        // Arrange / Act
        var catalog = HelpCatalog.Create(builder => builder.AddMountedSource(CatalogBuilding.LibA()));

        // Assert
        Assert.Equal(
            "Library A draws graphs and shapes. Start with the card `lib-a/graph` or the card `lib-a/shapes`.",
            catalog.Find("lib-a")!.Body);
    }

    /// <summary>
    ///     Proves that flat sources merge their roots into one catalog and keep their local ids.
    /// </summary>
    [Fact]
    public void Composition_Mounting_FlatSources_MergeRootsInRegistrationOrder()
    {
        // Arrange / Act
        var catalog = HelpCatalog.Create(builder => builder.AddSource(CatalogBuilding.LibA()).AddSource(CatalogBuilding.LibB()));

        // Assert: library A's roots follow its children list, then library B's roots; no mount root cards
        Assert.Equal(["shapes", "graph", "chart"], catalog.Roots.Select(card => card.Id));
        Assert.Equal(["lib-a", "lib-a", "lib-a", "lib-b", "lib-b"], catalog.Cards.Select(card => card.SourceName));
        Assert.All(catalog.Sources, info => Assert.Null(info.Mount));
        Assert.Equal("chart", catalog.Find("graph")!.SeeAlsoIds.Single());
    }

    /// <summary>
    ///     Proves that a source added mounted after a flat one orders its mount root after the flat roots.
    /// </summary>
    [Fact]
    public void Composition_Ordering_MixedSources_FollowsRegistrationOrderAndOrderValuesStayInOneSource()
    {
        // Arrange: order values 9 and 1 in different sources must not reorder the sources
        var first = CatalogBuilding.Named("first", ("z.md", SourceReading.Card(extraFrontMatter: "order: 9\n")), ("y.md", SourceReading.Card(extraFrontMatter: "order: 1\n")));
        var second = CatalogBuilding.Named("second", ("a.md", SourceReading.Card(extraFrontMatter: "order: 0\n")));

        // Act
        var catalog = HelpCatalog.Create(builder => builder.AddSource(first).AddMountedSource(second).AddSource(CatalogBuilding.Named("third", ("b.md", SourceReading.Card()))));

        // Assert
        Assert.Equal(["y", "z", "second", "b"], catalog.Roots.Select(card => card.Id));
        Assert.Equal(["y", "z", "second", "second/a", "b"], catalog.Cards.Select(card => card.Id));
    }

    /// <summary>
    ///     Proves that an importing source sees the tokens and the local card ids of the source it imports.
    /// </summary>
    [Fact]
    public void Composition_Imports_ImporterSeesTokensAndLocalIds()
    {
        // Arrange
        var libA = CatalogBuilding.LibA();

        // Act: library A is mounted, the application is flat and imports it
        var catalog = HelpCatalog.Create(builder => builder.ToolName("demo").AddMountedSource(libA).AddSource(CatalogBuilding.App(libA)));
        var start = catalog.Find("start")!;

        // Assert: bare links resolve to mounted global ids, the token of the import is replaced
        Assert.Equal(
            "Begin with the card `lib-a/graph` and the card `lib-a/shapes` of the imported library, with at most 10 nodes.\n"
            + "Charts are in the chart card. Launch with the example `launch`.",
            start.Body);
        Assert.Equal(["lib-a/graph"], start.SeeAlsoIds);
        Assert.Equal("demo launch --limit 10", start.Examples.Single().Content);
        Assert.Equal(["external-source-absent", "external-source-absent"], catalog.Diagnostics.Where(problem => problem.File == "start.md").Select(problem => problem.RuleId));
    }

    /// <summary>
    ///     Proves that a source that does not import another source does not see its tokens or its local ids.
    /// </summary>
    [Fact]
    public void Composition_Imports_WithoutImport_TokensAndBareIdsAreNotVisible()
    {
        // Arrange: the same application, but it does not declare the import
        var app = CatalogBuilding.Fixture("app");

        // Act
        var result = CatalogBuilding.Compose(builder => builder.AddMountedSource(CatalogBuilding.LibA()).AddSource(app));

        // Assert: the unknown token and the unknown bare ids are reported, the overlay target is not found
        Assert.Contains("token-unknown", result.Errors.Select(problem => problem.RuleId));
        Assert.Contains("link-target", result.Errors.Select(problem => problem.RuleId));
        Assert.Contains("overlay-target", result.Errors.Select(problem => problem.RuleId));
    }

    /// <summary>
    ///     Proves that a tool source sees the tokens and bare card ids of its companion library and overlays its cards.
    /// </summary>
    [Fact]
    public void Composition_Imports_ToolFixture_UsesLibraryTokensBareLinksAndOverlays()
    {
        // Arrange
        var libA = CatalogBuilding.LibA();

        // Act: both flat, the library first, as a tool and its companion library are composed
        var catalog = HelpCatalog.Create(builder => builder.AddSource(libA).AddSource(CatalogBuilding.Tool(libA)));

        // Assert
        Assert.Equal("The commands of the tool, schema 2.", catalog.Find("commands")!.Summary);
        Assert.Equal(
            "The commands draw a the card `graph` of at most 10 nodes. JSON output uses schema 2.",
            catalog.Find("commands")!.Body);
        var graph = catalog.Find("graph")!;
        Assert.EndsWith("\n\nThe tool draws graphs with the card `commands`.", graph.Body, StringComparison.Ordinal);
        Assert.Contains("commands", graph.SeeAlsoIds);
    }

    /// <summary>
    ///     Proves that an import is reported when it is added after its importer, is missing, or is the source itself.
    /// </summary>
    [Fact]
    public void Composition_Imports_OrderMissingAndCycle_AreReportedWithoutFollowUpErrors()
    {
        // Arrange
        var libA = CatalogBuilding.LibA();
        var app = CatalogBuilding.App(libA);
        var self = CatalogBuilding.Named("self", ("a.md", SourceReading.Card()));

        // Act
        var order = CatalogBuilding.Compose(builder => builder.AddSource(app).AddMountedSource(libA));
        var plain = CatalogBuilding.Named("app", ("a.md", SourceReading.Card(body: "Use [[graph]] with {{limit}}.", extraFrontMatter: "see-also:\n  - graph\n"))).Imports(libA);
        var missing = CatalogBuilding.Compose(builder => builder.AddSource(plain));
        var cycle = CatalogBuilding.Compose(builder => builder.AddSource(self.Imports(self.WithName("self"))));

        // Assert: the import problem and the overlay of a later source are the only errors; the tokens and bare ids
        // of the import cause no follow-ups
        Assert.Equal(["import-order", "extends-target"], order.Errors.Select(problem => problem.RuleId).Distinct());
        Assert.DoesNotContain(order.Errors, problem => problem.RuleId is "token-unknown" or "link-target" or "see-also-target");
        Assert.Equal([("app", "import-missing")], missing.Errors.Select(problem => (problem.SourceName, problem.RuleId)).Distinct());
        Assert.Equal([("self", "import-cycle")], cycle.Errors.Select(problem => (problem.SourceName, problem.RuleId)));
        Assert.Contains("the source 'lib-a', which is added after it", order.Errors[0].Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a bare id that two sources of one product both use is ambiguous and must be qualified.
    /// </summary>
    [Fact]
    public void Composition_Imports_BareIdInImporterAndImport_IsAmbiguous()
    {
        // Arrange
        var imported = CatalogBuilding.Named("lib", ("x.md", SourceReading.Card()));
        var importer = CatalogBuilding.Named(
            "tool",
            ("x.md", SourceReading.Card()),
            ("y.md", SourceReading.Card(body: "See [[x]] and [[lib:x|lib]].", extraFrontMatter: "see-also:\n  - x\n")))
            .Imports(imported);

        // Act
        var result = CatalogBuilding.Compose(builder => builder.AddSource(imported).AddMountedSource(importer));

        // Assert: the bare link and the bare see-also entry are ambiguous; the qualified link is fine
        Assert.Equal(
            [("y.md", "ambiguous-reference", 7), ("y.md", "ambiguous-reference", 5)],
            result.Errors.Select(problem => (problem.File, problem.RuleId, problem.Line)).OrderByDescending(item => item.Line));
    }

    /// <summary>
    ///     Proves that an overlay of the application changes the library card it extends and keeps the order of overlays.
    /// </summary>
    [Fact]
    public void Composition_Overlays_AppFixture_MergesIntoTheTargetCards()
    {
        // Arrange
        var libA = CatalogBuilding.LibA();

        // Act
        var catalog = HelpCatalog.Create(builder => builder.ToolName("demo").AddMountedSource(libA).AddSource(CatalogBuilding.App(libA)));
        var graph = catalog.Find("lib-a/graph")!;

        // Assert: title and summary replaced, body appended after a blank line, examples and see-also appended
        Assert.Equal(("Graph in the app", "Draw a graph in the application.", "lib-a"), (graph.Title, graph.Summary, graph.SourceName));
        Assert.Equal(
            "A graph may have 10 nodes. Lay it out with the card `lib-a/graph/layout` and run the example `render`.\n"
            + "Charts are described in the chart card.\n\n"
            + "In the application, begin at the card `start` and run the example `app-render` or the example `render`.",
            graph.Body);
        Assert.Equal(["render", "app-render"], graph.Examples.Select(example => example.Name));
        Assert.Equal(["lib-a", "app"], graph.Examples.Select(example => example.SourceName));
        Assert.Equal("demo render --app graph.json", graph.Examples[1].Content);
        Assert.Equal(["start"], graph.SeeAlsoIds);
        Assert.EndsWith("\n\nThe application adds a ruler to every shape.", catalog.Find("lib-a/shapes")!.Body, StringComparison.Ordinal);
        Assert.DoesNotContain(catalog.Cards, card => card.Id.Contains("overlay", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Proves that an optional overlay of an absent source is skipped with an informational problem only.
    /// </summary>
    [Fact]
    public void Composition_Overlays_OptionalOverlayOfAbsentSource_IsSkippedWithInfo()
    {
        // Arrange
        var libA = CatalogBuilding.LibA();

        // Act
        var catalog = HelpCatalog.Create(builder => builder.AddMountedSource(libA).AddSource(CatalogBuilding.App(libA)));

        // Assert
        var skipped = Assert.Single(catalog.Diagnostics, problem => problem.RuleId == "overlay-optional-skipped");
        Assert.Equal((HelpSeverity.Info, "app", "overlay-optional.md"), (skipped.Severity, skipped.SourceName, skipped.File));
    }

    /// <summary>
    ///     Proves that overlays apply in source registration order and then by file path, and that the last title wins.
    /// </summary>
    [Fact]
    public void Composition_Overlays_Order_IsRegistrationOrderThenOrdinalFilePath()
    {
        // Arrange: two overlays in the second source (written out of order) and one in the third
        var first = CatalogBuilding.Named("first", ("a.md", SourceReading.Card(title: "Original", body: "Base.")));
        var second = CatalogBuilding.Named(
            "second",
            ("o-b.md", "---\nextends: first:a\ntitle: From b\n---\nB."),
            ("o-a.md", "---\nextends: first:a\ntitle: From a\n---\nA."));
        var third = CatalogBuilding.Named("third", ("o.md", "---\nextends: first:a\ntitle: From third\n---\nThird."));

        // Act
        var card = HelpCatalog.Create(builder => builder.AddSource(first).AddSource(second).AddSource(third)).Find("a")!;

        // Assert
        Assert.Equal("From third", card.Title);
        Assert.Equal("Base.\n\nA.\n\nB.\n\nThird.", card.Body);
    }

    /// <summary>
    ///     Proves that example names an overlay adds count in the card it extends.
    /// </summary>
    [Fact]
    public void Composition_ExampleNames_OverlayExampleRepeatingName_IsDuplicateInTargetCard()
    {
        // Arrange: lib-a/graph has the example render; the overlay adds another one with that name, another one is
        // added to a different card
        var libA = CatalogBuilding.LibA();
        var example = "examples:\n  - name: render\n    kind: command\n    language: shell\n    content: run\n    description: Run.\n";
        var app = CatalogBuilding.Named(
            "app",
            ("o1.md", $"---\nextends: lib-a:graph\n{example}---\n"),
            ("o2.md", $"---\nextends: lib-a:shapes\n{example}---\n")).Imports(libA);

        // Act
        var result = CatalogBuilding.Compose(builder => builder.AddMountedSource(libA).AddSource(app));

        // Assert: only the overlay of graph repeats a name of its card; the same name in shapes and in lib-b is fine
        var problem = Assert.Single(result.Errors);
        Assert.Equal(("app", "o1.md", "duplicate-example"), (problem.SourceName, problem.File, problem.RuleId));
    }

    /// <summary>
    ///     Proves that the same example name may be used in different sources and different cards.
    /// </summary>
    [Fact]
    public void Composition_ExampleNames_SameNameInOtherSourcesAndCards_IsAllowed()
    {
        // Arrange / Act: lib-a/graph and lib-b/chart both have the example render
        var catalog = HelpCatalog.Create(builder => builder.AddSource(CatalogBuilding.LibA()).AddSource(CatalogBuilding.LibB()));

        // Assert
        Assert.Equal(2, catalog.FindExamples("render").Count);
        Assert.Equal(["graph", "chart"], catalog.FindExamples("render").Select(reference => reference.CardId));
    }

    /// <summary>
    ///     Proves that a link to a present source resolves and keeps the display text, and an absent source degrades.
    /// </summary>
    [Fact]
    public void Composition_ExternalLinks_PresentAndAbsentSources_ResolveOrDegrade()
    {
        // Arrange / Act
        var both = HelpCatalog.Create(builder => builder.AddMountedSource(CatalogBuilding.LibA()).AddMountedSource(CatalogBuilding.LibB()));
        var onlyB = HelpCatalog.Create(builder => builder.AddMountedSource(CatalogBuilding.LibB()));
        var plain = new HelpRenderOptions();

        // Assert: present, the plain style writes the global id; see-also entries resolve
        Assert.Contains("Compare the card `lib-a/graph`, and read\nthe cards of an absent library when", both.Find("lib-b/chart")!.Body, StringComparison.Ordinal);
        Assert.Equal(["lib-a/graph"], both.Find("lib-b/chart")!.SeeAlsoIds);
        Assert.Equal(["lib-b/chart"], both.Find("lib-a/graph")!.SeeAlsoIds);
        Assert.Contains("the card `lib-b/chart`", both.FormatBody(both.Find("lib-a/graph")!, plain), StringComparison.Ordinal);

        // Assert: absent, the display text is written and the see-also entry is left out
        Assert.Contains("Compare the graph card, and read", onlyB.Find("lib-b/chart")!.Body, StringComparison.Ordinal);
        Assert.Empty(onlyB.Find("lib-b/chart")!.SeeAlsoIds);
        Assert.Equal(["external-source-absent", "external-source-absent", "external-source-absent"], onlyB.Diagnostics.Select(problem => problem.RuleId));
        Assert.Single(onlyB.Diagnostics, problem => problem.Message.Contains("the see-also entry is left out", StringComparison.Ordinal));
        Assert.Equal(2, onlyB.Diagnostics.Count(problem => problem.Message.Contains("written as its display text", StringComparison.Ordinal)));
    }

    /// <summary>
    ///     Proves that a link to a present source whose card does not exist is an error and not a degradation.
    /// </summary>
    [Fact]
    public void Composition_ExternalLinks_PresentSourceWithMissingCard_IsAnError()
    {
        // Arrange
        var lib = CatalogBuilding.Named("lib", ("x.md", SourceReading.Card()));
        var app = CatalogBuilding.Named("app", ("y.md", SourceReading.Card(body: "See [[lib:nope|text]].", extraFrontMatter: "see-also:\n  - lib:nope\n")));

        // Act
        var result = CatalogBuilding.Compose(builder => builder.AddSource(lib).AddSource(app));

        // Assert
        Assert.Equal(
            [("y.md", "see-also-target"), ("y.md", "link-target")],
            result.Errors.Select(problem => (problem.File, problem.RuleId)));
    }

    /// <summary>
    ///     Proves that two cards with one global id collide, naming both sources and files, and the later one is left out.
    /// </summary>
    [Fact]
    public void Composition_Collisions_SameGlobalId_ReportsBothSourcesAndFiles()
    {
        // Arrange
        var first = CatalogBuilding.Named("first", ("a.md", SourceReading.Card()), ("a/child.md", SourceReading.Card()));
        var second = CatalogBuilding.Named("second", ("a.md", SourceReading.Card()), ("a/child.md", SourceReading.Card()));

        // Act
        var result = CatalogBuilding.Compose(builder => builder.AddSource(first).AddSource(second));

        // Assert: each id that two sources use is one collision at the later source, naming both; nothing else follows
        Assert.Equal(
            [("collision", "second", "a.md"), ("collision", "second", "a/child.md")],
            result.Errors.Select(problem => (problem.RuleId, problem.SourceName, problem.File)));
        Assert.Contains("'first'", result.Errors[0].Message, StringComparison.Ordinal);
        Assert.Contains("'a.md'", result.Errors[0].Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that mounts that equal each other or a flat root are collisions, and an invalid mount is reported.
    /// </summary>
    [Fact]
    public void Composition_Collisions_MountsAndFlatRoots_AreReported()
    {
        // Arrange
        var one = CatalogBuilding.Named("one", ("a.md", SourceReading.Card()));
        var two = CatalogBuilding.Named("two", ("b.md", SourceReading.Card()));
        var flat = CatalogBuilding.Named("flat", ("one.md", SourceReading.Card()));

        // Act
        var sameMount = CatalogBuilding.Compose(builder => builder.AddMountedSource(one, "m").AddMountedSource(two, "m"));
        var flatRoot = CatalogBuilding.Compose(builder => builder.AddSource(flat).AddMountedSource(one));
        var invalid = CatalogBuilding.Compose(builder => builder.AddMountedSource(one, "Bad Mount"));

        // Assert
        Assert.Equal([("two", "collision")], sameMount.Errors.Select(problem => (problem.SourceName, problem.RuleId)));
        Assert.Equal([("one", "collision")], flatRoot.Errors.Select(problem => (problem.SourceName, problem.RuleId)));
        Assert.Equal([("one", "mount-invalid")], invalid.Errors.Select(problem => (problem.SourceName, problem.RuleId)));
    }

    /// <summary>
    ///     Proves that a second source with an existing name is reported and left out without follow-up problems.
    /// </summary>
    [Fact]
    public void Composition_Collisions_DuplicateSourceName_IsReportedOnce()
    {
        // Arrange
        var first = CatalogBuilding.Named("same", ("a.md", SourceReading.Card()));
        var second = CatalogBuilding.Named("same", ("b.md", SourceReading.Card(body: "See [[a]].")));

        // Act
        var result = CatalogBuilding.Compose(builder => builder.AddSource(first).AddSource(second));

        // Assert
        var problem = Assert.Single(result.Errors);
        Assert.Equal(("source-name-duplicate", "same"), (problem.RuleId, problem.SourceName));
        Assert.Contains("position 1", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a root name or alias that two sources both use is a collision.
    /// </summary>
    [Fact]
    public void Composition_Collisions_RootAliasUsedByTwoSources_IsReported()
    {
        // Arrange: an alias of a flat root equals the name of a root of another source and the alias of a mount
        var one = CatalogBuilding.Named("one", ("a.md", SourceReading.Card(extraFrontMatter: "aliases:\n  - shared\n")));
        var two = CatalogBuilding.Named("two", ("shared.md", SourceReading.Card()));
        var three = CatalogBuilding.Named("three", ("_source.md", "---\naliases:\n  - shared\n---\n"), ("c.md", SourceReading.Card()));

        // Act
        var result = CatalogBuilding.Compose(builder => builder.AddSource(one).AddSource(two).AddMountedSource(three));

        // Assert: reported at the later claimant, ignoring case
        Assert.Equal(
            [("two", "shared.md", "collision"), ("three", "_source.md", "collision")],
            result.Errors.Select(problem => (problem.SourceName, problem.File, problem.RuleId)));
    }

    /// <summary>
    ///     Proves that a card of one source cannot sit under a card of another source.
    /// </summary>
    [Fact]
    public void Composition_Collisions_CardUnderForeignCard_IsTreeForeign()
    {
        // Arrange: the second source has a/b.md but no a.md; a.md belongs to the first source
        var first = CatalogBuilding.Named("first", ("a.md", SourceReading.Card()));
        var second = CatalogBuilding.Named("second", ("a/b.md", SourceReading.Card()), ("c/d.md", SourceReading.Card()));

        // Act
        var result = CatalogBuilding.Compose(builder => builder.AddSource(first).AddSource(second));

        // Assert: the foreign parent is tree-foreign; a parent that exists nowhere stays parent-missing
        Assert.Equal(
            [("a/b.md", "tree-foreign"), ("c/d.md", "parent-missing")],
            result.Errors.Select(problem => (problem.File, problem.RuleId)));
    }

    /// <summary>
    ///     Proves that all defects of several sources are collected in one pass and ordered by source, file, and line.
    /// </summary>
    [Fact]
    public void Composition_Failures_SeveralSources_CollectAllInOrder()
    {
        // Arrange: a syntax error in the first source, a token and a link error in the second, a schema in the third
        var first = CatalogBuilding.Named("first", ("a.md", "---\ntitle: [x]\n---\n"));
        var second = CatalogBuilding.Named("second", ("b.md", SourceReading.Card(body: "{{nope}} and [[missing]].")));
        var third = CatalogBuilding.Named("third", ("_source.md", "---\nschema: 3\n---\n"));

        // Act
        var result = CatalogBuilding.Compose(builder => builder.AddSource(third).AddSource(second).AddSource(first));

        // Assert: registration order decides, then file and line
        Assert.Equal(
            [("third", "schema-unsupported"), ("second", "link-target"), ("second", "token-unknown"), ("first", "syntax")],
            result.Errors.Select(problem => (problem.SourceName, problem.RuleId)));
        var exception = Assert.Throws<HelpCatalogException>(() => HelpCatalog.Create(builder => builder.AddSource(third).AddSource(first)));
        Assert.Equal(2, exception.Problems.Count);
    }

    /// <summary>
    ///     Proves that the schema of <c>_source.md</c> is handled per source: omitted, 1, and 2 are supported side by side.
    /// </summary>
    [Fact]
    public void Composition_Schema_OmittedOneAndTwo_AreSupportedPerSource()
    {
        // Arrange
        var omitted = CatalogBuilding.Named("omitted", ("a.md", SourceReading.Card()));
        var explicitOne = CatalogBuilding.Named("explicit", ("_source.md", "---\nschema: 1\n---\n"), ("b.md", SourceReading.Card()));
        var two = CatalogBuilding.Named("two", ("_source.md", "---\nschema: 2\n---\n"), ("c.md", SourceReading.Card()));

        // Act
        var catalog = HelpCatalog.Create(builder => builder.AddSource(omitted).AddMountedSource(explicitOne).AddMountedSource(two));

        // Assert
        Assert.Equal([1, 1, 2], catalog.Sources.Select(info => info.Schema));
    }

    /// <summary>
    ///     Proves that a view by source shows the mount root and cards of that source only.
    /// </summary>
    [Fact]
    public void Composition_Views_FilterSource_ShowsOneMountedSource()
    {
        // Arrange
        var catalog = HelpCatalog.Create(builder => builder.AddMountedSource(CatalogBuilding.LibA()).AddMountedSource(CatalogBuilding.LibB()));

        // Act
        var view = catalog.FilterSource(["lib-b"]);

        // Assert
        Assert.Equal(["lib-b", "lib-b/chart", "lib-b/chart/axis"], view.Cards.Select(card => card.Id));
        Assert.Equal(["lib-b"], view.Sources.Select(info => info.Name));
        Assert.Null(view.Find("lib-a"));
        Assert.Contains("the graph card", view.Find("lib-b/chart")!.Body, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that expected roots count the mount roots of mounted sources and the roots of flat sources.
    /// </summary>
    [Fact]
    public void Composition_ExpectedRoots_MixedSources_ComparesGlobalRoots()
    {
        // Arrange
        var libA = CatalogBuilding.LibA();

        // Act
        var good = CatalogBuilding.Compose(builder => builder.AddMountedSource(libA).AddSource(CatalogBuilding.LibB()).ExpectedRoots(["lib-a", "chart"]));
        var bad = CatalogBuilding.Compose(builder => builder.AddMountedSource(libA).AddSource(CatalogBuilding.LibB()).ExpectedRoots(["graph"]));

        // Assert
        Assert.Empty(good.Errors);
        var problem = Assert.Single(bad.Errors);
        Assert.Equal("roots-mismatch", problem.RuleId);
        Assert.Contains("missing: graph", problem.Message, StringComparison.Ordinal);
        Assert.Contains("unexpected: chart, lib-a", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a mounted source without a name is reported once, by Loading, and not again by the mount.
    /// </summary>
    [Fact]
    public void Composition_Mounting_SourceWithoutName_IsReportedOnce()
    {
        // Arrange
        var unnamed = HelpSource.FromFiles(SourceReading.Files(("a.md", SourceReading.Card())));

        // Act
        var result = CatalogBuilding.Compose(builder => builder.AddMountedSource(unnamed).AddMountedSource(CatalogBuilding.LibA()));

        // Assert
        Assert.Equal(["source-invalid"], result.Errors.Select(problem => problem.RuleId));
    }

    /// <summary>
    ///     Proves that an overlay of a card that failed to load adds no problem of its own.
    /// </summary>
    [Fact]
    public void Composition_Overlays_TargetFailedToLoad_ReportsOnlyTheFailure()
    {
        // Arrange
        var lib = CatalogBuilding.Named("lib", ("a.md", "---\ntitle: [unterminated\n---\nBody."));
        var app = CatalogBuilding.Named("app", ("o.md", "---\nextends: lib:a\ntitle: New\n---\nMore.")).Imports(lib);

        // Act
        var result = CatalogBuilding.Compose(builder => builder.AddMountedSource(lib).AddSource(app));

        // Assert
        var problem = Assert.Single(result.Errors);
        Assert.Equal(("lib", "a.md", "syntax"), (problem.SourceName, problem.File, problem.RuleId));
    }

    /// <summary>
    ///     Proves that the tag vocabulary applies to every source, but mount roots are exempt.
    /// </summary>
    [Fact]
    public void Composition_TagVocabulary_AppliesToEverySourceButNotMountRoots()
    {
        // Arrange / Act
        var result = CatalogBuilding.Compose(builder => builder
            .AddMountedSource(CatalogBuilding.LibA())
            .AddMountedSource(CatalogBuilding.LibB())
            .TagVocabulary(["authoring"], ["authoring"]));

        // Assert: the reference cards use a tag that is not allowed and have no required tag; the mount roots,
        // which have no tags, are not checked
        Assert.Equal(
            [("lib-a", "shapes.md", "tag-required"), ("lib-a", "shapes.md", "tag-unknown"), ("lib-b", "chart/axis.md", "tag-required"), ("lib-b", "chart/axis.md", "tag-unknown")],
            result.Errors.Select(problem => (problem.SourceName, problem.File, problem.RuleId)));
    }

    /// <summary>
    ///     Proves that composing the same sources twice gives equal catalogs in every rendering.
    /// </summary>
    [Fact]
    public void Composition_Determinism_TwoBuilds_RenderEqualOutput()
    {
        // Arrange
        HelpCatalog Build()
        {
            var libA = CatalogBuilding.LibA();
            return HelpCatalog.Create(builder => builder
                .ToolName("demo")
                .AddMountedSource(libA)
                .AddMountedSource(CatalogBuilding.LibB())
                .AddSource(CatalogBuilding.App(libA)));
        }

        // Act
        var first = Build();
        var second = Build();
        var options = new HelpRenderOptions { Format = HelpFormat.Json };

        // Assert
        Assert.Equal(first.RenderIndex(), second.RenderIndex());
        Assert.Equal(first.RenderIndex(options), second.RenderIndex(options));
        Assert.Equal(first.Render(first.Find("lib-a/graph")!), second.Render(second.Find("lib-a/graph")!));
        Assert.Equal(first.Cards.Select(card => card.Id), second.Cards.Select(card => card.Id));
    }

    /// <summary>
    ///     Proves that a composed catalog reads no file after it is created.
    /// </summary>
    [Fact]
    public void Composition_Reading_TwoSources_ReadsEachFileOnceAndNothingAfterCreate()
    {
        // Arrange
        var first = new CountingFileProvider(SourceReading.Files(("a.md", SourceReading.Card()), ("a/b.md", SourceReading.Card())));
        var second = new CountingFileProvider(SourceReading.Files(("c.md", SourceReading.Card(body: "See [[first:a/b|b]]."))));

        // Act
        var catalog = HelpCatalog.Create(builder => builder
            .AddMountedSource(HelpSource.FromProvider(first, "first"))
            .AddSource(HelpSource.FromProvider(second, "second")));
        var (listAfterCreate, readAfterCreate) = (first.ListCount + second.ListCount, first.ReadCount + second.ReadCount);
        _ = catalog.Render(catalog.Find("c")!);
        _ = catalog.RenderIndex();

        // Assert
        Assert.Equal((2, 3), (listAfterCreate, readAfterCreate));
        Assert.Equal((2, 3), (first.ListCount + second.ListCount, first.ReadCount + second.ReadCount));
    }
}
