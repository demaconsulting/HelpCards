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
    ///     The highest exit code an example may expect, the portable range of a process exit code.
    /// </summary>
    private const int MaximumExitCode = 255;

    /// <summary>
    ///     The key of schema 2 that names the file an example is saved as.
    /// </summary>
    private const string SaveAsKey = "save-as";

    /// <summary>
    ///     The keys an example may have in schema 1.
    /// </summary>
    private static readonly string[] ExampleKeysSchema1 = ["name", "kind", "language", "description", "file", "content", "exit", "expect", "tokens"];

    /// <summary>
    ///     The keys an example may have in schema 2.
    /// </summary>
    private static readonly string[] ExampleKeysSchema2 =
        ["name", "kind", "language", "description", "file", "content", "save-as", "exit", "expect", "tokens"];

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
        CheckExampleKeys(map, file);
        var before = _structureErrors;

        var name = ReadExampleName(map, file, item.Line);
        var kind = ReadExampleKind(map, file, item.Line);
        var language = ReadExampleLanguage(map, file, item.Line);
        var description = ReadExampleText(map, "description", file, item.Line);
        var fileEntry = Find(map, "file");
        var saveAsEntry = _schema >= 2 ? Find(map, SaveAsKey) : null;
        var dataFile = fileEntry is null ? null : ReadExampleFile(fileEntry, file);
        var saveAs = saveAsEntry is null ? null : ReadSaveAs(saveAsEntry, file);
        CheckFileKeys(map, kind, file, item.Line);
        var fileName = _schema >= 2 ? saveAs ?? LastSegment(dataFile) : dataFile;

        var substitute = ReadExampleTokens(map, file);
        var content = ReadExampleContent(map, kind, dataFile, file, item.Line, substitute);
        var exitCode = ReadExampleExit(map, kind, file);
        var expect = ReadExampleExpect(map, kind, file);
        if (_structureErrors > before || name is null || kind is null || language is null)
        {
            return null;
        }

        return new ExampleDefinition(
            name,
            kind,
            language,
            kind == CommandKind ? null : fileName,
            description,
            content,
            exitCode,
            expect,
            file,
            item.Line);
    }

    /// <summary>
    ///     Checks the keys of an example against the key set of the schema of the source.
    /// </summary>
    /// <remarks>
    ///     In schema 1 the key <c>save-as</c> does not exist; it gets its own message, which names the schema it needs and
    ///     what <c>file</c> means in schema 1, instead of the generic list of allowed keys.
    /// </remarks>
    /// <param name="map">The entries of the example.</param>
    /// <param name="file">The path of the file.</param>
    private void CheckExampleKeys(IReadOnlyList<FrontMatterEntry> map, string file)
    {
        if (_schema >= 2)
        {
            CheckKeys(map, file, ExampleKeysSchema2, "example", false);
            return;
        }

        var saveAs = Find(map, SaveAsKey);
        CheckKeys(map.Where(entry => entry != saveAs).ToList(), file, ExampleKeysSchema1, "example", false);
        if (saveAs is not null)
        {
            ReportStructure(
                RuleIds.KeyUnknown,
                file,
                saveAs.Line,
                $"The key '{SaveAsKey}' needs schema 2: add 'schema: 2' to '_source.md'. "
                + "In schema 1 the 'file' of an example with 'content' is the name to save it as.");
        }
    }

    /// <summary>
    ///     Checks how <c>file</c>, <c>save-as</c>, and <c>content</c> go together for the kind of an example.
    /// </summary>
    /// <remarks>
    ///     A command has neither <c>file</c> nor <c>save-as</c> in any schema. In schema 1 a <c>source</c> or <c>snippet</c>
    ///     needs a <c>file</c>, which is both the data file and the name to save it as. In schema 2 it needs inline
    ///     <c>content</c> with <c>save-as</c>, or a <c>file</c> that names a data file next to the card; <c>file</c> cannot be
    ///     combined with <c>content</c>.
    /// </remarks>
    /// <param name="map">The entries of the example.</param>
    /// <param name="kind">The kind of the example, or <see langword="null"/> when invalid.</param>
    /// <param name="file">The path of the file.</param>
    /// <param name="line">The line of the example.</param>
    private void CheckFileKeys(IReadOnlyList<FrontMatterEntry> map, string? kind, string file, int line)
    {
        var fileEntry = Find(map, "file");
        var saveAsEntry = _schema >= 2 ? Find(map, SaveAsKey) : null;
        var hasContent = Find(map, "content") is not null;
        if (kind == CommandKind)
        {
            if (fileEntry is not null)
            {
                ReportStructure(RuleIds.ValueInvalid, file, fileEntry.Line, "A command example has no 'file'; its 'content' is the command line.");
            }

            if (saveAsEntry is not null)
            {
                ReportStructure(RuleIds.ValueInvalid, file, saveAsEntry.Line, "A command example has no 'save-as'; its 'content' is the command line.");
            }

            return;
        }

        if (kind is not (SourceKind or SnippetKind))
        {
            return;
        }

        if (_schema < 2)
        {
            if (fileEntry is null)
            {
                ReportStructure(RuleIds.KeyMissing, file, line, $"A {kind} example needs a 'file'.");
            }
        }
        else if (hasContent && fileEntry is not null)
        {
            ReportStructure(
                RuleIds.ValueInvalid,
                file,
                fileEntry.Line,
                "In schema 2 'file' names a companion data file and cannot be combined with 'content'; remove 'file', "
                + "or put the text in the file. To name the file the content is saved as, use 'save-as'.");
        }
        else if (hasContent && saveAsEntry is null)
        {
            ReportStructure(RuleIds.KeyMissing, file, line, $"A {kind} example with inline 'content' needs 'save-as', the name to save it as.");
        }
        else if (!hasContent && fileEntry is null)
        {
            ReportStructure(RuleIds.KeyMissing, file, line, $"A {kind} example needs 'content' (inline text) or 'file' (a data file next to the card).");
        }
    }

    /// <summary>
    ///     Reads and validates the <c>save-as</c> of an example.
    /// </summary>
    /// <remarks>
    ///     The name has the grammar of an example file: a <c>/</c>-separated relative path whose parts are data file names,
    ///     so it cannot contain <c>..</c>, start with <c>/</c>, or name a drive, and it does not end in <c>.md</c>.
    /// </remarks>
    /// <param name="entry">The <c>save-as</c> entry.</param>
    /// <param name="file">The path of the file that contains the example.</param>
    /// <returns>The name as written, or <see langword="null"/> when invalid.</returns>
    private string? ReadSaveAs(FrontMatterEntry entry, string file)
    {
        var text = entry.Scalar?.Text ?? string.Empty;
        if (!IsExampleFileName(text))
        {
            ReportStructure(
                RuleIds.ValueInvalid,
                file,
                entry.Line,
                $"'{text}' is not a valid save-as name; use a relative '/'-separated path of data file names without '..' and not ending in '.md'.");
            return null;
        }

        return text;
    }

    /// <summary>
    ///     Gets the part of a path after its last <c>/</c>.
    /// </summary>
    /// <param name="path">The path, or <see langword="null"/>.</param>
    /// <returns>The last segment, or <see langword="null"/> when there is no path.</returns>
    private static string? LastSegment(string? path)
    {
        return path is null ? null : path[(path.LastIndexOf('/') + 1)..];
    }

    /// <summary>
    ///     Reads the <c>exit</c> of an example: the exit code a command is expected to end with.
    /// </summary>
    /// <param name="map">The entries of the example.</param>
    /// <param name="kind">The kind of the example, or <see langword="null"/> when invalid.</param>
    /// <param name="file">The path of the file.</param>
    /// <returns>The exit code from 0 to 255; 0 when the key is absent or invalid.</returns>
    private int ReadExampleExit(IReadOnlyList<FrontMatterEntry> map, string? kind, string file)
    {
        var entry = Find(map, "exit");
        if (entry is null)
        {
            return 0;
        }

        if (kind is SourceKind or SnippetKind)
        {
            ReportStructure(RuleIds.ValueInvalid, file, entry.Line, "Only a command example has 'exit'.");
            return 0;
        }

        var text = entry.Scalar?.Text ?? string.Empty;
        if (!int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var code) || code > MaximumExitCode)
        {
            ReportStructure(RuleIds.ValueInvalid, file, entry.Line, "The 'exit' of an example must be an integer from 0 to 255.");
            return 0;
        }

        return code;
    }

    /// <summary>
    ///     Reads the <c>tokens</c> of an example: whether tokens and escapes are processed in its content.
    /// </summary>
    /// <remarks>
    ///     The key is additive and needs no schema change. It applies only to the example <c>content</c> and its
    ///     companion data file; the <c>description</c> and the <c>expect</c> fragments always have their tokens
    ///     replaced.
    /// </remarks>
    /// <param name="map">The entries of the example.</param>
    /// <param name="file">The path of the file.</param>
    /// <returns>
    ///     <see langword="false"/> when the key is <c>false</c>; <see langword="true"/> when it is <c>true</c>,
    ///     absent or invalid.
    /// </returns>
    private bool ReadExampleTokens(IReadOnlyList<FrontMatterEntry> map, string file)
    {
        var entry = Find(map, "tokens");
        if (entry is null)
        {
            return true;
        }

        var text = entry.Scalar?.Text;
        if (text is "true" or "false")
        {
            return text == "true";
        }

        ReportStructure(RuleIds.ValueInvalid, file, entry.Line, "The value of 'tokens' must be 'true' or 'false'.");
        return true;
    }

    /// <summary>
    ///     Reads the <c>expect</c> of an example: the fragments the output of a command must contain.
    /// </summary>
    /// <remarks>
    ///     The value is one line of text, or a literal block in which every non-blank line is one fragment. Line endings
    ///     become LF and tokens are replaced.
    /// </remarks>
    /// <param name="map">The entries of the example.</param>
    /// <param name="kind">The kind of the example, or <see langword="null"/> when invalid.</param>
    /// <param name="file">The path of the file.</param>
    /// <returns>The fragments; empty when the key is absent or invalid.</returns>
    private List<string> ReadExampleExpect(IReadOnlyList<FrontMatterEntry> map, string? kind, string file)
    {
        var fragments = new List<string>();
        var entry = Find(map, "expect");
        if (entry is null)
        {
            return fragments;
        }

        if (kind is SourceKind or SnippetKind)
        {
            ReportStructure(RuleIds.ValueInvalid, file, entry.Line, "Only a command example has 'expect'.");
            return fragments;
        }

        var raw = entry.Scalar?.Text.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n') ?? string.Empty;
        if (raw.Length == 0 || string.IsNullOrWhiteSpace(raw.Split('\n')[0]))
        {
            ReportStructure(RuleIds.ValueInvalid, file, entry.Line, "The 'expect' of an example must not be empty and must not start with a blank line.");
            return fragments;
        }

        var text = _tokens.Substitute(raw, file, entry.Scalar!.Line, _problems);
        fragments.AddRange(text.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)));
        return fragments;
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
        if (!IsExampleFileName(text))
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
    ///     Tells whether a text is a valid example file name: a relative <c>/</c>-separated path of data file names that
    ///     does not end in <c>.md</c>.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns><see langword="true"/> when the text is valid.</returns>
    private static bool IsExampleFileName(string text)
    {
        return text.Length > 0
            && !text.EndsWith(".md", StringComparison.Ordinal)
            && text.Split('/').All(part => DataFilePattern().IsMatch(part));
    }

    /// <summary>
    ///     Reads the content of an example from its inline text or from its file.
    /// </summary>
    /// <param name="map">The entries of the example.</param>
    /// <param name="kind">The kind of the example, or <see langword="null"/> when invalid.</param>
    /// <param name="fileName">The valid data file named by <c>file</c>, or <see langword="null"/>.</param>
    /// <param name="file">The path of the file that contains the example.</param>
    /// <param name="line">The line of the example.</param>
    /// <param name="substitute">
    ///     <see langword="false"/> to take the content as written, without replacing tokens or processing escapes.
    /// </param>
    /// <returns>The normalized content; empty when it cannot be determined.</returns>
    private string ReadExampleContent(
        IReadOnlyList<FrontMatterEntry> map,
        string? kind,
        string? fileName,
        string file,
        int line,
        bool substitute)
    {
        var entry = Find(map, "content");
        if (entry is not null)
        {
            return entry.Scalar is null
                ? ReportInvalidContent(file, entry.Line, "The example 'content' must be text, not a list.")
                : NormalizeContent(entry.Scalar.Text, file, entry.Scalar.Line, kind, substitute);
        }

        if (kind == CommandKind)
        {
            ReportStructure(RuleIds.KeyMissing, file, line, "A command example needs 'content' with the command line.");
            return string.Empty;
        }

        return fileName is null ? string.Empty : ReadExampleFileContent(fileName, file, kind, line, substitute);
    }

    /// <summary>
    ///     Reads the content of an example from the file next to its card.
    /// </summary>
    /// <param name="fileName">The example file, relative to the folder of the card file.</param>
    /// <param name="file">The path of the card or overlay file.</param>
    /// <param name="kind">The kind of the example.</param>
    /// <param name="line">The line of the example.</param>
    /// <param name="substitute">
    ///     <see langword="false"/> to take the content as written, without replacing tokens or processing escapes.
    /// </param>
    /// <returns>The normalized content; empty when the file is missing or unreadable.</returns>
    private string ReadExampleFileContent(string fileName, string file, string? kind, int line, bool substitute)
    {
        var slash = file.LastIndexOf('/');
        var path = (slash < 0 ? string.Empty : file[..(slash + 1)]) + fileName;
        if (!_files.Contains(path))
        {
            Report(RuleIds.ExampleFileMissing, file, line, $"The example file '{path}' does not exist in the source.");
            return string.Empty;
        }

        return TryReadExampleText(path, out var text) ? NormalizeContent(text, path, 1, kind, substitute) : string.Empty;
    }

    /// <summary>
    ///     Reads the text of an example data file once, however many examples name it.
    /// </summary>
    /// <param name="path">The path of the data file inside the source.</param>
    /// <param name="text">The raw text, or an empty string on failure.</param>
    /// <returns><see langword="true"/> when the file was read, now or earlier.</returns>
    private bool TryReadExampleText(string path, out string text)
    {
        if (_exampleTexts.TryGetValue(path, out var cached))
        {
            text = cached ?? string.Empty;
            return cached is not null;
        }

        var read = TryReadText(path, out text);
        _exampleTexts[path] = read ? text : null;
        return read;
    }

    /// <summary>
    ///     Normalizes example content and checks it against its kind.
    /// </summary>
    /// <remarks>
    ///     Line endings become LF, trailing line feeds are removed, and tokens are replaced and escapes
    ///     processed unless <paramref name="substitute"/> is <see langword="false"/>. The content must
    ///     not be empty and must not start with a blank line; a command must be exactly one line.
    /// </remarks>
    /// <param name="raw">The raw content.</param>
    /// <param name="file">The path of the file the content came from.</param>
    /// <param name="firstLine">The one-based line of the first content line in that file.</param>
    /// <param name="kind">The kind of the example.</param>
    /// <param name="substitute">
    ///     <see langword="false"/> to keep the content as written, without replacing tokens or processing escapes.
    /// </param>
    /// <returns>The normalized content.</returns>
    private string NormalizeContent(string raw, string file, int firstLine, string? kind, bool substitute)
    {
        var text = raw.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n');
        if (substitute)
        {
            text = _tokens.Substitute(text, file, firstLine, _problems);
        }
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
