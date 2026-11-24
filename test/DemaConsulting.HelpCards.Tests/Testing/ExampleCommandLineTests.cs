using DemaConsulting.HelpCards.Testing;

namespace DemaConsulting.HelpCards.Tests.Testing;

/// <summary>
///     Unit tests for <see cref="ExampleCommandLine"/>.
/// </summary>
public class ExampleCommandLineTests
{
    /// <summary>
    ///     Parses a command that must be valid and returns its arguments.
    /// </summary>
    /// <param name="content">The command.</param>
    /// <returns>The arguments.</returns>
    private static IReadOnlyList<string> ParseValid(string content)
    {
        var parsed = ExampleCommandLine.TryParse(content, "tool", out var arguments, out var error);
        Assert.True(parsed, error);
        Assert.Null(error);
        return arguments;
    }

    /// <summary>
    ///     Parses a command that must be rejected and returns the error.
    /// </summary>
    /// <param name="content">The command.</param>
    /// <returns>The error.</returns>
    private static string ParseInvalid(string content)
    {
        var parsed = ExampleCommandLine.TryParse(content, "tool", out var arguments, out var error);
        Assert.False(parsed);
        Assert.Empty(arguments);
        Assert.NotNull(error);
        return error;
    }

    /// <summary>
    ///     Proves that words are separated by spaces and that the tool name is not part of the arguments.
    /// </summary>
    [Fact]
    public void ExampleCommandLine_TryParse_SimpleWords_ReturnsArgumentsAfterToolName()
    {
        // Act
        var arguments = ParseValid("tool  render   --width=80 file.txt");

        // Assert
        Assert.Equal(["render", "--width=80", "file.txt"], arguments);
    }

    /// <summary>
    ///     Proves that a command with no arguments is valid and has an empty argument list.
    /// </summary>
    [Fact]
    public void ExampleCommandLine_TryParse_ToolNameOnly_ReturnsNoArguments()
    {
        // Act
        var arguments = ParseValid("tool");

        // Assert
        Assert.Empty(arguments);
    }

    /// <summary>
    ///     Proves that single quotes are literal, that double quotes accept only two escapes, that adjacent parts
    ///     form one word, and that empty quotes make an empty argument.
    /// </summary>
    [Fact]
    public void ExampleCommandLine_TryParse_QuotedParts_FormWords()
    {
        // Act
        var arguments = ParseValid("tool 'a b' \"c \\\"d\\\" \\\\e\" x'y z'\"w\" '' '$HOME|'");

        // Assert
        Assert.Equal(["a b", "c \"d\" \\e", "xy zw", string.Empty, "$HOME|"], arguments);
    }

    /// <summary>
    ///     Proves that the first word must equal the tool name exactly and that the error names both.
    /// </summary>
    [Fact]
    public void ExampleCommandLine_TryParse_OtherToolName_IsRejectedWithBothNames()
    {
        // Act
        var wrongName = ParseInvalid("other run");
        var wrongCase = ParseInvalid("Tool run");

        // Assert
        Assert.Contains("'tool'", wrongName);
        Assert.Contains("'other'", wrongName);
        Assert.Contains("'Tool'", wrongCase);
    }

    /// <summary>
    ///     Proves that an empty command, and one with only spaces, is rejected.
    /// </summary>
    [Fact]
    public void ExampleCommandLine_TryParse_EmptyContent_IsRejected()
    {
        // Act
        var empty = ParseInvalid(string.Empty);
        var spaces = ParseInvalid("   ");

        // Assert
        Assert.Contains("empty", empty);
        Assert.Contains("empty", spaces);
    }

    /// <summary>
    ///     Proves that content with a line break is rejected and the error names the column of the break.
    /// </summary>
    [Fact]
    public void ExampleCommandLine_TryParse_LineBreak_IsRejectedWithColumn()
    {
        // Act
        var feed = ParseInvalid("tool a\nb");
        var carriage = ParseInvalid("tool a\r");

        // Assert
        Assert.Contains("one line", feed);
        Assert.Contains("column 7", feed);
        Assert.Contains("column 7", carriage);
    }

    /// <summary>
    ///     Proves that every shell operator is rejected outside quotes and that the error names the character and
    ///     the one-based column.
    /// </summary>
    [Fact]
    public void ExampleCommandLine_TryParse_ShellOperators_AreRejectedWithCharacterAndColumn()
    {
        foreach (var character in "|&;<>()")
        {
            // Act
            var error = ParseInvalid($"tool a {character} b");

            // Assert
            Assert.Equal($"shell operator '{character}' is not allowed in an example command (column 8)", error);
        }
    }

    /// <summary>
    ///     Proves that every expansion character is rejected outside quotes with its column and a hint to quote.
    /// </summary>
    [Fact]
    public void ExampleCommandLine_TryParse_ExpansionCharacters_AreRejectedWithCharacterAndColumn()
    {
        foreach (var character in "$`*?[]{}~!#\\%")
        {
            // Act
            var error = ParseInvalid($"tool a{character}b");

            // Assert
            Assert.Contains($"'{character}'", error);
            Assert.Contains("(column 7)", error);
            Assert.Contains("quote", error);
        }
    }

    /// <summary>
    ///     Proves that a trailing comment, a redirection, a pipe, and a line continuation are rejected.
    /// </summary>
    [Fact]
    public void ExampleCommandLine_TryParse_CommentRedirectionAndContinuation_AreRejected()
    {
        // Act and Assert
        Assert.Contains("'#'", ParseInvalid("tool run # now"));
        Assert.Contains("'>'", ParseInvalid("tool run > out.txt"));
        Assert.Contains("'|'", ParseInvalid("tool run | more"));
        Assert.Contains("'\\'", ParseInvalid("tool run \\"));
    }

    /// <summary>
    ///     Proves that a dollar sign or a backtick inside double quotes, and an unknown escape, are rejected.
    /// </summary>
    [Fact]
    public void ExampleCommandLine_TryParse_InsideDoubleQuotes_RejectsExpansionAndUnknownEscape()
    {
        // Act
        var dollar = ParseInvalid("tool \"a $b\"");
        var tick = ParseInvalid("tool \"a `b\"");
        var escape = ParseInvalid("tool \"a\\nb\"");

        // Assert
        Assert.Contains("'$'", dollar);
        Assert.Contains("column 9", dollar);
        Assert.Contains("'`'", tick);
        Assert.Contains("may only escape", escape);
        Assert.Contains("column 8", escape);
    }

    /// <summary>
    ///     Proves that a quote that is not closed is rejected with the column where it opened.
    /// </summary>
    [Fact]
    public void ExampleCommandLine_TryParse_UnclosedQuote_IsRejectedWithColumn()
    {
        // Act
        var single = ParseInvalid("tool 'abc");
        var doubled = ParseInvalid("tool a \"abc");
        var trailingBackslash = ParseInvalid("tool \"abc\\");

        // Assert
        Assert.Equal("the single quote at column 6 is not closed", single);
        Assert.Equal("the double quote at column 8 is not closed", doubled);
        Assert.Contains("may only escape", trailingBackslash);
    }

    /// <summary>
    ///     Proves that null content or tool names and an empty tool name are argument errors.
    /// </summary>
    [Fact]
    public void ExampleCommandLine_TryParse_InvalidArguments_Throw()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => ExampleCommandLine.TryParse(null!, "tool", out _, out _));
        Assert.Throws<ArgumentNullException>(() => ExampleCommandLine.TryParse("tool", null!, out _, out _));
        Assert.Throws<ArgumentException>(() => ExampleCommandLine.TryParse("tool", "  ", out _, out _));
    }
}
