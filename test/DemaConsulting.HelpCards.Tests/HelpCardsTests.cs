using System.Globalization;
using System.Reflection;
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;
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
}
