using System.Globalization;
using System.Reflection;
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Integration;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Presentation;
using DemaConsulting.HelpCards.Testing;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests;

/// <summary>
///     System-level integration tests for the HelpCards system.
/// </summary>
/// <remarks>
///     The tests in this file prove user-visible capabilities of the whole library. Each test
///     name starts with the system name followed by the capability under test.
/// </remarks>
public class HelpCardsTests
{
    /// <summary>
    ///     The assembly name of the source-less project that embeds a default Help folder.
    /// </summary>
    private const string DefaultEmbeddingAssemblyName = "DemaConsulting.HelpCards.Tests.EmbeddingDefault";

    /// <summary>
    ///     Proves that the packaged build files embed a <c>Help</c> folder under the default logical
    ///     name prefix <c>{AssemblyName}.Help</c> when the project overrides nothing.
    /// </summary>
    [Fact]
    public void HelpCards_Embedding_DefaultDirectory_UsesAssemblyNamePrefix()
    {
        // Arrange: load the project that imports the packaged files and keeps every default
        var assembly = Assembly.Load(DefaultEmbeddingAssemblyName);

        // Act: list the embedded resources in the deterministic order the loader uses
        var names = assembly.GetManifestResourceNames()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        // Assert: every file of the Help folder is embedded under "{AssemblyName}.Help/"
        Assert.Equal(
            [
                $"{DefaultEmbeddingAssemblyName}.Help/_source.md",
                $"{DefaultEmbeddingAssemblyName}.Help/first.md"
            ],
            names);
    }

    /// <summary>
    ///     Proves that a project-body override of <c>HelpCardsDirectory</c> is honored by the packaged
    ///     targets: every file below the overridden folder is embedded, including nested folders and
    ///     data files, with forward slashes in the logical names.
    /// </summary>
    [Fact]
    public void HelpCards_Embedding_OverriddenDirectory_EmbedsEveryFileUnderPrefix()
    {
        // Arrange: this test project sets HelpCardsDirectory to "Fixtures" in its project body
        var assembly = typeof(HelpCardsTests).Assembly;
        const string prefix = "DemaConsulting.HelpCards.Tests.Help/embedding/Help/";

        // Act: select the resources of the embedding fixture in deterministic order, converting
        // the backslashes MSBuild writes for RecursiveDir exactly as the loader does
        var names = assembly.GetManifestResourceNames()
            .Select(name => name.Replace('\\', '/'))
            .Where(name => name.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        // Assert: nested folders and non-Markdown data files are embedded with "/" separators
        Assert.Equal(
            [
                prefix + "_source.md",
                prefix + "guide.md",
                prefix + "guide/diagram.mmd",
                prefix + "guide/setup.md"
            ],
            names);
    }

    /// <summary>
    ///     Proves that an embedded resource can be read back as UTF-8 text with the authored content.
    /// </summary>
    [Fact]
    public void HelpCards_Embedding_EmbeddedResource_ReadsAuthoredContent()
    {
        // Arrange: pick one embedded card of the overridden fixture directory
        var assembly = typeof(HelpCardsTests).Assembly;
        const string name = "DemaConsulting.HelpCards.Tests.Help/embedding/Help/guide/setup.md";

        // Act: find the resource whose normalized name matches (MSBuild writes backslashes on
        // Windows) and read it as text
        var resourceName = assembly.GetManifestResourceNames()
            .Single(candidate => candidate.Replace('\\', '/') == name);
        using var stream = assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
        var text = reader.ReadToEnd();

        // Assert: the card text is the authored file content
        Assert.Contains("title: Setup", text, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that embedded cards, with their nested folders and example files, are read from an assembly.
    /// </summary>
    [Fact]
    public void HelpCards_Sources_Assembly_ReadsEmbeddedCards()
    {
        // Arrange: the embedded fixture of this test assembly as a source
        var source = HelpSource.FromAssembly(typeof(HelpCardsTests).Assembly, "DemaConsulting.HelpCards.Tests.Help/embedding/Help");

        // Act
        var definition = SourceReading.Read(source);

        // Assert: both cards, the source name, and the example read from the embedded data file
        Assert.Empty(definition.Problems);
        Assert.Equal("embedding", definition.Name);
        Assert.Equal(["guide", "guide/setup"], definition.Cards.Select(card => card.LocalId));
        Assert.Equal("graph TD\n  A --> B", Assert.Single(definition.Cards[0].Examples).Content);
    }

    /// <summary>
    ///     Proves that cards are read from the files of a directory.
    /// </summary>
    [Fact]
    public void HelpCards_Sources_Directory_ReadsFilesOnDisk()
    {
        // Arrange: a directory with one card
        using var directory = new TemporaryDirectory();
        directory.Write("note.md", SourceReading.Card(title: "On disk"));

        // Act
        var definition = SourceReading.Read(HelpSource.FromDirectory(directory.FullPath).WithName("disk"));

        // Assert
        Assert.Empty(definition.Problems);
        Assert.Equal("On disk", Assert.Single(definition.Cards).Title);
    }

    /// <summary>
    ///     Proves that cards are read from in-memory text.
    /// </summary>
    [Fact]
    public void HelpCards_Sources_Memory_ReadsFiles()
    {
        // Arrange / Act
        var definition = SourceReading.Read(SourceReading.Files(("note.md", SourceReading.Card(title: "In memory"))));

        // Assert
        Assert.Empty(definition.Problems);
        Assert.Equal("In memory", Assert.Single(definition.Cards).Title);
    }

    /// <summary>
    ///     Proves that creating a source performs no I/O.
    /// </summary>
    [Fact]
    public void HelpCards_Sources_Construction_PerformsNoIo()
    {
        // Arrange: a provider that counts calls
        var provider = new CountingFileProvider(SourceReading.Files(("note.md", SourceReading.Card())));

        // Act: create a source and derive others from it
        var source = HelpSource.FromProvider(provider)
            .WithName("lib")
            .WithTokens(new Dictionary<string, string> { ["a"] = "1" })
            .Imports(HelpSource.FromProvider(provider));

        // Assert
        Assert.NotNull(source);
        Assert.Equal(0, provider.ListCount);
        Assert.Equal(0, provider.ReadCount);
    }

    /// <summary>
    ///     Proves that a card that uses every construct of the front-matter subset is read.
    /// </summary>
    [Fact]
    public void HelpCards_FrontMatter_ValidSubset_ReadsCard()
    {
        // Arrange: comments, quoted values, lists, a list of maps, and a literal block for example content
        const string text = """
            ---
            # A comment between entries
            title: 'It''s a "card"'
            summary: "Quoted \\ summary."
            tags:
              - authoring
            examples:
              - name: config
                kind: source
                language: yaml
                file: settings.yaml
                content: |
                  title: Demo
                    nested: true
                description: An inline file.
            x-audience: agent
            ---
            Body.
            """;

        // Act
        var definition = SourceReading.Read(SourceReading.Files(("card.md", text)));

        // Assert
        Assert.Empty(definition.Problems);
        var card = Assert.Single(definition.Cards);
        Assert.Equal("It's a \"card\"", card.Title);
        Assert.Equal("Quoted \\ summary.", card.Summary);
        Assert.Equal("title: Demo\n  nested: true", Assert.Single(card.Examples).Content);
        Assert.Equal("agent", card.Extensions["x-audience"]);
    }

    /// <summary>
    ///     Proves that a construct outside the subset is rejected with the file and the line.
    /// </summary>
    [Fact]
    public void HelpCards_FrontMatter_UnsupportedConstruct_ReportsFileAndLine()
    {
        // Arrange: a flow sequence on line 3
        var definition = SourceReading.Read(SourceReading.Files(("docs/card.md", "---\ntitle: A\ntags: [a, b]\n---\n")));

        // Act
        var problem = Assert.Single(definition.Problems);

        // Assert
        Assert.Equal((HelpSeverity.Error, RuleIds.Syntax, "docs/card.md", 3), (problem.Severity, problem.RuleId, problem.File, problem.Line));
    }

    /// <summary>
    ///     Proves that an unknown key names itself and the keys that are allowed.
    /// </summary>
    [Fact]
    public void HelpCards_FrontMatter_UnknownKey_ReportsKeyAndAllowedKeys()
    {
        // Arrange
        var definition = SourceReading.Read(SourceReading.Files(("card.md", SourceReading.Card(extraFrontMatter: "tag: x\n"))));

        // Act
        var problem = Assert.Single(definition.Problems);

        // Assert
        Assert.Equal(RuleIds.KeyUnknown, problem.RuleId);
        Assert.Contains("'tag'", problem.Message, StringComparison.Ordinal);
        Assert.Contains("tags", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the built-in tokens and the tokens of the source are replaced in the text of a card.
    /// </summary>
    [Fact]
    public void HelpCards_Tokens_BuiltInAndSourceTokens_AreReplaced()
    {
        // Arrange
        var files = SourceReading.Files(("card.md", SourceReading.Card(title: "'{{tool}} {{maxDpi}}'", body: "Version {{version}}.")));

        // Act
        var definition = SourceReading.Read(files, tokens: new Dictionary<string, string> { ["maxDpi"] = "600" });

        // Assert
        var card = Assert.Single(definition.Cards);
        Assert.Equal("demo 600", card.Title);
        Assert.Equal("Version 1.2.", Assert.Single(card.Body).Text);
    }

    /// <summary>
    ///     Proves that an unknown token is an error that names its file and line.
    /// </summary>
    [Fact]
    public void HelpCards_Tokens_UnknownToken_ReportsFileAndLine()
    {
        // Arrange: the token is on body line 2, which is file line 6
        var definition = SourceReading.Read(SourceReading.Files(("card.md", SourceReading.Card(body: "one\n{{nope}}"))));

        // Act
        var problem = Assert.Single(definition.Problems);

        // Assert
        Assert.Equal((RuleIds.TokenUnknown, "card.md", 6), (problem.RuleId, problem.File, problem.Line));
    }

    /// <summary>
    ///     Proves that translations are chosen per file with fallback to less specific cultures and the neutral file.
    /// </summary>
    [Fact]
    public void HelpCards_Cultures_Translation_IsSelectedPerFileWithFallback()
    {
        // Arrange: a card with a German translation, a card without, and the culture de-DE
        var files = SourceReading.Files(
            ("a.md", SourceReading.Card(title: "One")),
            ("a.de.md", "---\ntitle: Translated one\n---\nText."),
            ("b.md", SourceReading.Card(title: "Two")));

        // Act
        var german = SourceReading.Read(files, culture: CultureInfo.GetCultureInfo("de-DE"));
        var neutral = SourceReading.Read(files);

        // Assert: the language translation is used for a, the neutral file for b, and the neutral culture ignores translations
        Assert.Equal(["Translated one", "Two"], german.Cards.Select(card => card.Title));
        Assert.Equal(["One", "Two"], neutral.Cards.Select(card => card.Title));
    }

    /// <summary>
    ///     Proves that links are recognized outside code and that a typo is reported at its line.
    /// </summary>
    [Fact]
    public void HelpCards_Links_OutsideCode_AreRecognizedAndTyposReported()
    {
        // Arrange: a local link, an external link, a link in code, and a typo
        var body = "Local [[a/b#x]] and [[lib:c|the c card]].\n`[[no]]`\nTypo [[B]].";
        var definition = SourceReading.Read(SourceReading.Files(("card.md", SourceReading.Card(body: body))));

        // Act
        var links = Assert.Single(definition.Cards).Body.Where(part => part.Link is not null).Select(part => part.Link!).ToList();

        // Assert
        Assert.Equal(2, links.Count);
        Assert.Equal("lib", links[1].SourceName);
        var problem = Assert.Single(definition.Problems);
        Assert.Equal((RuleIds.LinkSyntax, 7), (problem.RuleId, problem.Line));
    }

    /// <summary>
    ///     Proves that every defect of a source is reported with severity, rule, source, file, line, and message.
    /// </summary>
    [Fact]
    public void HelpCards_Diagnostics_Defects_AreReportedWithAllFields()
    {
        // Arrange
        var definition = SourceReading.Read(SourceReading.Files(("Bad.md", SourceReading.Card())));

        // Act
        var problem = Assert.Single(definition.Problems);

        // Assert: all six fields of a problem are filled
        Assert.Equal(HelpSeverity.Error, problem.Severity);
        Assert.Equal(RuleIds.FileName, problem.RuleId);
        Assert.Equal("test", problem.SourceName);
        Assert.Equal("Bad.md", problem.File);
        Assert.Equal(0, problem.Line);
        Assert.False(string.IsNullOrWhiteSpace(problem.Message));
    }

    /// <summary>
    ///     Proves that all defects of several files are collected instead of stopping at the first.
    /// </summary>
    [Fact]
    public void HelpCards_Diagnostics_SeveralDefectiveFiles_AreAllCollected()
    {
        // Arrange: three different defects in three files, and one valid card
        var definition = SourceReading.Read(SourceReading.Files(
            ("a.md", "not front matter"),
            ("b.md", SourceReading.Card(extraFrontMatter: "bogus: 1\n")),
            ("c.md", SourceReading.Card(body: "{{nope}}")),
            ("d.md", SourceReading.Card())));

        // Act
        var files = definition.Problems.Select(problem => problem.File).Order(StringComparer.Ordinal).ToList();

        // Assert
        Assert.Equal(["a.md", "b.md", "c.md"], files);
        Assert.Contains(definition.Cards, card => card.LocalId == "d");
    }

    /// <summary>
    ///     Proves that a catalog exposes the tree, aliases, tags, see-also entries, examples, and links of a source.
    /// </summary>
    [Fact]
    public void HelpCards_Catalog_SimpleFixture_ExposesTreeAliasesTagsExamplesAndLinks()
    {
        // Arrange / Act: the embedded simple fixture, loaded the way an adopter loads embedded cards
        var catalog = CatalogBuilding.Simple();
        var guide = catalog.Find("start")!;

        // Assert: tree and aliases
        Assert.Equal(["guide", "guide/setup", "guide/usage", "reference", "reference/limits"], catalog.Cards.Select(card => card.Id));
        Assert.Equal(["guide/setup", "guide/usage"], guide.ChildIds);
        Assert.Equal(["tutorial", "authoring"], guide.Tags);

        // Assert: see-also, examples, and links (the link to another library is plain text)
        Assert.Equal(["reference/limits"], catalog.GetSeeAlso(guide).Select(card => card.Id));
        Assert.Equal([HelpExample.CommandKind, HelpExample.SourceKind, HelpExample.SnippetKind], guide.Examples.Select(example => example.Kind));
        Assert.Contains("the card `guide/setup`", guide.Body, StringComparison.Ordinal);
        Assert.Contains("the thing lives in another library", guide.Body, StringComparison.Ordinal);
        Assert.Contains("A code span such as `[[x]]` is not a link.", guide.Body, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a created catalog never changes and never reads a file again.
    /// </summary>
    [Fact]
    public void HelpCards_Catalog_Create_IsImmutableAndReadsNoFilesAfterwards()
    {
        // Arrange
        var provider = new CountingFileProvider(SourceReading.Files(("a.md", SourceReading.Card()), ("a/b.md", SourceReading.Card())));
        var catalog = CatalogBuilding.Create(HelpSource.FromProvider(provider, "test"));
        var calls = (provider.ListCount, provider.ReadCount);

        // Act: use every kind of member and make views
        _ = catalog.Cards.Select(card => card.Body).ToList();
        _ = catalog.Filter(["x"]).Without(["a"]).Where(_ => true).Find("a b");
        var cards = catalog.Cards.ToList();

        // Assert
        Assert.Equal((1, 2), calls);
        Assert.Equal(calls, (provider.ListCount, provider.ReadCount));
        Assert.True(((ICollection<HelpCard>)catalog.Cards).IsReadOnly);
        Assert.Equal(cards, catalog.Cards);
    }

    /// <summary>
    ///     Proves that siblings are ordered by the children list, then by order value, then by name.
    /// </summary>
    [Fact]
    public void HelpCards_Ordering_OrderFixture_FollowsChildrenOrderThenName()
    {
        // Arrange / Act
        var catalog = CatalogBuilding.Create(CatalogBuilding.Fixture("order"));

        // Assert
        Assert.Equal(["zeta", "alpha", "beta", "delta", "eta", "gamma"], catalog.Roots.Select(card => card.Id));
        Assert.Equal(["alpha/three", "alpha/two", "alpha/one", "alpha/four"], catalog.Find("alpha")!.ChildIds);
    }

    /// <summary>
    ///     Proves that libraries mounted under their names and an application that imports one of them compose into
    ///     one catalog with global ids, shared tokens, bare links, overlays, and cross-library links.
    /// </summary>
    [Fact]
    public void HelpCards_MultiSource_MountedLibrariesAndApp_ComposeOneCatalog()
    {
        // Arrange
        var libA = CatalogBuilding.LibA();

        // Act
        var catalog = HelpCatalog.Create(builder => builder
            .ToolName("demo")
            .AddMountedSource(libA)
            .AddMountedSource(CatalogBuilding.LibB())
            .AddSource(CatalogBuilding.App(libA)));

        // Assert: one tree grouped by source, with the application's cards after the libraries
        Assert.Equal(["lib-a", "lib-b", "start"], catalog.Roots.Select(card => card.Id));
        Assert.Equal(["lib-a", "lib-b", "app"], catalog.Sources.Select(info => info.Name));

        // Assert: the application sees the tokens and bare ids of the library it imports and extends its cards
        Assert.Contains("at most 10 nodes", catalog.Find("start")!.Body, StringComparison.Ordinal);
        Assert.Equal("Graph in the app", catalog.Find("lib-a/graph")!.Title);
        Assert.Contains("In the application, begin at the card `start`", catalog.Find("lib-a/graph")!.Body, StringComparison.Ordinal);

        // Assert: links between the libraries resolve because both are present
        Assert.Equal(["lib-b/chart", "start"], catalog.Find("lib-a/graph")!.SeeAlsoIds);
        Assert.Contains("the card `lib-a/graph`", catalog.Find("lib-b/chart")!.Body, StringComparison.Ordinal);
        Assert.Contains("lib-b", catalog.RenderIndex(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that links and optional overlays that name a source that is not present degrade without failing.
    /// </summary>
    [Fact]
    public void HelpCards_MultiSource_AbsentSources_DegradeToDisplayTextAndSkipOptionalOverlays()
    {
        // Arrange
        var source = CatalogBuilding.Named(
            "app",
            ("a.md", SourceReading.Card(body: "See [[other:thing|the thing]].", extraFrontMatter: "see-also:\n  - other:thing\n")),
            ("o.md", "---\nextends: other:thing\noptional: true\n---\nExtra."));

        // Act
        var catalog = HelpCatalog.Create(builder => builder.AddSource(source));

        // Assert
        Assert.Equal("See the thing.", catalog.Find("a")!.Body);
        Assert.Empty(catalog.GetSeeAlso(catalog.Find("a")!));
        Assert.Equal(
            ["external-source-absent", "external-source-absent", "overlay-optional-skipped"],
            catalog.Diagnostics.Select(problem => problem.RuleId).Order(StringComparer.Ordinal));
    }

    /// <summary>
    ///     Proves that clashes between sources are reported together in one exception that names both sources.
    /// </summary>
    [Fact]
    public void HelpCards_MultiSource_Collisions_AreCollectedInOneException()
    {
        // Arrange: a shared card id, a shared mount, and a shared source name
        var one = CatalogBuilding.Named("one", ("a.md", SourceReading.Card()));
        var two = CatalogBuilding.Named("two", ("a.md", SourceReading.Card()));
        var three = CatalogBuilding.Named("one", ("c.md", SourceReading.Card()));

        // Act
        var exception = Assert.Throws<HelpCatalogException>(() => HelpCatalog.Create(builder => builder
            .AddSource(one)
            .AddSource(two)
            .AddMountedSource(three, "x")));

        // Assert
        Assert.Equal(
            [("two", "collision"), ("one", "source-name-duplicate")],
            exception.Problems.Select(problem => (problem.SourceName, problem.RuleId)));
        Assert.Contains("(and 1 more)", exception.Message, StringComparison.Ordinal);
    }
    /// <summary>
    ///     Proves that forward references are valid and broken references are reported with file and line.
    /// </summary>
    [Fact]
    public void HelpCards_References_BrokenAndForwardReferences_AreHandled()
    {
        // Arrange: a forward link, a broken card link, a broken example link, and a broken see-also entry
        var valid = CatalogBuilding.FromFiles(("a.md", SourceReading.Card(body: "Forward [[z]].")), ("z.md", SourceReading.Card()));
        var errors = CatalogBuilding.Errors(
            ("a.md", SourceReading.Card(extraFrontMatter: "see-also:\n  - nope\n", body: "[[missing]]\n[[a#none]]")));

        // Act / Assert
        Assert.Equal("Forward the card `z`.", valid.Find("a")!.Body);
        Assert.Equal(
            [("see-also-target", 5), ("link-target", 7), ("example-target", 8)],
            errors.Select(problem => (problem.RuleId, problem.Line)));
    }

    /// <summary>
    ///     Proves that paths, aliases, and example names find cards and examples and failures say what was valid.
    /// </summary>
    [Fact]
    public void HelpCards_Lookup_PathsAndExamples_FindCardsAndExamples()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act / Assert
        Assert.Equal("reference/limits", catalog.Find("ref limits")?.Id);
        Assert.Null(catalog.Find("limits"));
        Assert.Equal(["setup", "usage"], catalog.Resolve("guide x").ValidNames);
        Assert.Equal(["guide"], catalog.FindExamples("run").Select(reference => reference.CardId));
        Assert.Equal(["guide/setup"], catalog.FindExamples("guide#check").Select(reference => reference.CardId));
        Assert.Equal("check", catalog.FindExample(catalog.Find("guide")!, "check")?.Example.Name);
    }

    /// <summary>
    ///     Proves that views show subsets, compose, lift children of hidden cards, and hide targets in bodies.
    /// </summary>
    [Fact]
    public void HelpCards_Views_FilterSourceWhereWithout_ShowSubsetsAndHideTargets()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act
        var view = catalog.Filter(["tutorial"]).Without(["guide"]);

        // Assert
        Assert.Equal(["guide/setup", "guide/usage"], view.Roots.Select(card => card.Id));
        Assert.Equal(["guide", "reference"], catalog.Roots.Select(card => card.Id));
        Assert.Contains("Look at Guide for", catalog.Without(["guide"]).Find("reference")!.Body, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => catalog.Without(["nothing"]));
    }

    /// <summary>
    ///     Proves that a tag vocabulary and expected roots are enforced.
    /// </summary>
    [Fact]
    public void HelpCards_Tags_VocabularyAndExpectedRoots_AreEnforced()
    {
        // Arrange
        var source = CatalogBuilding.Fixture("simple", new Dictionary<string, string> { ["limit"] = "42" });

        // Act
        var exception = Assert.Throws<HelpCatalogException>(() => CatalogBuilding.Create(
            source,
            builder => builder.TagVocabulary(["tutorial"], ["tutorial"]).ExpectedRoots(["guide", "other"])));

        // Assert: the roots mismatch, then per file the unknown tags and the missing required tag
        Assert.Equal(
            ["roots-mismatch", "tag-unknown", "tag-required", "tag-unknown", "tag-required", "tag-unknown"],
            exception.Problems.Select(problem => problem.RuleId));
    }

    /// <summary>
    ///     Proves that defects throw the catalog exception with all problems and misuse throws argument exceptions.
    /// </summary>
    [Fact]
    public void HelpCards_Diagnostics_CreateFailure_ThrowsCatalogExceptionForDefectsAndArgumentExceptionForMisuse()
    {
        // Arrange
        var defective = HelpSource.FromFiles(SourceReading.Files(("a.md", SourceReading.Card(body: "[[x]]")), ("b.md", "no front matter")), "test");

        // Act
        var exception = Assert.Throws<HelpCatalogException>(() => CatalogBuilding.Create(defective));

        // Assert
        Assert.Equal(["link-target", "syntax"], exception.Problems.Select(problem => problem.RuleId));
        Assert.All(exception.Problems, problem => Assert.Equal(HelpSeverity.Error, problem.Severity));
        Assert.Throws<ArgumentNullException>(() => HelpCatalog.Create(null!));
        Assert.Throws<ArgumentException>(() => HelpCatalog.Create(_ => { }));
    }

    /// <summary>
    ///     Proves that many threads can use one catalog at the same time with consistent results.
    /// </summary>
    [Fact]
    public void HelpCards_ThreadSafety_ParallelUse_IsConsistent()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var expected = string.Join("|", catalog.Cards.Select(card => card.Body));
        var calls = 0;
        var inconsistent = 0;

        // Act
        Parallel.For(0, 50, _ =>
        {
            var view = catalog.Where(card =>
            {
                Interlocked.Increment(ref calls);
                return true;
            });
            if (string.Join("|", view.Cards.Select(card => card.Body)) != expected || catalog.Find("guide setup") is null)
            {
                Interlocked.Increment(ref inconsistent);
            }
        });

        // Assert
        Assert.Equal(0, inconsistent);
        Assert.Equal(250, calls);
    }

    /// <summary>
    ///     Proves that creating a catalog twice gives equal results whatever the current culture is.
    /// </summary>
    [Fact]
    public void HelpCards_Determinism_CreateTwice_IsEqualRegardlessOfCurrentCulture()
    {
        // Arrange
        var original = CultureInfo.CurrentCulture;
        static string Describe(HelpCatalog catalog) => string.Join("|", catalog.Cards.Select(card => $"{card.Id}:{string.Join(",", card.ChildIds)}:{card.Body}"));

        try
        {
            // Act
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var first = Describe(CatalogBuilding.Simple());
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var second = Describe(CatalogBuilding.Simple());

            // Assert
            Assert.Equal(first, second);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    ///     Proves that the library references only framework assemblies.
    /// </summary>
    [Fact]
    public void HelpCards_NoDependencies_ReferencedAssemblies_AreFrameworkOnly()
    {
        // Arrange / Act
        var referenced = typeof(HelpCatalog).Assembly.GetReferencedAssemblies().Select(name => name.Name!).ToList();

        // Assert: only the base class library, never a package
        Assert.NotEmpty(referenced);
        Assert.All(referenced, name => Assert.True(
            name is "netstandard" or "System" || name.StartsWith("System.", StringComparison.Ordinal),
            $"The library references '{name}', which is not part of the framework."));
    }

    /// <summary>
    ///     Proves that a catalog exposes the translation selected for its culture and the culture of each body.
    /// </summary>
    [Fact]
    public void HelpCards_Cultures_Catalog_ExposesTranslatedCardsWithBodyCulture()
    {
        // Arrange / Act
        var catalog = CatalogBuilding.Create(CatalogBuilding.Fixture("i18n"), builder => builder.Culture(CultureInfo.GetCultureInfo("de-DE")));

        // Assert
        Assert.Equal(("Welcome (culture de-DE)", "de-DE"), (catalog.Find("welcome")!.Title, catalog.Find("welcome")!.Culture));
        Assert.Equal(("Farewell (language de)", "de"), (catalog.Find("farewell")!.Title, catalog.Find("farewell")!.Culture));
        Assert.Equal(("Plain", "en"), (catalog.Find("plain")!.Title, catalog.Find("plain")!.Culture));
    }

    /// <summary>
    ///     Proves that the guide card of the simple fixture renders as text equal to the golden file.
    /// </summary>
    [Fact]
    public void HelpCards_Rendering_SimpleFixtureText_EqualsGolden()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act
        var card = catalog.Render(catalog.Find("guide")!);
        var index = catalog.RenderIndex();

        // Assert
        GoldenFiles.AssertEqual("guide.text.expected.txt", card);
        GoldenFiles.AssertEqual("index.text.expected.txt", index);
    }

    /// <summary>
    ///     Proves that the guide card of the simple fixture renders as Markdown equal to the golden file.
    /// </summary>
    [Fact]
    public void HelpCards_Rendering_SimpleFixtureMarkdown_EqualsGolden()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var options = new HelpRenderOptions { Format = HelpFormat.Markdown };

        // Act
        var card = catalog.Render(catalog.Find("guide")!, options);
        var index = catalog.RenderIndex(options);

        // Assert
        GoldenFiles.AssertEqual("guide.markdown.expected.txt", card);
        GoldenFiles.AssertEqual("index.markdown.expected.txt", index);
    }

    /// <summary>
    ///     Proves that the guide card of the simple fixture renders as JSON equal to the golden file.
    /// </summary>
    [Fact]
    public void HelpCards_Rendering_SimpleFixtureJson_EqualsGolden()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var options = new HelpRenderOptions { Format = HelpFormat.Json };

        // Act
        var card = catalog.Render(catalog.Find("guide")!, options);
        var index = catalog.RenderIndex(options);

        // Assert
        GoldenFiles.AssertEqual("guide.expected.json", card);
        GoldenFiles.AssertEqual("index.expected.json", index);
    }

    /// <summary>
    ///     Proves that links are written in the plain, command, and Markdown styles and by a callback.
    /// </summary>
    [Fact]
    public void HelpCards_Rendering_LinkStyles_WriteTargetsInEachStyle()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var guide = catalog.Find("guide")!;

        // Act
        var plain = catalog.FormatBody(guide);
        var command = catalog.FormatBody(guide, new HelpRenderOptions { LinkStyle = HelpLinkStyle.Command });
        var markdown = catalog.FormatBody(guide, new HelpRenderOptions { LinkStyle = HelpLinkStyle.Markdown });
        var custom = catalog.FormatBody(guide, new HelpRenderOptions { LinkFormatter = link => $"<{link.CardId}>" });

        // Assert
        Assert.Contains("Start with the card `guide/setup`", plain, StringComparison.Ordinal);
        Assert.Contains("Start with `simple help guide setup`", command, StringComparison.Ordinal);
        Assert.Contains("Start with [Setup](guide/setup)", markdown, StringComparison.Ordinal);
        Assert.Contains("Start with <guide/setup> and then read <guide/usage>.", custom, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a card's examples are printed in full, the cards below are only listed, and one example is
    ///     printed alone as its exact content.
    /// </summary>
    [Fact]
    public void HelpCards_Rendering_Examples_ArePrintedInFullOrAlone()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var guide = catalog.Find("guide")!;
        var options = new HelpRenderOptions { LinkStyle = HelpLinkStyle.Command };

        // Act
        var examples = catalog.RenderExamples(guide, options);
        var alone = catalog.RenderExample(guide, "check");

        // Assert
        GoldenFiles.AssertEqual("examples.guide.expected.txt", examples);
        Assert.Equal("simple --version", alone);
    }

    /// <summary>
    ///     Proves that the JSON documents follow the contract: one line, ASCII only, version 1, and no null.
    /// </summary>
    [Fact]
    public void HelpCards_JsonContract_Documents_AreOneAsciiLineWithoutNull()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var options = new HelpRenderOptions { Format = HelpFormat.Json, LinkStyle = HelpLinkStyle.Command };
        var guide = catalog.Find("guide")!;
        var documents = new List<string>
        {
            catalog.RenderIndex(options),
            catalog.Render(guide, options),
            catalog.RenderExamples(guide, options),
            catalog.RenderExample(guide, "run", options),
            catalog.RenderSearch("tool", [new("guide", "Guide", "Summary.", "simple", 7, ["title"])], options),
            catalog.RenderCardList(catalog.Cards, options),
        };

        // Act / Assert
        foreach (var json in documents)
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            Assert.Equal(1, document.RootElement.GetProperty("helpSchemaVersion").GetInt32());
            Assert.True(document.RootElement.GetProperty("ok").GetBoolean());
            Assert.DoesNotContain("null", json, StringComparison.Ordinal);
            Assert.All(json, character => Assert.True(character is >= ' ' and < '\u007F'));
        }

        Assert.Equal(
            ["index", "card", "examples", "example", "search", "cards"],
            documents.Select(json => System.Text.Json.JsonDocument.Parse(json).RootElement.GetProperty("kind").GetString()));
    }

    /// <summary>
    ///     Proves that many threads can render from one catalog at the same time with consistent results and that the
    ///     callbacks of the caller are counted, not serialized.
    /// </summary>
    [Fact]
    public void HelpCards_ThreadSafety_ParallelRendering_IsConsistent()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var guide = catalog.Find("guide")!;
        var expectedText = catalog.Render(guide);
        var expectedJson = catalog.Render(guide, new HelpRenderOptions { Format = HelpFormat.Json });
        var linkCalls = 0;
        var linkOptions = new HelpRenderOptions
        {
            LinkFormatter = link =>
            {
                Interlocked.Increment(ref linkCalls);
                return link.CardId ?? "local";
            },
        };
        var inconsistent = 0;

        // Act
        Parallel.For(0, 40, _ =>
        {
            var view = catalog.Where(card => card.Id != "reference");
            var text = catalog.Render(guide);
            var json = catalog.Render(guide, new HelpRenderOptions { Format = HelpFormat.Json });
            var viewText = view.Render(guide, linkOptions);
            if (text != expectedText || json != expectedJson || viewText.Length == 0 || catalog.RenderIndex().Length == 0)
            {
                Interlocked.Increment(ref inconsistent);
            }
        });

        // Assert: every render by every worker called the callback for the same visible links and labels
        Assert.Equal(0, inconsistent);
        Assert.True(linkCalls > 0);
        Assert.Equal(0, linkCalls % 40);
    }

    /// <summary>
    ///     Proves that the public types and members of the library are exactly the part of the frozen surface of the
    ///     specification that this release implements, so that nothing else becomes public by accident.
    /// </summary>
    [Fact]
    public void HelpCards_PublicApi_ExportedTypes_MatchTheFrozenSurface()
    {
        // Arrange: the surface of the specification, which this release implements entirely
        string[] expected =
        [
            "DemaConsulting.HelpCards.Catalog.HelpCard: Aliases, Body, ChildIds, Culture, Examples, Extensions, Id, LocalId, Name, ParentId, SeeAlsoIds, SourceName, Summary, Tags, Title",
            "DemaConsulting.HelpCards.Catalog.HelpCatalog: Cards, Create, Culture, Diagnostics, Filter, FilterSource, Find, FindExample, FindExamples, FormatBody, GetChildren, GetSeeAlso, Render, RenderCardList, RenderExample, RenderExamples, RenderIndex, RenderSearch, Resolve, Roots, Search, Sources, Suggest, TagNames, Where, Without",
            "DemaConsulting.HelpCards.Catalog.HelpCatalogException: Problems",
            "DemaConsulting.HelpCards.Catalog.HelpExample: CommandKind, Content, Description, FileName, Kind, Language, Name, SnippetKind, SourceKind, SourceName",
            "DemaConsulting.HelpCards.Catalog.HelpExampleReference: .ctor, CardId, Example",
            "DemaConsulting.HelpCards.Catalog.HelpLink: CardId, ExampleCardId, ExampleName, SourceName, Text, Title",
            "DemaConsulting.HelpCards.Catalog.HelpPathResolution: Card, ParentId, Suggestion, UnknownSegment, ValidNames",
            "DemaConsulting.HelpCards.Catalog.HelpProblem: .ctor, File, Line, Message, RuleId, Severity, SourceName",
            "DemaConsulting.HelpCards.Catalog.HelpSearchOptions: .ctor, MaxResults, Sources, Tags",
            "DemaConsulting.HelpCards.Catalog.HelpSearchResult: .ctor, Id, MatchedOn, Score, SourceName, Summary, Title",
            "DemaConsulting.HelpCards.Catalog.HelpSeverity: Error, Info, Warning",
            "DemaConsulting.HelpCards.Catalog.HelpSourceInfo: .ctor, Language, Mount, Name, Schema, Summary, Title",
            "DemaConsulting.HelpCards.Composition.HelpCatalogBuilder: AddMountedSource, AddSource, Culture, ExpectedRoots, TagVocabulary, ToolName, ToolVersion",
            "DemaConsulting.HelpCards.Integration.HelpAgentTool: Description, Invoke, Name, ParametersSchema",
            "DemaConsulting.HelpCards.Integration.HelpAgentToolOptions: .ctor, Description, IndexDepth, MaxOutputChars, MaxResults, Name, ToolName",
            "DemaConsulting.HelpCards.Integration.HelpAgentTools: Create",
            "DemaConsulting.HelpCards.Integration.HelpCli: Create, Footer, Run, TryRun",
            "DemaConsulting.HelpCards.Integration.HelpCliAnswer: .ctor, ExitCode, Text",
            "DemaConsulting.HelpCards.Integration.HelpCliOptions: .ctor, CatalogFactory, CommandTemplate, DefaultWidth, EnableLanguageOption, Error, FallbackResolver, IndexPreamble, IndexSections, Output, OutputWidthProvider, ToolName, UsageLines, Verb, Version",
            "DemaConsulting.HelpCards.Integration.HelpCliSection: .ctor, Heading, Lines",
            "DemaConsulting.HelpCards.Loading.HelpSource: FromAssembly, FromDirectory, FromFiles, Imports, WithName, WithTokens",
            "DemaConsulting.HelpCards.Presentation.HelpFormat: Json, Markdown, Text",
            "DemaConsulting.HelpCards.Presentation.HelpLinkStyle: Command, Markdown, Plain",
            "DemaConsulting.HelpCards.Presentation.HelpRenderOptions: .ctor, CommandTemplate, Format, IncludeExampleContent, IncludeTags, JsonEnvelope, LinkFormatter, LinkStyle, Width",
            "DemaConsulting.HelpCards.Testing.ExampleCommandLine: TryParse",
            "DemaConsulting.HelpCards.Testing.HelpCardMarkdown: Bullets, GetSection, Headings",
            "DemaConsulting.HelpCards.Testing.HelpCardValidator: AssertValid, CompareToDirectory, Validate",
            "DemaConsulting.HelpCards.Testing.HelpExamples: Enumerate",
            "DemaConsulting.HelpCards.Testing.HelpValidationOptions: .ctor, MaxCardChars, MaxLineLength, MaxRootIndexChars, MaxRootSummaryChars, MaxSummaryChars, RequireExternalSources, Rules, WarningsAreErrors",
            "DemaConsulting.HelpCards.Testing.IHelpCardRule: Check"
        ];

        // Act
        var actual = DescribePublicSurface(typeof(HelpCatalog).Assembly).ToList();

        // Assert
        Assert.Equal(expected, actual);
    }

    /// <summary>
    ///     Describes every exported type with the names of its public members, without the members that the compiler
    ///     writes for records and for properties.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <returns>One line per exported type in ordinal order of the type name.</returns>
    private static IEnumerable<string> DescribePublicSurface(Assembly assembly)
    {
        var generated = new HashSet<string>(StringComparer.Ordinal) { "Equals", "GetHashCode", "ToString", "Deconstruct", "<Clone>$", "value__" };
        foreach (var type in assembly.GetExportedTypes().OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(member => member is not MethodBase { IsSpecialName: true } || member is ConstructorInfo)
                .Select(member => member.Name)
                .Where(name => !generated.Contains(name))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal);
            yield return $"{type.FullName}: {string.Join(", ", members)}";
        }
    }

    /// <summary>
    ///     Proves that rendering twice gives equal output whatever the current culture of the machine is.
    /// </summary>
    [Fact]
    public void HelpCards_Determinism_RenderTwice_IsEqualRegardlessOfCurrentCulture()
    {
        // Arrange
        var original = CultureInfo.CurrentCulture;
        static string Describe(HelpCatalog catalog)
        {
            var guide = catalog.Find("guide")!;
            return string.Join(
                "\n=====\n",
                catalog.Render(guide),
                catalog.Render(guide, new HelpRenderOptions { Format = HelpFormat.Markdown, LinkStyle = HelpLinkStyle.Markdown }),
                catalog.Render(guide, new HelpRenderOptions { Format = HelpFormat.Json }),
                catalog.RenderIndex(),
                catalog.RenderExamples(guide));
        }

        try
        {
            // Act
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var first = Describe(CatalogBuilding.Simple());
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var second = Describe(CatalogBuilding.Simple());

            // Assert
            Assert.Equal(first, second);
            Assert.DoesNotContain('\r', first);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    ///     Proves that the validator accepts the embedded simple source, reports a defective source as problems
    ///     without throwing, and fails an assertion with one exception for the errors.
    /// </summary>
    [Fact]
    public void HelpCards_Validation_SourcesAndCatalogs_ReportProblemsAndAssert()
    {
        // Arrange: a valid embedded source and a source with a broken link and a summary that is too long
        var valid = CatalogBuilding.Fixture("simple", new Dictionary<string, string> { ["limit"] = "42" });
        var broken = HelpSource.FromFiles(
            SourceReading.Files(("a.md", SourceReading.Card(body: "See [[missing]].")), ("b.md", SourceReading.Card(summary: new string('x', 80)))),
            "test");
        var longSummary = HelpSource.FromFiles(SourceReading.Files(("b.md", SourceReading.Card(summary: new string('x', 80)))), "test");

        // Act
        var validProblems = HelpCardValidator.Validate(valid);
        var errors = HelpCardValidator.Validate(broken);
        var warnings = HelpCardValidator.Validate(longSummary);
        var exception = Assert.Throws<HelpCatalogException>(() => HelpCardValidator.AssertValid(broken));

        // Assert
        Assert.Empty(validProblems);
        Assert.Equal(RuleIds.LinkTarget, Assert.Single(errors).RuleId);
        Assert.Equal(HelpSeverity.Warning, Assert.Single(warnings).Severity);
        Assert.Equal(errors, exception.Problems);
        HelpCardValidator.AssertValid(CatalogBuilding.Create(longSummary));
    }

    /// <summary>
    ///     Proves that examples are enumerated with their cards and that command examples parse into the arguments
    ///     an adopter would pass to its own entry point.
    /// </summary>
    [Fact]
    public void HelpCards_Validation_Examples_AreEnumeratedAndCommandsParsed()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act
        var all = HelpExamples.Enumerate(catalog).ToList();
        var commands = HelpExamples.Enumerate(catalog, [HelpExample.CommandKind]).ToList();
        var command = commands[0];
        var parsed = ExampleCommandLine.TryParse(command.Example.Content, "simple", out var arguments, out var error);
        var rejected = ExampleCommandLine.TryParse("simple run | more", "simple", out _, out var rejection);

        // Assert
        Assert.Equal(catalog.Cards.Sum(card => card.Examples.Count), all.Count);
        Assert.Equal(2, commands.Count);
        Assert.Equal("guide", command.Card.Id);
        Assert.True(parsed, error);
        Assert.Equal(["run", "input.txt"], arguments);
        Assert.False(rejected);
        Assert.Contains("shell operator '|'", rejection);
    }

    /// <summary>
    ///     Proves that the Markdown helpers read headings, sections, and bullets, ignoring fenced blocks.
    /// </summary>
    [Fact]
    public void HelpCards_Validation_MarkdownHelpers_ReadHeadingsSectionsAndBullets()
    {
        // Arrange
        var body = "## Items\n\n- First: one.\n- Second: two.\n\n```text\n## Hidden\n```\n\n## Other\n\nText.";

        // Act
        var headings = HelpCardMarkdown.Headings(body);
        var bullets = HelpCardMarkdown.Bullets(body, "Items");
        var section = HelpCardMarkdown.GetSection(body, "Other");

        // Assert
        Assert.Equal(["Items", "Other"], headings);
        Assert.Equal(["First: one.", "Second: two."], bullets);
        Assert.Equal("Text.", section);
    }

    /// <summary>
    ///     Proves that embedded resources and a directory on disk are compared in both directions.
    /// </summary>
    [Fact]
    public void HelpCards_Validation_CompareToDirectory_ReportsFilesOnOneSideOnly()
    {
        // Arrange: a directory with every embedded file of the simple source, one of them removed and one added
        var source = CatalogBuilding.Fixture("simple");
        using var directory = new TemporaryDirectory();
        foreach (var path in source.Provider.ListFiles())
        {
            directory.Write(path, source.Provider.ReadText(path));
        }

        File.Delete(Path.Combine(directory.FullPath, "guide.md"));
        directory.Write("added.md", "text");

        // Act
        var problems = HelpCardValidator.CompareToDirectory(source, directory.FullPath);

        // Assert
        Assert.Equal(["added.md", "guide.md"], problems.Select(problem => problem.File));
        Assert.All(problems, problem => Assert.Equal(RuleIds.ResourcesMismatch, problem.RuleId));
    }

    /// <summary>
    ///     Proves that search ranks the cards of a twelve-card catalog exactly as the golden report pins, whatever the
    ///     current culture of the machine is.
    /// </summary>
    [Fact]
    public void HelpCards_Search_BigFixture_MatchesRankingGolden()
    {
        // Arrange
        var catalog = CatalogBuilding.Create(CatalogBuilding.Fixture("big"));
        var original = CultureInfo.CurrentCulture;

        try
        {
            // Act: the Turkish culture changes the case of the letter i, which must not change a ranking
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var report = RankingReport.Describe(catalog);

            // Assert
            GoldenFiles.AssertEqual("cli", "ranking.expected.txt", report);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    ///     Proves that a word that was not found gets the closest names, aliases, and ids of visible cards.
    /// </summary>
    [Fact]
    public void HelpCards_Suggestions_UnknownNames_SuggestClosest()
    {
        // Arrange
        var catalog = CatalogBuilding.Create(CatalogBuilding.Fixture("big"));

        // Act
        var name = catalog.Suggest("buid");
        var alias = catalog.Suggest("shipp");
        var id = catalog.Suggest("deploy/rolback");
        var path = catalog.Resolve("deploy stagin");

        // Assert
        Assert.Equal("build", name[0]);
        Assert.Equal(["ship"], alias);
        Assert.Equal(["deploy/rollback"], id);
        Assert.Equal("staging", path.Suggestion);
        Assert.Empty(catalog.Suggest("qqqqqqqq"));
    }

    /// <summary>
    ///     Proves that a tool answers the help verb with the helper and leaves every other command alone, without ever
    ///     creating its catalog for those.
    /// </summary>
    [Fact]
    public void HelpCards_Cli_HelpVerb_RunsHelpAndLeavesOtherCommandsAlone()
    {
        // Arrange
        var harness = new CliHarness(factory: _ => throw new InvalidOperationException("No catalog is needed."));
        var simple = new CliHarness(CatalogBuilding.Simple());

        // Act
        string[][] commands = [[], ["--help"], ["build"]];
        var others = commands.Select(args => harness.Cli.TryRun(args, out _)).ToList();
        var handled = simple.Cli.TryRun(["help", "guide"], out var exitCode);

        // Assert
        Assert.All(others, Assert.False);
        Assert.Equal("Run 'tool help' for topics and examples.", harness.Cli.Footer);
        Assert.Equal(0, harness.FactoryCalls);
        Assert.True(handled);
        Assert.Equal(0, exitCode);
        GoldenFiles.AssertEqual("cli", "card.text.expected.txt", simple.Output.ToString().TrimEnd('\n'));
    }

    /// <summary>
    ///     Proves that the options of the help command select the index, a card, examples, an example, a search, and a
    ///     tag listing, in text and JSON, as the golden files record.
    /// </summary>
    [Fact]
    public void HelpCards_Cli_Options_SelectModesAndFormats()
    {
        // Arrange
        var simple = new CliHarness(CatalogBuilding.Simple(), usage: ["tool [options]", "tool help [PATH]"]);
        var examples = new CliHarness(CatalogBuilding.Simple());
        var search = new CliHarness(CatalogBuilding.Create(CatalogBuilding.Fixture("big")));
        var tags = new CliHarness(CatalogBuilding.Create(CatalogBuilding.Fixture("big")));

        // Act
        simple.Run("--json");
        examples.Run("guide", "--examples");
        search.Run("--search", "build", "--json");
        tags.Run("--tag", "quality", "--json");

        // Assert
        GoldenFiles.AssertEqual("cli", "index.expected.json", simple.Output.ToString().TrimEnd('\n'));
        GoldenFiles.AssertEqual("cli", "examples.text.expected.txt", examples.Output.ToString().TrimEnd('\n'));
        GoldenFiles.AssertEqual("cli", "search.expected.json", search.Output.ToString().TrimEnd('\n'));
        GoldenFiles.AssertEqual("cli", "tags.expected.json", tags.Output.ToString().TrimEnd('\n'));
    }

    /// <summary>
    ///     Proves that wrong arguments and unknown names exit with 2, a defective catalog exits with 70, and the error is
    ///     written to the channel of the format.
    /// </summary>
    [Fact]
    public void HelpCards_Cli_Failures_UseExitCodes()
    {
        // Arrange
        var broken = HelpSource.FromFiles(SourceReading.Files(("a.md", SourceReading.Card(body: "See [[missing]]."))), "test");
        var unknown = new CliHarness(CatalogBuilding.Simple());
        var json = new CliHarness(CatalogBuilding.Simple());
        var defective = new CliHarness(factory: _ => CatalogBuilding.Create(broken));

        // Act
        var unknownCode = unknown.Run("guide", "usag");
        var jsonCode = json.Run("guide", "setupp", "--json");
        var defectiveCode = defective.Run("--json");

        // Assert
        Assert.Equal((2, 2, 70), (unknownCode, jsonCode, defectiveCode));
        GoldenFiles.AssertEqual("cli", "notfound.text.expected.txt", unknown.Error.ToString().TrimEnd('\n'));
        GoldenFiles.AssertEqual("cli", "notfound.expected.json", json.Output.ToString().TrimEnd('\n'));
        Assert.Contains("\"code\":\"catalog-error\"", defective.Output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a tool can add sections to the index, answer subjects of its own, choose the width, and pass a
    ///     culture to its catalog factory.
    /// </summary>
    [Fact]
    public void HelpCards_Cli_Extensions_FallbackSectionsWidthAndLanguage()
    {
        // Arrange
        var harness = new CliHarness(
            CatalogBuilding.Simple(),
            language: true,
            sections: [new HelpCliSection("Commands:", ["run  Run the tool"])],
            fallback: words => words[0] == "run" ? new HelpCliAnswer("Help of run.", 5) : null,
            width: () => 50);

        // Act
        var index = harness.Run("--lang", "de-DE", "--width", "auto");
        var answered = harness.Run("run");

        // Assert
        Assert.Equal((0, 5), (index, answered));
        Assert.Contains("Commands:\n  run  Run the tool\n\nHelp topics:", harness.Output.ToString(), StringComparison.Ordinal);
        Assert.EndsWith("Help of run.\n", harness.Output.ToString(), StringComparison.Ordinal);
        Assert.Equal("de-DE", harness.LastCulture!.Name);
    }

    /// <summary>
    ///     Proves that one knowledge tool gives an agent the index, a card, an example, and search results for the whole
    ///     catalog, with the name a host chooses.
    /// </summary>
    [Fact]
    public void HelpCards_AgentTool_Calls_AnswerIndexCardExampleAndSearch()
    {
        // Arrange
        var tool = HelpAgentTools.Create(CatalogBuilding.Simple(), new HelpAgentToolOptions { Name = "canvasnet_help" });

        // Act
        var index = tool.Invoke(null);
        var card = tool.Invoke("{\"topic\":\"guide/setup\"}");
        var example = tool.Invoke("{\"example\":\"check\"}");
        var search = tool.Invoke("{\"query\":\"setup\"}");

        // Assert
        Assert.Equal("canvasnet_help", tool.Name);
        Assert.StartsWith("Help index.", index, StringComparison.Ordinal);
        Assert.StartsWith("# Setup\n", card, StringComparison.Ordinal);
        Assert.Contains("simple --version", example, StringComparison.Ordinal);
        Assert.Contains("guide/setup", search, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the schema of the tool is the text of the contract, with no unknown property allowed.
    /// </summary>
    [Fact]
    public void HelpCards_AgentTool_Schema_IsTheSpecifiedText()
    {
        // Arrange
        var tool = HelpAgentTools.Create(CatalogBuilding.Simple());

        // Act
        var schema = tool.ParametersSchema;

        // Assert
        Assert.Equal(
            "{\"type\":\"object\",\"properties\":{\"topic\":{\"type\":\"string\",\"description\":\"Card id or path such as svg/framed-figure; names and aliases work, case does not matter.\"},\"example\":{\"type\":\"string\",\"description\":\"Example name, or card-id#name when several cards have an example of that name. With topic, only that card and the cards below it are searched.\"},\"query\":{\"type\":\"string\",\"description\":\"Words to search for in titles, summaries, tags, aliases, and ids.\"},\"source\":{\"type\":\"string\",\"description\":\"Restrict the index or the search to one source (library) name.\"},\"tag\":{\"type\":\"string\",\"description\":\"Restrict the index or the search to cards with this tag.\"},\"maxResults\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":20,\"description\":\"The most search results to return.\"}},\"additionalProperties\":false}",
            schema);
        Assert.StartsWith("Unknown argument 'x'.", tool.Invoke("{\"x\":1}"), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that an answer over the output limit is cut and says so, and that bad input never makes the tool throw.
    /// </summary>
    [Fact]
    public void HelpCards_AgentTool_BudgetAndBoundary_CutAnswersAndNeverThrowForBadInput()
    {
        // Arrange
        var body = string.Join("\n", Enumerable.Range(1, 300).Select(number => $"Line {number} of a long card body."));
        var catalog = CatalogBuilding.FromFiles(("long.md", SourceReading.Card(title: "Long", body: body)));
        var tool = HelpAgentTools.Create(catalog, new HelpAgentToolOptions { MaxOutputChars = 1000 });

        // Act
        var cut = tool.Invoke("{\"topic\":\"long\"}");
        var answers = new[] { "{", "[]", "\uD800", "{\"topic\":[]}", "{\"maxResults\":\"x\"}" }.Select(tool.Invoke).ToList();

        // Assert
        Assert.Contains("[Truncated: ", cut, StringComparison.Ordinal);
        Assert.All(answers, answer => Assert.False(string.IsNullOrWhiteSpace(answer)));
    }
}
