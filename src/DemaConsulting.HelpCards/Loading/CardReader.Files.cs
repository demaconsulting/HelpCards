using System.Text.RegularExpressions;
using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     File classification and the grammars of names, for the card reader.
/// </summary>
internal sealed partial class CardReader
{
    /// <summary>
    ///     The most characters a segment may have.
    /// </summary>
    private const int MaxSegmentLength = 48;

    /// <summary>
    ///     Determines whether text is a segment: lower-case letters and digits with single dashes, 1 to 48 characters.
    /// </summary>
    /// <remarks>
    ///     Segments name cards, folders, sources, mounts, examples, tags, and neutral aliases.
    /// </remarks>
    /// <param name="text">The text to test.</param>
    /// <returns><see langword="true"/> when the text is a segment.</returns>
    public static bool IsSegment(string? text)
    {
        return text is { Length: > 0 and <= MaxSegmentLength } && SegmentPattern().IsMatch(text);
    }

    /// <summary>
    ///     Determines whether text is a local card id: 1 to 8 segments joined by <c>/</c>.
    /// </summary>
    /// <param name="text">The text to test.</param>
    /// <returns><see langword="true"/> when the text is a local id.</returns>
    public static bool IsLocalId(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var segments = text.Split('/');
        return segments.Length <= MaxIdSegments && segments.All(IsSegment);
    }

    /// <summary>
    ///     Matches a segment.
    /// </summary>
    /// <returns>The pattern.</returns>
    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex SegmentPattern();

    /// <summary>
    ///     Matches the name of a data file.
    /// </summary>
    /// <returns>The pattern.</returns>
    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9._-]*$", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex DataFilePattern();

    /// <summary>
    ///     Sorts every listed file into cards, translations, the source file, and data files.
    /// </summary>
    /// <remarks>
    ///     Names that fit no pattern are reported as <c>file-name</c>. Data files need no further handling
    ///     here; examples find them through the listed paths.
    /// </remarks>
    private void ClassifyFiles()
    {
        foreach (var path in _files.Order(StringComparer.Ordinal))
        {
            var parts = path.Split('/');
            var name = parts[^1];
            var folders = parts[..^1];
            if (folders.Any(folder => !IsSegment(folder)))
            {
                ReportFileName(path, "Folder names must be segments: lower-case letters and digits with single dashes.");
            }
            else if (name.EndsWith(".md", StringComparison.Ordinal))
            {
                ClassifyMarkdown(path, folders, name[..^3]);
            }
            else if (!DataFilePattern().IsMatch(name))
            {
                ReportFileName(path, "A data file name must start with a letter or digit and use only letters, digits, '.', '_' and '-'.");
            }
        }
    }

    /// <summary>
    ///     Classifies one Markdown file.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <param name="folders">The folder names of the path.</param>
    /// <param name="stem">The file name without <c>.md</c>.</param>
    private void ClassifyMarkdown(string path, string[] folders, string stem)
    {
        var prefix = folders.Length == 0 ? string.Empty : string.Join('/', folders) + "/";
        var dot = stem.IndexOf('.', StringComparison.Ordinal);
        var baseName = dot < 0 ? stem : stem[..dot];
        var suffix = dot < 0 ? null : stem[(dot + 1)..];

        // The source description lives at the root and has the stem "_source"
        if (folders.Length == 0 && baseName == SourceStem)
        {
            ClassifySource(path, suffix);
        }
        else if (!IsSegment(baseName) || folders.Length + 1 > MaxIdSegments)
        {
            ReportFileName(path, "A card file is named '<segment>.md' with at most 8 segments in its path.");
        }
        else if (suffix is null)
        {
            _cards[prefix + baseName] = path;
        }
        else if (CultureSelector.IsCultureSuffix(suffix))
        {
            AddTranslation(prefix + baseName, suffix, path);
        }
        else
        {
            ReportFileName(path, $"'{suffix}' is not a culture suffix; use '<segment>.<culture>.md' such as 'flow.de.md'.");
        }
    }

    /// <summary>
    ///     Classifies a file whose stem starts with <c>_source</c>.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <param name="suffix">The text after the first dot of the stem, or <see langword="null"/>.</param>
    private void ClassifySource(string path, string? suffix)
    {
        if (suffix is null)
        {
            _sourceFile = path;
        }
        else if (CultureSelector.IsCultureSuffix(suffix))
        {
            AddTranslation(SourceStem, suffix, path);
        }
        else
        {
            ReportFileName(path, $"'{suffix}' is not a culture suffix; use '_source.<culture>.md' such as '_source.de.md'.");
        }
    }

    /// <summary>
    ///     Remembers a translation file under its neutral stem.
    /// </summary>
    /// <param name="stem">The local id of the neutral file, or <c>_source</c>.</param>
    /// <param name="suffix">The culture suffix.</param>
    /// <param name="path">The path of the translation file.</param>
    private void AddTranslation(string stem, string suffix, string path)
    {
        if (!_translations.TryGetValue(stem, out var byCulture))
        {
            byCulture = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _translations[stem] = byCulture;
        }

        if (!byCulture.TryAdd(suffix, path))
        {
            ReportFileName(path, $"The culture '{suffix}' is already used by '{byCulture[suffix]}'; culture names are case-insensitive.");
        }
    }

    /// <summary>
    ///     Reports a file name that fits no pattern.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <param name="reason">What is wrong and what is allowed.</param>
    private void ReportFileName(string path, string reason)
    {
        Report(RuleIds.FileName, path, 0, $"The file name is not valid. {reason}");
    }

    /// <summary>
    ///     Reports every translation file that has no neutral file.
    /// </summary>
    private void ReportOrphans()
    {
        foreach (var (stem, byCulture) in _translations.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (stem == SourceStem ? _sourceFile is not null : _cards.ContainsKey(stem))
            {
                continue;
            }

            foreach (var path in byCulture.Values)
            {
                Report(RuleIds.TranslationOrphan, path, 0, $"The translation has no neutral file '{stem}.md'.");
            }
        }
    }
}
