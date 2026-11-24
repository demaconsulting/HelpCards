using System.Globalization;
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Loading;

/// <summary>
///     Subsystem tests for Loading: sources, files, front matter, cards, tokens, cultures, and bodies working together.
/// </summary>
/// <remarks>
///     These tests use the Loading units through their documented collaboration (a source, its provider, and the
///     reader that uses the decoder, token resolver, culture selector, and body parser) plus the problem model
///     of the Catalog subsystem, and nothing else.
/// </remarks>
public class LoadingTests
{
    /// <summary>
    ///     The resource prefix of the embedding fixture in this test assembly.
    /// </summary>
    private const string FixturePrefix = "DemaConsulting.HelpCards.Tests.Help/embedding/Help";

    /// <summary>
    ///     The files of a small source with a nested card and an example file.
    /// </summary>
    private static readonly (string Path, string Text)[] SmallSource =
    [
        ("_source.md", "---\nname: small\ntitle: Small\nsummary: A small source.\n---\nWelcome."),
        ("guide.md", "---\ntitle: Guide\nsummary: The guide.\nexamples:\n  - name: chart\n    kind: source\n    language: mermaid\n    file: chart.mmd\n    description: A chart.\n---\nSee [[guide/setup]]."),
        ("guide/setup.md", "---\ntitle: Setup\nsummary: Setting up.\n---\nSetup {{tool}}."),
        ("chart.mmd", "graph TD\n  A --> B\n")
    ];

    /// <summary>
    ///     Proves that a source created from embedded resources is read into cards and examples.
    /// </summary>
    [Fact]
    public void Loading_AssemblySource_ReadsEmbeddedFixtureCards()
    {
        // Arrange: the embedding fixture compiled into this test assembly by the packaged targets
        var source = HelpSource.FromAssembly(typeof(LoadingTests).Assembly, FixturePrefix);

        // Act
        var definition = SourceReading.Read(source);

        // Assert: the source name from _source.md, both cards, and the example file read from its subfolder
        Assert.Empty(definition.Problems);
        Assert.Equal("embedding", definition.Name);
        Assert.Equal(["guide", "guide/setup"], definition.Cards.Select(card => card.LocalId));
        Assert.Equal("graph TD\n  A --> B", Assert.Single(definition.Cards[0].Examples).Content);
    }

    /// <summary>
    ///     Proves that a source created from a directory reads the same cards as the same files in memory.
    /// </summary>
    [Fact]
    public void Loading_DirectorySource_ReadsSameCardsAsMemorySource()
    {
        // Arrange: the same files in memory and on disk
        using var directory = new TemporaryDirectory();
        foreach (var (path, text) in SmallSource)
        {
            directory.Write(path, text);
        }

        // Act
        var fromMemory = SourceReading.Read(SourceReading.Files(SmallSource), name: null);
        var fromDisk = SourceReading.Read(HelpSource.FromDirectory(directory.FullPath));

        // Assert: the same ids, titles, example content, and no problems
        Assert.Empty(fromMemory.Problems);
        Assert.Empty(fromDisk.Problems);
        Assert.Equal(fromMemory.Cards.Select(Describe), fromDisk.Cards.Select(Describe));
    }

    /// <summary>
    ///     Proves that an in-memory source reads cards, links, tokens, and examples together.
    /// </summary>
    [Fact]
    public void Loading_MemorySource_ReadsCardsLinksTokensAndExamples()
    {
        // Arrange / Act
        var definition = SourceReading.Read(SourceReading.Files(SmallSource), name: null);

        // Assert
        Assert.Empty(definition.Problems);
        var guide = definition.Cards[0];
        Assert.Contains(guide.Body, part => part.Link is { CardId: "guide/setup" });
        Assert.Equal("Setup demo.", Assert.Single(definition.Cards[1].Body).Text);
        Assert.Equal("graph TD\n  A --> B", Assert.Single(guide.Examples).Content);
        Assert.Equal("Small", definition.Title);
    }

    /// <summary>
    ///     Proves that creating a source does no I/O and reading lists once and reads each file once.
    /// </summary>
    [Fact]
    public void Loading_SourceConstruction_PerformsNoIoUntilRead()
    {
        // Arrange: a provider that counts calls
        var provider = new CountingFileProvider(SourceReading.Files(SmallSource));

        // Act: construct the source with every With method, then read it
        var source = HelpSource.FromProvider(provider).WithName("small").WithTokens(new Dictionary<string, string>());
        var callsAfterConstruction = provider.ListCount + provider.ReadCount;
        SourceReading.Read(source);

        // Assert
        Assert.Equal(0, callsAfterConstruction);
        Assert.Equal(1, provider.ListCount);
        Assert.Equal(4, provider.ReadCount);
    }

    /// <summary>
    ///     Proves that problems of every kind are collected in one read.
    /// </summary>
    [Fact]
    public void Loading_Problems_AreCollectedAcrossFilesAndUnits()
    {
        // Arrange: a bad file name, a syntax error, an unknown key, an unknown token, a link typo, and a missing example file
        var definition = SourceReading.Read(SourceReading.Files(
            ("Bad.md", SourceReading.Card()),
            ("syntax.md", "---\ntitle: [x]\n---\n"),
            ("key.md", SourceReading.Card(extraFrontMatter: "bogus: 1\n")),
            ("token.md", SourceReading.Card(body: "{{nope}}")),
            ("link.md", SourceReading.Card(body: "[[Typo]]")),
            ("example.md", SourceReading.Card(extraFrontMatter: "examples:\n  - name: x\n    kind: source\n    language: yaml\n    file: gone.yaml\n    description: D.\n"))));

        // Act
        var rules = definition.Problems.Select(problem => problem.RuleId).Order(StringComparer.Ordinal).ToList();

        // Assert: one problem per defect, all errors
        Assert.Equal(
            ["example-file-missing", "file-name", "key-unknown", "link-syntax", "syntax", "token-unknown"],
            rules);
        Assert.All(definition.Problems, problem => Assert.Equal(HelpSeverity.Error, problem.Severity));
        Assert.Equal(["key", "syntax"], definition.FailedIds.Order(StringComparer.Ordinal));
    }

    /// <summary>
    ///     Proves that the translation is chosen for each file on its own.
    /// </summary>
    [Fact]
    public void Loading_Cultures_SelectTranslationPerFile()
    {
        // Arrange: one translated card and one that falls back to the neutral file
        var files = SourceReading.Files(
            ("a.md", SourceReading.Card(title: "One", body: "English one.")),
            ("a.de.md", "---\ntitle: Translated one\n---\nTranslated one."),
            ("b.md", SourceReading.Card(title: "Two", body: "English two.")));

        // Act
        var definition = SourceReading.Read(files, culture: CultureInfo.GetCultureInfo("de-DE"));

        // Assert
        Assert.Equal(["Translated one", "Two"], definition.Cards.Select(card => card.Title));
        Assert.Equal(["de", "en"], definition.Cards.Select(card => card.Culture));
    }

    /// <summary>
    ///     Proves that the file that supplied the body is recorded: the neutral file, or the selected translation file.
    /// </summary>
    [Fact]
    public void Loading_Cultures_BodyFileIsNeutralOrSelectedTranslation()
    {
        // Arrange
        var files = SourceReading.Files(
            ("a.md", SourceReading.Card(body: "English.")),
            ("a.de.md", "---\ntitle: One\n---\nGerman body."));

        // Act
        var neutral = SourceReading.Read(files);
        var german = SourceReading.Read(files, culture: CultureInfo.GetCultureInfo("de"));

        // Assert: the definition keeps the neutral file and records where the body came from
        Assert.Equal(("a.md", "a.md"), (Assert.Single(neutral.Cards).File, Assert.Single(neutral.Cards).BodyFile));
        Assert.Equal(("a.md", "a.de.md"), (Assert.Single(german.Cards).File, Assert.Single(german.Cards).BodyFile));
    }

    /// <summary>
    ///     Proves that the tokens of an imported source are in scope for the cards of the importing source.
    /// </summary>
    [Fact]
    public void Loading_Tokens_ImportedTokensAreInScope()
    {
        // Arrange: a source whose card uses a token that only the imported source defines
        var imported = HelpSource.FromFiles(new Dictionary<string, string>(), "base")
            .WithTokens(new Dictionary<string, string> { ["maxDpi"] = "600" });
        var source = HelpSource.FromFiles(SourceReading.Files(("a.md", SourceReading.Card(body: "Up to {{maxDpi}} dpi."))), "app")
            .Imports(imported);

        // Act: build the scope the way composition does, from the import list
        var definition = CardReader.Read(
            source,
            CultureInfo.InvariantCulture,
            name => TokenResolver.Create(
                name,
                source.Tokens,
                source.ImportedSources.Select(i => new KeyValuePair<string, IReadOnlyDictionary<string, string>>(i.Name!, i.Tokens)),
                name,
                string.Empty));

        // Assert
        Assert.Empty(definition.Problems);
        Assert.Equal("Up to 600 dpi.", Assert.Single(Assert.Single(definition.Cards).Body).Text);
    }

    /// <summary>
    ///     Proves that links are recognized outside code only and typos are reported with their lines.
    /// </summary>
    [Fact]
    public void Loading_BodyLinks_AreRecognizedOutsideCodeOnly()
    {
        // Arrange: a link, a link in a span, a link in a fence, and a typo
        const string body = "Real [[a/b]].\nSpan `[[x]]`.\n```\n[[y]]\n```\nTypo [[Z]].";
        var definition = SourceReading.Read(SourceReading.Files(("a.md", SourceReading.Card(body: body))));

        // Act
        var links = Assert.Single(definition.Cards).Body.Where(part => part.Link is not null).Select(part => part.Link!).ToList();

        // Assert: one real link, one typo at the right line
        Assert.Equal("a/b", Assert.Single(links).CardId);
        var problem = Assert.Single(definition.Problems);
        Assert.Equal((RuleIds.LinkSyntax, "a.md", 10), (problem.RuleId, problem.File, problem.Line));
    }

    /// <summary>
    ///     Proves that a front-matter error names the file and the line.
    /// </summary>
    [Fact]
    public void Loading_FrontMatter_ErrorNamesFileAndLine()
    {
        // Arrange: a duplicate key on line 4
        var definition = SourceReading.Read(SourceReading.Files(("docs/a.md", "---\ntitle: A\nsummary: S\ntitle: B\n---\n")));

        // Act
        var problem = Assert.Single(definition.Problems);

        // Assert
        Assert.Equal((RuleIds.Syntax, "docs/a.md", 4), (problem.RuleId, problem.File, problem.Line));
        Assert.Contains("title", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a file that cannot be read becomes a problem and the rest of the source is still read.
    /// </summary>
    [Fact]
    public void Loading_ReadFailure_IsReportedAndOtherFilesAreRead()
    {
        // Arrange
        var provider = new CountingFileProvider(
            SourceReading.Files(("a.md", SourceReading.Card()), ("b.md", SourceReading.Card())),
            failingReads: ["b.md"]);

        // Act
        var definition = SourceReading.Read(HelpSource.FromProvider(provider, "test"));

        // Assert
        Assert.Equal(RuleIds.SourceRead, Assert.Single(definition.Problems).RuleId);
        Assert.Equal(["a"], definition.Cards.Select(card => card.LocalId));
    }

    /// <summary>
    ///     Proves that reading the same source twice gives the same cards and problems in the same order.
    /// </summary>
    [Fact]
    public void Loading_Reading_IsDeterministic()
    {
        // Arrange: a source with several problems
        var files = SourceReading.Files(
            ("b.md", SourceReading.Card(body: "{{one}} {{two}}")),
            ("a.md", SourceReading.Card(extraFrontMatter: "x: 1\n")),
            ("C.md", SourceReading.Card()));

        // Act
        var first = SourceReading.Read(files);
        var second = SourceReading.Read(files);

        // Assert
        Assert.Equal(first.Problems, second.Problems);
        Assert.Equal(first.Cards.Select(Describe), second.Cards.Select(Describe));
    }

    /// <summary>
    ///     Describes a card by its id, title, culture, and example contents, for comparisons.
    /// </summary>
    /// <param name="card">The card.</param>
    /// <returns>A text that is equal for equal cards.</returns>
    private static string Describe(CardDefinition card)
    {
        var examples = string.Join("|", card.Examples.Select(example => example.Name + "=" + example.Content));
        var body = string.Concat(card.Body.Select(part => part.Text ?? "[" + part.Link!.CardId + "]"));
        return $"{card.LocalId}/{card.Title}/{card.Culture}/{examples}/{body}";
    }
}
