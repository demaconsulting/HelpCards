using DemaConsulting.HelpCards.Integration;
using DemaConsulting.HelpCards.Presentation;

namespace DemaConsulting.HelpCards.Tests.Integration;

/// <summary>
///     Unit tests for <see cref="CliArgumentParser"/>: every row of the option table of the command line.
/// </summary>
public class CliArgumentParserTests
{
    /// <summary>
    ///     Reads arguments with the language option disabled.
    /// </summary>
    /// <param name="arguments">The arguments after the verb.</param>
    /// <returns>The result.</returns>
    private static CliParseResult Parse(params string[] arguments)
    {
        return CliArgumentParser.Parse(arguments, false);
    }

    /// <summary>
    ///     Reads arguments that must be valid.
    /// </summary>
    /// <param name="arguments">The arguments after the verb.</param>
    /// <returns>The request.</returns>
    private static CliRequest Request(params string[] arguments)
    {
        var result = Parse(arguments);
        Assert.Null(result.Message);
        return Assert.IsType<CliRequest>(result.Request);
    }

    /// <summary>
    ///     Proves that no arguments ask for the index, and that words are one card path.
    /// </summary>
    [Fact]
    public void CliArgumentParser_Parse_WordsAndNone_AreCardAndIndexModes()
    {
        // Arrange / Act
        var index = Request();
        var card = Request("build", "compile");

        // Assert
        Assert.Equal(CliMode.Index, index.Mode);
        Assert.Empty(index.Path);
        Assert.Equal(HelpFormat.Text, index.Format);
        Assert.Equal(CliMode.Card, card.Mode);
        Assert.Equal(["build", "compile"], card.Path);
        Assert.Null(card.Culture);
        Assert.False(card.All);
    }

    /// <summary>
    ///     Proves that options may stand anywhere among the words and that <c>--</c> ends them.
    /// </summary>
    [Fact]
    public void CliArgumentParser_Parse_OptionsAnywhereAndDoubleDash_SplitWordsFromOptions()
    {
        // Arrange / Act
        var around = Request("--json", "build", "--examples", "compile");
        var ended = Request("build", "--", "--json", "x");
        var single = Request("-");

        // Assert
        Assert.Equal(CliMode.Examples, around.Mode);
        Assert.Equal(["build", "compile"], around.Path);
        Assert.Equal(HelpFormat.Json, around.Format);
        Assert.Equal(CliMode.Card, ended.Mode);
        Assert.Equal(["build", "--json", "x"], ended.Path);
        Assert.Equal(["-"], single.Path);
    }

    /// <summary>
    ///     Proves that <c>--json</c> and <c>--format</c> choose the format and that a different pair is an error.
    /// </summary>
    [Theory]
    [InlineData(new[] { "--json" }, HelpFormat.Json)]
    [InlineData(new[] { "--format", "json" }, HelpFormat.Json)]
    [InlineData(new[] { "--format", "JSON" }, HelpFormat.Json)]
    [InlineData(new[] { "--format", "markdown" }, HelpFormat.Markdown)]
    [InlineData(new[] { "--format", "text" }, HelpFormat.Text)]
    [InlineData(new[] { "--json", "--format", "json" }, HelpFormat.Json)]
    public void CliArgumentParser_Parse_Format_ChoosesTheFormat(string[] arguments, HelpFormat expected)
    {
        // Arrange / Act
        var request = Request(arguments);

        // Assert
        Assert.Equal(expected, request.Format);
    }

    /// <summary>
    ///     Proves that the usage errors of the table are reported with the message and, where there is one, the hint.
    /// </summary>
    [Theory]
    [InlineData(new[] { "--format", "xml" }, "The format 'xml' is not valid.")]
    [InlineData(new[] { "--json", "--format", "text" }, "option '--json' cannot be combined with '--format text'.")]
    [InlineData(new[] { "--format", "markdown", "--json" }, "option '--json' cannot be combined with '--format markdown'.")]
    [InlineData(new[] { "--width", "19" }, "The width '19' is not valid.")]
    [InlineData(new[] { "--width", "501" }, "The width '501' is not valid.")]
    [InlineData(new[] { "--width", "wide" }, "The width 'wide' is not valid.")]
    [InlineData(new[] { "--width", "-5" }, "The width '-5' is not valid.")]
    [InlineData(new[] { "--width", "1e2" }, "The width '1e2' is not valid.")]
    [InlineData(new[] { "--bogus" }, "Unknown option '--bogus'.")]
    [InlineData(new[] { "-x" }, "Unknown option '-x'.")]
    [InlineData(new[] { "--lang", "de" }, "Unknown option '--lang'.")]
    [InlineData(new[] { "--search" }, "option '--search' needs a value.")]
    [InlineData(new[] { "--search", "--json" }, "option '--search' needs a value.")]
    [InlineData(new[] { "--search", "--" }, "option '--search' needs a value.")]
    [InlineData(new[] { "--search", "" }, "option '--search' needs a value.")]
    [InlineData(new[] { "--search", "  " }, "option '--search' needs a value.")]
    [InlineData(new[] { "a", "--example" }, "option '--example' needs a value.")]
    [InlineData(new[] { "--tag" }, "option '--tag' needs a value.")]
    [InlineData(new[] { "--source" }, "option '--source' needs a value.")]
    [InlineData(new[] { "--width" }, "option '--width' needs a value.")]
    [InlineData(new[] { "--format" }, "option '--format' needs a value.")]
    public void CliArgumentParser_Parse_InvalidOptionsAndValues_ReportUsageErrors(string[] arguments, string message)
    {
        // Arrange / Act
        var result = Parse(arguments);

        // Assert
        Assert.Null(result.Request);
        Assert.Equal(message, result.Message);
    }

    /// <summary>
    ///     Proves that an unknown option lists every valid option as the hint.
    /// </summary>
    [Fact]
    public void CliArgumentParser_Parse_UnknownOption_HintListsValidOptions()
    {
        // Arrange / Act
        var disabled = Parse("--bogus");
        var enabled = CliArgumentParser.Parse(["--bogus"], true);

        // Assert
        Assert.Equal(
            "Valid options: --json, --examples, --all, --format, --example, --search, --tag, --source, --width.",
            disabled.Hint);
        Assert.EndsWith("--width, --lang.", enabled.Hint, StringComparison.Ordinal);
        Assert.Equal(["--json", "--examples", "--all", "--format", "--example", "--search", "--tag", "--source", "--width", "--lang"], CliArgumentParser.GetValidOptions(true));
    }

    /// <summary>
    ///     Proves that every option except the tag and the source may be given once only.
    /// </summary>
    [Theory]
    [InlineData(new[] { "--json", "--json" }, "--json")]
    [InlineData(new[] { "--format", "text", "--format", "text" }, "--format")]
    [InlineData(new[] { "a", "--examples", "--examples" }, "--examples")]
    [InlineData(new[] { "a", "--example", "x", "--example", "y" }, "--example")]
    [InlineData(new[] { "--search", "a", "--search", "b" }, "--search")]
    [InlineData(new[] { "--all", "--all" }, "--all")]
    [InlineData(new[] { "--width", "30", "--width", "40" }, "--width")]
    public void CliArgumentParser_Parse_RepeatedOption_IsAnError(string[] arguments, string option)
    {
        // Arrange / Act
        var result = Parse(arguments);

        // Assert
        Assert.Equal($"option '{option}' may be given only once.", result.Message);
    }

    /// <summary>
    ///     Proves that the tag and the source may repeat and that all values are kept.
    /// </summary>
    [Fact]
    public void CliArgumentParser_Parse_RepeatedTagAndSource_KeepEveryValue()
    {
        // Arrange / Act
        var request = Request("--search", "x", "--tag", "a", "--tag", "b", "--source", "s", "--source", "t");

        // Assert
        Assert.Equal(CliMode.Search, request.Mode);
        Assert.Equal("x", request.Name);
        Assert.Equal(["a", "b"], request.Tags);
        Assert.Equal(["s", "t"], request.Sources);
    }

    /// <summary>
    ///     Proves that the width is a number from 20 to 500 or <c>auto</c>.
    /// </summary>
    [Fact]
    public void CliArgumentParser_Parse_Width_AcceptsRangeAndAuto()
    {
        // Arrange / Act
        var low = Request("--width", "20");
        var high = Request("--width", "500");
        var auto = Request("--width", "AUTO");
        var none = Request();

        // Assert
        Assert.Equal(20, low.Width);
        Assert.Equal(500, high.Width);
        Assert.True(auto.AutoWidth);
        Assert.Null(auto.Width);
        Assert.Null(none.Width);
        Assert.False(none.AutoWidth);
    }

    /// <summary>
    ///     Proves that the modes are chosen by the options and the path.
    /// </summary>
    [Theory]
    [InlineData(new[] { "--all" }, "Index")]
    [InlineData(new[] { "--source", "s" }, "Index")]
    [InlineData(new[] { "a", "--examples" }, "Examples")]
    [InlineData(new[] { "a", "b", "--example", "x" }, "Example")]
    [InlineData(new[] { "--search", "x" }, "Search")]
    [InlineData(new[] { "--search", "x", "--tag", "t", "--source", "s" }, "Search")]
    [InlineData(new[] { "--tag", "t" }, "Tags")]
    [InlineData(new[] { "--tag", "t", "--source", "s" }, "Tags")]
    [InlineData(new[] { "a" }, "Card")]
    public void CliArgumentParser_Parse_OptionsAndPath_ChooseTheMode(string[] arguments, string expected)
    {
        // Arrange / Act
        var request = Request(arguments);

        // Assert
        Assert.Equal(expected, request.Mode.ToString());
    }

    /// <summary>
    ///     Proves that the conflicts between modes and options are usage errors.
    /// </summary>
    [Theory]
    [InlineData(new[] { "a", "--examples", "--example", "x" }, "options '--examples' and '--example' cannot be combined.")]
    [InlineData(new[] { "a", "--examples", "--search", "x" }, "options '--examples' and '--search' cannot be combined.")]
    [InlineData(new[] { "a", "--example", "x", "--search", "x" }, "options '--example' and '--search' cannot be combined.")]
    [InlineData(new[] { "a", "--examples", "--tag", "t" }, "options '--examples' and '--tag' cannot be combined.")]
    [InlineData(new[] { "a", "--example", "x", "--tag", "t" }, "options '--example' and '--tag' cannot be combined.")]
    [InlineData(new[] { "a", "--examples", "--all" }, "options '--examples' and '--all' cannot be combined.")]
    [InlineData(new[] { "a", "--example", "x", "--all" }, "options '--example' and '--all' cannot be combined.")]
    [InlineData(new[] { "--search", "x", "--all" }, "options '--search' and '--all' cannot be combined.")]
    [InlineData(new[] { "--tag", "t", "--all" }, "options '--tag' and '--all' cannot be combined.")]
    public void CliArgumentParser_Parse_ConflictingOptions_AreUsageErrors(string[] arguments, string message)
    {
        // Arrange / Act
        var result = Parse(arguments);

        // Assert: the message names the options in the fixed order and no hint is offered
        Assert.Null(result.Request);
        Assert.Equal(message, result.Message);
        Assert.Null(result.Hint);
    }

    /// <summary>
    ///     Proves that options that need a card path, and options that forbid one, are usage errors, and that a search
    ///     with extra words explains how to quote.
    /// </summary>
    [Theory]
    [InlineData(new[] { "--examples" }, "option '--examples' needs a card path.")]
    [InlineData(new[] { "--example", "x" }, "option '--example' needs a card path.")]
    [InlineData(new[] { "a", "--tag", "t" }, "option '--tag' cannot be used with a card path.")]
    [InlineData(new[] { "a", "--source", "s" }, "option '--source' cannot be used with a card path.")]
    [InlineData(new[] { "a", "--all" }, "option '--all' cannot be used with a card path.")]
    [InlineData(new[] { "a", "--search", "x" }, "option '--search' takes one query and no card path.")]
    [InlineData(new[] { "--search", "two", "words" }, "option '--search' takes one query and no card path.")]
    public void CliArgumentParser_Parse_PathRules_AreUsageErrors(string[] arguments, string message)
    {
        // Arrange / Act
        var result = Parse(arguments);

        // Assert
        Assert.Equal(message, result.Message);
        if (arguments.Contains("--search", StringComparer.Ordinal))
        {
            Assert.Contains("Quote", result.Hint, StringComparison.Ordinal);
        }
    }

    /// <summary>
    ///     Proves that the language option is read as a well-formed culture name when it is enabled.
    /// </summary>
    [Fact]
    public void CliArgumentParser_Parse_Language_ReadsWellFormedCultureNamesOnlyWhenEnabled()
    {
        // Arrange / Act
        var german = CliArgumentParser.Parse(["--lang", "de-DE"], true).Request;
        var neutral = CliArgumentParser.Parse(["--lang", "fr"], true).Request;
        var tooLong = CliArgumentParser.Parse(["--lang", "english"], true);
        var digits = CliArgumentParser.Parse(["--lang", "d3"], true);
        var subtag = CliArgumentParser.Parse(["--lang", "de-"], true);
        var twice = CliArgumentParser.Parse(["--lang", "de", "--lang", "fr"], true);
        var shortSubtag = CliArgumentParser.Parse(["--lang", "de-D"], true);

        // Assert
        Assert.Equal("de-DE", german!.Culture!.Name);
        Assert.Equal("fr", neutral!.Culture!.Name);
        Assert.Equal("The language 'english' is not a valid culture name.", tooLong.Message);
        Assert.Equal("The language 'd3' is not a valid culture name.", digits.Message);
        Assert.Equal("The language 'de-' is not a valid culture name.", subtag.Message);
        Assert.Equal("The language 'de-D' is not a valid culture name.", shortSubtag.Message);
        Assert.Equal("option '--lang' may be given only once.", twice.Message);
    }

    /// <summary>
    ///     Proves that a well-formed culture name that no operating system knows is accepted, so that it falls back to the
    ///     neutral files, and that this does not depend on the machine's globalization mode.
    /// </summary>
    [Theory]
    [InlineData("xx-YY")]
    [InlineData("zz")]
    [InlineData("qaa-Latn-ZZ")]
    [InlineData("tlh-12345678")]
    public void CliArgumentParser_Parse_Language_WellFormedUnknownCulture_IsAccepted(string name)
    {
        // Arrange / Act
        var result = CliArgumentParser.Parse(["--lang", name], true);

        // Assert: no usage error and a culture is carried; a platform that cannot represent the name (for example
        // under invariant globalization) carries the invariant culture, which also selects the neutral files
        Assert.Null(result.Message);
        Assert.NotNull(result.Request!.Culture);
    }

    /// <summary>
    ///     Proves that a search may be combined with tag and source restrictions, as the option table allows.
    /// </summary>
    [Fact]
    public void CliArgumentParser_Parse_SearchWithTagAndSource_IsAccepted()
    {
        // Arrange / Act
        var result = Parse("--search", "x", "--tag", "t", "--source", "s");

        // Assert
        Assert.Null(result.Message);
        Assert.Equal((CliMode.Search, "x"), (result.Request!.Mode, result.Request.Name));
        Assert.Equal(["t"], result.Request.Tags);
        Assert.Equal(["s"], result.Request.Sources);
    }

    /// <summary>
    ///     Proves that an error is reported as JSON when the arguments ask for JSON anywhere before <c>--</c>, however the
    ///     first error arose.
    /// </summary>
    [Theory]
    [InlineData(new[] { "--json", "--bogus" }, true)]
    [InlineData(new[] { "--bogus", "--json" }, true)]
    [InlineData(new[] { "--format", "json", "--bogus" }, true)]
    [InlineData(new[] { "--bogus", "--format", "text" }, false)]
    [InlineData(new[] { "--bogus", "--", "--json" }, false)]
    [InlineData(new[] { "a" }, false)]
    public void CliArgumentParser_Parse_JsonRequested_FollowsTheArguments(string[] arguments, bool expected)
    {
        // Arrange / Act
        var result = Parse(arguments);

        // Assert
        Assert.Equal(expected, result.JsonRequested);
    }

    /// <summary>
    ///     Proves that null arguments are rejected.
    /// </summary>
    [Fact]
    public void CliArgumentParser_Parse_NullArguments_Throw()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => CliArgumentParser.Parse(null!, false));
        Assert.Throws<ArgumentNullException>(() => CliArgumentParser.Parse(["a", null!], false));
    }
}
