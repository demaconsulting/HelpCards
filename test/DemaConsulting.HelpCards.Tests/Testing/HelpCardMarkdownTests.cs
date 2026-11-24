using DemaConsulting.HelpCards.Testing;

namespace DemaConsulting.HelpCards.Tests.Testing;

/// <summary>
///     Unit tests for <see cref="HelpCardMarkdown"/>.
/// </summary>
public class HelpCardMarkdownTests
{
    /// <summary>
    ///     A body with headings of several levels, a fenced block that contains heading-like lines, and bullets.
    /// </summary>
    private const string Body =
        "Intro text.\n"
        + "\n"
        + "## Options\n"
        + "\n"
        + "- `--width`: The width of the output.\n"
        + "  It wraps at the width.\n"
        + "* `--format`: The format.\n"
        + "+ `--quiet`: No output.\n"
        + "Not a bullet.\n"
        + "\n"
        + "### Details\n"
        + "\n"
        + "- A detail.\n"
        + "\n"
        + "## Example ##\n"
        + "\n"
        + "```text\n"
        + "## Not a heading\n"
        + "- Not a bullet\n"
        + "```\n"
        + "\n"
        + "#Not a heading either\n";

    /// <summary>
    ///     Proves that the headings are listed in order with their marks removed and that headings inside fences,
    ///     and marks without a space, are not headings.
    /// </summary>
    [Fact]
    public void HelpCardMarkdown_Headings_Body_ListsHeadingTextsOutsideFences()
    {
        // Act
        var headings = HelpCardMarkdown.Headings(Body);

        // Assert
        Assert.Equal(["Options", "Details", "Example"], headings);
    }

    /// <summary>
    ///     Proves that a body without headings has no headings and that every level from one to six is recognized.
    /// </summary>
    [Fact]
    public void HelpCardMarkdown_Headings_LevelsOneToSeven_RecognizesSixLevels()
    {
        // Act
        var none = HelpCardMarkdown.Headings("Just text.\nMore text.");
        var levels = HelpCardMarkdown.Headings("# A\n## B\n### C\n#### D\n##### E\n###### F\n####### G\n   ## Indented\n    ## Code");

        // Assert
        Assert.Empty(none);
        Assert.Equal(["A", "B", "C", "D", "E", "F", "Indented"], levels);
    }

    /// <summary>
    ///     Proves that a section runs to the next heading of the same or a higher level, includes deeper
    ///     headings and fenced blocks, and has no blank lines at its start or end.
    /// </summary>
    [Fact]
    public void HelpCardMarkdown_GetSection_Heading_ReturnsTextToNextHeadingOfSameLevel()
    {
        // Act
        var options = HelpCardMarkdown.GetSection(Body, "Options");
        var details = HelpCardMarkdown.GetSection(Body, "Details");
        var example = HelpCardMarkdown.GetSection(Body, "Example");

        // Assert
        Assert.StartsWith("- `--width`", options);
        Assert.EndsWith("- A detail.", options);
        Assert.Contains("### Details", options);
        Assert.Equal("- A detail.", details);
        Assert.Equal("```text\n## Not a heading\n- Not a bullet\n```\n\n#Not a heading either", example);
    }

    /// <summary>
    ///     Proves that a missing heading gives no section and no bullets.
    /// </summary>
    [Fact]
    public void HelpCardMarkdown_GetSection_MissingHeading_ReturnsNullAndNoBullets()
    {
        // Act
        var section = HelpCardMarkdown.GetSection(Body, "Missing");
        var bullets = HelpCardMarkdown.Bullets(Body, "Missing");

        // Assert
        Assert.Null(section);
        Assert.Empty(bullets);
    }

    /// <summary>
    ///     Proves that the bullets of a section are joined into one line each, that every marker is accepted,
    ///     and that text that is not indented ends a bullet.
    /// </summary>
    [Fact]
    public void HelpCardMarkdown_Bullets_Section_JoinsContinuationLines()
    {
        // Act
        var bullets = HelpCardMarkdown.Bullets(Body, "Options");

        // Assert
        Assert.Equal(
            ["`--width`: The width of the output. It wraps at the width.", "`--format`: The format.", "`--quiet`: No output.", "A detail."],
            bullets);
    }

    /// <summary>
    ///     Proves that bullets inside fenced blocks are ignored and that a fence ends a bullet.
    /// </summary>
    [Fact]
    public void HelpCardMarkdown_Bullets_FencedBlock_IsNotABullet()
    {
        // Act
        var bullets = HelpCardMarkdown.Bullets("## List\n- One\n```text\n- Two\n```\n- Three", "List");

        // Assert
        Assert.Equal(["One", "Three"], bullets);
    }

    /// <summary>
    ///     Proves that carriage returns do not change the result and that the first of equal headings is used.
    /// </summary>
    [Fact]
    public void HelpCardMarkdown_GetSection_CarriageReturnsAndRepeatedHeadings_UsesFirstHeading()
    {
        // Act
        var section = HelpCardMarkdown.GetSection("## Same\r\nfirst\r\n## Same\r\nsecond\r\n", "Same");

        // Assert
        Assert.Equal("first", section);
    }

    /// <summary>
    ///     Proves that the scan marks fence lines, reads the language, and reports a fence that is never closed.
    /// </summary>
    [Fact]
    public void HelpCardMarkdown_Scan_Fences_MarksLinesAndFindsUnclosedFence()
    {
        // Act
        var closed = HelpCardMarkdown.Scan("a\n~~~yaml\nb\n~~~\n```\nc", out var unclosed);

        // Assert
        Assert.Equal(
            [
                MarkdownLineKind.Text,
                MarkdownLineKind.FenceOpen,
                MarkdownLineKind.FenceBody,
                MarkdownLineKind.FenceClose,
                MarkdownLineKind.FenceOpen,
                MarkdownLineKind.FenceBody
            ],
            closed.Select(line => line.Kind));
        Assert.Equal("yaml", closed[1].Info);
        Assert.Equal(string.Empty, closed[4].Info);
        Assert.Equal(5, unclosed);
        Assert.Equal(6, closed[^1].Number);
    }

    /// <summary>
    ///     Proves that an inline code span that starts a line is not a fence and that a shorter run does not close a fence.
    /// </summary>
    [Fact]
    public void HelpCardMarkdown_Scan_InlineSpanAndShortRun_AreNotFenceLines()
    {
        // Act
        var lines = HelpCardMarkdown.Scan("```a``` text\n````text\n```\nstill code\n````", out var unclosed);

        // Assert
        Assert.Equal(MarkdownLineKind.Text, lines[0].Kind);
        Assert.Equal(MarkdownLineKind.FenceOpen, lines[1].Kind);
        Assert.Equal(MarkdownLineKind.FenceBody, lines[2].Kind);
        Assert.Equal(MarkdownLineKind.FenceBody, lines[3].Kind);
        Assert.Equal(MarkdownLineKind.FenceClose, lines[4].Kind);
        Assert.Equal(0, unclosed);
    }

    /// <summary>
    ///     Proves that null arguments and a white-space heading are argument errors.
    /// </summary>
    [Fact]
    public void HelpCardMarkdown_Members_InvalidArguments_Throw()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => HelpCardMarkdown.Headings(null!));
        Assert.Throws<ArgumentNullException>(() => HelpCardMarkdown.GetSection(null!, "A"));
        Assert.Throws<ArgumentNullException>(() => HelpCardMarkdown.GetSection("a", null!));
        Assert.Throws<ArgumentException>(() => HelpCardMarkdown.Bullets("a", " "));
        Assert.Throws<ArgumentNullException>(() => HelpCardMarkdown.Bullets(null!, "A"));
    }
}
