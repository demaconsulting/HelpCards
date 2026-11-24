using System.Text.RegularExpressions;
using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     Reading and validating examples, for the card reader.
/// </summary>
internal sealed partial class CardReader
{
    /// <summary>
    ///     The kind of an example that is one command line.
    /// </summary>
    private const string CommandKind = "command";

    /// <summary>
    ///     The kind of an example that is a complete file.
    /// </summary>
    private const string SourceKind = "source";

    /// <summary>
    ///     The kind of an example that is a fragment of a file.
    /// </summary>
    private const string SnippetKind = "snippet";

    /// <summary>
    ///     The keys an example may have.
    /// </summary>
    private static readonly string[] ExampleKeys = ["name", "kind", "language", "description", "file", "content"];

    /// <summary>
    ///     The keys an example of a translation may have.
    /// </summary>
    private static readonly string[] TranslationExampleKeys = ["name", "description"];

    /// <summary>
    ///     Matches the language of an example.
    /// </summary>
    /// <returns>The pattern.</returns>
    [GeneratedRegex(@"^[a-z0-9+#.-]+$", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex LanguagePattern();

    /// <summary>
    ///     Reads the <c>examples</c> of a card or overlay file.
    /// </summary>
    /// <param name="entries">The entries of the file.</param>
    /// <param name="file">The path of the file.</param>
    /// <returns>The examples whose structure is valid.</returns>
    private List<ExampleDefinition> ReadExamples(IReadOnlyList<FrontMatterEntry> entries, string file)
    {
        var result = new List<ExampleDefinition>();
        var entry = Find(entries, "examples");
        foreach (var item in ReadExampleItems(entry, file))
        {
            var example = ReadExample(item, file);
            if (example is not null)
            {
                result.Add(example);
            }
        }

        return result;
    }

    /// <summary>
    ///     Gets the map items of an <c>examples</c> entry.
    /// </summary>
    /// <param name="entry">The entry, or <see langword="null"/> when the key is absent.</param>
    /// <param name="file">The path of the file.</param>
    /// <returns>The items that are maps.</returns>
    private List<FrontMatterItem> ReadExampleItems(FrontMatterEntry? entry, string file)
    {
        var items = new List<FrontMatterItem>();
        if (entry is null || entry.Scalar is { Text.Length: 0 })
        {
            return items;
        }

        if (entry.Items is null)
        {
            ReportStructure(RuleIds.ValueInvalid, file, entry.Line, "The value of 'examples' must be a list of maps.");
            return items;
        }

        foreach (var item in entry.Items)
        {
            if (item.Map is null)
            {
                ReportStructure(RuleIds.ValueInvalid, file, item.Line, "Every example must be a map such as 'name: x'.");
            }
            else
            {
                items.Add(item);
            }
        }

        return items;
    }

    /// <summary>
    ///     Reads one example of a card or overlay.
    /// </summary>
    /// <param name="item">The map item.</param>
    /// <param name="file">The path of the file.</param>
    /// <returns>The example, or <see langword="null"/> when its structure is invalid.</returns>
    private ExampleDefinition? ReadExample(FrontMatterItem item, string file)
    {
        var map = item.Map!;
        CheckKeys(map, file, ExampleKeys, "example", false);
        var before = _structureErrors;

        var name = ReadExampleName(map, file, item.Line);
        var kind = ReadExampleKind(map, file, item.Line);
        var language = ReadExampleLanguage(map, file, item.Line);
        var description = ReadExampleText(map, "description", file, item.Line);
        var fileEntry = Find(map, "file");
        var fileName = fileEntry is null ? null : ReadExampleFile(fileEntry, file);
        if (kind == CommandKind && fileEntry is not null)
        {
            ReportStructure(RuleIds.ValueInvalid, file, fileEntry.Line, "A command example has no 'file'; its 'content' is the command line.");
        }
        else if (kind is SourceKind or SnippetKind && fileEntry is null)
        {
            ReportStructure(RuleIds.KeyMissing, file, item.Line, $"A {kind} example needs a 'file'.");
        }

        var content = ReadExampleContent(map, kind, fileName, file, item.Line);
        if (_structureErrors > before || name is null || kind is null || language is null)
        {
            return null;
        }

        return new ExampleDefinition(name, kind, language, kind == CommandKind ? null : fileName, description, content, file, item.Line);
    }

    /// <summary>
    ///     Reads the <c>name</c> of an example.
    /// </summary>
    /// <param name="map">The entries of the example.</param>
    /// <param name="file">The path of the file.</param>
    /// <param name="line">The line of the example.</param>
    /// <returns>The name, or <see langword="null"/> when missing or invalid.</returns>
    private string? ReadExampleName(IReadOnlyList<FrontMatterEntry> map, string file, int line)
    {
        var entry = Find(map, "name");
        if (entry is null)
        {
            ReportStructure(RuleIds.KeyMissing, file, line, "The example has no 'name'.");
            return null;
        }

        var text = entry.Scalar?.Text;
        if (!IsSegment(text))
        {
            ReportStructure(RuleIds.ValueInvalid, file, entry.Line, "The example 'name' must be lower-case letters and digits with single dashes.");
            return null;
        }

        return text;
    }

    /// <summary>
    ///     Reads the <c>kind</c> of an example.
    /// </summary>
    /// <param name="map">The entries of the example.</param>
    /// <param name="file">The path of the file.</param>
    /// <param name="line">The line of the example.</param>
    /// <returns>The kind, or <see langword="null"/> when missing or invalid.</returns>
    private string? ReadExampleKind(IReadOnlyList<FrontMatterEntry> map, string file, int line)
    {
        var entry = Find(map, "kind");
        if (entry is null)
        {
            ReportStructure(RuleIds.KeyMissing, file, line, "The example has no 'kind'.");
            return null;
        }

        var text = entry.Scalar?.Text;
        if (text is not (CommandKind or SourceKind or SnippetKind))
        {
            ReportStructure(RuleIds.ValueInvalid, file, entry.Line, "The example 'kind' must be 'command', 'source', or 'snippet'.");
            return null;
        }

        return text;
    }

    /// <summary>
    ///     Reads the <c>language</c> of an example.
    /// </summary>
    /// <param name="map">The entries of the example.</param>
    /// <param name="file">The path of the file.</param>
    /// <param name="line">The line of the example.</param>
    /// <returns>The language, or <see langword="null"/> when missing or invalid.</returns>
    private string? ReadExampleLanguage(IReadOnlyList<FrontMatterEntry> map, string file, int line)
    {
        var entry = Find(map, "language");
        if (entry is null)
        {
            ReportStructure(RuleIds.KeyMissing, file, line, "The example has no 'language'.");
            return null;
        }

        var text = entry.Scalar?.Text ?? string.Empty;
        if (!LanguagePattern().IsMatch(text))
        {
            ReportStructure(RuleIds.ValueInvalid, file, entry.Line, "The example 'language' must be one lower-case word such as 'shell' or 'c#'.");
            return null;
        }

        return text;
    }

    /// <summary>
    ///     Reads a required one-line text of an example with tokens replaced.
    /// </summary>
    /// <param name="map">The entries of the example.</param>
    /// <param name="key">The key.</param>
    /// <param name="file">The path of the file.</param>
    /// <param name="line">The line of the example.</param>
    /// <returns>The text, or an empty string when missing or invalid.</returns>
    private string ReadExampleText(IReadOnlyList<FrontMatterEntry> map, string key, string file, int line)
    {
        var entry = Find(map, key);
        if (entry is null)
        {
            ReportStructure(RuleIds.KeyMissing, file, line, $"The example has no '{key}'.");
            return string.Empty;
        }

        return ReadText(entry, file) ?? string.Empty;
    }

    /// <summary>
    ///     Reads and validates the <c>file</c> of an example.
    /// </summary>
    /// <remarks>
    ///     The file is a <c>/</c>-separated relative path whose parts are data file names, so it cannot
    ///     contain <c>..</c>, start with <c>/</c>, or name a drive.
    /// </remarks>
    /// <param name="entry">The <c>file</c> entry.</param>
    /// <param name="file">The path of the file that contains the example.</param>
    /// <returns>The file as written, or <see langword="null"/> when invalid.</returns>
    private string? ReadExampleFile(FrontMatterEntry entry, string file)
    {
        var text = entry.Scalar?.Text ?? string.Empty;
        var parts = text.Split('/');
        if (text.Length == 0 || parts.Any(part => !DataFilePattern().IsMatch(part)) || text.EndsWith(".md", StringComparison.Ordinal))
        {
            ReportStructure(
                RuleIds.ValueInvalid,
                file,
                entry.Line,
                $"'{text}' is not a valid example file; use a relative '/'-separated path of data file names without '..' and not ending in '.md'.");
            return null;
        }

        return text;
    }

    /// <summary>
    ///     Reads the content of an example from its inline text or from its file.
    /// </summary>
    /// <param name="map">The entries of the example.</param>
    /// <param name="kind">The kind of the example, or <see langword="null"/> when invalid.</param>
    /// <param name="fileName">The valid example file, or <see langword="null"/>.</param>
    /// <param name="file">The path of the file that contains the example.</param>
    /// <param name="line">The line of the example.</param>
    /// <returns>The normalized content; empty when it cannot be determined.</returns>
    private string ReadExampleContent(
        IReadOnlyList<FrontMatterEntry> map,
        string? kind,
        string? fileName,
        string file,
        int line)
    {
        var entry = Find(map, "content");
        if (entry is not null)
        {
            return entry.Scalar is null
                ? ReportInvalidContent(file, entry.Line, "The example 'content' must be text, not a list.")
                : NormalizeContent(entry.Scalar.Text, file, entry.Scalar.Line, kind);
        }

        if (kind == CommandKind)
        {
            ReportStructure(RuleIds.KeyMissing, file, line, "A command example needs 'content' with the command line.");
            return string.Empty;
        }

        return fileName is null ? string.Empty : ReadExampleFileContent(fileName, file, kind, line);
    }

    /// <summary>
    ///     Reads the content of an example from the file next to its card.
    /// </summary>
    /// <param name="fileName">The example file, relative to the folder of the card file.</param>
    /// <param name="file">The path of the card or overlay file.</param>
    /// <param name="kind">The kind of the example.</param>
    /// <param name="line">The line of the example.</param>
    /// <returns>The normalized content; empty when the file is missing or unreadable.</returns>
    private string ReadExampleFileContent(string fileName, string file, string? kind, int line)
    {
        var slash = file.LastIndexOf('/');
        var path = (slash < 0 ? string.Empty : file[..(slash + 1)]) + fileName;
        if (!_files.Contains(path))
        {
            Report(RuleIds.ExampleFileMissing, file, line, $"The example file '{path}' does not exist in the source.");
            return string.Empty;
        }

        return TryReadText(path, out var text) ? NormalizeContent(text, path, 1, kind) : string.Empty;
    }

    /// <summary>
    ///     Normalizes example content and checks it against its kind.
    /// </summary>
    /// <remarks>
    ///     Line endings become LF, trailing line feeds are removed, and tokens are replaced. The content must
    ///     not be empty and must not start with a blank line; a command must be exactly one line.
    /// </remarks>
    /// <param name="raw">The raw content.</param>
    /// <param name="file">The path of the file the content came from.</param>
    /// <param name="firstLine">The one-based line of the first content line in that file.</param>
    /// <param name="kind">The kind of the example.</param>
    /// <returns>The normalized content.</returns>
    private string NormalizeContent(string raw, string file, int firstLine, string? kind)
    {
        var text = raw.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n');
        text = _tokens.Substitute(text, file, firstLine, _problems);
        if (text.Length == 0)
        {
            return ReportInvalidContent(file, firstLine, "The example content must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(text.Split('\n')[0]))
        {
            return ReportInvalidContent(file, firstLine, "The example content must not start with a blank line.");
        }

        if (kind == CommandKind && text.Contains('\n', StringComparison.Ordinal))
        {
            return ReportInvalidContent(file, firstLine, "A command example must be exactly one line.");
        }

        return text;
    }

    /// <summary>
    ///     Reports invalid example content.
    /// </summary>
    /// <param name="file">The path of the file.</param>
    /// <param name="line">The one-based line.</param>
    /// <param name="message">The message.</param>
    /// <returns>An empty string, the content of an invalid example.</returns>
    private string ReportInvalidContent(string file, int line, string message)
    {
        Report(RuleIds.ExampleInvalid, file, line, message);
        return string.Empty;
    }

    /// <summary>
    ///     Reads the example descriptions of a translation file.
    /// </summary>
    /// <param name="entries">The entries of the translation file.</param>
    /// <param name="file">The path of the translation file.</param>
    /// <returns>The translated descriptions by example name.</returns>
    private Dictionary<string, string> ReadExampleDescriptions(IReadOnlyList<FrontMatterEntry> entries, string file)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in ReadExampleItems(Find(entries, "examples"), file))
        {
            CheckKeys(item.Map!, file, TranslationExampleKeys, "translated example", false, RuleIds.TranslationKey);
            var name = ReadExampleName(item.Map!, file, item.Line);
            var description = ReadExampleText(item.Map!, "description", file, item.Line);
            if (name is not null && description.Length > 0)
            {
                result[name] = description;
            }
        }

        return result;
    }
}
