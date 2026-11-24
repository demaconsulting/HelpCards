using System.Globalization;
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Loading;

/// <summary>
///     Unit tests for <see cref="CardReader"/>: culture variants and translations.
/// </summary>
public partial class CardReaderTests
{
    /// <summary>
    ///     A neutral card with an alias and an example, used by the culture tests.
    /// </summary>
    private const string NeutralCard = """
        ---
        title: Title
        summary: Summary.
        aliases:
          - alias-en
        examples:
          - name: render
            kind: command
            language: shell
            content: run
            description: Render.
        ---
        English text.
        """;

    /// <summary>
    ///     A complete translation of <see cref="NeutralCard"/>.
    /// </summary>
    private const string GermanCard = """
        ---
        title: Translated title
        summary: Translated summary.
        aliases:
          - alias-de
        examples:
          - name: render
            description: Translated render.
        ---
        Translated text.
        """;

    /// <summary>
    ///     Reads files for a requested culture.
    /// </summary>
    /// <param name="culture">The culture name; empty for the invariant culture.</param>
    /// <param name="files">The files of the source.</param>
    /// <returns>The definition of the source.</returns>
    private static SourceDefinition ReadFor(string culture, params (string Path, string Text)[] files)
    {
        return SourceReading.Read(SourceReading.Files(files), culture: CultureInfo.GetCultureInfo(culture));
    }

    /// <summary>
    ///     Proves that the file that supplied the body is recorded: the neutral file, or the selected translation.
    /// </summary>
    [Fact]
    public void CardReader_Read_BodyFile_IsTheFileThatSuppliedTheBody()
    {
        // Arrange
        var files = new[] { ("a.md", NeutralCard), ("a.de.md", GermanCard) };

        // Act
        var neutral = ReadFor(string.Empty, files).Cards.Single();
        var german = ReadFor("de-DE", files).Cards.Single();

        // Assert
        Assert.Equal(("a.md", "a.md"), (neutral.File, neutral.BodyFile));
        Assert.Equal(("a.md", "a.de.md"), (german.File, german.BodyFile));
    }

    /// <summary>
    ///     Proves that the invariant culture reads neutral files only.
    /// </summary>
    [Fact]
    public void CardReader_Read_InvariantCulture_UsesNeutralFiles()
    {
        // Arrange / Act
        var source = ReadFor(string.Empty, ("a.md", NeutralCard), ("a.de.md", GermanCard));

        // Assert
        var card = Assert.Single(source.Cards);
        Assert.Empty(source.Problems);
        Assert.Equal("Title", card.Title);
        Assert.Equal("en", card.Culture);
        Assert.Equal("English text.", Assert.Single(card.Body).Text);
    }

    /// <summary>
    ///     Proves that the thread's UI culture is never used.
    /// </summary>
    [Fact]
    public void CardReader_Read_InvariantCultureWithGermanUiCulture_IgnoresUiCulture()
    {
        // Arrange: a German current UI culture on this thread
        var saved = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de");

            // Act: read for the invariant culture
            var source = ReadFor(string.Empty, ("a.md", NeutralCard), ("a.de.md", GermanCard));

            // Assert: the neutral text is used
            Assert.Equal("Title", Assert.Single(source.Cards).Title);
        }
        finally
        {
            CultureInfo.CurrentUICulture = saved;
        }
    }

    /// <summary>
    ///     Proves that a selected translation replaces title, summary, body, and culture and adds aliases.
    /// </summary>
    [Fact]
    public void CardReader_Read_TranslationSelected_AppliesTranslation()
    {
        // Arrange / Act
        var source = ReadFor("de", ("a.md", NeutralCard), ("a.de.md", GermanCard));

        // Assert
        var card = Assert.Single(source.Cards);
        Assert.Empty(source.Problems);
        Assert.Equal("Translated title", card.Title);
        Assert.Equal("Translated summary.", card.Summary);
        Assert.Equal(["alias-en", "alias-de"], card.Aliases.Select(alias => alias.Value));
        Assert.Equal("de", card.Culture);
        Assert.Equal("Translated text.", Assert.Single(card.Body).Text);
        Assert.Equal(10, card.BodyLine);
    }

    /// <summary>
    ///     Proves that example descriptions are translated while the example content stays neutral.
    /// </summary>
    [Fact]
    public void CardReader_Read_TranslationSelected_TranslatesExampleDescriptions()
    {
        // Arrange / Act
        var source = ReadFor("de", ("a.md", NeutralCard), ("a.de.md", GermanCard));

        // Assert
        var example = Assert.Single(Assert.Single(source.Cards).Examples);
        Assert.Equal("Translated render.", example.Description);
        Assert.Equal("run", example.Content);
    }

    /// <summary>
    ///     Proves that a region culture falls back to the language translation.
    /// </summary>
    [Fact]
    public void CardReader_Read_RegionCultureWithLanguageTranslation_FallsBackToLanguage()
    {
        // Arrange / Act
        var source = ReadFor("de-DE", ("a.md", NeutralCard), ("a.de.md", GermanCard));

        // Assert
        Assert.Equal("de", Assert.Single(source.Cards).Culture);
    }

    /// <summary>
    ///     Proves that the most specific translation wins over the language translation.
    /// </summary>
    [Fact]
    public void CardReader_Read_RegionTranslationAvailable_PrefersRegion()
    {
        // Arrange
        var swiss = GermanCard.Replace("Translated title", "Translated title CH", StringComparison.Ordinal);

        // Act
        var source = ReadFor("de-CH", ("a.md", NeutralCard), ("a.de.md", GermanCard), ("a.de-CH.md", swiss));

        // Assert
        var card = Assert.Single(source.Cards);
        Assert.Equal("Translated title CH", card.Title);
        Assert.Equal("de-CH", card.Culture);
    }

    /// <summary>
    ///     Proves that a culture without a translation uses the neutral file.
    /// </summary>
    [Fact]
    public void CardReader_Read_CultureWithoutTranslation_UsesNeutralFile()
    {
        // Arrange / Act
        var source = ReadFor("fr", ("a.md", NeutralCard), ("a.de.md", GermanCard));

        // Assert
        var card = Assert.Single(source.Cards);
        Assert.Equal("Title", card.Title);
        Assert.Equal("en", card.Culture);
    }

    /// <summary>
    ///     Proves that a missing title or summary of a translation falls back individually to the neutral value.
    /// </summary>
    [Fact]
    public void CardReader_Read_TranslationWithoutTitle_FallsBackToNeutralTitle()
    {
        // Arrange: a translation that has a summary and a body only
        var source = ReadFor("de", ("a.md", NeutralCard), ("a.de.md", "---\nsummary: Only translated summary.\n---\nText."));

        // Act
        var card = Assert.Single(source.Cards);

        // Assert
        Assert.Equal("Title", card.Title);
        Assert.Equal("Only translated summary.", card.Summary);
        Assert.Equal("de", card.Culture);
    }

    /// <summary>
    ///     Proves that a translation without a body is not used at all.
    /// </summary>
    [Fact]
    public void CardReader_Read_TranslationWithoutBody_UsesNeutralFileWhole()
    {
        // Arrange: a translation with a title but an empty body
        var source = ReadFor("de", ("a.md", NeutralCard), ("a.de.md", "---\ntitle: Translated title\n---\n\n"));

        // Act
        var card = Assert.Single(source.Cards);

        // Assert: the neutral title and culture
        Assert.Equal("Title", card.Title);
        Assert.Equal("en", card.Culture);
        Assert.Empty(source.Problems);
    }

    /// <summary>
    ///     Proves that the culture is chosen for each file on its own.
    /// </summary>
    [Fact]
    public void CardReader_Read_TranslationForOneCardOnly_OtherCardsStayNeutral()
    {
        // Arrange
        var source = ReadFor("de", ("a.md", NeutralCard), ("a.de.md", GermanCard), ("b.md", NeutralCard));

        // Act
        var cultures = source.Cards.ToDictionary(card => card.LocalId, card => card.Culture);

        // Assert
        Assert.Equal("de", cultures["a"]);
        Assert.Equal("en", cultures["b"]);
    }

    /// <summary>
    ///     Proves that the language of the source file is the culture of neutral bodies.
    /// </summary>
    [Fact]
    public void CardReader_Read_SourceLanguage_IsCultureOfNeutralFiles()
    {
        // Arrange
        var source = ReadFor(
            string.Empty,
            ("_source.md", "---\nname: test\nlanguage: de\n---\n"),
            ("a.md", NeutralCard));

        // Act / Assert
        Assert.Equal("de", Assert.Single(source.Cards).Culture);
        Assert.Equal("de", source.Language);
    }

    /// <summary>
    ///     Proves that a translation key that translations may not have is reported even for the neutral culture.
    /// </summary>
    [Fact]
    public void CardReader_Read_TranslationWithStructuralKey_ReportsTranslationKey()
    {
        // Arrange: tags and extends are not allowed in a translation
        var source = ReadFor(
            string.Empty,
            ("a.md", NeutralCard),
            ("a.de.md", "---\ntitle: T\ntags:\n  - x\nextends: y\nx-note: z\n---\nText."));

        // Act
        var lines = source.Problems.Select(problem => (problem.RuleId, problem.File, problem.Line)).ToList();

        // Assert: tags (3), extends (5), and the extension (6) are all structural for a translation
        Assert.Equal(
            [
                (RuleIds.TranslationKey, "a.de.md", 3),
                (RuleIds.TranslationKey, "a.de.md", 5),
                (RuleIds.TranslationKey, "a.de.md", 6)
            ],
            lines);
        Assert.Single(source.Cards);
        Assert.Empty(source.FailedIds);
    }

    /// <summary>
    ///     Proves that a syntax error of a translation is reported even when it is not requested.
    /// </summary>
    [Fact]
    public void CardReader_Read_TranslationSyntaxError_IsReportedForOtherCulture()
    {
        // Arrange / Act
        var source = ReadFor(string.Empty, ("a.md", NeutralCard), ("a.fr.md", "---\ntitle: [x]\n---\n"));

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.Syntax, "a.fr.md", 2);
        Assert.Single(source.Cards);
    }

    /// <summary>
    ///     Proves that example translations may only have a name and a description.
    /// </summary>
    [Fact]
    public void CardReader_Read_TranslatedExampleWithOtherKey_ReportsTranslationKey()
    {
        // Arrange
        var source = ReadFor(
            string.Empty,
            ("a.md", NeutralCard),
            ("a.de.md", "---\nexamples:\n  - name: render\n    description: D.\n    kind: command\n---\nText."));

        // Act / Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.TranslationKey, "a.de.md", 5);
    }

    /// <summary>
    ///     Proves that aliases of a translation are normalized to NFC and validated.
    /// </summary>
    [Fact]
    public void CardReader_Read_TranslationAliases_AreNormalizedAndValidated()
    {
        // Arrange: a decomposed é, and an upper-case alias that is not allowed
        var source = ReadFor(
            "de",
            ("a.md", NeutralCard),
            ("a.de.md", "---\naliases:\n  - cafe\u0301\n  - Bad\n---\nText."));

        // Act
        var problem = Assert.Single(source.Problems);
        var card = Assert.Single(source.Cards);

        // Assert
        AssertProblem(problem, RuleIds.ValueInvalid, "a.de.md", 4);
        Assert.Contains("caf\u00E9", card.Aliases.Select(alias => alias.Value));
    }

    /// <summary>
    ///     Proves that a translation without a neutral file is reported as an orphan.
    /// </summary>
    [Fact]
    public void CardReader_Read_TranslationWithoutNeutralFile_ReportsTranslationOrphan()
    {
        // Arrange / Act
        var source = ReadFor(string.Empty, ("x.de.md", GermanCard));

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.TranslationOrphan, "x.de.md", 0);
    }

    /// <summary>
    ///     Proves that a source file translation without a neutral source file is reported as an orphan.
    /// </summary>
    [Fact]
    public void CardReader_Read_SourceTranslationWithoutSourceFile_ReportsTranslationOrphan()
    {
        // Arrange / Act
        var source = ReadFor(string.Empty, ("_source.de.md", "---\ntitle: T\n---\nText."));

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.TranslationOrphan, "_source.de.md", 0);
    }

    /// <summary>
    ///     Proves that an overlay has its body translated by the translation file of the overlay.
    /// </summary>
    [Fact]
    public void CardReader_Read_OverlayTranslation_TranslatesOverlayBody()
    {
        // Arrange
        var source = ReadFor(
            "de",
            ("ov.md", "---\nextends: a\ntitle: Over\n---\nEnglish overlay."),
            ("ov.de.md", "---\ntitle: Uber\n---\nTranslated overlay."));

        // Act
        var overlay = Assert.Single(source.Overlays);

        // Assert
        Assert.Empty(source.Problems);
        Assert.Equal("Uber", overlay.Title);
        Assert.Equal("de", overlay.Culture);
        Assert.Equal("Translated overlay.", Assert.Single(overlay.Body).Text);
    }

    /// <summary>
    ///     Proves that an overlay translation may not add aliases, because overlays have none.
    /// </summary>
    [Fact]
    public void CardReader_Read_OverlayTranslationWithAliases_ReportsTranslationKey()
    {
        // Arrange / Act
        var source = ReadFor(
            string.Empty,
            ("ov.md", "---\nextends: a\n---\nText."),
            ("ov.de.md", "---\naliases:\n  - x\n---\nText."));

        // Assert
        AssertProblem(Assert.Single(source.Problems), RuleIds.TranslationKey, "ov.de.md", 2);
    }

    /// <summary>
    ///     Proves that the source description is translated like a card.
    /// </summary>
    [Fact]
    public void CardReader_Read_SourceTranslation_AppliesToSourceDescription()
    {
        // Arrange
        var source = ReadFor(
            "de",
            ("_source.md", "---\nname: test\ntitle: Library\nsummary: A library.\n---\nWelcome."),
            ("_source.de.md", "---\ntitle: Translated library\n---\nTranslated welcome."));

        // Act / Assert
        Assert.Empty(source.Problems);
        Assert.Equal("Translated library", source.Title);
        Assert.Equal("A library.", source.Summary);
        Assert.Equal("de", source.Culture);
        Assert.Equal("Translated welcome.", Assert.Single(source.Body).Text);
    }
}
