using System.Globalization;
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Loading;

/// <summary>
///     Unit tests for <see cref="CardReader"/>: the source file and read failures.
/// </summary>
public partial class CardReaderTests
{
    /// <summary>
    ///     Reads a source whose name is not set in code, so the name must come from <c>_source.md</c>.
    /// </summary>
    /// <param name="files">The files of the source.</param>
    /// <returns>The definition of the source.</returns>
    private static SourceDefinition ReadUnnamed(params (string Path, string Text)[] files)
    {
        return SourceReading.Read(SourceReading.Files(files), name: null);
    }

    /// <summary>
    ///     Proves that every key of <c>_source.md</c> is read with exact values.
    /// </summary>
    [Fact]
    public void CardReader_Read_SourceFileWithEveryKey_ReturnsExactDescription()
    {
        // Arrange
        const string text = """
            ---
            name: mermaid-lib
            title: Mermaid library
            summary: Diagrams from text.
            language: en
            schema: 1
            aliases:
              - mm
            children:
              - flow
            x-owner: team
            ---
            Welcome to {{tool}}.
            """;

        // Act
        var source = ReadUnnamed(("_source.md", text), ("flow.md", SourceReading.Card()));

        // Assert
        Assert.Empty(source.Problems);
        Assert.Equal("mermaid-lib", source.Name);
        Assert.Equal("_source.md", source.File);
        Assert.Equal("Mermaid library", source.Title);
        Assert.Equal("Diagrams from text.", source.Summary);
        Assert.Equal("en", source.Language);
        Assert.Equal(1, source.Schema);
        Assert.Equal(new LocatedValue("mm", "_source.md", 8), Assert.Single(source.Aliases));
        Assert.Equal(new LocatedValue("flow", "_source.md", 10), Assert.Single(source.Children));
        Assert.Equal("team", source.Extensions["x-owner"]);
        Assert.Equal("Welcome to demo.", Assert.Single(source.Body).Text);
        Assert.Equal(13, source.BodyLine);
        Assert.Equal("en", source.Culture);
        Assert.Single(source.Cards);
    }

    /// <summary>
    ///     Proves that a source without a source file gets the defaults of title, summary, language, and schema.
    /// </summary>
    [Fact]
    public void CardReader_Read_NoSourceFile_UsesDefaults()
    {
        // Arrange / Act
        var source = Read(("a.md", SourceReading.Card()));

        // Assert
        Assert.Empty(source.Problems);
        Assert.Equal("test", source.Name);
        Assert.Equal(string.Empty, source.File);
        Assert.Equal("test", source.Title);
        Assert.Equal("Help cards of test.", source.Summary);
        Assert.Equal("en", source.Language);
        Assert.Equal(1, source.Schema);
        Assert.Empty(source.Body);
    }

    /// <summary>
    ///     Proves that a name set in code takes precedence over the name of the source file.
    /// </summary>
    [Fact]
    public void CardReader_Read_NameInCodeAndInFile_CodeWins()
    {
        // Arrange / Act
        var source = Read(("_source.md", "---\nname: from-file\n---\n"));

        // Assert: the name set by SourceReading.Read is "test"
        Assert.Equal("test", source.Name);
        Assert.Empty(source.Problems);
    }

    /// <summary>
    ///     Proves that a source without any name is reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_NoNameAnywhere_ReportsSourceInvalid()
    {
        // Arrange / Act
        var source = ReadUnnamed(("a.md", SourceReading.Card()));

        // Assert: the problem uses the placeholder name
        var problem = Assert.Single(source.Problems);
        Assert.Equal(RuleIds.SourceInvalid, problem.RuleId);
        Assert.Equal(CardReader.UnnamedSource, problem.SourceName);
        Assert.Equal(string.Empty, problem.File);
    }

    /// <summary>
    ///     Proves that a source file without a name is reported against the source file.
    /// </summary>
    [Fact]
    public void CardReader_Read_SourceFileWithoutName_ReportsSourceInvalidForFile()
    {
        // Arrange / Act
        var source = ReadUnnamed(("_source.md", "---\ntitle: T\n---\n"));

        // Assert
        var problem = Assert.Single(source.Problems);
        Assert.Equal(RuleIds.SourceInvalid, problem.RuleId);
        Assert.Equal("_source.md", problem.File);
    }

    /// <summary>
    ///     Proves that a name that is not a segment is reported, whether it comes from code or from the file.
    /// </summary>
    [Fact]
    public void CardReader_Read_InvalidName_ReportsSourceInvalid()
    {
        // Arrange / Act
        var fromCode = SourceReading.Read(SourceReading.Files(("a.md", SourceReading.Card())), name: "Bad Name");
        var fromFile = ReadUnnamed(("_source.md", "---\nname: Bad_Name\n---\n"));

        // Assert: from code there is no file or line; from the file the line of the name
        var codeProblem = Assert.Single(fromCode.Problems);
        Assert.Equal((RuleIds.SourceInvalid, string.Empty, 0), (codeProblem.RuleId, codeProblem.File, codeProblem.Line));
        var fileProblem = Assert.Single(fromFile.Problems);
        Assert.Equal((RuleIds.SourceInvalid, "_source.md", 2), (fileProblem.RuleId, fileProblem.File, fileProblem.Line));
    }

    /// <summary>
    ///     Proves that a schema above the supported one is reported and no card is read.
    /// </summary>
    [Fact]
    public void CardReader_Read_UnsupportedSchema_ReportsSchemaUnsupportedAndReadsNoCards()
    {
        // Arrange
        var source = Read(("_source.md", "---\nschema: 3\n---\n"), ("a.md", SourceReading.Card()));

        // Act
        var problem = Assert.Single(source.Problems);

        // Assert: the message names the source, its schema, and the highest supported schema
        AssertProblem(problem, RuleIds.SchemaUnsupported, "_source.md", 2);
        Assert.Contains("'test'", problem.Message, StringComparison.Ordinal);
        Assert.Contains("schema 3", problem.Message, StringComparison.Ordinal);
        Assert.Contains("supports is 2", problem.Message, StringComparison.Ordinal);
        Assert.Empty(source.Cards);
        Assert.Equal(3, source.Schema);
    }

    /// <summary>
    ///     Proves that an omitted schema means schema 1 and that a schema that is not a positive integer is reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_SchemaValues_DefaultsToOneAndRejectsInvalid()
    {
        // Arrange / Act
        var omitted = Read(("_source.md", "---\ntitle: T\n---\n"));
        var zero = Read(("_source.md", "---\nschema: 0\n---\n"));
        var text = Read(("_source.md", "---\nschema: one\n---\n"));

        // Assert
        Assert.Equal(1, omitted.Schema);
        AssertProblem(Assert.Single(zero.Problems), RuleIds.ValueInvalid, "_source.md", 2);
        AssertProblem(Assert.Single(text.Problems), RuleIds.ValueInvalid, "_source.md", 2);
    }

    /// <summary>
    ///     Proves that an invalid language is reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_InvalidLanguage_ReportsCultureInvalid()
    {
        // Arrange / Act
        var source = Read(("_source.md", "---\nlanguage: English\n---\n"));

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.CultureInvalid, "_source.md", 2);
        Assert.Equal("en", source.Language);
    }

    /// <summary>
    ///     Proves that card-only keys are unknown in the source file.
    /// </summary>
    [Fact]
    public void CardReader_Read_CardKeysInSourceFile_ReportKeyUnknown()
    {
        // Arrange / Act
        var source = Read(("_source.md", "---\ntags:\n  - x\norder: 1\n---\n"));

        // Assert
        Assert.Equal([2, 4], source.Problems.Select(problem => problem.Line));
        Assert.All(source.Problems, problem => Assert.Equal(RuleIds.KeyUnknown, problem.RuleId));
    }

    /// <summary>
    ///     Proves that a broken source file is reported once and does not stop the cards from being read.
    /// </summary>
    [Fact]
    public void CardReader_Read_SourceFileSyntaxError_ReportsOnceAndReadsCards()
    {
        // Arrange: no name in code, so a second "no name" problem would be wrong
        var source = ReadUnnamed(("_source.md", "---\ntitle: [x]\n---\n"), ("a.md", SourceReading.Card()));

        // Act / Assert
        Assert.Equal(RuleIds.Syntax, Assert.Single(source.Problems).RuleId);
        Assert.Single(source.Cards);
    }

    /// <summary>
    ///     Proves that a file that cannot be read becomes a read problem and the other files are still read.
    /// </summary>
    [Fact]
    public void CardReader_Read_FileReadFails_ReportsSourceReadAndContinues()
    {
        // Arrange: a provider whose read of one card fails
        var provider = new CountingFileProvider(
            SourceReading.Files(("a.md", SourceReading.Card()), ("b.md", SourceReading.Card())),
            failingReads: ["a.md"]);
        var source = SourceReading.Read(HelpSource.FromProvider(provider, "test"));

        // Act
        var problem = Assert.Single(source.Problems);

        // Assert: one problem with the exception type and message, and the other card is read
        AssertProblem(problem, RuleIds.SourceRead, "a.md", 0);
        Assert.Contains("IOException", problem.Message, StringComparison.Ordinal);
        Assert.Contains("on purpose", problem.Message, StringComparison.Ordinal);
        Assert.Equal(["b"], source.Cards.Select(card => card.LocalId));
        Assert.Equal(["a"], source.FailedIds);
    }

    /// <summary>
    ///     Proves that a failure to list the files becomes one read problem and an empty definition.
    /// </summary>
    [Fact]
    public void CardReader_Read_ListFails_ReportsSourceReadWithoutCards()
    {
        // Arrange
        var provider = new CountingFileProvider(new Dictionary<string, string>(), failList: true);
        var source = SourceReading.Read(HelpSource.FromProvider(provider, "test"));

        // Act
        var problem = Assert.Single(source.Problems);

        // Assert
        AssertProblem(problem, RuleIds.SourceRead, string.Empty, 0);
        Assert.Contains("IOException", problem.Message, StringComparison.Ordinal);
        Assert.Empty(source.Cards);
    }

    /// <summary>
    ///     Proves that a missing directory is reported as a read problem when the source is read.
    /// </summary>
    [Fact]
    public void CardReader_Read_MissingDirectory_ReportsSourceRead()
    {
        // Arrange
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        // Act
        var source = SourceReading.Read(HelpSource.FromProvider(new DirectoryFileProvider(directory)).WithName("test"));

        // Assert
        var problem = Assert.Single(source.Problems);
        Assert.Equal(RuleIds.SourceRead, problem.RuleId);
        Assert.Contains("DirectoryNotFoundException", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a data file that several examples name is read once.
    /// </summary>
    [Fact]
    public void CardReader_Read_SharedExampleDataFile_IsReadOnce()
    {
        // Arrange: two cards whose examples use the same data file
        const string example = "examples:\n  - name: x\n    kind: source\n    language: mermaid\n    file: shared.mmd\n    description: D.\n";
        var provider = new CountingFileProvider(SourceReading.Files(
            ("a.md", SourceReading.Card(extraFrontMatter: example)),
            ("b.md", SourceReading.Card(extraFrontMatter: example)),
            ("shared.mmd", "graph TD")));

        // Act
        var source = SourceReading.Read(HelpSource.FromProvider(provider, "test"));

        // Assert: two cards and one data file are read; both examples have the content
        Assert.Empty(source.Problems);
        Assert.Equal(3, provider.ReadCount);
        Assert.All(source.Cards, card => Assert.Equal("graph TD", Assert.Single(card.Examples).Content));
    }

    /// <summary>
    ///     Proves that the problems of the token scope are reported even when the files cannot be listed.
    /// </summary>
    [Fact]
    public void CardReader_Read_ListFails_StillReportsTokenScopeProblems()
    {
        // Arrange: a token scope that redefines the built-in tool token
        var provider = new CountingFileProvider(new Dictionary<string, string>(), failList: true);
        var source = HelpSource.FromProvider(provider, "test");
        var redefined = new Dictionary<string, string> { ["tool"] = "mine" };

        // Act
        var definition = CardReader.Read(
            source,
            CultureInfo.InvariantCulture,
            name => TokenResolver.Create(name, redefined, [], SourceReading.ToolName, SourceReading.ToolVersion));

        // Assert
        Assert.Equal([RuleIds.SourceRead, RuleIds.TokenRedefined], definition.Problems.Select(problem => problem.RuleId).Order(StringComparer.Ordinal));
    }

    /// <summary>
    ///     Proves that a normal read lists once and reads each Markdown file once, and never reads unused data files.
    /// </summary>
    [Fact]
    public void CardReader_Read_NormalSource_ListsOnceAndReadsEachMarkdownFileOnce()
    {
        // Arrange
        var provider = new CountingFileProvider(SourceReading.Files(
            ("_source.md", "---\nname: test\n---\n"),
            ("a.md", SourceReading.Card()),
            ("a/b.md", SourceReading.Card()),
            ("unused.mmd", "graph TD")));

        // Act
        SourceReading.Read(HelpSource.FromProvider(provider));

        // Assert
        Assert.Equal(1, provider.ListCount);
        Assert.Equal(3, provider.ReadCount);
    }

    /// <summary>
    ///     Proves that every translation file is read, whether or not its culture is requested.
    /// </summary>
    [Fact]
    public void CardReader_Read_TranslationFiles_AreReadEvenWhenNotRequested()
    {
        // Arrange
        var provider = new CountingFileProvider(SourceReading.Files(
            ("a.md", SourceReading.Card()),
            ("a.de.md", "---\ntitle: T\n---\nText."),
            ("a.fr.md", "---\ntitle: T\n---\nText.")));

        // Act
        SourceReading.Read(HelpSource.FromProvider(provider, "test"), CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(3, provider.ReadCount);
    }
}
