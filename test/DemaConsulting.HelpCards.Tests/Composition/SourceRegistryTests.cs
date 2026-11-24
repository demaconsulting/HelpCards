using System.Globalization;
using DemaConsulting.HelpCards.Composition;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Composition;

/// <summary>
///     Unit tests for <see cref="SourceRegistry"/>: reading sources in registration order, resolving imports, and
///     looking up the cards that references name.
/// </summary>
public class SourceRegistryTests
{
    /// <summary>
    ///     Creates the registrations of flat sources.
    /// </summary>
    /// <param name="sources">The sources in registration order.</param>
    /// <returns>The registrations.</returns>
    private static List<SourceRegistration> Flat(params HelpSource[] sources)
    {
        return sources.Select(source => new SourceRegistration(source, false, null)).ToList();
    }

    /// <summary>
    ///     Reads registrations with the invariant culture and no tool settings.
    /// </summary>
    /// <param name="registrations">The registrations.</param>
    /// <returns>The registry.</returns>
    private static SourceRegistry Read(List<SourceRegistration> registrations)
    {
        return SourceRegistry.Read(registrations, CultureInfo.InvariantCulture, null, null);
    }

    /// <summary>
    ///     Places every card of every source as a build, as the pipeline does, so lookups can find them.
    /// </summary>
    /// <param name="registry">The registry.</param>
    private static void Place(SourceRegistry registry)
    {
        foreach (var entry in registry.Entries)
        {
            foreach (var card in entry.Definition.Cards)
            {
                entry.Cards[card.LocalId] = new CardBuild(entry, card, card.LocalId);
            }
        }
    }

    /// <summary>
    ///     Proves that every source is listed and read once, in registration order, and named from its source file.
    /// </summary>
    [Fact]
    public void SourceRegistry_Read_TwoSources_ReadsEachFileOnceInOrder()
    {
        // Arrange
        var first = new CountingFileProvider(SourceReading.Files(("_source.md", "---\nname: one\n---\n"), ("a.md", SourceReading.Card())));
        var second = new CountingFileProvider(SourceReading.Files(("b.md", SourceReading.Card())));

        // Act
        var registry = Read(Flat(HelpSource.FromProvider(first), HelpSource.FromProvider(second, "two")));

        // Assert
        Assert.Equal(["one", "two"], registry.Entries.Select(entry => entry.Name));
        Assert.Equal([0, 1], registry.Entries.Select(entry => entry.Index));
        Assert.Equal((1, 2), (first.ListCount, first.ReadCount));
        Assert.Equal((1, 1), (second.ListCount, second.ReadCount));
        Assert.Empty(registry.Problems);
    }

    /// <summary>
    ///     Proves that a source is found by its declared name ignoring case and that an unknown name finds nothing.
    /// </summary>
    [Fact]
    public void SourceRegistry_FindByName_DeclaredName_IgnoresCase()
    {
        // Arrange
        var registry = Read(Flat(CatalogBuilding.Named("lib", ("a.md", SourceReading.Card()))));

        // Act / Assert
        Assert.Same(registry.Entries[0], registry.FindByName("LIB"));
        Assert.Null(registry.FindByName("other"));
    }

    /// <summary>
    ///     Proves that a second source with a declared name is excluded, reported once, and not found by name.
    /// </summary>
    [Fact]
    public void SourceRegistry_Read_DuplicateName_ExcludesSecondSource()
    {
        // Arrange / Act
        var registry = Read(Flat(
            CatalogBuilding.Named("same", ("a.md", SourceReading.Card())),
            CatalogBuilding.Named("same", ("b.md", SourceReading.Card()))));

        // Assert
        Assert.Equal([false, true], registry.Entries.Select(entry => entry.Excluded));
        Assert.Same(registry.Entries[0], registry.FindByName("same"));
        var (index, problem) = Assert.Single(registry.Problems);
        Assert.Equal((1, "source-name-duplicate"), (index, problem.RuleId));
        Assert.Contains("position 1", problem.Message);
    }

    /// <summary>
    ///     Proves that a bare id is found in the source itself and then in its imports, and a qualified one in its source.
    /// </summary>
    [Fact]
    public void SourceRegistry_Lookup_BareAndQualifiedIds_FindOwnThenImportedCards()
    {
        // Arrange
        var lib = CatalogBuilding.Named("lib", ("x.md", SourceReading.Card()), ("y.md", SourceReading.Card()));
        var app = CatalogBuilding.Named("app", ("y.md", SourceReading.Card()), ("z.md", SourceReading.Card())).Imports(lib);
        var registry = Read(Flat(lib, app));
        Place(registry);
        var (libEntry, appEntry) = (registry.Entries[0], registry.Entries[1]);

        // Act / Assert: own card, imported card, qualified card, ambiguous card
        Assert.Same(appEntry.Cards["z"], registry.Lookup(appEntry, null, "z").Build);
        Assert.Same(libEntry.Cards["x"], registry.Lookup(appEntry, null, "x").Build);
        Assert.Same(libEntry.Cards["y"], registry.Lookup(appEntry, "lib", "y").Build);
        var ambiguous = registry.Lookup(appEntry, null, "y");
        Assert.Equal(LookupKind.Ambiguous, ambiguous.Kind);
        Assert.Contains("'app'", ambiguous.Detail, StringComparison.Ordinal);
        Assert.Contains("'lib'", ambiguous.Detail, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that an id that is nowhere is not found, an absent source is reported as absent, and a source that
    ///     does not import another source does not see its ids.
    /// </summary>
    [Fact]
    public void SourceRegistry_Lookup_UnknownAbsentAndUnimported_AreNotFound()
    {
        // Arrange
        var lib = CatalogBuilding.Named("lib", ("x.md", SourceReading.Card()));
        var app = CatalogBuilding.Named("app", ("z.md", SourceReading.Card()));
        var registry = Read(Flat(lib, app));
        Place(registry);
        var appEntry = registry.Entries[1];

        // Act / Assert
        Assert.Equal(LookupKind.NotFound, registry.Lookup(appEntry, null, "x").Kind);
        Assert.Equal(LookupKind.NotFound, registry.Lookup(appEntry, "lib", "nope").Kind);
        Assert.Equal(LookupKind.SourceAbsent, registry.Lookup(appEntry, "other", "x").Kind);
        Assert.Equal(LookupKind.Found, registry.Lookup(appEntry, "LIB", "x").Kind);
    }

    /// <summary>
    ///     Proves that a card that failed to load, a card of an excluded source, and an id after a broken import are
    ///     suppressed so that they are not reported twice.
    /// </summary>
    [Fact]
    public void SourceRegistry_Lookup_FailedExcludedAndBrokenImport_AreSuppressed()
    {
        // Arrange
        var broken = CatalogBuilding.Named("broken", ("bad.md", "---\ntitle: [x]\n---\n"));
        var importer = CatalogBuilding.Named("importer", ("i.md", SourceReading.Card())).Imports(CatalogBuilding.Named("missing", ("m.md", SourceReading.Card())));
        var registry = Read(Flat(broken, importer));
        Place(registry);
        foreach (var entry in registry.Entries)
        {
            entry.Tree = new SourceTree(
                entry.Cards.ToDictionary(pair => pair.Key, pair => pair.Value.Definition),
                new Dictionary<string, IReadOnlyList<DemaConsulting.HelpCards.Loading.CardDefinition>> { [string.Empty] = [] },
                entry.Definition.FailedIds.ToHashSet(StringComparer.Ordinal));
        }

        // Act / Assert
        Assert.Equal(LookupKind.Suppressed, registry.Lookup(registry.Entries[1], "broken", "bad").Kind);
        Assert.Equal(LookupKind.Suppressed, registry.Lookup(registry.Entries[1], null, "anything").Kind);
        registry.Entries[0].Excluded = true;
        Assert.Equal(LookupKind.Suppressed, registry.Lookup(registry.Entries[1], "broken", "other").Kind);
    }

    /// <summary>
    ///     Proves that imports are transitive: the closure lists the imports of imports, each once, in search order.
    /// </summary>
    [Fact]
    public void SourceRegistry_Read_TransitiveImports_ListsClosureAndSeesTokensOfEverySource()
    {
        // Arrange: c imports b, b imports a; each source defines one token that its own card uses
        var a = CatalogBuilding.Named("a", ("a.md", SourceReading.Card(body: "A {{ta}}."))).WithTokens(new Dictionary<string, string> { ["ta"] = "1" });
        var b = CatalogBuilding.Named("b", ("b.md", SourceReading.Card(body: "B {{ta}} {{tb}}."))).WithTokens(new Dictionary<string, string> { ["tb"] = "2" }).Imports(a);
        var c = CatalogBuilding.Named("c", ("c.md", SourceReading.Card(body: "C {{ta}} {{tb}} {{tc}}."))).WithTokens(new Dictionary<string, string> { ["tc"] = "3" }).Imports(b);

        // Act
        var registry = Read(Flat(a, b, c));

        // Assert: the closure of c is b then a; every card has its tokens replaced
        Assert.Equal(["b", "a"], registry.Entries[2].ImportClosure.Select(entry => entry.Name));
        Assert.Empty(registry.Problems);
        Assert.Equal("C 1 2 3.", string.Concat(registry.Entries[2].Definition.Cards[0].Body.Select(part => part.Text)));
    }

    /// <summary>
    ///     Proves that an import is matched to a registered source by the files it reads or by its declared name.
    /// </summary>
    [Fact]
    public void SourceRegistry_Read_ImportMatchedByFilesOrByName_ResolvesToRegisteredSource()
    {
        // Arrange: the importer imports a copy of the library (shared files) and another source with the same name
        var lib = CatalogBuilding.Named("lib", ("x.md", SourceReading.Card()));
        var twin = CatalogBuilding.Named("twin", ("t.md", SourceReading.Card()));
        var sameName = CatalogBuilding.Named("twin", ("other.md", SourceReading.Card()));
        var importer = CatalogBuilding.Named("app", ("a.md", SourceReading.Card())).Imports(lib.WithTokens(new Dictionary<string, string>())).Imports(sameName);

        // Act
        var registry = Read(Flat(lib, twin, importer));

        // Assert
        Assert.Equal(["lib", "twin"], registry.Entries[2].Imports.Select(entry => entry.Name));
        Assert.Empty(registry.Problems);
    }

    /// <summary>
    ///     Proves that a token defined by a source and by an import is reported once as redefined.
    /// </summary>
    [Fact]
    public void SourceRegistry_Read_TokenDefinedTwice_ReportsTokenRedefined()
    {
        // Arrange
        var tokens = new Dictionary<string, string> { ["same"] = "1" };
        var lib = CatalogBuilding.Named("lib", ("x.md", SourceReading.Card())).WithTokens(tokens);
        var app = CatalogBuilding.Named("app", ("a.md", SourceReading.Card())).WithTokens(tokens).Imports(lib);

        // Act
        var registry = Read(Flat(lib, app));

        // Assert
        var (index, problem) = Assert.Single(registry.Problems);
        Assert.Equal((1, "token-redefined", "app"), (index, problem.RuleId, problem.SourceName));
        Assert.Contains("'lib'", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the built-in tool token is the name of the first source unless the tool name is set.
    /// </summary>
    [Fact]
    public void SourceRegistry_Read_ToolToken_DefaultsToFirstSourceName()
    {
        // Arrange
        var first = CatalogBuilding.Named("first", ("a.md", SourceReading.Card(body: "{{tool}}")));
        var second = CatalogBuilding.Named("second", ("b.md", SourceReading.Card(body: "{{tool}}")));

        // Act
        var byDefault = SourceRegistry.Read(Flat(first, second), CultureInfo.InvariantCulture, null, null);
        var named = SourceRegistry.Read(Flat(first, second), CultureInfo.InvariantCulture, "demo", null);

        // Assert
        Assert.Equal(["first", "first"], byDefault.Entries.Select(entry => string.Concat(entry.Definition.Cards[0].Body.Select(part => part.Text))));
        Assert.Equal(["demo", "demo"], named.Entries.Select(entry => string.Concat(entry.Definition.Cards[0].Body.Select(part => part.Text))));
    }

    /// <summary>
    ///     Proves that null arguments are argument errors.
    /// </summary>
    [Fact]
    public void SourceRegistry_Read_NullArguments_ThrowArgumentNullException()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => SourceRegistry.Read(null!, CultureInfo.InvariantCulture, null, null));
        Assert.Throws<ArgumentNullException>(() => SourceRegistry.Read([], null!, null, null));
    }

    /// <summary>
    ///     Proves that an import that is not registered, is registered later, or is the source itself is reported once
    ///     per import and that the imports of such a source are not listed.
    /// </summary>
    [Fact]
    public void SourceRegistry_Read_MissingLaterAndSelfImports_ReportEachRule()
    {
        // Arrange
        var later = CatalogBuilding.Named("later", ("l.md", SourceReading.Card()));
        var missing = CatalogBuilding.Named("missing", ("m.md", SourceReading.Card()));
        var self = CatalogBuilding.Named("self", ("s.md", SourceReading.Card()));
        var importer = CatalogBuilding.Named("importer", ("i.md", SourceReading.Card())).Imports(later).Imports(missing);

        // Act
        var registry = Read(Flat(importer, later, self.Imports(self.WithName("self"))));

        // Assert
        Assert.Equal(
            [(0, "import-order"), (0, "import-missing"), (2, "import-cycle")],
            registry.Problems.OrderBy(item => item.SourceIndex).Select(item => (item.SourceIndex, item.Problem.RuleId)));
        Assert.Empty(registry.Entries[0].Imports);
        Assert.True(registry.Entries[0].BrokenImports);
        Assert.False(registry.Entries[1].BrokenImports);
    }

    /// <summary>
    ///     Proves that an import matched by declared name to a later source that is named in its <c>_source.md</c>
    ///     is reported as <c>import-order</c>, not <c>import-missing</c>.
    /// </summary>
    [Fact]
    public void SourceRegistry_Read_ImportOfLaterSourceNamedInSourceFile_ReportsImportOrder()
    {
        // Arrange: the later source takes its name from _source.md; the import names it only by that name
        var importedByName = CatalogBuilding.Named("later", ("x.md", SourceReading.Card()));
        var importer = CatalogBuilding.Named("importer", ("i.md", SourceReading.Card())).Imports(importedByName);
        var later = HelpSource.FromFiles(SourceReading.Files(("_source.md", "---\nname: later\n---\n"), ("l.md", SourceReading.Card())));

        // Act
        var registry = Read(Flat(importer, later));

        // Assert
        var problem = Assert.Single(registry.Problems);
        Assert.Equal((0, "import-order"), (problem.SourceIndex, problem.Problem.RuleId));
        Assert.Contains("later", problem.Problem.Message);
        Assert.True(registry.Entries[0].BrokenImports);
        Assert.Empty(registry.Entries[0].Imports);
    }
}
