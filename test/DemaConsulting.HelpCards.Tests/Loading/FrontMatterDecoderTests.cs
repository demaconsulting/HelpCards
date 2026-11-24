using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Tests.Loading;

/// <summary>
///     Unit tests for <see cref="FrontMatterDecoder"/>: the accepted and rejected constructs of the
///     restricted front-matter subset.
/// </summary>
/// <remarks>
///     Every accepted construct has a test named <c>FrontMatterDecoder_TryDecode_{Construct}_Accepted</c> and
///     every rejected construct a test named <c>FrontMatterDecoder_TryDecode_{Construct}_Rejected</c>, so the
///     documentation can cite the test that proves each row of its tables.
/// </remarks>
public class FrontMatterDecoderTests
{
    /// <summary>
    ///     Decodes text that must be valid.
    /// </summary>
    /// <param name="text">The file text.</param>
    /// <returns>The decoded document.</returns>
    private static FrontMatterDocument Accept(string text)
    {
        var decoded = FrontMatterDecoder.TryDecode(text, "demo", "card.md", out var document, out var problem);
        Assert.True(decoded, problem?.Message);
        return document!;
    }

    /// <summary>
    ///     Decodes text that must be rejected and checks the rule and the line of the problem.
    /// </summary>
    /// <param name="text">The file text.</param>
    /// <param name="rule">The expected rule identifier.</param>
    /// <param name="line">The expected one-based line.</param>
    private static void Reject(string text, string rule, int line)
    {
        var decoded = FrontMatterDecoder.TryDecode(text, "demo", "card.md", out var document, out var problem);
        Assert.False(decoded);
        Assert.Null(document);
        Assert.NotNull(problem);
        Assert.Equal(HelpSeverity.Error, problem.Severity);
        Assert.Equal(rule, problem.RuleId);
        Assert.Equal(line, problem.Line);
        Assert.Equal("demo", problem.SourceName);
        Assert.Equal("card.md", problem.File);
        Assert.False(string.IsNullOrWhiteSpace(problem.Message));
    }

    /// <summary>
    ///     Finds a top-level entry of a document.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="key">The key.</param>
    /// <returns>The entry.</returns>
    private static FrontMatterEntry Entry(FrontMatterDocument document, string key)
    {
        return document.Entries.Single(entry => entry.Key == key);
    }

    /// <summary>
    ///     Proves that null arguments are rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_NullText_ThrowsArgumentNullException()
    {
        // Arrange / Act / Assert: the text must not be null
        Assert.Throws<ArgumentNullException>(() => FrontMatterDecoder.TryDecode(null!, "demo", "a.md", out _, out _));
    }

    /// <summary>
    ///     Proves that a plain scalar is accepted with its line.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_PlainScalar_Accepted()
    {
        // Arrange / Act: decode a plain value
        var document = Accept("---\ntitle: Hello world\n---\nBody");

        // Assert: value, line, and body line are exact
        var entry = Entry(document, "title");
        Assert.Equal("Hello world", entry.Scalar!.Text);
        Assert.Equal(2, entry.Line);
        Assert.Equal("Body", document.Body);
        Assert.Equal(4, document.BodyLine);
    }

    /// <summary>
    ///     Proves that a single-quoted scalar with a doubled quote is accepted.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_SingleQuotedScalar_Accepted()
    {
        // Arrange / Act: a quoted value with a doubled quote, a colon-space, and a hash
        var document = Accept("---\ntitle: 'It''s: ok # fine'\n---\n");

        // Assert: quotes are removed and the doubled quote is one quote
        Assert.Equal("It's: ok # fine", Entry(document, "title").Scalar!.Text);
    }

    /// <summary>
    ///     Proves that a double-quoted scalar with the two supported escapes is accepted.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_DoubleQuotedScalar_Accepted()
    {
        // Arrange / Act: a quoted value with an escaped backslash and an escaped quote
        var document = Accept("---\ntitle: \"a \\\\ b \\\" c: d\"\n---\n");

        // Assert: both escapes are resolved
        Assert.Equal("a \\ b \" c: d", Entry(document, "title").Scalar!.Text);
    }

    /// <summary>
    ///     Proves that full-line comments between entries are accepted, with any indentation.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_FullLineComments_Accepted()
    {
        // Arrange / Act: comments before, between, and inside a list
        var document = Accept("---\n# first\ntitle: A\n  # indented comment\ntags:\n  # in list\n  - one\n---\n");

        // Assert: the comments are ignored
        Assert.Equal(2, document.Entries.Count);
        Assert.Equal("one", Entry(document, "tags").Items!.Single().Scalar!.Text);
    }

    /// <summary>
    ///     Proves that an indented list of scalars is accepted.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_IndentedListOfScalars_Accepted()
    {
        // Arrange / Act: a list under a key
        var document = Accept("---\naliases:\n  - first\n  - second\nsummary: S\n---\n");

        // Assert: both items with their lines, then the next key
        var items = Entry(document, "aliases").Items!;
        Assert.Equal(["first", "second"], items.Select(item => item.Scalar!.Text));
        Assert.Equal([3, 4], items.Select(item => item.Line));
        Assert.Equal("S", Entry(document, "summary").Scalar!.Text);
    }

    /// <summary>
    ///     Proves that a list whose dashes start in the first column is accepted.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_ListAtFirstColumn_Accepted()
    {
        // Arrange / Act: dashes at column zero, then another key
        var document = Accept("---\ntags:\n- a\n- b\nsummary: S\n---\n");

        // Assert: the list ends at the next key
        Assert.Equal(["a", "b"], Entry(document, "tags").Items!.Select(item => item.Scalar!.Text));
        Assert.Equal("S", Entry(document, "summary").Scalar!.Text);
    }

    /// <summary>
    ///     Proves that a list of maps of scalars is accepted.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_ListOfMaps_Accepted()
    {
        // Arrange / Act: two example items
        var document = Accept(
            "---\nexamples:\n  - name: one\n    kind: command\n  - name: two\n    kind: source\n---\n");

        // Assert: two maps with their entries and lines
        var items = Entry(document, "examples").Items!;
        Assert.Equal(2, items.Count);
        Assert.Equal(["name", "kind"], items[0].Map!.Select(entry => entry.Key));
        Assert.Equal("two", items[1].Map![0].Scalar!.Text);
        Assert.Equal(5, items[1].Line);
    }

    /// <summary>
    ///     Proves that an unquoted value with a colon that is not followed by a space is accepted.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_ColonWithoutSpaceInValue_Accepted()
    {
        // Arrange / Act: an external reference as a scalar item
        var document = Accept("---\nsee-also:\n  - plexus:graph/layout\n---\n");

        // Assert: the item is a scalar, not a map
        Assert.Equal("plexus:graph/layout", Entry(document, "see-also").Items!.Single().Scalar!.Text);
    }

    /// <summary>
    ///     Proves that a key without a value is an empty scalar.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_EmptyValue_Accepted()
    {
        // Arrange / Act: a key with nothing after it
        var document = Accept("---\nsummary:\ntitle: A\n---\n");

        // Assert: an empty scalar
        Assert.Equal(string.Empty, Entry(document, "summary").Scalar!.Text);
    }

    /// <summary>
    ///     Proves that an extension key is decoded like any other key.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_ExtensionKey_Accepted()
    {
        // Arrange / Act
        var document = Accept("---\nx-audience: agent\n---\n");

        // Assert
        Assert.Equal("agent", Entry(document, "x-audience").Scalar!.Text);
    }

    /// <summary>
    ///     Proves that a literal block scalar with <c>|</c> is accepted for example content.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_ExampleBlockScalarPipe_Accepted()
    {
        // Arrange / Act: a two-line block that ends at the next key of the item
        var document = Accept(
            "---\nexamples:\n  - name: x\n    content: |\n      line one\n      line two\n    kind: source\n---\n");

        // Assert: the indent is removed, the block ends at the less indented key, the line is the first block line
        var map = Entry(document, "examples").Items!.Single().Map!;
        Assert.Equal("line one\nline two", map.Single(e => e.Key == "content").Scalar!.Text);
        Assert.Equal(5, map.Single(e => e.Key == "content").Scalar!.Line);
        Assert.Equal("source", map.Single(e => e.Key == "kind").Scalar!.Text);
    }

    /// <summary>
    ///     Proves that a literal block is accepted for the expected output of an example, with its line.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_ExampleExpectBlockScalar_Accepted()
    {
        // Arrange / Act: a two-line block for expect that ends at the next key of the item
        var document = Accept(
            "---\nexamples:\n  - name: x\n    expect: |\n      first\n      second\n    kind: command\n---\n");

        // Assert: the indent is removed and the line is the first block line
        var map = Entry(document, "examples").Items!.Single().Map!;
        Assert.Equal("first\nsecond", map.Single(e => e.Key == "expect").Scalar!.Text);
        Assert.Equal(5, map.Single(e => e.Key == "expect").Scalar!.Line);
        Assert.Equal("command", map.Single(e => e.Key == "kind").Scalar!.Text);
    }

    /// <summary>
    ///     Proves that a block scalar for a key named <c>expect</c> outside an example list is not allowed.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_BlockScalarExpectOutsideExamples_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\nexpect: |\n  text\n---\n", RuleIds.BlockScalarNotAllowed, 2);
    }

    /// <summary>
    ///     Proves that a block scalar for <c>content</c> or <c>expect</c> inside a list other than the examples is not allowed.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_BlockScalarContentOrExpectInOtherList_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\nlinks:\n  - expect: |\n      text\n---\n", RuleIds.BlockScalarNotAllowed, 3);
        Reject("---\nlinks:\n  - content: |\n      text\n---\n", RuleIds.BlockScalarNotAllowed, 3);
    }

    /// <summary>
    ///     Proves that a block scalar is still rejected for the other keys of an example.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_BlockScalarForOtherExampleKey_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\nexamples:\n  - name: x\n    description: |\n      text\n---\n", RuleIds.BlockScalarNotAllowed, 4);
    }

    /// <summary>
    ///     Proves that the <c>|-</c> header is accepted and gives the same text as <c>|</c>.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_ExampleBlockScalarPipeMinus_Accepted()
    {
        // Arrange / Act: both headers
        var keep = Accept("---\nexamples:\n  - content: |\n      text\n---\n");
        var strip = Accept("---\nexamples:\n  - content: |-\n      text\n---\n");

        // Assert: trailing line feeds are always removed, so both are the same text
        Assert.Equal("text", Entry(keep, "examples").Items!.Single().Map![0].Scalar!.Text);
        Assert.Equal("text", Entry(strip, "examples").Items!.Single().Map![0].Scalar!.Text);
    }

    /// <summary>
    ///     Proves that blank lines, <c>#</c> lines, deeper indentation, and tabs after the indent stay in a block.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_BlockScalarKeepsBlankHashAndNestedLines_Accepted()
    {
        // Arrange / Act: a block with a blank line, a hash line, a more indented line, and a tab after the indent
        var document = Accept(
            "---\nexamples:\n  - content: |\n      a\n\n      # not a comment\n        deeper\n      \ttabbed\n---\n");

        // Assert: all of it is content with the block indent removed
        Assert.Equal(
            "a\n\n# not a comment\n  deeper\n\ttabbed",
            Entry(document, "examples").Items!.Single().Map![0].Scalar!.Text);
    }

    /// <summary>
    ///     Proves that trailing blank lines of a block are not part of the text.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_BlockScalarTrailingBlankLines_Accepted()
    {
        // Arrange / Act: a block followed by blank lines and another key
        var document = Accept(
            "---\nexamples:\n  - content: |\n      text\n\n\n  - content: other\n---\n");

        // Assert: no trailing line feeds
        var items = Entry(document, "examples").Items!;
        Assert.Equal("text", items[0].Map![0].Scalar!.Text);
        Assert.Equal("other", items[1].Map![0].Scalar!.Text);
    }

    /// <summary>
    ///     Proves that CRLF line endings are normalized and do not change line numbers.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_CrLfLineEndings_Accepted()
    {
        // Arrange / Act: a file with Windows line endings
        var document = Accept("---\r\ntitle: A\r\nsummary: B\r\n---\r\nBody\r\nmore\r\n");

        // Assert: LF in the body, correct lines
        Assert.Equal("Body\nmore", document.Body);
        Assert.Equal(3, Entry(document, "summary").Line);
    }

    /// <summary>
    ///     Proves that a leading byte order mark is removed.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_ByteOrderMark_Accepted()
    {
        // Arrange / Act: a file that starts with the byte order mark character
        var document = Accept(char.ConvertFromUtf32(0xFEFF) + "---\ntitle: A\n---\n");

        // Assert: the file is decoded
        Assert.Equal("A", Entry(document, "title").Scalar!.Text);
    }

    /// <summary>
    ///     Proves that one blank line after the closing delimiter is not part of the body.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_BlankLineBeforeBody_Accepted()
    {
        // Arrange / Act: a blank separator line, then the body
        var document = Accept("---\ntitle: A\n---\n\nBody\n\n");

        // Assert: the body starts after the blank line and trailing line feeds are trimmed
        Assert.Equal("Body", document.Body);
        Assert.Equal(5, document.BodyLine);
    }

    /// <summary>
    ///     Proves that a file without a body decodes to an empty body.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_NoBody_Accepted()
    {
        // Arrange / Act
        var document = Accept("---\ntitle: A\n---\n");

        // Assert
        Assert.Equal(string.Empty, document.Body);
    }

    /// <summary>
    ///     Proves that a body may contain a line of three dashes after the front matter.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_ThematicBreakInBody_Accepted()
    {
        // Arrange / Act: a body with a horizontal rule
        var document = Accept("---\ntitle: A\n---\nOne\n\n---\n\nTwo");

        // Assert: everything after the first closing delimiter is body
        Assert.Equal("One\n\n---\n\nTwo", document.Body);
    }

    /// <summary>
    ///     Proves that the complete valid example of the specification is accepted.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_SpecificationExample_Accepted()
    {
        // Arrange: the valid example with every construct
        const string text = """
            ---
            # Full-line comments are allowed between entries.
            title: Flow charts
            summary: Tasks, dates, sections, and the axis of a flow chart.
            aliases:
              - flow-chart
            tags:
              - authoring
            order: 20
            children:
              - settings
            see-also:
              - mermaid/style
              - plexus:graph/layout
            examples:
              - name: flow
                kind: source
                language: mermaid
                file: flow.mmd
                description: A small flow chart with two sections.
              - name: render
                kind: command
                language: shell
                content: '{{tool}} render flow.mmd -o flow.png'
                description: Render the chart.
              - name: config
                kind: source
                language: yaml
                file: settings.yaml
                content: |
                  title: Demo
                  sections:
                    - name: Plan
                description: A minimal inline configuration file.
            x-audience: agent
            ---
            Body in Markdown.
            """;

        // Act
        var document = Accept(text);

        // Assert: every key is present and the block content is exact
        Assert.Equal(
            ["title", "summary", "aliases", "tags", "order", "children", "see-also", "examples", "x-audience"],
            document.Entries.Select(entry => entry.Key));
        var config = Entry(document, "examples").Items![2].Map!;
        Assert.Equal("title: Demo\nsections:\n  - name: Plan", config.Single(e => e.Key == "content").Scalar!.Text);
        Assert.Equal("Body in Markdown.", document.Body);
    }

    /// <summary>
    ///     Proves that a file that does not start with the delimiter is rejected on line 1.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_MissingOpeningDelimiter_Rejected()
    {
        // Arrange / Act / Assert
        Reject("title: A\n---\n", RuleIds.Syntax, 1);
    }

    /// <summary>
    ///     Proves that a front-matter block that is never closed is rejected on line 1.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_MissingClosingDelimiter_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: A\n", RuleIds.Syntax, 1);
    }

    /// <summary>
    ///     Proves that a flow sequence is rejected at its line.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_FlowSequence_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: A\ntags: [a, b]\n---\n", RuleIds.Syntax, 3);
    }

    /// <summary>
    ///     Proves that a flow mapping is rejected at its line.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_FlowMapping_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\nx-a: {b: c}\n---\n", RuleIds.Syntax, 2);
    }

    /// <summary>
    ///     Proves that a flow sequence as a list item is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_FlowSequenceAsListItem_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntags:\n  - [a]\n---\n", RuleIds.Syntax, 3);
    }

    /// <summary>
    ///     Proves that an anchor is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_Anchor_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: &anchor value\n---\n", RuleIds.Syntax, 2);
    }

    /// <summary>
    ///     Proves that an alias is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_Alias_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: A\nsummary: *anchor\n---\n", RuleIds.Syntax, 3);
    }

    /// <summary>
    ///     Proves that a tag is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_Tag_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: !!str value\n---\n", RuleIds.Syntax, 2);
    }

    /// <summary>
    ///     Proves that a trailing comment is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_TrailingComment_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: A\nsummary: text # comment\n---\n", RuleIds.Syntax, 3);
    }

    /// <summary>
    ///     Proves that a comment after a quoted value is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_CommentAfterQuotedValue_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: 'text' # comment\n---\n", RuleIds.Syntax, 2);
    }

    /// <summary>
    ///     Proves that a value that starts with <c>#</c> is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_ValueStartingWithHash_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: #tag\n---\n", RuleIds.Syntax, 2);
    }

    /// <summary>
    ///     Proves that a plain value that starts with <c>%</c> is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_ValueStartingWithPercent_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: A\nx-note: %y\n---\n", RuleIds.Syntax, 3);
    }

    /// <summary>
    ///     Proves that a plain value that starts with <c>@</c> is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_ValueStartingWithAt_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: @user\n---\n", RuleIds.Syntax, 2);
    }

    /// <summary>
    ///     Proves that a plain value that starts with a backtick is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_ValueStartingWithBacktick_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: `code`\n---\n", RuleIds.Syntax, 2);
    }

    /// <summary>
    ///     Proves that a plain value that contains a colon and a space is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_PlainValueWithColonSpace_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: Note: this\n---\n", RuleIds.Syntax, 2);
    }

    /// <summary>
    ///     Proves that a plain value that ends with a colon is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_PlainValueEndingWithColon_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: Setup:\n---\n", RuleIds.Syntax, 2);
    }

    /// <summary>
    ///     Proves that a tab in indentation is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_TabInIndentation_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\naliases:\n\t- a\n---\n", RuleIds.Syntax, 3);
    }

    /// <summary>
    ///     Proves that a duplicate top-level key is rejected at the second use.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_DuplicateKey_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: A\nsummary: B\ntitle: C\n---\n", RuleIds.Syntax, 4);
    }

    /// <summary>
    ///     Proves that a duplicate key inside one list item is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_DuplicateKeyInListItem_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\nexamples:\n  - name: a\n    name: b\n---\n", RuleIds.Syntax, 4);
    }

    /// <summary>
    ///     Proves that a mapping nested below a key is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_NestedMappingBelowKey_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: A\nmeta:\n  author: me\n---\n", RuleIds.Syntax, 4);
    }

    /// <summary>
    ///     Proves that a list below a key of a list item (deeper than a list of maps) is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_ListInsideListItem_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\nexamples:\n  - name: a\n    kind:\n      - x\n---\n", RuleIds.Syntax, 5);
    }

    /// <summary>
    ///     Proves that a document marker inside the front matter is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_DocumentMarker_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: A\n--- !tag\nsummary: B\n---\n", RuleIds.Syntax, 3);
    }

    /// <summary>
    ///     Proves that a document end marker inside the front matter is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_DocumentEndMarker_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: A\n...\n---\n", RuleIds.Syntax, 3);
    }

    /// <summary>
    ///     Proves that an unterminated single-quoted value is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_UnterminatedSingleQuote_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: 'open\n---\n", RuleIds.Syntax, 2);
    }

    /// <summary>
    ///     Proves that an unterminated double-quoted value is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_UnterminatedDoubleQuote_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: \"open\n---\n", RuleIds.Syntax, 2);
    }

    /// <summary>
    ///     Proves that an escape other than backslash and quote is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_UnsupportedEscape_Rejected()
    {
        // Arrange / Act / Assert: "\n" is not a supported escape
        Reject("---\ntitle: \"a\\nb\"\n---\n", RuleIds.Syntax, 2);
    }

    /// <summary>
    ///     Proves that text after a closing quote is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_TextAfterClosingQuote_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: \"a\" b\n---\n", RuleIds.Syntax, 2);
    }

    /// <summary>
    ///     Proves that a block scalar for an ordinary key reports <c>block-scalar-not-allowed</c>.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_BlockScalarForOrdinaryKey_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: A\nsummary: |\n  text\n---\n", RuleIds.BlockScalarNotAllowed, 3);
    }

    /// <summary>
    ///     Proves that a block scalar as a list item reports <c>block-scalar-not-allowed</c>.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_BlockScalarAsListItem_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\naliases:\n  - |\n    text\n---\n", RuleIds.BlockScalarNotAllowed, 3);
    }

    /// <summary>
    ///     Proves that a block scalar for a key named <c>content</c> outside an example list is not allowed.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_BlockScalarContentOutsideExamples_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ncontent: |\n  text\n---\n", RuleIds.BlockScalarNotAllowed, 2);
    }

    /// <summary>
    ///     Proves that the keep chomping indicator <c>|+</c> is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_BlockScalarKeepIndicator_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\nexamples:\n  - content: |+\n      text\n---\n", RuleIds.Syntax, 3);
    }

    /// <summary>
    ///     Proves that an indentation indicator such as <c>|2</c> is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_BlockScalarIndentationIndicator_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\nexamples:\n  - content: |2\n      text\n---\n", RuleIds.Syntax, 3);
    }

    /// <summary>
    ///     Proves that a folded block scalar <c>&gt;</c> is rejected even for example content.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_FoldedBlockScalar_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\nexamples:\n  - content: >\n      text\n---\n", RuleIds.Syntax, 3);
    }

    /// <summary>
    ///     Proves that a list item without a key above it is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_ListItemWithoutKey_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\n- a\n---\n", RuleIds.Syntax, 2);
    }

    /// <summary>
    ///     Proves that an indented line that belongs to no entry is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_UnexpectedIndentation_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle: A\n  stray: b\n---\n", RuleIds.Syntax, 3);
    }

    /// <summary>
    ///     Proves that a key with a space before the colon is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_KeyWithSpaceBeforeColon_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntitle : A\n---\n", RuleIds.Syntax, 2);
    }

    /// <summary>
    ///     Proves that a lone dash as a list item is rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_EmptyListItem_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\ntags:\n  -\n---\n", RuleIds.Syntax, 3);
    }

    /// <summary>
    ///     Proves that keys of one list item that start in different columns are rejected.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_MisalignedKeysInListItem_Rejected()
    {
        // Arrange / Act / Assert
        Reject("---\nexamples:\n  - name: a\n     kind: b\n---\n", RuleIds.Syntax, 4);
    }

    /// <summary>
    ///     Proves that the problem of a rejected file names the source and the file it was given.
    /// </summary>
    [Fact]
    public void FrontMatterDecoder_TryDecode_Rejected_ProblemNamesSourceAndFile()
    {
        // Arrange / Act: decode invalid text with a source name and a file path
        var decoded = FrontMatterDecoder.TryDecode("title: A", "lib", "a/b.md", out _, out var problem);

        // Assert: the problem carries both
        Assert.False(decoded);
        Assert.Equal("lib", problem!.SourceName);
        Assert.Equal("a/b.md", problem.File);
    }
}
