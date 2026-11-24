using System.Text.RegularExpressions;
using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     Decodes the restricted subset of YAML front matter that help cards use, and splits off the body.
/// </summary>
/// <remarks>
///     <para>
///         HelpCards owns its decoder so adopters stay free of a YAML dependency, errors are deterministic
///         and name the exact line, and no YAML dialect can drift. The subset is: a block of
///         <c>key: value</c> entries; values that are plain, single-quoted, or double-quoted scalars; lists of
///         scalars; lists of maps of scalars; full-line comments; and, only for the <c>content</c> of an
///         example, the literal block scalar <c>|</c> or <c>|-</c>. Everything else (flow collections, anchors,
///         aliases, tags, trailing comments, multi-document streams, tabs in indentation, deeper nesting,
///         duplicate keys, folded or chomped block scalars) is rejected with an error that names the line and
///         says how to fix it.
///     </para>
///     <para>
///         The decoder stops at the first defect of a file, which matches the rule that a syntax error ends
///         that file only. It performs no validation of key names or value grammar; that belongs to the card
///         reader. Decoding is pure, so the class is safe for concurrent use.
///     </para>
/// </remarks>
internal sealed partial class FrontMatterDecoder
{
    /// <summary>
    ///     The line that opens and closes the front matter.
    /// </summary>
    private const string Delimiter = "---";

    /// <summary>
    ///     The one-based file line of the first front-matter line (the line after the opening delimiter).
    /// </summary>
    private const int FirstFrontMatterLine = 2;

    /// <summary>
    ///     The character that a byte order mark decodes to.
    /// </summary>
    private const char ByteOrderMark = (char)0xFEFF;

    /// <summary>
    ///     The front-matter lines between the delimiters.
    /// </summary>
    private readonly string[] _lines;

    /// <summary>
    ///     The index of the next line that has not been consumed.
    /// </summary>
    private int _index;

    /// <summary>
    ///     Creates a decoder over the front-matter lines.
    /// </summary>
    /// <param name="lines">The lines between the delimiters, without line endings.</param>
    private FrontMatterDecoder(string[] lines)
    {
        _lines = lines;
    }

    /// <summary>
    ///     Decodes a card file.
    /// </summary>
    /// <remarks>
    ///     A leading byte order mark is removed and CRLF line endings are converted to LF before decoding, so
    ///     the line numbers of problems match the lines an author sees. One blank line directly after the
    ///     closing delimiter is not part of the body, and trailing line feeds of the body are removed.
    /// </remarks>
    /// <param name="text">The whole text of the file.</param>
    /// <param name="sourceName">The name of the source, for the problem.</param>
    /// <param name="file">The path of the file inside the source, for the problem.</param>
    /// <param name="document">The decoded document, or <see langword="null"/> when decoding failed.</param>
    /// <param name="problem">The problem that ended decoding, or <see langword="null"/> on success.</param>
    /// <returns><see langword="true"/> when the file was decoded; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static bool TryDecode(
        string text,
        string sourceName,
        string file,
        out FrontMatterDocument? document,
        out HelpProblem? problem)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(sourceName);
        ArgumentNullException.ThrowIfNull(file);

        try
        {
            document = Decode(text);
            problem = null;
            return true;
        }
        catch (FormatException ex) when (ex.Data["rule"] is string ruleId && ex.Data["line"] is int line)
        {
            document = null;
            problem = HelpProblem.CreateError(ruleId, sourceName, file, line, ex.Message);
            return false;
        }
    }

    /// <summary>
    ///     Splits the file into front matter and body and decodes the front matter.
    /// </summary>
    /// <param name="text">The whole text of the file.</param>
    /// <returns>The decoded document.</returns>
    /// <exception cref="FormatException">Thrown at the first defect.</exception>
    private static FrontMatterDocument Decode(string text)
    {
        // Normalize first so every later line number is the line an author sees in an editor
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        if (normalized.Length > 0 && normalized[0] == ByteOrderMark)
        {
            normalized = normalized[1..];
        }

        // The file is a delimiter line, the front matter, a delimiter line, and the body
        var lines = normalized.Split('\n');
        if (lines[0] != Delimiter)
        {
            throw Syntax(1, "The file must start with a line '---' that opens the front matter.");
        }

        var close = Array.IndexOf(lines, Delimiter, 1);
        if (close < 0)
        {
            throw Syntax(1, "The front matter is not closed; add a line '---' after the last key.");
        }

        var entries = new FrontMatterDecoder(lines[1..close]).ParseEntries();

        // One blank line after the closing delimiter separates the body and is not part of it
        var bodyStart = close + 1;
        if (bodyStart < lines.Length && string.IsNullOrWhiteSpace(lines[bodyStart]))
        {
            bodyStart++;
        }

        var body = string.Join('\n', lines.Skip(bodyStart)).TrimEnd('\n');
        return new FrontMatterDocument(entries, body, bodyStart + 1);
    }

    /// <summary>
    ///     Creates the exception for a front-matter syntax error.
    /// </summary>
    /// <param name="line">The one-based file line.</param>
    /// <param name="message">The message.</param>
    /// <returns>The exception to throw.</returns>
    private static FormatException Syntax(int line, string message)
    {
        return Failure(RuleIds.Syntax, line, message);
    }

    /// <summary>
    ///     Creates the exception that ends the decoding of one file.
    /// </summary>
    /// <remarks>
    ///     The decoder walks nested structures and stops at the first defect of a file, so an exception is
    ///     the simplest way to leave the walk. A <see cref="FormatException"/> that carries the rule and the
    ///     line in its <see cref="Exception.Data"/> is used instead of a private exception type; it never
    ///     leaves <see cref="TryDecode"/>, which converts it into a problem.
    /// </remarks>
    /// <param name="ruleId">The rule that was broken.</param>
    /// <param name="line">The one-based file line.</param>
    /// <param name="message">The message.</param>
    /// <returns>The exception to throw.</returns>
    private static FormatException Failure(string ruleId, int line, string message)
    {
        var exception = new FormatException(message);
        exception.Data["rule"] = ruleId;
        exception.Data["line"] = line;
        return exception;
    }

    /// <summary>
    ///     Matches an entry <c>key: value</c> or <c>key:</c> and captures the key and the text after the colon.
    /// </summary>
    /// <returns>The pattern.</returns>
    [GeneratedRegex(@"^(?<key>[A-Za-z0-9][A-Za-z0-9_-]*):(?: +(?<rest>.*))?$", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex EntryPattern();

    /// <summary>
    ///     Converts a line index to the one-based line of the file.
    /// </summary>
    /// <param name="index">The index in the front-matter lines.</param>
    /// <returns>The file line.</returns>
    private static int LineNumber(int index)
    {
        return index + FirstFrontMatterLine;
    }

    /// <summary>
    ///     Parses every top-level entry of the front matter.
    /// </summary>
    /// <returns>The entries in file order.</returns>
    /// <exception cref="FormatException">Thrown at the first defect.</exception>
    private List<FrontMatterEntry> ParseEntries()
    {
        var entries = new List<FrontMatterEntry>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (var next = NextSignificant(_index); next >= 0; next = NextSignificant(_index))
        {
            // Top-level entries start in the first column; anything else belongs to no entry
            if (MeasureIndent(next) != 0)
            {
                throw Syntax(LineNumber(next), "Unexpected indentation; an entry or list item must belong to a key above it.");
            }

            var entry = ParseEntry(next, 0, _lines[next].TrimEnd(), null);
            if (!keys.Add(entry.Key))
            {
                throw Syntax(entry.Line, $"The key '{entry.Key}' is used twice.");
            }

            entries.Add(entry);
        }

        return entries;
    }

    /// <summary>
    ///     Parses one <c>key: value</c> entry, including the list or block scalar below it.
    /// </summary>
    /// <param name="index">The index of the line the key is on.</param>
    /// <param name="column">The zero-based column the key starts at; 0 for a top-level entry.</param>
    /// <param name="text">The text of the line from the key on.</param>
    /// <param name="listKey">The top-level key whose list the entry belongs to, or <see langword="null"/>.</param>
    /// <returns>The entry. After the call the next unconsumed line follows the entry.</returns>
    /// <exception cref="FormatException">Thrown at the first defect.</exception>
    private FrontMatterEntry ParseEntry(int index, int column, string text, string? listKey)
    {
        var line = LineNumber(index);
        var match = EntryPattern().Match(text);
        if (!match.Success)
        {
            throw Syntax(line, DescribeNotAnEntry(text));
        }

        var key = match.Groups["key"].Value;
        var rest = match.Groups["rest"].Success ? match.Groups["rest"].Value.TrimEnd() : string.Empty;
        _index = index + 1;
        if (rest.Length == 0)
        {
            return ParseEmptyValue(key, line, column);
        }

        // A literal block scalar is allowed only as the content of an example
        var allowBlock = listKey == "examples" && key == "content";
        return new FrontMatterEntry(key, line, ParseScalar(rest, line, index, column, allowBlock), null);
    }

    /// <summary>
    ///     Parses the value of a key that has nothing after its colon: a list below it, or an empty scalar.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="line">The file line of the key.</param>
    /// <param name="column">The column of the key; 0 for a top-level key.</param>
    /// <returns>The entry.</returns>
    /// <exception cref="FormatException">Thrown when the value below the key is not a supported list.</exception>
    private FrontMatterEntry ParseEmptyValue(string key, int line, int column)
    {
        var next = NextSignificant(_index);
        if (next < 0)
        {
            return new FrontMatterEntry(key, line, new FrontMatterScalar(string.Empty, line), null);
        }

        var indent = MeasureIndent(next);
        if (column > 0)
        {
            // Inside a map item nothing may nest below a key
            if (indent > column)
            {
                throw Syntax(LineNumber(next), "Nesting deeper than a list of maps is not supported.");
            }
        }
        else if (IsListItem(_lines[next]))
        {
            return new FrontMatterEntry(key, line, null, ParseList(key));
        }
        else if (indent > 0)
        {
            throw Syntax(LineNumber(next), $"The value of '{key}' must be a scalar or a list; nested mappings are not supported.");
        }

        return new FrontMatterEntry(key, line, new FrontMatterScalar(string.Empty, line), null);
    }

    /// <summary>
    ///     Parses the list that starts at the next significant line.
    /// </summary>
    /// <param name="key">The key the list belongs to.</param>
    /// <returns>The items.</returns>
    /// <exception cref="FormatException">Thrown at the first defect.</exception>
    private List<FrontMatterItem> ParseList(string key)
    {
        var items = new List<FrontMatterItem>();
        var listIndent = MeasureIndent(NextSignificant(_index));
        for (var next = NextSignificant(_index); next >= 0; next = NextSignificant(_index))
        {
            var indent = MeasureIndent(next);
            if (indent < listIndent)
            {
                break;
            }

            if (indent > listIndent)
            {
                throw Syntax(LineNumber(next), "Unexpected indentation inside a list.");
            }

            if (!IsListItem(_lines[next]))
            {
                // A list at the first column ends at the next top-level key; elsewhere this is misaligned
                if (listIndent == 0)
                {
                    break;
                }

                throw Syntax(LineNumber(next), "Expected a list item that starts with '- '.");
            }

            items.Add(ParseItem(next, listIndent, key));
        }

        return items;
    }

    /// <summary>
    ///     Parses one list item: a scalar or a map of scalars.
    /// </summary>
    /// <param name="index">The index of the line with the dash.</param>
    /// <param name="dashColumn">The column of the dash.</param>
    /// <param name="listKey">The top-level key that owns the list.</param>
    /// <returns>The item.</returns>
    /// <exception cref="FormatException">Thrown at the first defect.</exception>
    private FrontMatterItem ParseItem(int index, int dashColumn, string listKey)
    {
        var line = LineNumber(index);
        var afterDash = _lines[index][(dashColumn + 1)..];
        var text = afterDash.TrimStart(' ').TrimEnd();
        var column = dashColumn + 1 + (afterDash.Length - afterDash.TrimStart(' ').Length);
        if (text.Length == 0)
        {
            throw Syntax(line, "A list item needs its value on the same line as the '-'.");
        }

        // A quoted text is always a scalar; an unquoted 'key: value' starts a map; anything else is plain text
        if (text[0] is not ('\'' or '"') && EntryPattern().IsMatch(text))
        {
            return new FrontMatterItem(line, null, ParseMap(index, column, text, dashColumn, listKey));
        }

        _index = index + 1;
        return new FrontMatterItem(line, ParseScalar(text, line, index, column, false), null);
    }

    /// <summary>
    ///     Parses the entries of a map item, starting with the one on the dash line.
    /// </summary>
    /// <param name="index">The index of the dash line.</param>
    /// <param name="column">The column of the first key, which every other key of the map must share.</param>
    /// <param name="firstText">The text of the dash line from the first key on.</param>
    /// <param name="dashColumn">The column of the dash.</param>
    /// <param name="listKey">The top-level key that owns the list.</param>
    /// <returns>The entries of the map.</returns>
    /// <exception cref="FormatException">Thrown at the first defect.</exception>
    private List<FrontMatterEntry> ParseMap(int index, int column, string firstText, int dashColumn, string listKey)
    {
        var entries = new List<FrontMatterEntry>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var first = ParseEntry(index, column, firstText, listKey);
        keys.Add(first.Key);
        entries.Add(first);

        for (var next = NextSignificant(_index); next >= 0; next = NextSignificant(_index))
        {
            // The map continues while lines start in its key column; a shallower line ends it
            var indent = MeasureIndent(next);
            if (indent <= dashColumn)
            {
                break;
            }

            if (indent != column)
            {
                throw Syntax(LineNumber(next), $"Keys of one list item must start in the same column (column {column + 1}).");
            }

            if (IsListItem(_lines[next]))
            {
                throw Syntax(LineNumber(next), "Nesting deeper than a list of maps is not supported.");
            }

            var entry = ParseEntry(next, column, _lines[next].TrimStart(' ').TrimEnd(), listKey);
            if (!keys.Add(entry.Key))
            {
                throw Syntax(entry.Line, $"The key '{entry.Key}' is used twice in one list item.");
            }

            entries.Add(entry);
        }

        return entries;
    }

    /// <summary>
    ///     Finds the next line that is neither blank nor a full-line comment.
    /// </summary>
    /// <param name="from">The index to start at.</param>
    /// <returns>The index of the line, or -1 when no line is left.</returns>
    private int NextSignificant(int from)
    {
        for (var i = from; i < _lines.Length; i++)
        {
            var trimmed = _lines[i].TrimStart(' ', '\t');
            if (trimmed.Length > 0 && trimmed[0] != '#')
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    ///     Counts the leading spaces of a line and rejects tabs in the indentation.
    /// </summary>
    /// <param name="index">The index of the line.</param>
    /// <returns>The number of leading spaces.</returns>
    /// <exception cref="FormatException">Thrown when the indentation contains a tab.</exception>
    private int MeasureIndent(int index)
    {
        var line = _lines[index];
        var count = 0;
        while (count < line.Length && line[count] is ' ' or '\t')
        {
            if (line[count] == '\t')
            {
                throw Syntax(LineNumber(index), "Tabs are not allowed in front-matter indentation; use spaces.");
            }

            count++;
        }

        return count;
    }

    /// <summary>
    ///     Determines whether a line is a list item (<c>- value</c> or a lone <c>-</c>).
    /// </summary>
    /// <param name="line">The line.</param>
    /// <returns><see langword="true"/> for a list item.</returns>
    private static bool IsListItem(string line)
    {
        var trimmed = line.TrimStart(' ').TrimEnd();
        return trimmed == "-" || trimmed.StartsWith("- ", StringComparison.Ordinal);
    }

    /// <summary>
    ///     Explains why a line is not a <c>key: value</c> entry.
    /// </summary>
    /// <param name="text">The text of the line from where the key should start.</param>
    /// <returns>The message.</returns>
    private static string DescribeNotAnEntry(string text)
    {
        if (text.StartsWith("---", StringComparison.Ordinal) || text == "...")
        {
            return "Document markers are not supported; the front matter is a single document.";
        }

        if (text.StartsWith('-'))
        {
            return "A list item must belong to a key such as 'aliases:'; this one has none.";
        }

        return "Expected 'key: value'; a key is made of letters, digits, '-' and '_' and is followed by ':' and a space.";
    }
}
