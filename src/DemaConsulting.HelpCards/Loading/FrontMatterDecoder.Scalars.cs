using System.Text;
using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     The scalar part of the front-matter decoder: plain, quoted, and literal block scalars.
/// </summary>
internal sealed partial class FrontMatterDecoder
{
    /// <summary>
    ///     Parses the text after a colon (or after a list dash) as one scalar.
    /// </summary>
    /// <remarks>
    ///     The first character decides the kind. Quotes start a quoted scalar, <c>|</c> and <c>&gt;</c> a
    ///     block scalar, and the indicator characters of YAML features that the subset does not support
    ///     (flow collections, anchors, aliases, tags, comments, directives) are rejected with a message that
    ///     tells the author to quote the text. Everything else is a plain scalar.
    /// </remarks>
    /// <param name="text">The trimmed text of the value. Not empty.</param>
    /// <param name="line">The file line the value starts on.</param>
    /// <param name="index">The index of the line the value starts on.</param>
    /// <param name="column">The column of the key that owns the value; a block must be indented more.</param>
    /// <param name="allowBlock">Whether a literal block scalar is allowed here.</param>
    /// <returns>The scalar.</returns>
    /// <exception cref="FormatException">Thrown when the text is not a supported scalar.</exception>
    private FrontMatterScalar ParseScalar(string text, int line, int index, int column, bool allowBlock)
    {
        switch (text[0])
        {
            case '\'':
                return new FrontMatterScalar(ParseSingleQuoted(text, line), line);
            case '"':
                return new FrontMatterScalar(ParseDoubleQuoted(text, line), line);
            case '|' or '>':
                return ParseBlockScalar(text, index, column, allowBlock);
            case '[' or '{':
                throw Syntax(line, "Flow collections ('[...]' and '{...}') are not supported; use a block list, or quote the text.");
            case '&':
                throw Syntax(line, "Anchors ('&') are not supported; quote the text if the '&' is part of it.");
            case '*':
                throw Syntax(line, "Aliases ('*') are not supported; quote the text if the '*' is part of it.");
            case '!':
                throw Syntax(line, "Tags ('!') are not supported; quote the text if the '!' is part of it.");
            case '#':
                throw Syntax(line, "A value cannot start with '#' (trailing comments are not supported); quote the text.");
            case '%' or '@' or '`':
                throw Syntax(line, $"A plain value cannot start with '{text[0]}'; quote the text.");
            default:
                _index = index + 1;
                return new FrontMatterScalar(CheckPlain(text, line), line);
        }
    }

    /// <summary>
    ///     Checks that a plain scalar contains nothing that YAML would read differently.
    /// </summary>
    /// <param name="text">The trimmed text.</param>
    /// <param name="line">The file line.</param>
    /// <returns>The text, unchanged.</returns>
    /// <exception cref="FormatException">Thrown when the text must be quoted.</exception>
    private static string CheckPlain(string text, int line)
    {
        if (text.Contains(" #", StringComparison.Ordinal))
        {
            throw Syntax(line, "Trailing comments are not supported; quote the text if the '#' is part of it.");
        }

        if (text.Contains(": ", StringComparison.Ordinal))
        {
            throw Syntax(line, "A plain value cannot contain ': '; quote the text.");
        }

        if (text.EndsWith(':'))
        {
            throw Syntax(line, "A plain value cannot end with ':'; quote the text.");
        }

        return text;
    }

    /// <summary>
    ///     Parses a single-quoted scalar, in which <c>''</c> stands for one quote.
    /// </summary>
    /// <param name="text">The trimmed text, starting with the opening quote.</param>
    /// <param name="line">The file line.</param>
    /// <returns>The unquoted text.</returns>
    /// <exception cref="FormatException">Thrown when the quote is not closed or text follows it.</exception>
    private static string ParseSingleQuoted(string text, int line)
    {
        var result = new StringBuilder();
        var i = 1;
        while (i < text.Length)
        {
            if (text[i] != '\'')
            {
                result.Append(text[i]);
                i++;
            }
            else if (i + 1 < text.Length && text[i + 1] == '\'')
            {
                // Two quotes in a row stand for one quote character
                result.Append('\'');
                i += 2;
            }
            else
            {
                RequireNothingAfterQuote(text, i + 1, line);
                return result.ToString();
            }
        }

        throw Syntax(line, "The single-quoted value is not closed; add the closing quote on the same line.");
    }

    /// <summary>
    ///     Parses a double-quoted scalar, in which only <c>\\</c> and <c>\"</c> are escapes.
    /// </summary>
    /// <param name="text">The trimmed text, starting with the opening quote.</param>
    /// <param name="line">The file line.</param>
    /// <returns>The unquoted and unescaped text.</returns>
    /// <exception cref="FormatException">Thrown for another escape, a missing closing quote, or text after it.</exception>
    private static string ParseDoubleQuoted(string text, int line)
    {
        var result = new StringBuilder();
        var i = 1;
        while (i < text.Length)
        {
            var next = i + 1 < text.Length ? text[i + 1] : '\0';
            if (text[i] == '"')
            {
                RequireNothingAfterQuote(text, i + 1, line);
                return result.ToString();
            }

            if (text[i] == '\\' && next is '\\' or '"')
            {
                // A backslash escapes only a backslash or a double quote
                result.Append(next);
                i += 2;
            }
            else if (text[i] == '\\')
            {
                throw Syntax(line, "Only the escapes '\\\\' and '\\\"' are supported in a double-quoted value.");
            }
            else
            {
                result.Append(text[i]);
                i++;
            }
        }

        throw Syntax(line, "The double-quoted value is not closed; add the closing quote on the same line.");
    }

    /// <summary>
    ///     Requires that only white space follows the closing quote of a quoted scalar.
    /// </summary>
    /// <param name="text">The trimmed text of the value.</param>
    /// <param name="start">The index after the closing quote.</param>
    /// <param name="line">The file line.</param>
    /// <exception cref="FormatException">Thrown when other text follows the closing quote.</exception>
    private static void RequireNothingAfterQuote(string text, int start, int line)
    {
        if (!string.IsNullOrWhiteSpace(text[start..]))
        {
            throw Syntax(line, "Unexpected text after the closing quote (trailing comments are not supported).");
        }
    }

    /// <summary>
    ///     Parses a literal block scalar, which is allowed only for the content and the expected output of an example.
    /// </summary>
    /// <remarks>
    ///     The block starts on the line after the header. Its indent is the number of leading spaces of the
    ///     first non-blank line and must exceed the indent of the key. The block continues while lines are
    ///     blank or have at least that indent; the indent is removed, inner blank lines are kept, a line that
    ///     starts with <c>#</c> is content, and trailing blank lines are dropped. Only the headers <c>|</c> and
    ///     <c>|-</c> are supported, and they mean the same because trailing line feeds are always removed.
    /// </remarks>
    /// <param name="header">The trimmed header text.</param>
    /// <param name="headerIndex">The index of the line with the header.</param>
    /// <param name="keyColumn">The column of the key that owns the block.</param>
    /// <param name="allowBlock">Whether a block scalar is allowed for this key.</param>
    /// <returns>The scalar whose line is the first line of the block.</returns>
    /// <exception cref="FormatException">Thrown when the block is not allowed or the header is unsupported.</exception>
    private FrontMatterScalar ParseBlockScalar(string header, int headerIndex, int keyColumn, bool allowBlock)
    {
        var headerLine = LineNumber(headerIndex);
        if (!allowBlock)
        {
            throw Failure(
                RuleIds.BlockScalarNotAllowed,
                headerLine,
                "A block scalar is allowed only for the 'content' or 'expect' of an example; write the value on one line, or quote it.");
        }

        if (header != "|" && header != "|-")
        {
            throw Syntax(
                headerLine,
                $"The block scalar header '{header}' is not supported; use '|' or '|-' (no '+', indentation indicator, folding, or comment).");
        }

        // Find the end of the block and its indent from the first line that is not blank
        var start = headerIndex + 1;
        var end = start;
        var blockIndent = -1;
        for (var i = start; i < _lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(_lines[i]))
            {
                continue;
            }

            var leading = _lines[i].Length - _lines[i].TrimStart(' ').Length;
            if (blockIndent < 0)
            {
                if (leading <= keyColumn)
                {
                    break;
                }

                blockIndent = leading;
            }
            else if (leading < blockIndent)
            {
                break;
            }

            end = i + 1;
        }

        // Remove the indent; inner blank lines stay, trailing blank lines are outside [start, end)
        var parts = new List<string>();
        for (var i = start; i < end; i++)
        {
            var text = _lines[i];
            parts.Add(text.Length > blockIndent ? text[blockIndent..] : string.Empty);
        }

        _index = end;
        return new FrontMatterScalar(string.Join('\n', parts), end > start ? LineNumber(start) : headerLine);
    }
}
