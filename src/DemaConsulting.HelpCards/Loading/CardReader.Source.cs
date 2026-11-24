using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     Reading <c>_source.md</c>, for the card reader.
/// </summary>
internal sealed partial class CardReader
{
    /// <summary>
    ///     Reads and decodes <c>_source.md</c> when the source has one.
    /// </summary>
    /// <returns>The decoded file, or <see langword="null"/> when absent or unreadable.</returns>
    private FrontMatterDocument? DecodeSourceFile()
    {
        if (_sourceFile is null)
        {
            return null;
        }

        return TryDecode(_sourceFile, out var document) ? document : null;
    }

    /// <summary>
    ///     Decides the name of the source.
    /// </summary>
    /// <remarks>
    ///     A name set with <see cref="HelpSource.WithName"/> takes precedence over the <c>name</c> of
    ///     <c>_source.md</c>. A missing or malformed name is a <c>source-invalid</c> problem.
    /// </remarks>
    /// <param name="document">The decoded source file, or <see langword="null"/>.</param>
    /// <returns>The name; <see cref="UnnamedSource"/> when there is none.</returns>
    private string ResolveName(FrontMatterDocument? document)
    {
        var declared = document is null ? null : Find(document.Entries, "name");
        var name = _source.Name ?? declared?.Scalar?.Text;
        var file = _source.Name is null && declared is not null ? _sourceFile ?? string.Empty : string.Empty;
        var line = file.Length > 0 ? declared!.Line : 0;

        if (name is null)
        {
            // A source file that failed to decode has already been reported; do not pile a second problem on it
            if (_sourceFile is null || document is not null)
            {
                ReportStructure(
                    RuleIds.SourceInvalid,
                    _sourceFile ?? string.Empty,
                    0,
                    "The source has no name; add a 'name' to '_source.md' or call HelpSource.WithName.");
            }

            return UnnamedSource;
        }

        if (!IsSegment(name))
        {
            ReportStructure(
                RuleIds.SourceInvalid,
                file,
                line,
                $"The source name '{name}' is not valid; use lower-case letters and digits with single dashes (1 to 48 characters).");
        }

        return name;
    }

    /// <summary>
    ///     Reads the description of the source from <c>_source.md</c> and its translations.
    /// </summary>
    /// <param name="document">The decoded source file, or <see langword="null"/>.</param>
    /// <returns>The definition of the source with no cards.</returns>
    private SourceDefinition ReadSourceHeader(FrontMatterDocument? document)
    {
        var path = _sourceFile ?? string.Empty;
        var entries = document?.Entries ?? [];
        if (document is not null)
        {
            CheckKeys(entries, path, SourceKeys, "_source.md");
        }

        var language = ReadLanguage(entries, path);
        var schema = ReadSchema(entries, path);
        var header = CreateHeader(document, language, schema);
        if (schema > HighestSchema || document is null)
        {
            _ = ReadTranslations("_source", false);
            return header with { Problems = _problems.ToList() };
        }

        header = header with
        {
            Title = ReadOptionalText(entries, "title", path) ?? header.Title,
            Summary = ReadOptionalText(entries, "summary", path) ?? header.Summary,
            Aliases = ReadSegmentList(entries, "aliases", path, false),
            Children = ReadSegmentList(entries, "children", path, false),
            Extensions = ReadExtensions(entries, path),
            Body = ReadBody(document, path)
        };
        return ApplyTranslation(header, ReadTranslations("_source", false));
    }

    /// <summary>
    ///     Reads the <c>language</c> of the source.
    /// </summary>
    /// <param name="entries">The entries of <c>_source.md</c>.</param>
    /// <param name="path">The path of <c>_source.md</c>.</param>
    /// <returns>The language; <c>en</c> when absent or invalid.</returns>
    private string ReadLanguage(IReadOnlyList<FrontMatterEntry> entries, string path)
    {
        var entry = Find(entries, "language");
        if (entry is null)
        {
            return DefaultLanguage;
        }

        var text = entry.Scalar?.Text ?? string.Empty;
        if (!CultureSelector.IsCultureSuffix(text))
        {
            ReportStructure(
                RuleIds.CultureInvalid,
                path,
                entry.Line,
                $"'{text}' is not a valid culture name; use a name such as 'en' or 'de-DE'.");
            return DefaultLanguage;
        }

        return text;
    }

    /// <summary>
    ///     Reads the <c>schema</c> of the source and rejects one this loader does not support.
    /// </summary>
    /// <param name="entries">The entries of <c>_source.md</c>.</param>
    /// <param name="path">The path of <c>_source.md</c>.</param>
    /// <returns>The schema; 1 when absent or invalid.</returns>
    private int ReadSchema(IReadOnlyList<FrontMatterEntry> entries, string path)
    {
        var entry = Find(entries, "schema");
        if (entry is null)
        {
            return 1;
        }

        var text = entry.Scalar?.Text ?? string.Empty;
        if (!int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var schema) || schema < 1)
        {
            ReportStructure(RuleIds.ValueInvalid, path, entry.Line, "The value of 'schema' must be a positive integer.");
            return 1;
        }

        if (schema > HighestSchema)
        {
            ReportStructure(
                RuleIds.SchemaUnsupported,
                path,
                entry.Line,
                $"The source '{_name}' uses schema {schema}; the highest schema this version supports is {HighestSchema}.");
        }

        return schema;
    }

    /// <summary>
    ///     Applies the selected translation to the source description.
    /// </summary>
    /// <param name="header">The neutral description.</param>
    /// <param name="translations">The valid translations of <c>_source.md</c>.</param>
    /// <returns>The description for the requested culture, with the final problem list.</returns>
    private SourceDefinition ApplyTranslation(
        SourceDefinition header,
        (Dictionary<string, TranslationDefinition> Definitions, int StructureErrors) translations)
    {
        var chosen = CultureSelector.Select(_chain, translations.Definitions.Keys);
        var translation = chosen is null ? null : translations.Definitions[chosen];
        if (translation is not { HasBody: true })
        {
            return header with { Problems = _problems.ToList() };
        }

        return header with
        {
            Title = translation.Title ?? header.Title,
            Summary = translation.Summary ?? header.Summary,
            Aliases = [.. header.Aliases, .. translation.Aliases],
            Culture = translation.Culture,
            Body = translation.Body,
            BodyLine = translation.BodyLine
        };
    }
}
