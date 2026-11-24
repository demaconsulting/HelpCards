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
///     in-memory sources, and then the public face of the Presentation subsystem: <see cref="HelpRenderOptions"/>
///     and the <c>Render</c> members of <see cref="HelpCatalog"/>. The error document, which only the command-line
///     helper writes, is reached through the internal writer. Nothing is mocked.
/// </remarks>
public class PresentationTests
{
    /// <summary>
    ///     A letter that is not ASCII (u with a diaeresis), built at run time so that the source stays plain ASCII.
    /// </summary>
    private static readonly string Umlaut = char.ConvertFromUtf32(0xFC);

    /// <summary>
    ///     A dash that is not ASCII (an en dash).
    /// </summary>
    private static readonly string Dash = char.ConvertFromUtf32(0x2013);

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
    ///     Proves that a link to an example of the card that holds the link is the full command in the command style, a
    ///     link that names the card in the Markdown style, and consistent in the JSON body and its links.
    /// </summary>
    [Fact]
    public void Presentation_Links_LocalExample_IsTheFullCommandInCommandStyle()
    {
        // Arrange: the guide card links to its own example with [[#run]]
        var catalog = CatalogBuilding.Simple();
        var guide = catalog.Find("guide")!;

        // Act
        var command = catalog.FormatBody(guide, new HelpRenderOptions { LinkStyle = HelpLinkStyle.Command });
        var markdown = catalog.FormatBody(guide, new HelpRenderOptions { LinkStyle = HelpLinkStyle.Markdown });
        var plain = catalog.FormatBody(guide);
        var json = catalog.Render(guide, new HelpRenderOptions { Format = HelpFormat.Json, LinkStyle = HelpLinkStyle.Command });

        // Assert: the plain style stays prose, the others can be run or followed from anywhere
        Assert.Contains("Run it with `simple help guide --example run` or", command, StringComparison.Ordinal);
        Assert.Contains("Run it with [run](guide#run) or", markdown, StringComparison.Ordinal);
        Assert.Contains("Run it with the example `run` or", plain, StringComparison.Ordinal);
        Assert.Contains("Run it with `simple help guide --example run` or", json, StringComparison.Ordinal);
        Assert.Contains("{\"exampleName\":\"run\",\"exampleCardId\":\"guide\"", json, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the examples document of a card shows the expected results of its commands after each example, and
    ///     the JSON document writes them when set, through the options and renderers working together.
    /// </summary>
    [Fact]
    public void Presentation_ExpectedResults_RunnableFixture_ShowResultAfterExample()
    {
        // Arrange
        var catalog = CatalogBuilding.Runnable();
        var check = catalog.Find("check")!;

        // Act
        var text = catalog.RenderExamples(check);
        var alone = catalog.RenderExample(check, "missing");
        var json = catalog.RenderExamples(check, new HelpRenderOptions { Format = HelpFormat.Json });

        // Assert: text shows the result, one example alone is exactly its content, JSON appends the keys
        Assert.Contains("```\nExpected exit code: 2\nExpected output contains: Cannot open absent.yaml\n", text, StringComparison.Ordinal);
        Assert.Equal("demo check absent.yaml", alone);
        Assert.Contains("\"exitCode\":2,\"expect\":[\"Cannot open absent.yaml\"]", json, StringComparison.Ordinal);
        Assert.DoesNotContain("null", json, StringComparison.Ordinal);
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

        // A catalog whose texts hold non-ASCII characters, with a link, an example, and a child
        var unicode = CatalogBuilding.FromFiles(
            (
                "a.md",
                SourceReading.Card(
                    title: $"{Umlaut} over view",
                    summary: $"A {Umlaut} summary {Dash} here.",
                    extraFrontMatter: $"examples:\n  - name: run\n    kind: command\n    language: shell\n    content: tool run\n    description: Run it {Dash} now.\n",
                    body: $"See [[b]] and {Dash} {Umlaut}.")),
            ("b.md", SourceReading.Card(title: "Second card", summary: "Second.")));
        var jsonOptions = new HelpRenderOptions { Format = HelpFormat.Json };
        var a = unicode.Find("a")!;

        // Act
        var card = catalog.Render(requirements, options);
        var custom = catalog.Render(requirements, enveloped);
        var documents = new Dictionary<string, string>
        {
            ["card"] = unicode.Render(a, jsonOptions),
            ["index"] = unicode.RenderIndex(jsonOptions),
            ["examples"] = unicode.RenderExamples(a, jsonOptions),
            ["example"] = unicode.RenderExample(a, "run", jsonOptions),
            ["search"] = unicode.RenderSearch("over", unicode.Search("over"), jsonOptions),
            ["cards"] = unicode.RenderCardList(unicode.Cards, jsonOptions),
            ["error"] = JsonDocumentWriter.WriteError(jsonOptions, JsonDocumentWriter.NotFoundCode, $"Unknown {Umlaut}.", null, ["a"]),
            ["custom"] = unicode.Render(a, new HelpRenderOptions { Format = HelpFormat.Json, JsonEnvelope = enveloped.JsonEnvelope }),
        };

        // Assert: the contract fixture for this card, then the replaced envelope
        Assert.StartsWith("{\"helpSchemaVersion\":1,\"ok\":true,\"kind\":\"card\",\"id\":\"requirements\"", card, StringComparison.Ordinal);
        Assert.Contains("\"body\":\"Write one file per section.\\n\\nSee `reqstream help tests`.\"", card, StringComparison.Ordinal);
        Assert.StartsWith("{\"type\":\"card\",\"id\":\"requirements\"", custom, StringComparison.Ordinal);
        Assert.DoesNotContain("null", card + custom, StringComparison.Ordinal);

        // Every kind is one line of ASCII that parses, has the contract version, and holds no JSON null
        foreach (var (kind, document) in documents)
        {
            Assert.DoesNotContain('\n', document);
            Assert.DoesNotContain('\r', document);
            Assert.All(document, character => Assert.True(character < 0x80, $"{kind} holds a non-ASCII character."));
            using var parsed = JsonDocument.Parse(document);
            Assert.False(ContainsNull(parsed.RootElement), $"{kind} holds a JSON null.");
            if (kind != "custom")
            {
                Assert.Equal(1, parsed.RootElement.GetProperty("helpSchemaVersion").GetInt32());
                Assert.Equal(kind, parsed.RootElement.GetProperty("kind").GetString());
            }
        }

        Assert.Contains("\\u00fc over view", documents["card"], StringComparison.OrdinalIgnoreCase);
        Assert.Equal("not-found", JsonDocument.Parse(documents["error"]).RootElement.GetProperty("code").GetString());
        Assert.StartsWith("{\"type\":\"card\"", documents["custom"], StringComparison.Ordinal);

        // The four error codes are the only ones that can be written
        foreach (var code in new[] { JsonDocumentWriter.UsageErrorCode, JsonDocumentWriter.NotFoundCode, JsonDocumentWriter.AmbiguousCode, JsonDocumentWriter.CatalogErrorCode })
        {
            using var error = JsonDocument.Parse(JsonDocumentWriter.WriteError(null, code, "m"));
            Assert.Equal(code, error.RootElement.GetProperty("code").GetString());
        }

        Assert.Throws<ArgumentException>(() => JsonDocumentWriter.WriteError(null, "oops", "m"));
    }

    /// <summary>
    ///     Determines whether a JSON value holds a null anywhere.
    /// </summary>
    /// <param name="element">The value.</param>
    /// <returns><see langword="true"/> when a null is found.</returns>
    private static bool ContainsNull(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => true,
            JsonValueKind.Array => element.EnumerateArray().Any(ContainsNull),
            JsonValueKind.Object => element.EnumerateObject().Any(property => ContainsNull(property.Value)),
            _ => false,
        };
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
