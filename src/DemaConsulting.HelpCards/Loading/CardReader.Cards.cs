using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     Reading card and overlay files and their translations, for the card reader.
/// </summary>
internal sealed partial class CardReader
{
    /// <summary>
    ///     Reads one neutral card or overlay file and the translations of it.
    /// </summary>
    /// <remarks>
    ///     A file with <c>extends</c> is an overlay; every other file is a card. The neutral file is read
    ///     first. If its structure is unusable its id is recorded as failed and it contributes nothing; the
    ///     translations are still validated so that their defects are reported too.
    /// </remarks>
    /// <param name="localId">The local id of the file.</param>
    /// <param name="path">The path of the neutral file.</param>
    /// <param name="language">The language of the neutral files of the source.</param>
    /// <param name="cards">Receives the card when the file is a card.</param>
    /// <param name="overlays">Receives the overlay when the file is an overlay.</param>
    /// <param name="failed">Receives the id when the file is unusable.</param>
    private void ReadCardFile(
        string localId,
        string path,
        string language,
        List<CardDefinition> cards,
        List<OverlayDefinition> overlays,
        List<string> failed)
    {
        if (!TryDecode(path, out var document))
        {
            failed.Add(localId);
            _ = ReadTranslations(localId, false);
            return;
        }

        var isOverlay = Find(document!.Entries, "extends") is not null;
        var before = _structureErrors;
        var card = isOverlay ? null : ReadCard(localId, path, document, language);
        var overlay = isOverlay ? ReadOverlay(localId, path, document, language) : null;
        var translations = ReadTranslations(localId, isOverlay);
        if (_structureErrors > before + translations.StructureErrors)
        {
            failed.Add(localId);
            return;
        }

        var chosen = CultureSelector.Select(_chain, translations.Definitions.Keys);
        var translation = chosen is null ? null : translations.Definitions[chosen];
        if (card is not null)
        {
            cards.Add(ApplyTranslation(card, translation));
        }
        else
        {
            overlays.Add(ApplyTranslation(overlay!, translation));
        }
    }

    /// <summary>
    ///     Reads the structure of a card file.
    /// </summary>
    /// <param name="localId">The local id of the card.</param>
    /// <param name="path">The path of the file.</param>
    /// <param name="document">The decoded file.</param>
    /// <param name="language">The language of the neutral files.</param>
    /// <returns>The card; its values are empty where the file is invalid.</returns>
    private CardDefinition ReadCard(string localId, string path, FrontMatterDocument document, string language)
    {
        var entries = document.Entries;
        CheckKeys(entries, path, CardKeys, "card");
        return new CardDefinition(
            _name,
            localId,
            path,
            ReadRequiredText(entries, "title", path),
            ReadRequiredText(entries, "summary", path),
            ReadSegmentList(entries, "aliases", path, false),
            ReadSegmentList(entries, "tags", path, true),
            ReadOrder(entries, path),
            ReadSegmentList(entries, "children", path, false),
            ReadReferences(entries, "see-also", path),
            ReadExamples(entries, path),
            ReadExtensions(entries, path),
            language,
            ReadBody(document, path),
            document.BodyLine,
            path);
    }

    /// <summary>
    ///     Reads the structure of an overlay file.
    /// </summary>
    /// <param name="localId">The local id of the overlay.</param>
    /// <param name="path">The path of the file.</param>
    /// <param name="document">The decoded file.</param>
    /// <param name="language">The language of the neutral files.</param>
    /// <returns>The overlay; its values are empty where the file is invalid.</returns>
    private OverlayDefinition ReadOverlay(string localId, string path, FrontMatterDocument document, string language)
    {
        var entries = document.Entries;
        CheckKeys(entries, path, OverlayKeys, "overlay");
        var extendsEntry = Find(entries, "extends")!;
        var extends = ParseReference(extendsEntry.Scalar?.Text ?? string.Empty, path, extendsEntry.Line, "extends")
            ?? new ReferenceDefinition(null, localId, path, extendsEntry.Line);
        return new OverlayDefinition(
            _name,
            localId,
            path,
            extends,
            ReadOptional(entries, path),
            ReadOptionalText(entries, "title", path),
            ReadOptionalText(entries, "summary", path),
            ReadReferences(entries, "see-also", path),
            ReadExamples(entries, path),
            ReadExtensions(entries, path),
            language,
            ReadBody(document, path),
            document.BodyLine,
            path);
    }

    /// <summary>
    ///     Reads the <c>optional</c> flag of an overlay.
    /// </summary>
    /// <param name="entries">The entries of the file.</param>
    /// <param name="path">The path of the file.</param>
    /// <returns>The flag; <see langword="false"/> when absent or invalid.</returns>
    private bool ReadOptional(IReadOnlyList<FrontMatterEntry> entries, string path)
    {
        var entry = Find(entries, "optional");
        if (entry is null)
        {
            return false;
        }

        var text = entry.Scalar?.Text;
        if (text is not ("true" or "false"))
        {
            ReportStructure(RuleIds.ValueInvalid, path, entry.Line, "The value of 'optional' must be 'true' or 'false'.");
        }

        return text == "true";
    }

    /// <summary>
    ///     Reads and validates every translation file of a neutral file.
    /// </summary>
    /// <param name="stem">The local id of the neutral file, or <c>_source</c>.</param>
    /// <param name="isOverlay">Whether the neutral file is an overlay, which has no aliases to add to.</param>
    /// <returns>The valid translations by culture suffix and the number of structure errors found in them.</returns>
    private (Dictionary<string, TranslationDefinition> Definitions, int StructureErrors) ReadTranslations(
        string stem,
        bool isOverlay)
    {
        var definitions = new Dictionary<string, TranslationDefinition>(StringComparer.OrdinalIgnoreCase);
        var before = _structureErrors;
        if (_translations.TryGetValue(stem, out var files))
        {
            foreach (var (suffix, path) in files)
            {
                var definition = ReadTranslation(suffix, path, isOverlay);
                if (definition is not null)
                {
                    definitions[suffix] = definition;
                }
            }
        }

        return (definitions, _structureErrors - before);
    }

    /// <summary>
    ///     Reads one translation file.
    /// </summary>
    /// <param name="suffix">The culture suffix of the file name.</param>
    /// <param name="path">The path of the file.</param>
    /// <param name="isOverlay">Whether the translation belongs to an overlay.</param>
    /// <returns>The translation, or <see langword="null"/> when its front matter cannot be read.</returns>
    private TranslationDefinition? ReadTranslation(string suffix, string path, bool isOverlay)
    {
        if (!TryDecode(path, out var document))
        {
            return null;
        }

        var entries = document!.Entries;
        var allowed = isOverlay ? TranslationKeys.Where(key => key != "aliases").ToArray() : TranslationKeys;
        CheckKeys(entries, path, allowed, "translation", false, RuleIds.TranslationKey);
        return new TranslationDefinition(
            suffix,
            path,
            ReadOptionalText(entries, "title", path),
            ReadOptionalText(entries, "summary", path),
            ReadTranslationAliases(entries, path),
            ReadExampleDescriptions(entries, path),
            !string.IsNullOrWhiteSpace(document.Body),
            ReadBody(document, path),
            document.BodyLine);
    }

    /// <summary>
    ///     Applies the selected translation to a card.
    /// </summary>
    /// <remarks>
    ///     Title, summary, and example descriptions fall back individually to the neutral values; aliases are
    ///     added; the body and its culture come from the translation. A translation without a body is not
    ///     used at all.
    /// </remarks>
    /// <param name="card">The neutral card.</param>
    /// <param name="translation">The selected translation, or <see langword="null"/>.</param>
    /// <returns>The card as it appears for the requested culture.</returns>
    private static CardDefinition ApplyTranslation(CardDefinition card, TranslationDefinition? translation)
    {
        if (translation is not { HasBody: true })
        {
            return card;
        }

        return card with
        {
            Title = translation.Title ?? card.Title,
            Summary = translation.Summary ?? card.Summary,
            Aliases = [.. card.Aliases, .. translation.Aliases],
            Examples = TranslateExamples(card.Examples, translation),
            Culture = translation.Culture,
            Body = translation.Body,
            BodyLine = translation.BodyLine,
            BodyFile = translation.File
        };
    }

    /// <summary>
    ///     Applies the selected translation to an overlay.
    /// </summary>
    /// <param name="overlay">The neutral overlay.</param>
    /// <param name="translation">The selected translation, or <see langword="null"/>.</param>
    /// <returns>The overlay as it appears for the requested culture.</returns>
    private static OverlayDefinition ApplyTranslation(OverlayDefinition overlay, TranslationDefinition? translation)
    {
        if (translation is not { HasBody: true })
        {
            return overlay;
        }

        return overlay with
        {
            Title = translation.Title ?? overlay.Title,
            Summary = translation.Summary ?? overlay.Summary,
            Examples = TranslateExamples(overlay.Examples, translation),
            Culture = translation.Culture,
            Body = translation.Body,
            BodyLine = translation.BodyLine,
            BodyFile = translation.File
        };
    }

    /// <summary>
    ///     Replaces example descriptions with their translations where one exists.
    /// </summary>
    /// <param name="examples">The neutral examples.</param>
    /// <param name="translation">The selected translation.</param>
    /// <returns>The examples with translated descriptions.</returns>
    private static List<ExampleDefinition> TranslateExamples(
        IReadOnlyList<ExampleDefinition> examples,
        TranslationDefinition translation)
    {
        return examples
            .Select(example => translation.ExampleDescriptions.TryGetValue(example.Name, out var description)
                ? example with { Description = description }
                : example)
            .ToList();
    }
}
