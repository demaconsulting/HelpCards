using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Loading;

/// <summary>
///     Unit tests for <see cref="CardReader"/>: file classification, cards, overlays, and tokens.
/// </summary>
/// <remarks>
///     The tests of the reader are split over several files of one partial class: this file covers file
///     names, cards, overlays, and tokens; the others cover examples, cultures, the source file, and failures.
/// </remarks>
public partial class CardReaderTests
{
    /// <summary>
    ///     Reads an in-memory source named <c>test</c> from path and text pairs.
    /// </summary>
    /// <param name="files">The files of the source.</param>
    /// <returns>The definition of the source.</returns>
    private static SourceDefinition Read(params (string Path, string Text)[] files)
    {
        return SourceReading.Read(SourceReading.Files(files));
    }

    /// <summary>
    ///     Checks one problem completely.
    /// </summary>
    /// <param name="problem">The problem.</param>
    /// <param name="rule">The expected rule identifier.</param>
    /// <param name="file">The expected file.</param>
    /// <param name="line">The expected line.</param>
    private static void AssertProblem(HelpProblem problem, string rule, string file, int line)
    {
        Assert.Equal(HelpSeverity.Error, problem.Severity);
        Assert.Equal(rule, problem.RuleId);
        Assert.Equal("test", problem.SourceName);
        Assert.Equal(file, problem.File);
        Assert.Equal(line, problem.Line);
        Assert.False(string.IsNullOrWhiteSpace(problem.Message));
    }

    /// <summary>
    ///     Proves that a null source is rejected.
    /// </summary>
    [Fact]
    public void CardReader_Read_NullSource_ThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(
            () => CardReader.Read(null!, System.Globalization.CultureInfo.InvariantCulture, _ => null!));
    }

    /// <summary>
    ///     Proves that a card with every key is read with exact values and lines.
    /// </summary>
    [Fact]
    public void CardReader_Read_CardWithEveryKey_ReturnsExactDefinition()
    {
        // Arrange: a card that uses every card key
        const string text = """
            ---
            title: Flow charts
            summary: Tasks and dates.
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
            x-audience: agent
            ---
            Body text.
            """;

        // Act
        var source = Read(("a.md", text));

        // Assert: one card, no problems, exact fields and lines
        var card = Assert.Single(source.Cards);
        Assert.Empty(source.Problems);
        Assert.Equal("test", card.SourceName);
        Assert.Equal("a", card.LocalId);
        Assert.Equal("a.md", card.File);
        Assert.Equal("Flow charts", card.Title);
        Assert.Equal("Tasks and dates.", card.Summary);
        Assert.Equal(new LocatedValue("flow-chart", "a.md", 5), Assert.Single(card.Aliases));
        Assert.Equal(new LocatedValue("authoring", "a.md", 7), Assert.Single(card.Tags));
        Assert.Equal(20, card.Order);
        Assert.Equal(new LocatedValue("settings", "a.md", 10), Assert.Single(card.Children));
        Assert.Equal(
            [new ReferenceDefinition(null, "mermaid/style", "a.md", 12), new ReferenceDefinition("plexus", "graph/layout", "a.md", 13)],
            card.SeeAlso);
        Assert.Equal("agent", card.Extensions["x-audience"]);
        Assert.Equal("en", card.Culture);
        Assert.Equal("Body text.", Assert.Single(card.Body).Text);
        Assert.Equal(16, card.BodyLine);
    }

    /// <summary>
    ///     Proves that a card in a folder gets the folder path as its id and that cards are in ordinal id order.
    /// </summary>
    [Fact]
    public void CardReader_Read_NestedAndUnorderedFiles_ReturnsCardsInOrdinalIdOrder()
    {
        // Arrange: files given out of order, one nested
        var source = Read(
            ("b.md", SourceReading.Card()),
            ("a/z.md", SourceReading.Card()),
            ("a.md", SourceReading.Card()));

        // Act / Assert: ordinal order of the ids
        Assert.Equal(["a", "a/z", "b"], source.Cards.Select(card => card.LocalId));
        Assert.Empty(source.Problems);
    }

    /// <summary>
    ///     Proves that a data file needs no card and raises no problem.
    /// </summary>
    [Fact]
    public void CardReader_Read_DataFile_IsIgnoredWithoutProblem()
    {
        // Arrange
        var source = Read(("a.md", SourceReading.Card()), ("diagram.mmd", "graph TD"), ("sub/settings.yaml", "a: b"));

        // Act / Assert
        Assert.Single(source.Cards);
        Assert.Empty(source.Problems);
    }

    /// <summary>
    ///     Proves that a card file with an upper-case name is reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_UpperCaseCardFileName_ReportsFileName()
    {
        // Arrange / Act
        var source = Read(("Upper.md", SourceReading.Card()));

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.FileName, "Upper.md", 0);
        Assert.Empty(source.Cards);
    }

    /// <summary>
    ///     Proves that a file name with an underscore is reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_UnderscoreCardFileName_ReportsFileName()
    {
        // Arrange / Act
        var source = Read(("bad_name.md", SourceReading.Card()));

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.FileName, "bad_name.md", 0);
    }

    /// <summary>
    ///     Proves that a folder name that is not a segment is reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_UpperCaseFolderName_ReportsFileName()
    {
        // Arrange / Act
        var source = Read(("Docs/card.md", SourceReading.Card()));

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.FileName, "Docs/card.md", 0);
    }

    /// <summary>
    ///     Proves that a source file below the root is reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_SourceFileInFolder_ReportsFileName()
    {
        // Arrange / Act
        var source = Read(("docs/_source.md", SourceReading.Card()));

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.FileName, "docs/_source.md", 0);
    }

    /// <summary>
    ///     Proves that a suffix that is not a culture is reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_InvalidCultureSuffix_ReportsFileName()
    {
        // Arrange: an upper-case suffix and a suffix with two dots
        var source = Read(
            ("a.md", SourceReading.Card()),
            ("a.EN.md", SourceReading.Card()),
            ("a.de.fr.md", SourceReading.Card()));

        // Act / Assert: both translation names are reported in ordinal order
        Assert.Equal(2, source.Problems.Count);
        AssertProblem(source.Problems[0], RuleIds.FileName, "a.EN.md", 0);
        AssertProblem(source.Problems[1], RuleIds.FileName, "a.de.fr.md", 0);
    }

    /// <summary>
    ///     Proves that a data file name that starts with a dot is reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_HiddenDataFileName_ReportsFileName()
    {
        // Arrange / Act
        var source = Read((".hidden", "x"));

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.FileName, ".hidden", 0);
    }

    /// <summary>
    ///     Proves that an id with more than eight segments is reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_NineSegmentPath_ReportsFileName()
    {
        // Arrange / Act
        var source = Read(("a/b/c/d/e/f/g/h/i.md", SourceReading.Card()), ("a/b/c/d/e/f/g/h.md", SourceReading.Card()));

        // Assert: eight segments are fine, nine are not
        AssertProblem(Assert.Single(source.Problems), RuleIds.FileName, "a/b/c/d/e/f/g/h/i.md", 0);
        Assert.Equal(["a/b/c/d/e/f/g/h"], source.Cards.Select(card => card.LocalId));
    }

    /// <summary>
    ///     Proves that two translation files whose cultures differ only in case are reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_CultureSuffixesDifferingInCase_ReportsFileName()
    {
        // Arrange / Act
        var source = Read(
            ("a.md", SourceReading.Card()),
            ("a.de-DE.md", SourceReading.Card()),
            ("a.de-de.md", SourceReading.Card()));

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.FileName, "a.de-de.md", 0);
    }

    /// <summary>
    ///     Proves that an unknown key reports the key, its line, and the allowed keys, and that the card is dropped.
    /// </summary>
    [Fact]
    public void CardReader_Read_UnknownKey_ReportsKeyUnknownWithAllowedKeys()
    {
        // Arrange: a typo of "tags"
        var source = Read(("a.md", SourceReading.Card(extraFrontMatter: "tag: x\n")));

        // Act
        var problem = Assert.Single(source.Problems);

        // Assert
        AssertProblem(problem, RuleIds.KeyUnknown, "a.md", 4);
        Assert.Contains("'tag'", problem.Message, StringComparison.Ordinal);
        Assert.Contains("title, summary, aliases, tags, order, children, see-also, examples, x-*", problem.Message, StringComparison.Ordinal);
        Assert.Empty(source.Cards);
        Assert.Equal(["a"], source.FailedIds);
    }

    /// <summary>
    ///     Proves that source-only keys are unknown in a card.
    /// </summary>
    [Fact]
    public void CardReader_Read_SourceKeysInCard_ReportKeyUnknown()
    {
        // Arrange: schema, name, and language belong to the source file
        var source = Read(("a.md", SourceReading.Card(extraFrontMatter: "schema: 1\nname: x\nlanguage: en\n")));

        // Act / Assert: one problem per key, at its line
        Assert.Equal([4, 5, 6], source.Problems.Select(problem => problem.Line));
        Assert.All(source.Problems, problem => Assert.Equal(RuleIds.KeyUnknown, problem.RuleId));
    }

    /// <summary>
    ///     Proves that a missing title and a missing summary are reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_MissingTitleAndSummary_ReportsKeyMissing()
    {
        // Arrange: a card with only a tag list
        var source = Read(("a.md", "---\ntags:\n  - x\n---\nbody"));

        // Act / Assert: both required keys are reported at line 1
        Assert.Equal(2, source.Problems.Count);
        AssertProblem(source.Problems[0], RuleIds.KeyMissing, "a.md", 1);
        AssertProblem(source.Problems[1], RuleIds.KeyMissing, "a.md", 1);
        Assert.Contains("'title'", source.Problems[0].Message, StringComparison.Ordinal);
        Assert.Contains("'summary'", source.Problems[1].Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that an empty title is reported at its line.
    /// </summary>
    [Fact]
    public void CardReader_Read_EmptyTitle_ReportsValueInvalid()
    {
        // Arrange / Act
        var source = Read(("a.md", "---\ntitle:\nsummary: S\n---\n"));

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.ValueInvalid, "a.md", 2);
    }

    /// <summary>
    ///     Proves that a list where a single value is required is reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_ListAsTitle_ReportsValueInvalid()
    {
        // Arrange / Act
        var source = Read(("a.md", "---\ntitle:\n  - a\nsummary: S\n---\n"));

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.ValueInvalid, "a.md", 2);
    }

    /// <summary>
    ///     Proves that an alias that is not a segment is reported at its line.
    /// </summary>
    [Fact]
    public void CardReader_Read_InvalidAlias_ReportsValueInvalid()
    {
        // Arrange / Act
        var source = Read(("a.md", SourceReading.Card(extraFrontMatter: "aliases:\n  - ok-alias\n  - Bad Alias\n")));

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.ValueInvalid, "a.md", 6);
    }

    /// <summary>
    ///     Proves that a repeated tag is reported at the repeat.
    /// </summary>
    [Fact]
    public void CardReader_Read_DuplicateTag_ReportsValueInvalid()
    {
        // Arrange / Act
        var source = Read(("a.md", SourceReading.Card(extraFrontMatter: "tags:\n  - x\n  - y\n  - x\n")));

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.ValueInvalid, "a.md", 7);
    }

    /// <summary>
    ///     Proves that a repeated <c>children</c> entry is not reported by the reader, because composition reports it.
    /// </summary>
    [Fact]
    public void CardReader_Read_DuplicateChildren_AreKeptForComposition()
    {
        // Arrange / Act
        var source = Read(("a.md", SourceReading.Card(extraFrontMatter: "children:\n  - x\n  - x\n")));

        // Assert
        Assert.Empty(source.Problems);
        Assert.Equal(["x", "x"], Assert.Single(source.Cards).Children.Select(child => child.Value));
    }

    /// <summary>
    ///     Proves that valid order values are read and invalid ones are reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_OrderValues_AcceptsIntegersAndRejectsOthers()
    {
        // Arrange / Act
        var positive = Read(("a.md", SourceReading.Card(extraFrontMatter: "order: 5\n")));
        var negative = Read(("a.md", SourceReading.Card(extraFrontMatter: "order: -3\n")));
        var largest = Read(("a.md", SourceReading.Card(extraFrontMatter: "order: 9999999\n")));
        var text = Read(("a.md", SourceReading.Card(extraFrontMatter: "order: abc\n")));
        var tooLong = Read(("a.md", SourceReading.Card(extraFrontMatter: "order: 12345678\n")));

        // Assert
        Assert.Equal(5, Assert.Single(positive.Cards).Order);
        Assert.Equal(-3, Assert.Single(negative.Cards).Order);
        Assert.Equal(9999999, Assert.Single(largest.Cards).Order);
        AssertProblem(Assert.Single(text.Problems), RuleIds.ValueInvalid, "a.md", 4);
        AssertProblem(Assert.Single(tooLong.Problems), RuleIds.ValueInvalid, "a.md", 4);
    }

    /// <summary>
    ///     Proves that a child name that is not a segment is reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_InvalidChild_ReportsValueInvalid()
    {
        // Arrange / Act
        var source = Read(("a.md", SourceReading.Card(extraFrontMatter: "children:\n  - Bad\n")));

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.ValueInvalid, "a.md", 5);
    }

    /// <summary>
    ///     Proves that malformed see-also references are reported at their lines.
    /// </summary>
    [Fact]
    public void CardReader_Read_InvalidSeeAlso_ReportsValueInvalid()
    {
        // Arrange: an upper-case id and a reference with two colons
        var source = Read(("a.md", SourceReading.Card(extraFrontMatter: "see-also:\n  - A/B\n  - a:b:c\n  - fine\n")));

        // Act / Assert
        Assert.Equal([5, 6], source.Problems.Select(problem => problem.Line));
        Assert.All(source.Problems, problem => Assert.Equal(RuleIds.ValueInvalid, problem.RuleId));
    }

    /// <summary>
    ///     Proves that a scalar where a list is required is reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_ScalarForListKey_ReportsValueInvalid()
    {
        // Arrange / Act
        var source = Read(("a.md", SourceReading.Card(extraFrontMatter: "tags: authoring\n")));

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.ValueInvalid, "a.md", 4);
    }

    /// <summary>
    ///     Proves that a list-valued extension key is reported and a key with nothing after it is an empty list.
    /// </summary>
    [Fact]
    public void CardReader_Read_ExtensionKeyAsList_ReportsValueInvalidAndEmptyTagsAreAccepted()
    {
        // Arrange / Act
        var bad = Read(("a.md", SourceReading.Card(extraFrontMatter: "x-list:\n  - a\n")));
        var empty = Read(("a.md", SourceReading.Card(extraFrontMatter: "tags:\n")));

        // Assert
        AssertProblem(Assert.Single(bad.Problems), RuleIds.ValueInvalid, "a.md", 4);
        Assert.Empty(Assert.Single(empty.Cards).Tags);
        Assert.Empty(empty.Problems);
    }

    /// <summary>
    ///     Proves that a front-matter syntax error ends that file only and names its line.
    /// </summary>
    [Fact]
    public void CardReader_Read_SyntaxErrorInOneFile_EndsOnlyThatFile()
    {
        // Arrange: one broken file between two good ones
        var source = Read(
            ("a.md", SourceReading.Card()),
            ("b.md", "---\ntitle: B\nsummary: [x]\n---\n"),
            ("c.md", SourceReading.Card()));

        // Act / Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.Syntax, "b.md", 3);
        Assert.Equal(["a", "c"], source.Cards.Select(card => card.LocalId));
        Assert.Equal(["b"], source.FailedIds);
    }

    /// <summary>
    ///     Proves that all problems of several files are collected in one read.
    /// </summary>
    [Fact]
    public void CardReader_Read_SeveralDefectiveFiles_CollectsEveryProblem()
    {
        // Arrange: a syntax error, an unknown key, a bad file name, and an unknown token
        var source = Read(
            ("a.md", "title: no front matter"),
            ("b.md", SourceReading.Card(extraFrontMatter: "bogus: 1\n")),
            ("C.md", SourceReading.Card()),
            ("d.md", SourceReading.Card(body: "{{nope}}")));

        // Act
        var rules = source.Problems.Select(problem => (problem.File, problem.RuleId)).ToList();

        // Assert: every defect is reported
        Assert.Contains(("C.md", RuleIds.FileName), rules);
        Assert.Contains(("a.md", RuleIds.Syntax), rules);
        Assert.Contains(("b.md", RuleIds.KeyUnknown), rules);
        Assert.Contains(("d.md", RuleIds.TokenUnknown), rules);
        Assert.Equal(4, rules.Count);
        Assert.Equal(["d"], source.Cards.Select(card => card.LocalId));
    }

    /// <summary>
    ///     Proves that a file with <c>extends</c> is read as an overlay with exact values.
    /// </summary>
    [Fact]
    public void CardReader_Read_OverlayWithEveryKey_ReturnsExactOverlay()
    {
        // Arrange
        const string text = """
            ---
            extends: lib:mermaid/flow
            optional: true
            title: New title
            summary: New summary.
            see-also:
              - other
            x-note: n
            ---
            Overlay body.
            """;

        // Act
        var source = Read(("overlays/extra.md", text));

        // Assert
        Assert.Empty(source.Problems);
        Assert.Empty(source.Cards);
        var overlay = Assert.Single(source.Overlays);
        Assert.Equal("overlays/extra", overlay.LocalId);
        Assert.Equal("overlays/extra.md", overlay.File);
        Assert.Equal(new ReferenceDefinition("lib", "mermaid/flow", "overlays/extra.md", 2), overlay.Extends);
        Assert.True(overlay.Optional);
        Assert.Equal("New title", overlay.Title);
        Assert.Equal("New summary.", overlay.Summary);
        Assert.Equal("other", Assert.Single(overlay.SeeAlso).LocalId);
        Assert.Equal("n", overlay.Extensions["x-note"]);
        Assert.Equal("Overlay body.", Assert.Single(overlay.Body).Text);
        Assert.Equal(10, overlay.BodyLine);
    }

    /// <summary>
    ///     Proves that an overlay with only <c>extends</c> has no title, no summary, and is not optional.
    /// </summary>
    [Fact]
    public void CardReader_Read_OverlayWithOnlyExtends_HasDefaults()
    {
        // Arrange / Act
        var source = Read(("ov.md", "---\nextends: a\n---\n"));

        // Assert
        var overlay = Assert.Single(source.Overlays);
        Assert.Null(overlay.Title);
        Assert.Null(overlay.Summary);
        Assert.False(overlay.Optional);
        Assert.Empty(overlay.Body);
    }

    /// <summary>
    ///     Proves that card-only keys are unknown in an overlay.
    /// </summary>
    [Fact]
    public void CardReader_Read_CardKeysInOverlay_ReportKeyUnknown()
    {
        // Arrange: tags, order, aliases, and children belong to cards
        var source = Read(("ov.md", "---\nextends: a\ntags:\n  - x\norder: 1\naliases:\n  - y\nchildren:\n  - z\n---\n"));

        // Act / Assert: four keys at their lines; the overlay is dropped
        Assert.Equal([3, 5, 6, 8], source.Problems.Select(problem => problem.Line));
        Assert.All(source.Problems, problem => Assert.Equal(RuleIds.KeyUnknown, problem.RuleId));
        Assert.Equal(["ov"], source.FailedIds);
    }

    /// <summary>
    ///     Proves that a flag other than true or false is reported.
    /// </summary>
    [Fact]
    public void CardReader_Read_InvalidOptionalFlag_ReportsValueInvalid()
    {
        // Arrange / Act
        var source = Read(("ov.md", "---\nextends: a\noptional: maybe\n---\n"));

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.ValueInvalid, "ov.md", 3);
    }

    /// <summary>
    ///     Proves that an invalid <c>extends</c> reference is reported and the overlay is dropped.
    /// </summary>
    [Fact]
    public void CardReader_Read_InvalidExtends_ReportsValueInvalid()
    {
        // Arrange / Act
        var source = Read(("ov.md", "---\nextends: Bad Id\n---\n"));

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.ValueInvalid, "ov.md", 2);
        Assert.Empty(source.Overlays);
        Assert.Equal(["ov"], source.FailedIds);
    }

    /// <summary>
    ///     Proves that built-in and source tokens are replaced in title, summary, and body.
    /// </summary>
    [Fact]
    public void CardReader_Read_TokensInTitleSummaryAndBody_AreReplaced()
    {
        // Arrange
        var files = SourceReading.Files(("a.md", "---\ntitle: Use {{tool}}\nsummary: Max {{maxDpi}} dpi.\n---\nRun {{tool}} v{{version}}."));

        // Act
        var source = SourceReading.Read(files, tokens: new Dictionary<string, string> { ["maxDpi"] = "600" });

        // Assert
        var card = Assert.Single(source.Cards);
        Assert.Empty(source.Problems);
        Assert.Equal("Use demo", card.Title);
        Assert.Equal("Max 600 dpi.", card.Summary);
        Assert.Equal("Run demo v1.2.", Assert.Single(card.Body).Text);
    }

    /// <summary>
    ///     Proves that an unknown token in the body reports the file and the exact line, and keeps the card.
    /// </summary>
    [Fact]
    public void CardReader_Read_UnknownTokenInBody_ReportsLineAndKeepsCard()
    {
        // Arrange: the token is on the second body line, which is file line 6
        var source = Read(("a.md", SourceReading.Card(body: "first\nuse {{nope}} here")));

        // Act / Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.TokenUnknown, "a.md", 6);
        Assert.Single(source.Cards);
        Assert.Empty(source.FailedIds);
    }

    /// <summary>
    ///     Proves that an escape renders literal braces in the title, summary, and body without a problem.
    /// </summary>
    [Fact]
    public void CardReader_Read_EscapedBracesInTitleSummaryAndBody_RenderLiteral()
    {
        // Arrange
        var files = SourceReading.Files(("a.md", "---\ntitle: Use \\{{name}}\nsummary: Max \\{{maxDpi}} dpi.\n---\nWrite \\{{x}} and run {{tool}}."));

        // Act
        var source = SourceReading.Read(files);

        // Assert
        var card = Assert.Single(source.Cards);
        Assert.Empty(source.Problems);
        Assert.Equal("Use {{name}}", card.Title);
        Assert.Equal("Max {{maxDpi}} dpi.", card.Summary);
        Assert.Equal("Write {{x}} and run demo.", Assert.Single(card.Body).Text);
    }

    /// <summary>
    ///     Proves that an unknown token beside an escape reports its line and keeps the card.
    /// </summary>
    [Fact]
    public void CardReader_Read_UnknownTokenBesideEscape_ReportsLineAndKeepsCard()
    {
        // Arrange: an escape on the first body line and an unknown token on the second, file line 6
        var source = Read(("a.md", SourceReading.Card(body: "first \\{{x}}\nuse {{nope}} here")));

        // Act / Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.TokenUnknown, "a.md", 6);
        Assert.Single(source.Cards);
        Assert.Empty(source.FailedIds);
    }

    /// <summary>
    ///     Proves that an unknown token in the summary reports the line of the summary.
    /// </summary>
    [Fact]
    public void CardReader_Read_UnknownTokenInSummary_ReportsSummaryLine()
    {
        // Arrange / Act
        var source = Read(("a.md", "---\ntitle: T\nsummary: Uses {{nope}}\n---\n"));

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.TokenUnknown, "a.md", 3);
    }

    /// <summary>
    ///     Proves that a token defined by a source that redefines a built-in is reported at the source level.
    /// </summary>
    [Fact]
    public void CardReader_Read_SourceRedefinesBuiltInToken_ReportsTokenRedefined()
    {
        // Arrange
        var files = SourceReading.Files(("a.md", SourceReading.Card()));

        // Act
        var source = SourceReading.Read(files, tokens: new Dictionary<string, string> { ["tool"] = "x" });

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.TokenRedefined, string.Empty, 0);
    }

    /// <summary>
    ///     Proves that body links are parsed and typos are reported with the body's line offset.
    /// </summary>
    [Fact]
    public void CardReader_Read_BodyLinks_AreParsedAndTyposReported()
    {
        // Arrange: a valid link on the first body line and a typo on the second
        var source = Read(("a.md", SourceReading.Card(body: "See [[b]].\nBad [[B]].")));

        // Act
        var card = Assert.Single(source.Cards);

        // Assert
        Assert.Contains(card.Body, part => part.Link is { CardId: "b", Line: 5 });
        AssertProblem(Assert.Single(source.Problems), RuleIds.LinkSyntax, "a.md", 6);
    }

    /// <summary>
    ///     Proves that a body with only a link has no text parts.
    /// </summary>
    [Fact]
    public void CardReader_Read_EmptyBody_HasNoParts()
    {
        // Arrange / Act
        var source = Read(("a.md", "---\ntitle: T\nsummary: S\n---\n"));

        // Assert
        Assert.Empty(Assert.Single(source.Cards).Body);
    }
}
