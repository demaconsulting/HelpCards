using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Tests.Loading;

/// <summary>
///     Unit tests for <see cref="BodyParser"/>.
/// </summary>
public class BodyParserTests
{
    /// <summary>
    ///     Parses a body and returns its parts and problems.
    /// </summary>
    /// <param name="body">The body.</param>
    /// <param name="firstLine">The line of the file the body starts on.</param>
    /// <returns>The parts and the problems.</returns>
    private static (IReadOnlyList<BodyPart> Parts, List<HelpProblem> Problems) Parse(string body, int firstLine = 1)
    {
        var problems = new List<HelpProblem>();
        var parts = BodyParser.Parse(body, firstLine, "demo", "a.md", problems);
        return (parts, problems);
    }

    /// <summary>
    ///     Gets the links of a list of parts.
    /// </summary>
    /// <param name="parts">The parts.</param>
    /// <returns>The links in order.</returns>
    private static List<ParsedLink> Links(IReadOnlyList<BodyPart> parts)
    {
        return parts.Where(part => part.Link is not null).Select(part => part.Link!).ToList();
    }

    /// <summary>
    ///     Proves that a body without links is one text part.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_NoLinks_ReturnsOneTextPart()
    {
        // Arrange / Act
        var (parts, problems) = Parse("Just text\nwith lines.");

        // Assert
        Assert.Equal("Just text\nwith lines.", Assert.Single(parts).Text);
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that an empty body has no parts.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_EmptyBody_ReturnsNoParts()
    {
        // Arrange / Act
        var (parts, problems) = Parse(string.Empty);

        // Assert
        Assert.Empty(parts);
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that a card link is recognized with its id and line.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_CardLink_ReturnsLinkWithIdAndLine()
    {
        // Arrange / Act: a link on the second line of a body that starts on file line 5
        var (parts, problems) = Parse("See\n[[mermaid/flow]] now.", 5);

        // Assert: text, link, text
        Assert.Equal(3, parts.Count);
        Assert.Equal("See\n", parts[0].Text);
        Assert.Equal(new ParsedLink(null, "mermaid/flow", null, null, 6), parts[1].Link);
        Assert.Equal(" now.", parts[2].Text);
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that a link to an example of a card is recognized.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_CardExampleLink_ReturnsCardAndExample()
    {
        // Arrange / Act
        var (parts, _) = Parse("[[a/b#render]]");

        // Assert
        Assert.Equal(new ParsedLink(null, "a/b", "render", null, 1), Assert.Single(Links(parts)));
    }

    /// <summary>
    ///     Proves that a link to an example of the containing card is recognized.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_LocalExampleLink_ReturnsExampleOnly()
    {
        // Arrange / Act
        var (parts, _) = Parse("Use [[#render]].");

        // Assert
        Assert.Equal(new ParsedLink(null, null, "render", null, 1), Assert.Single(Links(parts)));
    }

    /// <summary>
    ///     Proves that an external link with display text is recognized.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_ExternalLink_ReturnsSourceCardAndText()
    {
        // Arrange / Act
        var (parts, problems) = Parse("[[plexus:graph/layout|the layout card]]");

        // Assert
        Assert.Equal(new ParsedLink("plexus", "graph/layout", null, "the layout card", 1), Assert.Single(Links(parts)));
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that an external link to an example is recognized and that the first bar splits the text.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_ExternalExampleLinkWithBarInText_FirstBarSplits()
    {
        // Arrange / Act
        var (parts, _) = Parse("[[lib:a#ex|left | right]]");

        // Assert: the text keeps its later bar
        Assert.Equal(new ParsedLink("lib", "a", "ex", "left | right", 1), Assert.Single(Links(parts)));
    }

    /// <summary>
    ///     Proves that several links keep their order and the text between them.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_SeveralLinks_KeepsOrderAndText()
    {
        // Arrange / Act
        var (parts, _) = Parse("[[a]] and [[b]]");

        // Assert
        Assert.Equal([null, " and ", null], parts.Select(part => part.Text));
        Assert.Equal(["a", "b"], Links(parts).Select(link => link.CardId));
    }

    /// <summary>
    ///     Proves that a link inside an inline code span is not a link.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_LinkInsideCodeSpan_IsLiteralText()
    {
        // Arrange / Act
        var (parts, problems) = Parse("Write `[[x]]` to link.");

        // Assert
        Assert.Equal("Write `[[x]]` to link.", Assert.Single(parts).Text);
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that a link inside a double-backtick span that contains a backtick is literal text.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_LinkInsideDoubleBacktickSpan_IsLiteralText()
    {
        // Arrange / Act
        var (parts, problems) = Parse("``a ` [[x]] ``");

        // Assert
        Assert.Single(parts);
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that a code span may continue across a line break within a paragraph.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_CodeSpanAcrossLineBreak_IsLiteralText()
    {
        // Arrange / Act
        var (parts, problems) = Parse("`start\n[[x]] end`");

        // Assert
        Assert.Single(parts);
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that an unmatched backtick does not swallow links in later paragraphs.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_UnmatchedBacktickInEarlierParagraph_DoesNotHideLaterLink()
    {
        // Arrange / Act
        var (parts, _) = Parse("a ` b\n\n[[x]]");

        // Assert
        Assert.Equal("x", Assert.Single(Links(parts)).CardId);
    }

    /// <summary>
    ///     Proves that links inside a backtick fence are not links, and links after it are.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_LinkInsideBacktickFence_IsLiteralAndLaterLinkIsLink()
    {
        // Arrange / Act
        var (parts, problems) = Parse("```mermaid\ngraph TD\n  A[[fork]]\n```\n[[x]]");

        // Assert: only the link after the fence is a link, and the fence content raises nothing
        Assert.Equal("x", Assert.Single(Links(parts)).CardId);
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that links inside a tilde fence are not links.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_LinkInsideTildeFence_IsLiteral()
    {
        // Arrange / Act
        var (parts, problems) = Parse("~~~\n[[Fork]]\n~~~");

        // Assert
        Assert.Empty(Links(parts));
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that a fence is closed only by a fence at least as long as the opening one.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_ShortFenceInsideLongFence_DoesNotClose()
    {
        // Arrange / Act: a four-backtick fence containing a three-backtick line and a link
        var (parts, _) = Parse("````\n```\n[[x]]\n````\n[[y]]");

        // Assert: [[x]] is still code
        Assert.Equal("y", Assert.Single(Links(parts)).CardId);
    }

    /// <summary>
    ///     Proves that an unclosed fence runs to the end of the body.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_UnclosedFence_RunsToEndOfBody()
    {
        // Arrange / Act
        var (parts, problems) = Parse("```\n[[x]]\nmore");

        // Assert
        Assert.Empty(Links(parts));
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that a line of three backticks with text after them on the same line is an inline span, not a fence.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_BackticksWithClosingOnSameLine_AreNotAFence()
    {
        // Arrange / Act
        var (parts, _) = Parse("```code``` [[x]]");

        // Assert: the link after the span is a link
        Assert.Equal("x", Assert.Single(Links(parts)).CardId);
    }

    /// <summary>
    ///     Proves that a link with upper-case letters is reported as a link typo at its line.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_UpperCaseLink_ReportsLinkSyntaxAtLine()
    {
        // Arrange / Act: the typo is on the second line of a body that starts on line 7
        var (parts, problems) = Parse("fine\nsee [[Mermaid/Flow]]", 7);

        // Assert
        var problem = Assert.Single(problems);
        Assert.Equal(HelpSeverity.Error, problem.Severity);
        Assert.Equal(RuleIds.LinkSyntax, problem.RuleId);
        Assert.Equal("demo", problem.SourceName);
        Assert.Equal("a.md", problem.File);
        Assert.Equal(8, problem.Line);
        Assert.Empty(Links(parts));
    }

    /// <summary>
    ///     Proves that a link with a space is reported as a link typo.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_LinkWithSpace_ReportsLinkSyntax()
    {
        // Arrange / Act
        var (_, problems) = Parse("[[a b]]");

        // Assert
        Assert.Equal(RuleIds.LinkSyntax, Assert.Single(problems).RuleId);
    }

    /// <summary>
    ///     Proves that an external link without display text is reported as a link typo.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_ExternalLinkWithoutText_ReportsLinkSyntax()
    {
        // Arrange / Act
        var (_, problems) = Parse("[[lib:a]]");

        // Assert
        Assert.Equal(RuleIds.LinkSyntax, Assert.Single(problems).RuleId);
    }

    /// <summary>
    ///     Proves that display text with edge spaces or more than 120 characters is reported.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_ExternalLinkTextTooLongOrPadded_ReportsLinkSyntax()
    {
        // Arrange / Act
        var (_, padded) = Parse("[[lib:a| text]]");
        var (_, tooLong) = Parse($"[[lib:a|{new string('x', 121)}]]");
        var (parts, exact) = Parse($"[[lib:a|{new string('x', 120)}]]");

        // Assert: 120 characters is the limit
        Assert.Single(padded);
        Assert.Single(tooLong);
        Assert.Empty(exact);
        Assert.Single(Links(parts));
    }

    /// <summary>
    ///     Proves that a card id with more than eight segments is reported.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_IdWithNineSegments_ReportsLinkSyntax()
    {
        // Arrange / Act
        var (_, problems) = Parse("[[a/b/c/d/e/f/g/h/i]]");
        var (parts, ok) = Parse("[[a/b/c/d/e/f/g/h]]");

        // Assert: eight segments are allowed
        Assert.Single(problems);
        Assert.Empty(ok);
        Assert.Single(Links(parts));
    }

    /// <summary>
    ///     Proves that brackets that do not start like a link stay literal text.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_BracketsNotStartingLikeLink_AreLiteral()
    {
        // Arrange / Act
        var (parts, problems) = Parse("[[ ]] [[]] [[-]] [[[x]]]");

        // Assert: only the inner [[x]] is a link; the rest is plain text without problems
        Assert.Equal("x", Assert.Single(Links(parts)).CardId);
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that every typo of a body is reported, not only the first.
    /// </summary>
    [Fact]
    public void BodyParser_Parse_SeveralTypos_ReportsEach()
    {
        // Arrange / Act
        var (_, problems) = Parse("[[A]]\n[[B]]");

        // Assert
        Assert.Equal([1, 2], problems.Select(problem => problem.Line));
    }
}
