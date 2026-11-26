using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Loading;

/// <summary>
///     Unit tests for <see cref="CardReader"/>: the schemas of an example, <c>save-as</c> and <c>file</c>.
/// </summary>
public partial class CardReaderTests
{
    /// <summary>
    ///     The file that declares schema 2 for the sources of these tests.
    /// </summary>
    private static readonly (string Path, string Text) SchemaTwo = ("_source.md", "---\nschema: 2\n---\n");

    /// <summary>
    ///     Proves that in schema 2 an inline source example needs <c>save-as</c>.
    /// </summary>
    [Fact]
    public void CardReader_Read_Schema2InlineSource_RequiresSaveAs()
    {
        // Arrange
        var source = Read(SchemaTwo, ("a.md", ExampleCard("  - name: x\n    kind: source\n    language: yaml\n    content: 'a: b'\n    description: D.\n")));

        // Act
        var problem = Assert.Single(source.Problems);

        // Assert: reported at the example, and the message names the key to add
        AssertProblem(problem, RuleIds.KeyMissing, "a.md", 5);
        Assert.Contains("needs 'save-as'", problem.Message, StringComparison.Ordinal);
        Assert.Equal(["a"], source.FailedIds);
    }

    /// <summary>
    ///     Proves that inline content with <c>save-as</c> is a complete example that reads no data file.
    /// </summary>
    [Fact]
    public void CardReader_Read_Schema2InlineSourceWithSaveAs_UsesTheName()
    {
        // Arrange: a nested save-as name, for a snippet too
        var source = Read(SchemaTwo, ("a.md", ExampleCard(
            "  - name: x\n    kind: source\n    language: yaml\n    content: 'a: b'\n    save-as: conf/a.yaml\n    description: D.\n"
            + "  - name: y\n    kind: snippet\n    language: yaml\n    content: 'c: d'\n    save-as: c.yaml\n    description: D.\n")));

        // Act
        var examples = Assert.Single(source.Cards).Examples;

        // Assert
        Assert.Empty(source.Problems);
        Assert.Equal(["conf/a.yaml", "c.yaml"], examples.Select(example => example.FileName));
        Assert.Equal(["a: b", "c: d"], examples.Select(example => example.Content));
    }

    /// <summary>
    ///     Proves that in schema 2 <c>file</c> and <c>content</c> cannot be combined.
    /// </summary>
    [Fact]
    public void CardReader_Read_Schema2FileAndContent_IsValueInvalid()
    {
        // Arrange
        var source = Read(SchemaTwo, ("a.md", ExampleCard(
            "  - name: x\n    kind: source\n    language: yaml\n    file: a.yaml\n    content: 'a: b'\n    save-as: a.yaml\n    description: D.\n")));

        // Act
        var problem = Assert.Single(source.Problems);

        // Assert: at the line of file, and the message explains both keys
        AssertProblem(problem, RuleIds.ValueInvalid, "a.md", 8);
        Assert.Contains("companion data file", problem.Message, StringComparison.Ordinal);
        Assert.Contains("'save-as'", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that in schema 2 an example with only <c>file</c> reads the data file and saves as its last segment.
    /// </summary>
    [Fact]
    public void CardReader_Read_Schema2FileOnly_SaveAsDefaultsToLastSegment()
    {
        // Arrange
        var source = Read(
            SchemaTwo,
            ("a.md", ExampleCard("  - name: x\n    kind: source\n    language: yaml\n    file: data/x.yaml\n    description: D.\n")),
            ("data/x.yaml", "k: v\n"));

        // Act
        var example = Assert.Single(Assert.Single(source.Cards).Examples);

        // Assert
        Assert.Empty(source.Problems);
        Assert.Equal("x.yaml", example.FileName);
        Assert.Equal("k: v", example.Content);
    }

    /// <summary>
    ///     Proves that in schema 2 <c>save-as</c> next to <c>file</c> overrides the default name.
    /// </summary>
    [Fact]
    public void CardReader_Read_Schema2FileWithSaveAs_UsesSaveAs()
    {
        // Arrange
        var source = Read(
            SchemaTwo,
            ("a.md", ExampleCard("  - name: x\n    kind: source\n    language: yaml\n    file: data/x.yaml\n    save-as: data/x.yaml\n    description: D.\n")),
            ("data/x.yaml", "k: v\n"));

        // Act
        var example = Assert.Single(Assert.Single(source.Cards).Examples);

        // Assert
        Assert.Empty(source.Problems);
        Assert.Equal("data/x.yaml", example.FileName);
    }

    /// <summary>
    ///     Proves that a source or snippet in schema 2 needs content or a file.
    /// </summary>
    [Fact]
    public void CardReader_Read_Schema2NeitherContentNorFile_IsKeyMissing()
    {
        // Arrange
        var source = Read(SchemaTwo, ("a.md", ExampleCard("  - name: x\n    kind: snippet\n    language: yaml\n    save-as: x.yaml\n    description: D.\n")));

        // Act
        var problem = Assert.Single(source.Problems);

        // Assert
        AssertProblem(problem, RuleIds.KeyMissing, "a.md", 5);
        Assert.Contains("'content' (inline text) or 'file'", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a command example has no <c>save-as</c> in schema 2.
    /// </summary>
    [Fact]
    public void CardReader_Read_Schema2SaveAsOnCommand_IsValueInvalid()
    {
        // Arrange
        var source = Read(SchemaTwo, ("a.md", ExampleCard(
            "  - name: x\n    kind: command\n    language: shell\n    content: run\n    save-as: a.txt\n    description: D.\n")));

        // Act
        var problem = Assert.Single(source.Problems);

        // Assert: at the line of save-as
        AssertProblem(problem, RuleIds.ValueInvalid, "a.md", 9);
        Assert.Contains("no 'save-as'", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a command with exit and expect is read in schema 2 as in schema 1.
    /// </summary>
    [Fact]
    public void CardReader_Read_Schema2CommandWithResult_IsRead()
    {
        // Arrange
        var source = Read(SchemaTwo, ("a.md", ExampleCard(
            "  - name: x\n    kind: command\n    language: shell\n    content: run\n    exit: 2\n    expect: failed\n    description: D.\n")));

        // Act
        var example = Assert.Single(Assert.Single(source.Cards).Examples);

        // Assert
        Assert.Empty(source.Problems);
        Assert.Equal(2, example.ExitCode);
        Assert.Equal(["failed"], example.Expect);
        Assert.Null(example.FileName);
    }

    /// <summary>
    ///     Proves that a <c>save-as</c> that could leave the folder or name a card is reported at its line.
    /// </summary>
    [Fact]
    public void CardReader_Read_Schema2InvalidSaveAs_IsValueInvalid()
    {
        // Arrange: parent folder, absolute path, Markdown file, doubled slash, and a space
        string[] values = ["../x.txt", "/x.txt", "x.md", "a//b.txt", "a b.txt"];

        foreach (var value in values)
        {
            var source = Read(SchemaTwo, ("a.md", ExampleCard(
                $"  - name: x\n    kind: source\n    language: yaml\n    content: a\n    save-as: '{value}'\n    description: D.\n")));

            // Act
            var problem = Assert.Single(source.Problems);

            // Assert
            AssertProblem(problem, RuleIds.ValueInvalid, "a.md", 9);
            Assert.Contains("not a valid save-as name", problem.Message, StringComparison.Ordinal);
        }
    }

    /// <summary>
    ///     Proves that the allowed keys of schema 2 include <c>save-as</c> and are listed for an unknown key.
    /// </summary>
    [Fact]
    public void CardReader_Read_Schema2UnknownExampleKey_ListsSaveAs()
    {
        // Arrange
        var source = Read(SchemaTwo, ("a.md", ExampleCard(
            "  - name: x\n    kind: command\n    language: shell\n    content: run\n    description: D.\n    title: no\n")));

        // Act
        var problem = Assert.Single(source.Problems);

        // Assert
        AssertProblem(problem, RuleIds.KeyUnknown, "a.md", 10);
        Assert.Contains("file, content, save-as, exit, expect, tokens", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the key <c>tokens</c> is accepted in schema 1 and schema 2 without a schema change.
    /// </summary>
    [Fact]
    public void CardReader_Read_Schema1AndSchema2ExampleTokens_AreAccepted()
    {
        // Arrange: the same command example with tokens off, in a schema 1 source and a schema 2 source
        var card = ("a.md", ExampleCard("  - name: x\n    kind: command\n    language: shell\n    content: '{{tool}} {{a}}'\n    tokens: false\n    description: D.\n"));

        // Act
        var schema1 = Read(card);
        var schema2 = Read(SchemaTwo, card);

        // Assert: no problem in either schema and the content is verbatim
        Assert.Empty(schema1.Problems);
        Assert.Empty(schema2.Problems);
        Assert.Equal("{{tool}} {{a}}", Assert.Single(Assert.Single(schema1.Cards).Examples).Content);
        Assert.Equal("{{tool}} {{a}}", Assert.Single(Assert.Single(schema2.Cards).Examples).Content);
    }

    /// <summary>
    ///     Proves that in schema 1 the key <c>save-as</c> is unknown and the message names schema 2.
    /// </summary>
    [Fact]
    public void CardReader_Read_Schema1SaveAs_IsKeyUnknownWithSchemaHint()
    {
        // Arrange: no schema means schema 1
        var source = Read(("a.md", ExampleCard(
            "  - name: x\n    kind: source\n    language: yaml\n    file: a.yaml\n    content: a\n    save-as: a.yaml\n    description: D.\n")));

        // Act
        var problem = Assert.Single(source.Problems);

        // Assert: at the line of save-as, with the fix and the schema 1 meaning of file
        AssertProblem(problem, RuleIds.KeyUnknown, "a.md", 10);
        Assert.Contains("needs schema 2", problem.Message, StringComparison.Ordinal);
        Assert.Contains("'schema: 2'", problem.Message, StringComparison.Ordinal);
        Assert.Contains("'file' of an example with 'content' is the name to save it as", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that other unknown keys of schema 1 still get the generic message, without <c>save-as</c> in the list.
    /// </summary>
    [Fact]
    public void CardReader_Read_Schema1UnknownKey_ListsKeysWithoutSaveAs()
    {
        // Arrange
        var source = Read(("a.md", ExampleCard(
            "  - name: x\n    kind: command\n    language: shell\n    content: run\n    description: D.\n    title: no\n")));

        // Act
        var problem = Assert.Single(source.Problems);

        // Assert
        AssertProblem(problem, RuleIds.KeyUnknown, "a.md", 10);
        Assert.DoesNotContain("save-as", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that in schema 1 <c>file</c> with <c>content</c> is still the name to save the content as.
    /// </summary>
    [Fact]
    public void CardReader_Read_Schema1FileWithContent_StillMeansSaveAs()
    {
        // Arrange: also with an explicit schema 1, and a file that does not exist because the content is inline
        var source = Read(
            ("_source.md", "---\nschema: 1\n---\n"),
            ("a.md", ExampleCard("  - name: x\n    kind: source\n    language: yaml\n    file: conf/absent.yaml\n    content: 'a: b'\n    description: D.\n")));

        // Act
        var example = Assert.Single(Assert.Single(source.Cards).Examples);

        // Assert
        Assert.Empty(source.Problems);
        Assert.Equal("conf/absent.yaml", example.FileName);
        Assert.Equal("a: b", example.Content);
    }

    /// <summary>
    ///     Proves that in schema 1 an example with only <c>file</c> keeps the whole path, with its folder prefix, as its name.
    /// </summary>
    [Fact]
    public void CardReader_Read_Schema1FileOnly_KeepsFolderPrefix()
    {
        // Arrange
        var source = Read(
            ("a.md", ExampleCard("  - name: x\n    kind: source\n    language: yaml\n    file: data/x.yaml\n    description: D.\n")),
            ("data/x.yaml", "k: v\n"));

        // Act
        var example = Assert.Single(Assert.Single(source.Cards).Examples);

        // Assert
        Assert.Empty(source.Problems);
        Assert.Equal("data/x.yaml", example.FileName);
        Assert.Equal("k: v", example.Content);
    }

    /// <summary>
    ///     Proves that the schema of an overlay file follows the source it is in: schema 2 rules for its examples too.
    /// </summary>
    [Fact]
    public void CardReader_Read_Schema2OverlayExample_FollowsTheSourceSchema()
    {
        // Arrange: an overlay with an inline example without save-as
        var overlay = "---\nextends: other:a\nexamples:\n  - name: x\n    kind: source\n    language: yaml\n    content: a\n    description: D.\n---\nBody\n";
        var source = Read(SchemaTwo, ("o.md", overlay));

        // Act
        var problem = Assert.Single(source.Problems);

        // Assert
        AssertProblem(problem, RuleIds.KeyMissing, "o.md", 4);
        Assert.Contains("needs 'save-as'", problem.Message, StringComparison.Ordinal);
    }
}
