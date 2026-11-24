using System.Text;
using DemaConsulting.HelpCards.Presentation;

namespace DemaConsulting.HelpCards.Tests.Presentation;

/// <summary>
///     Unit tests for <see cref="TextWrapper"/>.
/// </summary>
public class TextWrapperTests
{
    /// <summary>
    ///     Wraps text and returns the lines that were written.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="width">The width.</param>
    /// <param name="first">The first-line prefix.</param>
    /// <param name="rest">The hanging indent.</param>
    /// <returns>The lines without line feeds.</returns>
    private static string[] Wrap(string text, int width, string first = "", string rest = "")
    {
        var builder = new StringBuilder();
        TextWrapper.Append(builder, text, width, first, rest);
        return builder.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    ///     Proves that text is broken between words so that no line is longer than the width.
    /// </summary>
    [Fact]
    public void TextWrapper_Append_LongText_BreaksBetweenWordsWithinWidth()
    {
        // Act
        var lines = Wrap("one two three four five six seven", 14);

        // Assert
        Assert.Equal(["one two three", "four five six", "seven"], lines);
        Assert.All(lines, line => Assert.True(line.Length <= 14));
    }

    /// <summary>
    ///     Proves that the prefix counts toward the width and that later lines start with the hanging indent.
    /// </summary>
    [Fact]
    public void TextWrapper_Append_PrefixAndHangingIndent_CountTowardWidth()
    {
        // Act
        var lines = Wrap("alpha beta gamma delta", 14, "- ", "  ");

        // Assert
        Assert.Equal(["- alpha beta", "  gamma delta"], lines);
    }

    /// <summary>
    ///     Proves that a word that is longer than the width sits on a line of its own and is never split.
    /// </summary>
    [Fact]
    public void TextWrapper_Append_WordLongerThanWidth_SitsOnItsOwnLine()
    {
        // Act
        var lines = Wrap("see internationalization now", 10);

        // Assert
        Assert.Equal(["see", "internationalization", "now"], lines);
    }

    /// <summary>
    ///     Proves that a width of zero writes the text on one line.
    /// </summary>
    [Fact]
    public void TextWrapper_Append_WidthZero_WritesOneLine()
    {
        // Act
        var lines = Wrap("one two three four five six seven eight nine ten", 0, "> ", "  ");

        // Assert
        Assert.Equal(["> one two three four five six seven eight nine ten"], lines);
    }

    /// <summary>
    ///     Proves that runs of spaces collapse and that text without a word writes nothing.
    /// </summary>
    [Fact]
    public void TextWrapper_Append_SpacesAndEmptyText_AreCollapsedOrSkipped()
    {
        // Act
        var collapsed = Wrap("a   b    c", 0);
        var empty = Wrap("   ", 20, "- ", "  ");

        // Assert
        Assert.Equal(["a b c"], collapsed);
        Assert.Empty(empty);
    }

    /// <summary>
    ///     Proves that every line ends with exactly one line feed and no carriage return.
    /// </summary>
    [Fact]
    public void TextWrapper_Append_Lines_EndWithLineFeedOnly()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        TextWrapper.Append(builder, "one two three", 8, string.Empty, string.Empty);

        // Assert
        Assert.Equal("one two\nthree\n", builder.ToString());
    }

    /// <summary>
    ///     Proves that the hanging variant indents later lines by the width of the prefix.
    /// </summary>
    [Fact]
    public void TextWrapper_AppendHanging_IndentsLaterLinesByPrefixWidth()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act
        TextWrapper.AppendHanging(builder, "alpha beta gamma delta", 18, "name   ");

        // Assert
        Assert.Equal("name   alpha beta\n       gamma delta\n", builder.ToString());
    }

    /// <summary>
    ///     Proves that null arguments are rejected.
    /// </summary>
    [Fact]
    public void TextWrapper_NullArguments_ThrowArgumentNullException()
    {
        // Arrange
        var builder = new StringBuilder();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => TextWrapper.Append(null!, "a", 0, string.Empty, string.Empty));
        Assert.Throws<ArgumentNullException>(() => TextWrapper.Append(builder, null!, 0, string.Empty, string.Empty));
        Assert.Throws<ArgumentNullException>(() => TextWrapper.Append(builder, "a", 0, null!, string.Empty));
        Assert.Throws<ArgumentNullException>(() => TextWrapper.Append(builder, "a", 0, string.Empty, null!));
        Assert.Throws<ArgumentNullException>(() => TextWrapper.AppendHanging(builder, "a", 0, null!));
    }
}
