using System.Text.RegularExpressions;
using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     Splits a card body into literal text and links.
/// </summary>
/// <remarks>
///     <para>
///         The link forms are <c>[[id]]</c>, <c>[[id#example]]</c>, <c>[[#example]]</c>, and the external forms
///         <c>[[source:id|text]]</c> and <c>[[source:id#example|text]]</c>. Ids are canonical lower-case local
///         ids. Links are recognized only outside fenced code blocks and inline code spans, because diagram
///         and code syntax uses the same brackets. Outside code, text that starts like a link
///         (<c>[[</c> followed by a letter, a digit, or <c>#</c>) but is not one is reported as
///         <c>link-syntax</c>, so a typo can never silently become literal text.
///     </para>
///     <para>
///         The parser checks syntax only; whether a link names an existing card or example is decided later
///         over the final tree. Parsing is pure and the class is safe for concurrent use.
///     </para>
/// </remarks>
internal static partial class BodyParser
{
    /// <summary>
    ///     The most characters the display text of an external link may have.
    /// </summary>
    private const int MaxLinkTextLength = 120;

    /// <summary>
    ///     The most segments a card id may have.
    /// </summary>
    private const int MaxIdSegments = 8;

    /// <summary>
    ///     The most characters a segment may have.
    /// </summary>
    private const int MaxSegmentLength = 48;

    /// <summary>
    ///     Splits a body into text and link parts.
    /// </summary>
    /// <param name="body">The body with tokens already replaced.</param>
    /// <param name="firstLine">The one-based line of the file the body starts on.</param>
    /// <param name="sourceName">The name of the source, for problems.</param>
    /// <param name="file">The path of the file, for problems.</param>
    /// <param name="problems">Receives a <c>link-syntax</c> problem for each text that is not a valid link.</param>
    /// <returns>The parts in body order; adjacent text is merged and empty text is omitted.</returns>
    public static IReadOnlyList<BodyPart> Parse(
        string body,
        int firstLine,
        string sourceName,
        string file,
        ICollection<HelpProblem> problems)
    {
        var code = FindCode(body);
        var parts = new List<BodyPart>();
        var textStart = 0;
        var i = 0;
        while (i < body.Length - 1)
        {
            // Only an opening "[[" outside code can start a link
            if (body[i] != '[' || body[i + 1] != '[' || code[i])
            {
                i++;
                continue;
            }

            var line = firstLine + body.AsSpan(0, i).Count('\n');
            var link = MatchLink(body, i, line, out var length);
            if (link is null)
            {
                // Text that starts like a link but is not one is a typo, never literal text
                var isTypo = i + 2 < body.Length && (char.IsAsciiLetterOrDigit(body[i + 2]) || body[i + 2] == '#');
                if (isTypo)
                {
                    problems.Add(HelpProblem.CreateError(
                        RuleIds.LinkSyntax,
                        sourceName,
                        file,
                        line,
                        "Text that starts like a link is not one; write [[a/b]], [[a/b#example]], [[#example]], or "
                        + "[[source:a/b|text]] with canonical lower-case names, or put the text in a code span."));
                }

                // Step one character for other text so a link that overlaps the brackets, as in "[[[x]]]", is found
                i += isTypo ? 2 : 1;
                continue;
            }

            if (i > textStart)
            {
                parts.Add(new BodyPart(body[textStart..i], null));
            }

            parts.Add(new BodyPart(null, link));
            i += length;
            textStart = i;
        }

        if (textStart < body.Length)
        {
            parts.Add(new BodyPart(body[textStart..], null));
        }

        return parts;
    }

    /// <summary>
    ///     Matches a local link.
    /// </summary>
    /// <returns>The pattern.</returns>
    [GeneratedRegex(
        @"\G\[\[(?<card>[a-z0-9]+(?:-[a-z0-9]+)*(?:/[a-z0-9]+(?:-[a-z0-9]+)*)*)?(?:#(?<ex>[a-z0-9]+(?:-[a-z0-9]+)*))?\]\]",
        RegexOptions.CultureInvariant,
        1000)]
    private static partial Regex LocalLinkPattern();

    /// <summary>
    ///     Matches an external link.
    /// </summary>
    /// <returns>The pattern.</returns>
    [GeneratedRegex(
        @"\G\[\[(?<src>[a-z0-9]+(?:-[a-z0-9]+)*):(?<card>[a-z0-9]+(?:-[a-z0-9]+)*(?:/[a-z0-9]+(?:-[a-z0-9]+)*)*)(?:#(?<ex>[a-z0-9]+(?:-[a-z0-9]+)*))?\|(?<text>[^\]\n]+)\]\]",
        RegexOptions.CultureInvariant,
        1000)]
    private static partial Regex ExternalLinkPattern();

    /// <summary>
    ///     Tries to read a link that starts at an offset.
    /// </summary>
    /// <param name="body">The body.</param>
    /// <param name="start">The offset of the opening brackets.</param>
    /// <param name="line">The file line of the offset.</param>
    /// <param name="length">The number of characters the link spans.</param>
    /// <returns>The link, or <see langword="null"/> when the text is not a valid link.</returns>
    private static ParsedLink? MatchLink(string body, int start, int line, out int length)
    {
        length = 0;
        var external = ExternalLinkPattern().Match(body, start);
        if (external.Success)
        {
            var text = external.Groups["text"].Value;
            var card = external.Groups["card"].Value;
            var example = external.Groups["ex"].Success ? external.Groups["ex"].Value : null;
            if (text.Length > MaxLinkTextLength || text.Trim() != text || !IsValidId(card) || !IsValidSegment(example))
            {
                return null;
            }

            length = external.Length;
            return new ParsedLink(external.Groups["src"].Value, card, example, text, line);
        }

        var local = LocalLinkPattern().Match(body, start);
        if (!local.Success || (!local.Groups["card"].Success && !local.Groups["ex"].Success))
        {
            return null;
        }

        var localCard = local.Groups["card"].Success ? local.Groups["card"].Value : null;
        var localExample = local.Groups["ex"].Success ? local.Groups["ex"].Value : null;
        if ((localCard is not null && !IsValidId(localCard)) || !IsValidSegment(localExample))
        {
            return null;
        }

        length = local.Length;
        return new ParsedLink(null, localCard, localExample, null, line);
    }

    /// <summary>
    ///     Checks the length limits of a card id.
    /// </summary>
    /// <param name="id">An id that already matches the segment grammar.</param>
    /// <returns><see langword="true"/> when the id has at most eight segments of at most 48 characters.</returns>
    private static bool IsValidId(string id)
    {
        var segments = id.Split('/');
        return segments.Length <= MaxIdSegments && segments.All(s => s.Length <= MaxSegmentLength);
    }

    /// <summary>
    ///     Checks the length limit of an optional example name.
    /// </summary>
    /// <param name="name">The name, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the name is absent or at most 48 characters.</returns>
    private static bool IsValidSegment(string? name)
    {
        return name is null || name.Length <= MaxSegmentLength;
    }

    /// <summary>
    ///     Finds the characters of a body that are code: fenced blocks and inline code spans.
    /// </summary>
    /// <remarks>
    ///     A fence is a line that starts (after spaces) with three or more backticks or tildes and ends at a
    ///     line made of at least as many of the same character; an unclosed fence runs to the end of the body.
    ///     A code span opens with a run of backticks and closes with the next run of the same length in the
    ///     same paragraph (a span may cross a line break); a run without a match is literal text.
    /// </remarks>
    /// <param name="body">The body.</param>
    /// <returns>One flag per character, set for code.</returns>
    private static bool[] FindCode(string body)
    {
        var code = new bool[body.Length];
        var fenceChar = '\0';
        var fenceLength = 0;
        var paragraphStart = -1;
        var paragraphEnd = -1;
        var start = 0;
        while (start <= body.Length)
        {
            // Look at one line at a time; "end" is the line feed or the end of the body
            var end = body.IndexOf('\n', start);
            end = end < 0 ? body.Length : end;
            var line = body.AsSpan(start, end - start).TrimStart(' ');
            if (fenceChar != '\0')
            {
                // Inside a fence every character is code; a long enough run of the fence character closes it
                Array.Fill(code, true, start, end - start);
                if (IsFenceClose(line, fenceChar, fenceLength))
                {
                    fenceChar = '\0';
                }
            }
            else if (TryGetFenceOpen(line, out var openChar, out var openLength))
            {
                MarkSpans(body, paragraphStart, paragraphEnd, code);
                paragraphStart = -1;
                fenceChar = openChar;
                fenceLength = openLength;
                Array.Fill(code, true, start, end - start);
            }
            else if (line.IsWhiteSpace())
            {
                // A blank line ends a paragraph and any code span in it
                MarkSpans(body, paragraphStart, paragraphEnd, code);
                paragraphStart = -1;
            }
            else
            {
                paragraphStart = paragraphStart < 0 ? start : paragraphStart;
                paragraphEnd = end;
            }

            start = end + 1;
        }

        MarkSpans(body, paragraphStart, paragraphEnd, code);
        return code;
    }

    /// <summary>
    ///     Determines whether a line opens a fenced code block.
    /// </summary>
    /// <param name="line">The line without leading spaces.</param>
    /// <param name="fenceChar">The fence character when the line opens a fence.</param>
    /// <param name="fenceLength">The length of the opening run when the line opens a fence.</param>
    /// <returns><see langword="true"/> when the line opens a fence.</returns>
    private static bool TryGetFenceOpen(ReadOnlySpan<char> line, out char fenceChar, out int fenceLength)
    {
        fenceChar = line.Length > 0 ? line[0] : '\0';
        fenceLength = 0;
        if (fenceChar is not ('`' or '~'))
        {
            return false;
        }

        while (fenceLength < line.Length && line[fenceLength] == fenceChar)
        {
            fenceLength++;
        }

        // A backtick run followed by more backticks on the same line is an inline span, not a fence
        return fenceLength >= 3 && !(fenceChar == '`' && line[fenceLength..].Contains('`'));
    }

    /// <summary>
    ///     Determines whether a line closes the open fence.
    /// </summary>
    /// <param name="line">The line without leading spaces.</param>
    /// <param name="fenceChar">The fence character of the open fence.</param>
    /// <param name="fenceLength">The length of the opening run.</param>
    /// <returns><see langword="true"/> when the line is a run of at least the opening length and nothing else.</returns>
    private static bool IsFenceClose(ReadOnlySpan<char> line, char fenceChar, int fenceLength)
    {
        var run = 0;
        while (run < line.Length && line[run] == fenceChar)
        {
            run++;
        }

        return run >= fenceLength && line[run..].IsWhiteSpace();
    }

    /// <summary>
    ///     Marks the inline code spans of one paragraph as code.
    /// </summary>
    /// <param name="body">The body.</param>
    /// <param name="start">The offset of the paragraph, or a negative number when there is none.</param>
    /// <param name="end">The offset after the paragraph.</param>
    /// <param name="code">The flags to set.</param>
    private static void MarkSpans(string body, int start, int end, bool[] code)
    {
        if (start < 0)
        {
            return;
        }

        var position = start;
        while (position < end)
        {
            if (body[position] != '`')
            {
                position++;
                continue;
            }

            // A span closes at the next backtick run of exactly the opening length
            var openLength = RunLength(body, position, end);
            var close = FindClosingRun(body, position + openLength, end, openLength);
            if (close < 0)
            {
                position += openLength;
                continue;
            }

            Array.Fill(code, true, position, close + openLength - position);
            position = close + openLength;
        }
    }

    /// <summary>
    ///     Finds the next backtick run of an exact length.
    /// </summary>
    /// <param name="body">The body.</param>
    /// <param name="from">The offset to start at.</param>
    /// <param name="end">The offset after the paragraph.</param>
    /// <param name="length">The run length to find.</param>
    /// <returns>The offset of the run, or -1 when there is none.</returns>
    private static int FindClosingRun(string body, int from, int end, int length)
    {
        var position = from;
        while (position < end)
        {
            if (body[position] != '`')
            {
                position++;
                continue;
            }

            var run = RunLength(body, position, end);
            if (run == length)
            {
                return position;
            }

            position += run;
        }

        return -1;
    }

    /// <summary>
    ///     Counts the backticks of the run that starts at an offset.
    /// </summary>
    /// <param name="body">The body.</param>
    /// <param name="position">The offset of the first backtick.</param>
    /// <param name="end">The offset after the paragraph.</param>
    /// <returns>The number of consecutive backticks.</returns>
    private static int RunLength(string body, int position, int end)
    {
        var run = 0;
        while (position + run < end && body[position + run] == '`')
        {
            run++;
        }

        return run;
    }
}
