using System.Globalization;
using System.Text.RegularExpressions;
using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     The key matrix and the value grammars of the front matter, for the card reader.
/// </summary>
internal sealed partial class CardReader
{
    /// <summary>
    ///     The keys a card file may have.
    /// </summary>
    private static readonly string[] CardKeys =
        ["title", "summary", "aliases", "tags", "order", "children", "see-also", "examples"];

    /// <summary>
    ///     The keys an overlay file may have.
    /// </summary>
    private static readonly string[] OverlayKeys = ["extends", "optional", "title", "summary", "see-also", "examples"];

    /// <summary>
    ///     The keys <c>_source.md</c> may have.
    /// </summary>
    private static readonly string[] SourceKeys =
        ["title", "summary", "aliases", "children", "schema", "name", "language"];

    /// <summary>
    ///     The keys a translation file may have.
    /// </summary>
    private static readonly string[] TranslationKeys = ["title", "summary", "aliases", "examples"];

    /// <summary>
    ///     Matches an order value.
    /// </summary>
    /// <returns>The pattern.</returns>
    [GeneratedRegex(@"^-?\d{1,7}$", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex OrderPattern();

    /// <summary>
    ///     Matches a translation alias: lower-case letters, other letters, and digits, with single dashes.
    /// </summary>
    /// <returns>The pattern.</returns>
    [GeneratedRegex(@"^[\p{Ll}\p{Lo}\p{Nd}]+(-[\p{Ll}\p{Lo}\p{Nd}]+)*$", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex TranslationAliasPattern();

    /// <summary>
    ///     Finds an entry by key.
    /// </summary>
    /// <param name="entries">The entries.</param>
    /// <param name="key">The key.</param>
    /// <returns>The entry, or <see langword="null"/>.</returns>
    private static FrontMatterEntry? Find(IReadOnlyList<FrontMatterEntry> entries, string key)
    {
        return entries.FirstOrDefault(entry => entry.Key == key);
    }

    /// <summary>
    ///     Reports every key that the kind of file does not allow.
    /// </summary>
    /// <param name="entries">The entries of the file.</param>
    /// <param name="file">The path of the file.</param>
    /// <param name="allowed">The allowed keys.</param>
    /// <param name="kind">The kind of file, for the message.</param>
    /// <param name="allowExtensions">Whether keys that start with <c>x-</c> are allowed.</param>
    /// <param name="ruleId">The rule to report, <c>key-unknown</c> unless a translation is checked.</param>
    private void CheckKeys(
        IReadOnlyList<FrontMatterEntry> entries,
        string file,
        string[] allowed,
        string kind,
        bool allowExtensions = true,
        string ruleId = RuleIds.KeyUnknown)
    {
        foreach (var entry in entries)
        {
            var isExtension = allowExtensions && entry.Key.StartsWith("x-", StringComparison.Ordinal) && entry.Key.Length > 2;
            if (!isExtension && !allowed.Contains(entry.Key))
            {
                var list = string.Join(", ", allowExtensions ? allowed.Append("x-*") : allowed);
                ReportStructure(
                    ruleId,
                    file,
                    entry.Line,
                    $"The key '{entry.Key}' is not allowed in a {kind} file; allowed keys: {list}.");
            }
        }
    }

    /// <summary>
    ///     Reads a required single-line text with tokens replaced.
    /// </summary>
    /// <param name="entries">The entries of the file.</param>
    /// <param name="key">The key.</param>
    /// <param name="file">The path of the file.</param>
    /// <returns>The text, or an empty string when it is missing or invalid.</returns>
    private string ReadRequiredText(IReadOnlyList<FrontMatterEntry> entries, string key, string file)
    {
        var entry = Find(entries, key);
        if (entry is null)
        {
            ReportStructure(RuleIds.KeyMissing, file, 1, $"The required key '{key}' is missing.");
            return string.Empty;
        }

        return ReadText(entry, file) ?? string.Empty;
    }

    /// <summary>
    ///     Reads an optional single-line text with tokens replaced.
    /// </summary>
    /// <param name="entries">The entries of the file.</param>
    /// <param name="key">The key.</param>
    /// <param name="file">The path of the file.</param>
    /// <returns>The text, or <see langword="null"/> when the key is absent or invalid.</returns>
    private string? ReadOptionalText(IReadOnlyList<FrontMatterEntry> entries, string key, string file)
    {
        var entry = Find(entries, key);
        return entry is null ? null : ReadText(entry, file);
    }

    /// <summary>
    ///     Reads the single-line text of an entry and replaces its tokens.
    /// </summary>
    /// <param name="entry">The entry.</param>
    /// <param name="file">The path of the file.</param>
    /// <returns>The text, or <see langword="null"/> when the value is a list or empty.</returns>
    private string? ReadText(FrontMatterEntry entry, string file)
    {
        if (entry.Scalar is null || entry.Scalar.Text.Length == 0)
        {
            ReportStructure(RuleIds.ValueInvalid, file, entry.Line, $"The value of '{entry.Key}' must be one line of text.");
            return null;
        }

        return _tokens.Substitute(entry.Scalar.Text, file, entry.Scalar.Line, _problems);
    }

    /// <summary>
    ///     Reads a list of scalars; a key with nothing after it is an empty list.
    /// </summary>
    /// <param name="entry">The entry, or <see langword="null"/> when the key is absent.</param>
    /// <param name="file">The path of the file.</param>
    /// <returns>The values with their lines; empty when the key is absent or invalid.</returns>
    private List<LocatedValue> ReadScalarList(FrontMatterEntry? entry, string file)
    {
        var values = new List<LocatedValue>();
        if (entry is null || (entry.Scalar is { Text.Length: 0 }))
        {
            return values;
        }

        if (entry.Items is null)
        {
            ReportStructure(RuleIds.ValueInvalid, file, entry.Line, $"The value of '{entry.Key}' must be a list ('- item' lines).");
            return values;
        }

        foreach (var item in entry.Items)
        {
            if (item.Scalar is null || item.Scalar.Text.Length == 0)
            {
                ReportStructure(RuleIds.ValueInvalid, file, item.Line, $"Every item of '{entry.Key}' must be a non-empty value.");
            }
            else
            {
                values.Add(new LocatedValue(item.Scalar.Text, file, item.Line));
            }
        }

        return values;
    }

    /// <summary>
    ///     Reads a list of segments such as tags, children, or neutral aliases.
    /// </summary>
    /// <param name="entries">The entries of the file.</param>
    /// <param name="key">The key.</param>
    /// <param name="file">The path of the file.</param>
    /// <param name="unique">Whether a repeated value is an error.</param>
    /// <returns>The valid values.</returns>
    private List<LocatedValue> ReadSegmentList(IReadOnlyList<FrontMatterEntry> entries, string key, string file, bool unique)
    {
        var result = new List<LocatedValue>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in ReadScalarList(Find(entries, key), file))
        {
            if (!IsSegment(value.Value))
            {
                ReportStructure(
                    RuleIds.ValueInvalid,
                    file,
                    value.Line,
                    $"'{value.Value}' is not a valid name in '{key}'; use lower-case letters and digits with single dashes (1 to 48 characters).");
            }
            else if (unique && !seen.Add(value.Value))
            {
                ReportStructure(RuleIds.ValueInvalid, file, value.Line, $"'{value.Value}' is listed twice in '{key}'.");
            }
            else
            {
                result.Add(value);
            }
        }

        return result;
    }

    /// <summary>
    ///     Reads the aliases of a translation: letters of any script, normalized to NFC.
    /// </summary>
    /// <param name="entries">The entries of the file.</param>
    /// <param name="file">The path of the file.</param>
    /// <returns>The valid aliases.</returns>
    private List<LocatedValue> ReadTranslationAliases(IReadOnlyList<FrontMatterEntry> entries, string file)
    {
        var result = new List<LocatedValue>();
        foreach (var value in ReadScalarList(Find(entries, "aliases"), file))
        {
            var normalized = value.Value.Normalize(System.Text.NormalizationForm.FormC);
            if (normalized.Length > MaxSegmentLength || !TranslationAliasPattern().IsMatch(normalized))
            {
                ReportStructure(
                    RuleIds.ValueInvalid,
                    file,
                    value.Line,
                    $"'{value.Value}' is not a valid alias; use lower-case letters and digits with single dashes (1 to 48 characters).");
            }
            else
            {
                result.Add(value with { Value = normalized });
            }
        }

        return result;
    }

    /// <summary>
    ///     Reads the <c>order</c> value.
    /// </summary>
    /// <param name="entries">The entries of the file.</param>
    /// <param name="file">The path of the file.</param>
    /// <returns>The order, or <see langword="null"/> when absent or invalid.</returns>
    private int? ReadOrder(IReadOnlyList<FrontMatterEntry> entries, string file)
    {
        var entry = Find(entries, "order");
        if (entry is null)
        {
            return null;
        }

        var text = entry.Scalar?.Text ?? string.Empty;
        if (!OrderPattern().IsMatch(text))
        {
            ReportStructure(RuleIds.ValueInvalid, file, entry.Line, "The value of 'order' must be an integer of at most 7 digits.");
            return null;
        }

        return int.Parse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Reads one reference <c>[source:]localId</c> from a scalar.
    /// </summary>
    /// <param name="text">The reference text.</param>
    /// <param name="file">The path of the file.</param>
    /// <param name="line">The one-based line.</param>
    /// <param name="key">The key, for the message.</param>
    /// <returns>The reference, or <see langword="null"/> when invalid.</returns>
    private ReferenceDefinition? ParseReference(string text, string file, int line, string key)
    {
        var colon = text.IndexOf(':', StringComparison.Ordinal);
        var source = colon < 0 ? null : text[..colon];
        var id = colon < 0 ? text : text[(colon + 1)..];
        if ((source is not null && !IsSegment(source)) || !IsLocalId(id))
        {
            ReportStructure(
                RuleIds.ValueInvalid,
                file,
                line,
                $"'{text}' is not a valid reference in '{key}'; write 'card/id' or 'source:card/id' with lower-case names.");
            return null;
        }

        return new ReferenceDefinition(source, id, file, line);
    }

    /// <summary>
    ///     Reads a list of references such as <c>see-also</c>.
    /// </summary>
    /// <param name="entries">The entries of the file.</param>
    /// <param name="key">The key.</param>
    /// <param name="file">The path of the file.</param>
    /// <returns>The valid references.</returns>
    private List<ReferenceDefinition> ReadReferences(IReadOnlyList<FrontMatterEntry> entries, string key, string file)
    {
        return ReadScalarList(Find(entries, key), file)
            .Select(value => ParseReference(value.Value, file, value.Line, key))
            .OfType<ReferenceDefinition>()
            .ToList();
    }

    /// <summary>
    ///     Reads the <c>x-</c> keys of a file.
    /// </summary>
    /// <param name="entries">The entries of the file.</param>
    /// <param name="file">The path of the file.</param>
    /// <returns>The extension values by key.</returns>
    private Dictionary<string, string> ReadExtensions(IReadOnlyList<FrontMatterEntry> entries, string file)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in entries.Where(e => e.Key.StartsWith("x-", StringComparison.Ordinal) && e.Key.Length > 2))
        {
            if (entry.Scalar is null)
            {
                ReportStructure(RuleIds.ValueInvalid, file, entry.Line, $"The value of '{entry.Key}' must be one line of text.");
            }
            else
            {
                result[entry.Key] = entry.Scalar.Text;
            }
        }

        return result;
    }

    /// <summary>
    ///     Replaces the tokens of a body and splits it into text and link parts.
    /// </summary>
    /// <param name="document">The decoded document.</param>
    /// <param name="file">The path of the file the body came from.</param>
    /// <returns>The body parts.</returns>
    private IReadOnlyList<BodyPart> ReadBody(FrontMatterDocument document, string file)
    {
        var text = _tokens.Substitute(document.Body, file, document.BodyLine, _problems);
        return BodyParser.Parse(text, document.BodyLine, _name, file, _problems);
    }
}
