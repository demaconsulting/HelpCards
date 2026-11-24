using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Loading;

/// <summary>
///     Unit tests for <see cref="CardReader"/>: examples.
/// </summary>
public partial class CardReaderTests
{
    /// <summary>
    ///     Builds a card whose front matter has the given example items, which start on file line 5.
    /// </summary>
    /// <param name="items">The example items, each line indented under <c>examples:</c> and ending with a line feed.</param>
    /// <returns>The text of the card file.</returns>
    private static string ExampleCard(string items)
    {
        return SourceReading.Card(extraFrontMatter: "examples:\n" + items);
    }

    /// <summary>
    ///     Proves that a command example is read with its content, tokens replaced, and its line.
    /// </summary>
    [Fact]
    public void CardReader_Read_CommandExample_ReturnsExactExample()
    {
        // Arrange
        var source = Read(("a.md", ExampleCard(
            "  - name: render\n    kind: command\n    language: shell\n    content: '{{tool}} render a.mmd'\n    description: Render it with {{tool}}.\n")));

        // Act
        var example = Assert.Single(Assert.Single(source.Cards).Examples);

        // Assert: the fragment list is compared separately because a record compares lists by reference
        Assert.Empty(source.Problems);
        Assert.Empty(example.Expect);
        Assert.Equal(
            new ExampleDefinition("render", "command", "shell", null, "Render it with demo.", "demo render a.mmd", 0, example.Expect, "a.md", 5),
            example);
    }

    /// <summary>
    ///     Proves that the exit code and the expected output of a command are read, with the output as one fragment.
    /// </summary>
    [Fact]
    public void CardReader_Read_ExitAndExpect_AreReadForCommands()
    {
        // Arrange
        var source = Read(("a.md", ExampleCard(
            "  - name: run\n    kind: command\n    language: shell\n    content: '{{tool}} run'\n    exit: 3\n    expect: Ran {{tool}}\n    description: D.\n")));

        // Act
        var example = Assert.Single(Assert.Single(source.Cards).Examples);

        // Assert
        Assert.Empty(source.Problems);
        Assert.Equal(3, example.ExitCode);
        Assert.Equal(["Ran demo"], example.Expect);
    }

    /// <summary>
    ///     Proves that a literal block of expected output gives one fragment per non-blank line.
    /// </summary>
    [Fact]
    public void CardReader_Read_ExpectBlock_SplitsFragmentsOnLines()
    {
        // Arrange: a block with a blank line between two fragments and CRLF line endings
        var source = Read(("a.md", ExampleCard(
            "  - name: run\r\n    kind: command\r\n    language: shell\r\n    content: run\r\n    expect: |\r\n      first fragment\r\n\r\n      second  fragment\r\n    description: D.\r\n")));

        // Act
        var example = Assert.Single(Assert.Single(source.Cards).Examples);

        // Assert
        Assert.Empty(source.Problems);
        Assert.Equal(0, example.ExitCode);
        Assert.Equal(["first fragment", "second  fragment"], example.Expect);
    }

    /// <summary>
    ///     Proves that tokens in the expected output are replaced.
    /// </summary>
    [Fact]
    public void CardReader_Read_ExpectTokens_AreReplaced()
    {
        // Arrange: the second line of the block has an unknown token
        var source = Read(("a.md", ExampleCard(
            "  - name: run\n    kind: command\n    language: shell\n    content: run\n    expect: |\n      {{tool}} {{version}}\n      {{nope}}\n    description: D.\n")));

        // Act
        var example = Assert.Single(Assert.Single(source.Cards).Examples);

        // Assert: reported at the line of the token, the card is kept
        AssertProblem(Assert.Single(source.Problems), RuleIds.TokenUnknown, "a.md", 11);
        Assert.Equal("demo 1.2", example.Expect[0]);
    }

    /// <summary>
    ///     Proves that an exit code outside 0 to 255 or that is not an integer is reported at its line.
    /// </summary>
    [Fact]
    public void CardReader_Read_ExitOutOfRange_IsValueInvalid()
    {
        // Arrange: too large, negative, signed, not a number, empty, and too long for an integer
        string[] values = ["256", "-1", "+1", "two", "''", "99999999999"];

        foreach (var value in values)
        {
            var source = Read(("a.md", ExampleCard(
                $"  - name: run\n    kind: command\n    language: shell\n    content: run\n    exit: {value}\n    description: D.\n")));

            // Act
            var problem = Assert.Single(source.Problems);

            // Assert
            AssertProblem(problem, RuleIds.ValueInvalid, "a.md", 9);
            Assert.Contains("'exit'", problem.Message, StringComparison.Ordinal);
        }
    }

    /// <summary>
    ///     Proves that the largest and the smallest exit codes are accepted.
    /// </summary>
    [Fact]
    public void CardReader_Read_ExitBounds_AreAccepted()
    {
        // Arrange
        var source = Read(("a.md", ExampleCard(
            "  - name: low\n    kind: command\n    language: shell\n    content: run\n    exit: 0\n    description: D.\n"
            + "  - name: high\n    kind: command\n    language: shell\n    content: run\n    exit: 255\n    description: D.\n")));

        // Act
        var examples = Assert.Single(source.Cards).Examples;

        // Assert
        Assert.Empty(source.Problems);
        Assert.Equal([0, 255], examples.Select(example => example.ExitCode));
    }

    /// <summary>
    ///     Proves that an empty expectation and one that starts with a blank line are reported at the key.
    /// </summary>
    [Fact]
    public void CardReader_Read_InvalidExpect_IsValueInvalid()
    {
        // Arrange: empty and a block that starts with a blank line
        string[] expects = ["''", "|\n\n      text"];

        foreach (var expect in expects)
        {
            var source = Read(("a.md", ExampleCard(
                $"  - name: run\n    kind: command\n    language: shell\n    content: run\n    expect: {expect}\n    description: D.\n")));

            // Act
            var problem = Assert.Single(source.Problems);

            // Assert
            AssertProblem(problem, RuleIds.ValueInvalid, "a.md", 9);
            Assert.Contains("'expect'", problem.Message, StringComparison.Ordinal);
        }
    }

    /// <summary>
    ///     Proves that only a command has an exit code or expected output.
    /// </summary>
    [Fact]
    public void CardReader_Read_ExitAndExpectOnSourceAndSnippet_AreValueInvalid()
    {
        // Arrange: a source with an exit code and a snippet with expected output
        var source = Read(("a.md", ExampleCard(
            "  - name: a\n    kind: source\n    language: yaml\n    file: a.yaml\n    content: 'k: v'\n    exit: 1\n    description: D.\n"
            + "  - name: b\n    kind: snippet\n    language: yaml\n    file: b.yaml\n    content: 'k: v'\n    expect: x\n    description: D.\n")));

        // Act
        var problems = source.Problems;

        // Assert
        Assert.Equal(2, problems.Count);
        AssertProblem(problems[0], RuleIds.ValueInvalid, "a.md", 10);
        AssertProblem(problems[1], RuleIds.ValueInvalid, "a.md", 17);
        Assert.Contains("Only a command example has 'exit'", problems[0].Message, StringComparison.Ordinal);
        Assert.Contains("Only a command example has 'expect'", problems[1].Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a source example without inline content reads the file next to the card, normalized.
    /// </summary>
    [Fact]
    public void CardReader_Read_SourceExampleWithFile_ReadsFileNextToCard()
    {
        // Arrange: a card in a folder and its data file in the same folder, with CRLF and a trailing newline
        var source = Read(
            ("guide/a.md", ExampleCard("  - name: flow\n    kind: source\n    language: mermaid\n    file: flow.mmd\n    description: A chart.\n")),
            ("guide/flow.mmd", "graph TD\r\n  A --> B\r\n\r\n"));

        // Act
        var example = Assert.Single(Assert.Single(source.Cards).Examples);

        // Assert: LF only, trailing line feeds removed, file name as written
        Assert.Empty(source.Problems);
        Assert.Equal("graph TD\n  A --> B", example.Content);
        Assert.Equal("flow.mmd", example.FileName);
        Assert.Equal("source", example.Kind);
    }

    /// <summary>
    ///     Proves that an example file in a subfolder is resolved relative to the card's folder.
    /// </summary>
    [Fact]
    public void CardReader_Read_ExampleFileInSubfolder_IsRelativeToCardFolder()
    {
        // Arrange
        var source = Read(
            ("a.md", ExampleCard("  - name: x\n    kind: snippet\n    language: yaml\n    file: data/x.yaml\n    description: D.\n")),
            ("data/x.yaml", "k: v"));

        // Act
        var example = Assert.Single(Assert.Single(source.Cards).Examples);

        // Assert
        Assert.Empty(source.Problems);
        Assert.Equal("k: v", example.Content);
        Assert.Equal("data/x.yaml", example.FileName);
    }

    /// <summary>
    ///     Proves that inline content wins and the named file is neither required nor read.
    /// </summary>
    [Fact]
    public void CardReader_Read_SourceExampleWithInlineContent_DoesNotReadFile()
    {
        // Arrange: a provider that counts reads; the example file does not exist
        var provider = new CountingFileProvider(SourceReading.Files(
            ("a.md", ExampleCard("  - name: x\n    kind: source\n    language: yaml\n    file: absent.yaml\n    content: 'a: b'\n    description: D.\n"))));
        var source = SourceReading.Read(HelpSource.FromProvider(provider, "test"));

        // Act
        var example = Assert.Single(Assert.Single(source.Cards).Examples);

        // Assert: one read for the card, none for the example file, and no problem
        Assert.Empty(source.Problems);
        Assert.Equal("a: b", example.Content);
        Assert.Equal(1, provider.ReadCount);
    }

    /// <summary>
    ///     Proves that a missing example file is reported at the example and the card is kept.
    /// </summary>
    [Fact]
    public void CardReader_Read_MissingExampleFile_ReportsExampleFileMissing()
    {
        // Arrange
        var source = Read(("a.md", ExampleCard("  - name: x\n    kind: source\n    language: yaml\n    file: absent.yaml\n    description: D.\n")));

        // Act / Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.ExampleFileMissing, "a.md", 5);
        Assert.Single(source.Cards);
        Assert.Empty(source.FailedIds);
    }

    /// <summary>
    ///     Proves that tokens in file content are replaced and an unknown token names the data file and its line.
    /// </summary>
    [Fact]
    public void CardReader_Read_ExampleFileWithTokens_ReplacesAndReportsInDataFile()
    {
        // Arrange: the second line of the data file has an unknown token
        var source = Read(
            ("a.md", ExampleCard("  - name: x\n    kind: source\n    language: shell\n    file: run.sh\n    description: D.\n")),
            ("run.sh", "{{tool}} one\n{{nope}} two\n"));

        // Act
        var example = Assert.Single(Assert.Single(source.Cards).Examples);

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.TokenUnknown, "run.sh", 2);
        Assert.Equal("demo one\n{{nope}} two", example.Content);
    }

    /// <summary>
    ///     Proves that a source or snippet example needs a file.
    /// </summary>
    [Fact]
    public void CardReader_Read_SourceExampleWithoutFile_ReportsKeyMissing()
    {
        // Arrange
        var source = Read(("a.md", ExampleCard("  - name: x\n    kind: snippet\n    language: yaml\n    description: D.\n")));

        // Act / Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.KeyMissing, "a.md", 5);
        Assert.Equal(["a"], source.FailedIds);
    }

    /// <summary>
    ///     Proves that a command example may not have a file.
    /// </summary>
    [Fact]
    public void CardReader_Read_CommandExampleWithFile_ReportsValueInvalid()
    {
        // Arrange
        var source = Read(("a.md", ExampleCard(
            "  - name: x\n    kind: command\n    language: shell\n    file: a.txt\n    content: run\n    description: D.\n")));

        // Act / Assert: reported at the line of the file key
        AssertProblem(Assert.Single(source.Problems), RuleIds.ValueInvalid, "a.md", 8);
    }

    /// <summary>
    ///     Proves that a command example needs content.
    /// </summary>
    [Fact]
    public void CardReader_Read_CommandExampleWithoutContent_ReportsKeyMissing()
    {
        // Arrange
        var source = Read(("a.md", ExampleCard("  - name: x\n    kind: command\n    language: shell\n    description: D.\n")));

        // Act / Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.KeyMissing, "a.md", 5);
    }

    /// <summary>
    ///     Proves that a command with more than one line is reported and the card is kept.
    /// </summary>
    [Fact]
    public void CardReader_Read_MultiLineCommand_ReportsExampleInvalid()
    {
        // Arrange: a block scalar with two lines for a command
        var source = Read(("a.md", ExampleCard(
            "  - name: x\n    kind: command\n    language: shell\n    content: |\n      one\n      two\n    description: D.\n")));

        // Act / Assert: reported at the first content line
        AssertProblem(Assert.Single(source.Problems), RuleIds.ExampleInvalid, "a.md", 9);
        Assert.Single(source.Cards);
    }

    /// <summary>
    ///     Proves that empty content is reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_EmptyContent_ReportsExampleInvalid()
    {
        // Arrange
        var source = Read(("a.md", ExampleCard(
            "  - name: x\n    kind: command\n    language: shell\n    content: ''\n    description: D.\n")));

        // Act / Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.ExampleInvalid, "a.md", 8);
    }

    /// <summary>
    ///     Proves that content that starts with a blank line is reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_ContentStartingWithBlankLine_ReportsExampleInvalid()
    {
        // Arrange: a block scalar whose first line is blank
        var source = Read(("a.md", ExampleCard(
            "  - name: x\n    kind: snippet\n    language: yaml\n    file: x.yaml\n    content: |\n\n      a: b\n    description: D.\n")));

        // Act / Assert: reported at the first line of the block
        AssertProblem(Assert.Single(source.Problems), RuleIds.ExampleInvalid, "a.md", 10);
    }

    /// <summary>
    ///     Proves that a literal block scalar becomes the content with the indent removed.
    /// </summary>
    [Fact]
    public void CardReader_Read_BlockScalarContent_RemovesIndentAndKeepsStructure()
    {
        // Arrange
        var source = Read(("a.md", ExampleCard(
            "  - name: config\n    kind: source\n    language: yaml\n    file: settings.yaml\n    content: |\n      title: Demo\n      items:\n        - a\n\n      end: 1\n    description: D.\n")));

        // Act
        var example = Assert.Single(Assert.Single(source.Cards).Examples);

        // Assert
        Assert.Empty(source.Problems);
        Assert.Equal("title: Demo\nitems:\n  - a\n\nend: 1", example.Content);
    }

    /// <summary>
    ///     Proves that an unknown example kind is reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_UnknownExampleKind_ReportsValueInvalid()
    {
        // Arrange
        var source = Read(("a.md", ExampleCard("  - name: x\n    kind: output\n    language: shell\n    content: a\n    description: D.\n")));

        // Act / Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.ValueInvalid, "a.md", 6);
    }

    /// <summary>
    ///     Proves that a language that is not one lower-case word is reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_InvalidExampleLanguage_ReportsValueInvalid()
    {
        // Arrange
        var source = Read(("a.md", ExampleCard("  - name: x\n    kind: command\n    language: Shell Script\n    content: a\n    description: D.\n")));

        // Act / Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.ValueInvalid, "a.md", 7);
    }

    /// <summary>
    ///     Proves that missing required example keys are reported at the example.
    /// </summary>
    [Fact]
    public void CardReader_Read_ExampleWithoutRequiredKeys_ReportsKeyMissing()
    {
        // Arrange: only a name
        var source = Read(("a.md", ExampleCard("  - name: x\n")));

        // Act: kind, language, and description are missing
        var rules = source.Problems.Select(problem => (problem.RuleId, problem.Line)).ToList();

        // Assert
        Assert.Equal(3, rules.Count);
        Assert.All(rules, rule => Assert.Equal((RuleIds.KeyMissing, 5), rule));
    }

    /// <summary>
    ///     Proves that an unknown example key is reported with the allowed keys.
    /// </summary>
    [Fact]
    public void CardReader_Read_UnknownExampleKey_ReportsKeyUnknown()
    {
        // Arrange
        var source = Read(("a.md", ExampleCard(
            "  - name: x\n    kind: command\n    language: shell\n    content: a\n    description: D.\n    title: no\n")));

        // Act
        var problem = Assert.Single(source.Problems);

        // Assert
        AssertProblem(problem, RuleIds.KeyUnknown, "a.md", 10);
        Assert.Contains("name, kind, language, description, file, content, exit, expect", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that an example name that is not a segment is reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_InvalidExampleName_ReportsValueInvalid()
    {
        // Arrange
        var source = Read(("a.md", ExampleCard("  - name: Bad Name\n    kind: command\n    language: shell\n    content: a\n    description: D.\n")));

        // Act / Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.ValueInvalid, "a.md", 5);
    }

    /// <summary>
    ///     Proves that example file names that could leave the card folder or name a Markdown file are reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_UnsafeExampleFileNames_AreReported()
    {
        // Arrange: parent folder, absolute path, drive, Markdown file, and backslash
        string[] files = ["../x.txt", "/x.txt", "C:x.txt", "x.md", "a\\b.txt", "a//b.txt"];

        foreach (var file in files)
        {
            // Act
            var source = Read(("a.md", ExampleCard($"  - name: x\n    kind: source\n    language: yaml\n    file: '{file}'\n    content: a\n    description: D.\n")));

            // Assert: each is a value error at the file line
            var problem = Assert.Single(source.Problems);
            AssertProblem(problem, RuleIds.ValueInvalid, "a.md", 8);
        }
    }

    /// <summary>
    ///     Proves that examples that are not maps, or an examples value that is not a list, are reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_ExamplesNotListOfMaps_ReportsValueInvalid()
    {
        // Arrange / Act
        var scalarItem = Read(("a.md", SourceReading.Card(extraFrontMatter: "examples:\n  - just-text\n")));
        var scalarValue = Read(("a.md", SourceReading.Card(extraFrontMatter: "examples: text\n")));

        // Assert
        AssertProblem(Assert.Single(scalarItem.Problems), RuleIds.ValueInvalid, "a.md", 5);
        AssertProblem(Assert.Single(scalarValue.Problems), RuleIds.ValueInvalid, "a.md", 4);
    }

    /// <summary>
    ///     Proves that an example problem that does not break the structure keeps the card, while a structure
    ///     problem drops it.
    /// </summary>
    [Fact]
    public void CardReader_Read_ExampleProblems_KeepCardOnlyWhenStructureIsValid()
    {
        // Arrange: a missing file keeps the card, a missing kind drops it
        var kept = Read(("a.md", ExampleCard("  - name: x\n    kind: source\n    language: yaml\n    file: no.yaml\n    description: D.\n")));
        var dropped = Read(("a.md", ExampleCard("  - name: x\n    language: yaml\n    content: a\n    description: D.\n")));

        // Act / Assert
        Assert.Single(kept.Cards);
        Assert.Empty(dropped.Cards);
        Assert.Equal(["a"], dropped.FailedIds);
    }

    /// <summary>
    ///     Proves that an overlay's examples are read with the overlay's folder as the base for example files.
    /// </summary>
    [Fact]
    public void CardReader_Read_OverlayExampleFile_IsRelativeToOverlayFolder()
    {
        // Arrange
        var source = Read(
            ("extra/ov.md", "---\nextends: a\nexamples:\n  - name: x\n    kind: snippet\n    language: yaml\n    file: x.yaml\n    description: D.\n---\n"),
            ("extra/x.yaml", "k: v"));

        // Act
        var example = Assert.Single(Assert.Single(source.Overlays).Examples);

        // Assert
        Assert.Empty(source.Problems);
        Assert.Equal("k: v", example.Content);
    }
}
