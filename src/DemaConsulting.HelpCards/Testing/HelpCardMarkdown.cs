using System.Text;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Testing;

/// <summary>
///     Reads the structure of a card body for tests that compare cards with other documents.
/// </summary>
/// <remarks>
///     <para>
///         Parity tests check that a user guide and the help cards agree, for example that the options table of
///         a guide lists the same names as the bullets of a card. The helpers read the headings, the sections,
///         and the bullets of a body, ignoring everything inside fenced code blocks, so a line that only looks
///         like a heading in a code sample is never mistaken for one.
///     </para>
///     <para>
///         Headings are ATX headings (one to six <c>#</c>, a space, and the text). A section runs from its
///         heading to the next heading of the same or a higher level. A bullet starts with <c>-</c>, <c>*</c>,
///         or <c>+</c> and a space at the start of a line; indented lines that follow belong to it and are joined
///         with single spaces. The class is stateless and safe for concurrent use.
///     </para>
/// </remarks>
public static class HelpCardMarkdown
{
    /// <summary>
    ///     The largest number of leading spaces a heading may have.
    /// </summary>
    private const int MaxHeadingIndent = 3;

    /// <summary>
    ///     The largest level of a heading.
    /// </summary>
    private const int MaxHeadingLevel = 6;

    /// <summary>
    ///     Gets the texts of the headings of a body, in order, outside fenced code blocks.
    /// </summary>
    /// <param name="body">The Markdown body. Must not be null.</param>
    /// <returns>The heading texts without the <c>#</c> marks, trimmed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="body"/> is null.</exception>
    public static IReadOnlyList<string> Headings(string body)
    {
        ArgumentNullException.ThrowIfNull(body);
        return Scan(body, out _)
            .Where(line => line.Kind == MarkdownLineKind.Text)
            .Select(line => TryParseHeading(line.Text, out _, out var text) ? text : null)
            .OfType<string>()
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    ///     Gets the text of a section: everything below a heading up to the next heading of the same or a higher level.
    /// </summary>
    /// <remarks>
    ///     Deeper headings and fenced blocks inside the section are part of the text. Blank lines at the start
    ///     and the end are removed. When several headings have the same text, the first one is used.
    /// </remarks>
    /// <param name="body">The Markdown body. Must not be null.</param>
    /// <param name="heading">The text of the heading, as returned by <see cref="Headings"/>. Must not be null or white space.</param>
    /// <returns>The text of the section, or <see langword="null"/> when the body has no such heading.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="body"/> or <paramref name="heading"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="heading"/> is white space.</exception>
    public static string? GetSection(string body, string heading)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentException.ThrowIfNullOrWhiteSpace(heading);
        var section = FindSection(body, heading);
        if (section is null)
        {
            return null;
        }

        var lines = section.Select(line => line.Text).ToList();
        while (lines.Count > 0 && lines[0].Trim().Length == 0)
        {
            lines.RemoveAt(0);
        }

        while (lines.Count > 0 && lines[^1].Trim().Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return string.Join('\n', lines);
    }

    /// <summary>
    ///     Gets the bullets of a section, each joined into one line.
    /// </summary>
    /// <param name="body">The Markdown body. Must not be null.</param>
    /// <param name="heading">The text of the heading of the section. Must not be null or white space.</param>
    /// <returns>The bullets without their marker, in order; empty when the heading or any bullet is missing.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="body"/> or <paramref name="heading"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="heading"/> is white space.</exception>
    public static IReadOnlyList<string> Bullets(string body, string heading)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentException.ThrowIfNullOrWhiteSpace(heading);
        var bullets = new List<string>();
        var current = new StringBuilder();
        var inBullet = false;
        foreach (var line in FindSection(body, heading) ?? [])
        {
            // Fenced code is not a bullet and ends the bullet before it
            if (line.Kind != MarkdownLineKind.Text)
            {
                Flush();
            }
            else if (IsBulletStart(line.Text))
            {
                Flush();
                current.Append(line.Text[2..].Trim());
                inBullet = true;
            }
            else if (inBullet && line.Text.StartsWith("  ", StringComparison.Ordinal) && line.Text.Trim().Length > 0)
            {
                current.Append(' ').Append(line.Text.Trim());
            }
            else
            {
                Flush();
            }
        }

        Flush();
        return bullets.AsReadOnly();

        void Flush()
        {
            if (inBullet)
            {
                bullets.Add(current.ToString());
                current.Clear();
                inBullet = false;
            }
        }
    }

    /// <summary>
    ///     Splits a body into lines and classifies each line with respect to fenced code blocks.
    /// </summary>
    /// <remarks>
    ///     A fence opens with three or more backticks or tildes at the start of a line (after spaces) and closes
    ///     with a line made only of at least as many of the same character; a backtick fence line that contains
    ///     another backtick is an inline code span, not a fence. This is the same rule the loader uses to find
    ///     code, so the validator and the loader agree on what is code.
    /// </remarks>
    /// <param name="body">The body.</param>
    /// <param name="unclosedFenceLine">Receives the number of the line that opened a fence that never closed, or 0.</param>
    /// <returns>One entry per line.</returns>
    internal static IReadOnlyList<MarkdownLine> Scan(string body, out int unclosedFenceLine)
    {
        var result = new List<MarkdownLine>();
        var fenceChar = '\0';
        var fenceLength = 0;
        var openedAt = 0;
        var number = 0;
        foreach (var rawLine in body.Split('\n'))
        {
            number++;
            var text = rawLine.TrimEnd('\r');
            var trimmed = text.AsSpan().TrimStart(' ');
            if (fenceChar != '\0')
            {
                var closes = BodyParser.IsFenceClose(trimmed, fenceChar, fenceLength);
                result.Add(new MarkdownLine(number, text, closes ? MarkdownLineKind.FenceClose : MarkdownLineKind.FenceBody, string.Empty));
                fenceChar = closes ? '\0' : fenceChar;
            }
            else if (BodyParser.TryGetFenceOpen(trimmed, out var openChar, out var openLength))
            {
                fenceChar = openChar;
                fenceLength = openLength;
                openedAt = number;
                result.Add(new MarkdownLine(number, text, MarkdownLineKind.FenceOpen, trimmed[openLength..].Trim().ToString()));
            }
            else
            {
                result.Add(new MarkdownLine(number, text, MarkdownLineKind.Text, string.Empty));
            }
        }

        unclosedFenceLine = fenceChar != '\0' ? openedAt : 0;
        return result;
    }

    /// <summary>
    ///     Determines whether a line is an ATX heading and reads its level and text.
    /// </summary>
    /// <param name="line">The line, outside any fence.</param>
    /// <param name="level">Receives the level from 1 to 6.</param>
    /// <param name="text">Receives the text of the heading without marks.</param>
    /// <returns><see langword="true"/> when the line is a heading.</returns>
    internal static bool TryParseHeading(string line, out int level, out string text)
    {
        level = 0;
        text = string.Empty;
        var span = line.AsSpan();
        var indent = span.Length - span.TrimStart(' ').Length;
        if (indent > MaxHeadingIndent)
        {
            return false;
        }

        span = span[indent..];
        while (level < span.Length && span[level] == '#')
        {
            level++;
        }

        if (level is 0 or > MaxHeadingLevel || (level < span.Length && span[level] != ' '))
        {
            return false;
        }

        var content = span[level..].Trim();

        // A closing run of marks after a space is not part of the heading text
        var closing = content.TrimEnd('#');
        if (closing.Length < content.Length && (closing.Length == 0 || closing[^1] == ' '))
        {
            content = closing.TrimEnd();
        }

        text = content.ToString();
        return true;
    }

    /// <summary>
    ///     Finds the lines of the section of a heading.
    /// </summary>
    /// <param name="body">The body.</param>
    /// <param name="heading">The heading text.</param>
    /// <returns>The lines below the heading, or <see langword="null"/> when there is no such heading.</returns>
    private static List<MarkdownLine>? FindSection(string body, string heading)
    {
        List<MarkdownLine>? section = null;
        var sectionLevel = 0;
        foreach (var line in Scan(body, out _))
        {
            var isHeading = false;
            var level = 0;
            var text = string.Empty;
            if (line.Kind == MarkdownLineKind.Text)
            {
                isHeading = TryParseHeading(line.Text, out level, out text);
            }

            if (section is null)
            {
                // The section starts after the first heading with the requested text
                if (isHeading && text == heading)
                {
                    section = [];
                    sectionLevel = level;
                }
            }
            else if (isHeading && level <= sectionLevel)
            {
                break;
            }
            else
            {
                section.Add(line);
            }
        }

        return section;
    }

    /// <summary>
    ///     Determines whether a line starts a bullet.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <returns><see langword="true"/> when the line starts with a marker and a space.</returns>
    private static bool IsBulletStart(string line)
    {
        return line.Length >= 2 && (line[0] is '-' or '*' or '+') && line[1] == ' ';
    }
}
