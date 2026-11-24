using System.Text;

namespace DemaConsulting.HelpCards.Presentation;

/// <summary>
///     Wraps generated text to a width with a first-line prefix and a hanging indent.
/// </summary>
/// <remarks>
///     Only lines the library generates are wrapped; authored bodies never are. Words are the runs of characters
///     between spaces, so no word is ever split, and a word that is longer than the width stands on a line of its
///     own. The prefix counts toward the width. A width of 0 switches wrapping off. The wrapper is stateless and
///     uses ordinal rules only, so the result does not depend on the culture of the machine.
/// </remarks>
internal static class TextWrapper
{
    /// <summary>
    ///     Appends text to a builder as wrapped lines, each ending with a line feed.
    /// </summary>
    /// <param name="builder">The builder to append to. Must not be null.</param>
    /// <param name="text">The text to wrap. Must not be null. Runs of spaces collapse to one space.</param>
    /// <param name="width">The line width, or 0 for no wrapping.</param>
    /// <param name="firstPrefix">The text that starts the first line, such as <c>- </c> or an indent.</param>
    /// <param name="restPrefix">The text that starts every later line, the hanging indent.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static void Append(StringBuilder builder, string text, int width, string firstPrefix, string restPrefix)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(firstPrefix);
        ArgumentNullException.ThrowIfNull(restPrefix);

        // Text without a word writes nothing, so an empty summary leaves no stray prefix behind
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return;
        }

        var line = new StringBuilder(firstPrefix);
        var lineHasWord = false;
        foreach (var word in words)
        {
            // Start a new line when the word does not fit and the line already holds a word
            if (lineHasWord && width > 0 && line.Length + 1 + word.Length > width)
            {
                builder.Append(line).Append('\n');
                line.Clear().Append(restPrefix);
                lineHasWord = false;
            }

            if (lineHasWord)
            {
                line.Append(' ');
            }

            line.Append(word);
            lineHasWord = true;
        }

        builder.Append(line).Append('\n');
    }

    /// <summary>
    ///     Appends text that is wrapped at a hanging indent that is as wide as the first-line prefix.
    /// </summary>
    /// <param name="builder">The builder to append to. Must not be null.</param>
    /// <param name="text">The text to wrap. Must not be null.</param>
    /// <param name="width">The line width, or 0 for no wrapping.</param>
    /// <param name="prefix">The text that starts the first line; later lines start with spaces of the same length.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static void AppendHanging(StringBuilder builder, string text, int width, string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        Append(builder, text, width, prefix, new string(' ', prefix.Length));
    }
}
