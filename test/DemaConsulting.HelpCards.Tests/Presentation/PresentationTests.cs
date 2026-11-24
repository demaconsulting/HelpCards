using System.Globalization;
using System.Text.Json;
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Presentation;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Presentation;

/// <summary>
///     Subsystem tests for Presentation: the options, link styles, text, Markdown, and JSON renderers working together
///     through the <c>Render</c> members of the catalog.
/// </summary>
/// <remarks>
///     The tests use catalogs that the Composition subsystem creates from the embedded <c>simple</c> fixture and from
///     in-memory sources, and then only the public face of the Presentation subsystem: <see cref="HelpRenderOptions"/>
///     and the <c>Render</c> members of <see cref="HelpCatalog"/>. Nothing is mocked.
/// </remarks>
public class PresentationTests
{
    /// <summary>
    ///     Proves that the text format renders the card and the index of the simple fixture as the golden files.
    /// </summary>
    [Fact]
    public void Presentation_Text_SimpleFixture_RendersGoldenCardAndIndex()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act
        var card = catalog.Render(catalog.Find("guide")!);
        var index = catalog.RenderIndex();

        // Assert
        GoldenFiles.AssertEqual("guide.text.expected.txt", card);
        GoldenFiles.AssertEqual("index.text.expected.txt", index);
    }

    /// <summary>
    ///     Proves that the Markdown format has the layout of the text format with bold labels and no wrapping.
    /// </summary>
    [Fact]
    public void Presentation_Markdown_SimpleFixture_RendersGoldenAndDoesNotWrap()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var markdown = new HelpRenderOptions { Format = HelpFormat.Markdown, Width = 20 };

        // Act
        var card = catalog.Render(catalog.Find("guide")!, new HelpRenderOptions { Format = HelpFormat.Markdown });
        var narrow = catalog.Render(catalog.Find("guide")!, markdown);

        // Assert
        GoldenFiles.AssertEqual("guide.markdown.expected.txt", card);
        Assert.Equal(card, narrow);
    }

    /// <summary>
    ///     Proves that the JSON format renders the card, the index, and the examples as the golden files.
    /// </summary>
    [Fact]
    public void Presentation_Json_SimpleFixture_RendersGoldenDocuments()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var json = new HelpRenderOptions { Format = HelpFormat.Json };

        // Act
        var card = catalog.Render(catalog.Find("guide")!, json);
        var index = catalog.RenderIndex(json);
        var examples = catalog.RenderExamples(
            catalog.Find("guide")!,
            new HelpRenderOptions { Format = HelpFormat.Json, LinkStyle = HelpLinkStyle.Command });

        // Assert
        GoldenFiles.AssertEqual("guide.expected.json", card);
        GoldenFiles.AssertEqual("index.expected.json", index);
        GoldenFiles.AssertEqual("examples.guide.expected.json", examples);
    }

    /// <summary>
    ///     Proves that the three link styles and a callback write the body of a card, the lists, and the JSON body.
    /// </summary>
    [Fact]
    public void Presentation_LinkStyles_GuideCard_WriteLinksAndLists()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var guide = catalog.Find("guide")!;

        // Act
        var command = catalog.Render(guide, new HelpRenderOptions { LinkStyle = HelpLinkStyle.Command });
        var markdown = catalog.Render(guide, new HelpRenderOptions { Format = HelpFormat.Markdown, LinkStyle = HelpLinkStyle.Markdown });
        var jsonCommand = catalog.Render(guide, new HelpRenderOptions { Format = HelpFormat.Json, LinkStyle = HelpLinkStyle.Command });
        var template = catalog.Render(guide, new HelpRenderOptions { LinkStyle = HelpLinkStyle.Command, CommandTemplate = "{tool} --man {path}" });

        // Assert
        GoldenFiles.AssertEqual("guide.command.expected.txt", command);
        GoldenFiles.AssertEqual("guide.markdown-links.expected.txt", markdown);
        Assert.Contains("\"command\":\"simple help guide setup\"", jsonCommand, StringComparison.Ordinal);
        Assert.Contains("- simple --man guide setup\n", template, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the examples document prints the own examples in full and lists the cards below by name only.
    /// </summary>
    [Fact]
    public void Presentation_Examples_GuideCard_PrintsOwnExamplesAndListsDescendants()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var guide = catalog.Find("guide")!;

        // Act
        var text = catalog.RenderExamples(guide, new HelpRenderOptions { LinkStyle = HelpLinkStyle.Command });
        var withContent = catalog.Render(guide, new HelpRenderOptions { IncludeExampleContent = true, LinkStyle = HelpLinkStyle.Command });
        var one = catalog.RenderExample(guide, "fragment", new HelpRenderOptions { Format = HelpFormat.Markdown });

        // Assert
        GoldenFiles.AssertEqual("examples.guide.expected.txt", text);
        GoldenFiles.AssertEqual("guide.content.expected.txt", withContent);
        Assert.Equal("limit: 42\nmode: fast", one);
        Assert.DoesNotContain("simple --version", text, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the index, the card list, and the search results render one entry per card and honor the depth.
    /// </summary>
    [Fact]
    public void Presentation_Lists_IndexCardListAndSearch_RenderEntries()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        IReadOnlyList<HelpSearchResult> results = [new("guide/setup", "Setup", "Prepare the tool before the first run.", "simple", 90, ["id"])];

        // Act
        var roots = catalog.RenderIndex(null, 0);
        var list = catalog.RenderCardList(catalog.Cards.Where(card => card.Tags.Contains("reference")).ToList());
        var search = catalog.RenderSearch("setup", results);
        var none = catalog.RenderSearch("zzz", []);

        // Assert
        Assert.Equal("guide      How to set up and use the tool.\nreference  Facts about limits and options.", roots);
        Assert.Equal(
            "reference         Facts about limits and options.\nreference/limits  The numbers the tool enforces.",
            list);
        Assert.Equal("guide/setup  Prepare the tool before the first run.", search);
        Assert.Equal("No help cards match 'zzz'.", none);
    }

    /// <summary>
    ///     Proves that generated lines wrap at the width and that a width of zero switches wrapping off.
    /// </summary>
    [Fact]
    public void Presentation_Wrapping_Width_WrapsGeneratedLinesOnly()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var guide = catalog.Find("guide")!;

        // Act
        var narrow = catalog.Render(guide, new HelpRenderOptions { Width = 30 });
        var unwrapped = catalog.Render(guide, new HelpRenderOptions { Width = 0 });

        // Assert: the body keeps its authored lines whatever the width
        GoldenFiles.AssertEqual("guide.narrow.expected.txt", narrow);
        Assert.Contains("- fragment [snippet] fragment.yaml: A fragment of a settings file.", unwrapped, StringComparison.Ordinal);
        Assert.Contains("Run it with the example `run` or see the example `settings` of the card `guide` for the settings file.", narrow, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the options have the documented defaults and reject invalid values.
    /// </summary>
    [Fact]
    public void Presentation_Options_DefaultsAndValidation_FollowTheContract()
    {
        // Arrange
        var options = new HelpRenderOptions();

        // Act / Assert
        Assert.Equal((HelpFormat.Text, HelpLinkStyle.Plain, 78, true, false), (options.Format, options.LinkStyle, options.Width, options.IncludeTags, options.IncludeExampleContent));
        Assert.ThrowsAny<ArgumentException>(() => new HelpRenderOptions { Width = 19 });
        Assert.ThrowsAny<ArgumentException>(() => new HelpRenderOptions { Width = 501 });
        Assert.ThrowsAny<ArgumentException>(() => new HelpRenderOptions { Format = (HelpFormat)9 });
        Assert.ThrowsAny<ArgumentException>(() => new HelpRenderOptions { CommandTemplate = " " });
    }

    /// <summary>
    ///     Proves that every JSON document is one line of ASCII with the contract version and no null, and that an
    ///     envelope callback replaces the top-level object.
    /// </summary>
    [Fact]
    public void Presentation_JsonContract_AllKinds_AreOneAsciiLineWithoutNull()
    {
        // Arrange
        var catalog = RenderSources.Card();
        var requirements = catalog.Find("requirements")!;
        var options = new HelpRenderOptions { Format = HelpFormat.Json, LinkStyle = HelpLinkStyle.Command };
        var enveloped = new HelpRenderOptions
        {
            Format = HelpFormat.Json,
            JsonEnvelope = (kind, writeFields) =>
            {
                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject();
                    writer.WriteString("type", kind);
                    writeFields(writer);
                    writer.WriteEndObject();
                }

                return System.Text.Encoding.UTF8.GetString(stream.ToArray());
            },
        };

        // Act
        var card = catalog.Render(requirements, options);
        var custom = catalog.Render(requirements, enveloped);

        // Assert: the contract fixture for this card, then the replaced envelope
        Assert.StartsWith("{\"helpSchemaVersion\":1,\"ok\":true,\"kind\":\"card\",\"id\":\"requirements\"", card, StringComparison.Ordinal);
        Assert.Contains("\"body\":\"Write one file per section.\\n\\nSee `reqstream help tests`.\"", card, StringComparison.Ordinal);
        Assert.StartsWith("{\"type\":\"card\",\"id\":\"requirements\"", custom, StringComparison.Ordinal);
        Assert.DoesNotContain("null", card + custom, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that rendering is deterministic: two renderings are equal whatever the culture of the machine, with
    ///     LF line endings and no trailing newline.
    /// </summary>
    [Fact]
    public void Presentation_Determinism_CultureAndRepeat_GiveIdenticalOutput()
    {
        // Arrange
        var original = CultureInfo.CurrentCulture;
        var catalog = CatalogBuilding.Simple();
        string Describe() => string.Join("\n", catalog.Cards.Select(card => catalog.Render(card, new HelpRenderOptions { LinkStyle = HelpLinkStyle.Command })));

        try
        {
            // Act
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var first = Describe();
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var second = Describe();

            // Assert
            Assert.Equal(first, second);
            Assert.DoesNotContain('\r', first);
            Assert.All(catalog.Cards, card => Assert.False(catalog.Render(card).EndsWith('\n')));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
