using System.Text;

namespace DemaConsulting.HelpCards.Testing;

/// <summary>
///     Reads the command line of a <c>command</c> example into its arguments with a small, safe grammar.
/// </summary>
/// <remarks>
///     <para>
///         The content of a command example is one line. Words are separated by spaces. A word is a sequence of
///         unquoted characters (anything except space, quotes, and the characters
///         <c>| &amp; ; &lt; &gt; ( ) $ ` * ? [ ] { } ~ ! # \ %</c>), single-quoted text (literal, without
///         escapes), and double-quoted text (only <c>\\</c> and <c>\"</c> are escapes; <c>$</c> and the backtick
///         are errors inside). Anything else is an error that names the character and its column, for example
///         <c>shell operator '|' is not allowed in an example command (column 7)</c>.
///     </para>
///     <para>
///         The grammar rejects redirection, pipes, variables, globs, comments, and line continuations on
///         purpose: an adopter runs a command itself, for example by passing the arguments to its own
///         <c>Main</c> in a test, and the library never starts a process or asks a shell to interpret text.
///         The class is stateless and safe for concurrent use.
///     </para>
/// </remarks>
public static class ExampleCommandLine
{
    /// <summary>
    ///     The characters that are shell operators and are rejected outside quotes.
    /// </summary>
    private const string Operators = "|&;<>()";

    /// <summary>
    ///     The characters that expand to something else in a shell and are rejected outside quotes.
    /// </summary>
    private const string Expansions = "$`*?[]{}~!#\\%";

    /// <summary>
    ///     Reads the arguments of a command example.
    /// </summary>
    /// <remarks>
    ///     The first word must be <paramref name="toolName"/>, compared exactly (ordinal, case-sensitive); the
    ///     arguments are the words after it. When parsing fails, <paramref name="arguments"/> is empty and
    ///     <paramref name="error"/> says what is wrong and where.
    /// </remarks>
    /// <param name="content">The content of the example. Must not be null.</param>
    /// <param name="toolName">The name the command must start with. Must not be null, empty, or white space.</param>
    /// <param name="arguments">Receives the arguments after the tool name, or an empty list on failure.</param>
    /// <param name="error">Receives the reason for a failure, or <see langword="null"/> on success.</param>
    /// <returns><see langword="true"/> when the content is a valid command line for the tool.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> or <paramref name="toolName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="toolName"/> is empty or white space.</exception>
    public static bool TryParse(string content, string toolName, out IReadOnlyList<string> arguments, out string? error)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        arguments = [];
        if (!TrySplit(content, out var words, out error))
        {
            return false;
        }

        if (words.Count == 0)
        {
            error = "the example command is empty";
            return false;
        }

        if (!string.Equals(words[0], toolName, StringComparison.Ordinal))
        {
            error = $"the example command must start with the tool name '{toolName}' but starts with '{words[0]}'";
            return false;
        }

        arguments = words.Skip(1).ToList().AsReadOnly();
        error = null;
        return true;
    }

    /// <summary>
    ///     Splits one line into words, or reports the first character the grammar does not allow.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <param name="words">Receives the words on success.</param>
    /// <param name="error">Receives the error on failure.</param>
    /// <returns><see langword="true"/> when the whole line is valid.</returns>
    private static bool TrySplit(string content, out List<string> words, out string? error)
    {
        words = [];
        error = null;
        var lineBreak = content.AsSpan().IndexOfAny('\n', '\r');
        if (lineBreak >= 0)
        {
            error = $"an example command must be one line (line break at column {lineBreak + 1})";
            return false;
        }

        // The words are collected in a local list because a local function cannot use an out parameter
        var found = new List<string>();
        var word = new StringBuilder();
        var inWord = false;
        var index = 0;
        while (index < content.Length)
        {
            var current = content[index];
            if (current == ' ')
            {
                EndWord();
                index++;
                continue;
            }

            inWord = true;
            var next = current switch
            {
                '\'' => ReadSingleQuoted(content, index, word, out error),
                '"' => ReadDoubleQuoted(content, index, word, out error),
                _ => ReadUnquoted(content, index, word, out error)
            };
            if (error is not null)
            {
                return false;
            }

            index = next;
        }

        EndWord();
        words = found;
        return true;

        void EndWord()
        {
            if (inWord)
            {
                found.Add(word.ToString());
                word.Clear();
                inWord = false;
            }
        }
    }

    /// <summary>
    ///     Reads one unquoted character.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <param name="index">The index of the character.</param>
    /// <param name="word">The word that receives the character.</param>
    /// <param name="error">Receives the error when the character is not allowed.</param>
    /// <returns>The index after the character.</returns>
    private static int ReadUnquoted(string content, int index, StringBuilder word, out string? error)
    {
        var current = content[index];
        error = null;
        if (Operators.Contains(current, StringComparison.Ordinal))
        {
            error = $"shell operator '{current}' is not allowed in an example command (column {index + 1})";
        }
        else if (Expansions.Contains(current, StringComparison.Ordinal))
        {
            error = $"shell expansion character '{current}' is not allowed in an example command (column {index + 1}); "
                + "quote it with single quotes if it is meant literally";
        }
        else
        {
            word.Append(current);
        }

        return index + 1;
    }

    /// <summary>
    ///     Reads a single-quoted part, which is literal up to the next single quote.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <param name="index">The index of the opening quote.</param>
    /// <param name="word">The word that receives the text.</param>
    /// <param name="error">Receives the error when the quote is not closed.</param>
    /// <returns>The index after the closing quote.</returns>
    private static int ReadSingleQuoted(string content, int index, StringBuilder word, out string? error)
    {
        var close = content.IndexOf('\'', index + 1);
        if (close < 0)
        {
            error = $"the single quote at column {index + 1} is not closed";
            return content.Length;
        }

        word.Append(content, index + 1, close - index - 1);
        error = null;
        return close + 1;
    }

    /// <summary>
    ///     Reads a double-quoted part, in which only a backslash before a backslash or a double quote is an escape.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <param name="index">The index of the opening quote.</param>
    /// <param name="word">The word that receives the text.</param>
    /// <param name="error">Receives the error for a character that is not allowed or a quote that is not closed.</param>
    /// <returns>The index after the closing quote.</returns>
    private static int ReadDoubleQuoted(string content, int index, StringBuilder word, out string? error)
    {
        error = null;
        var position = index + 1;
        while (position < content.Length)
        {
            var current = content[position];
            if (current == '"')
            {
                return position + 1;
            }

            if (current is '$' or '`')
            {
                error = $"'{current}' is not allowed inside double quotes in an example command (column {position + 1}); "
                    + "use single quotes for literal text";
                return content.Length;
            }

            if (current == '\\')
            {
                if (position + 1 < content.Length && content[position + 1] is '\\' or '"')
                {
                    word.Append(content[position + 1]);
                    position += 2;
                    continue;
                }

                error = $"'\\' may only escape a backslash or a double quote inside double quotes (column {position + 1})";
                return content.Length;
            }

            word.Append(current);
            position++;
        }

        error = $"the double quote at column {index + 1} is not closed";
        return content.Length;
    }
}
