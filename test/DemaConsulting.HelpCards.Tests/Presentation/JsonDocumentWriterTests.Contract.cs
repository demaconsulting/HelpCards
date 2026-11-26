using System.Globalization;
using System.Text;
using System.Text.Json;
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Presentation;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Presentation;

/// <summary>
///     The tests of the rules of the JSON contract: one line, ASCII only, no null, optional keys, envelopes, and
///     error codes.
/// </summary>
public partial class JsonDocumentWriterTests
{
    /// <summary>
    ///     Writes every kind of document for every card of the simple fixture and for the contract sources.
    /// </summary>
    /// <returns>The documents.</returns>
    private static List<string> EveryDocument()
    {
        var documents = new List<string>();
        foreach (var style in new[] { HelpLinkStyle.Plain, HelpLinkStyle.Command, HelpLinkStyle.Markdown })
        {
            var catalog = CatalogBuilding.Simple();
            var context = RenderContext.Create(catalog, new HelpRenderOptions { Format = HelpFormat.Json, LinkStyle = style });
            documents.Add(JsonDocumentWriter.WriteIndex(context, int.MaxValue, ["usage line"]));
            documents.Add(JsonDocumentWriter.WriteCardList(context, catalog.Cards, [], []));
            documents.Add(JsonDocumentWriter.WriteSearch(context, "q", [new("guide", "Guide", "Summary.", "simple", 5, [])]));
            foreach (var card in catalog.Cards)
            {
                documents.Add(JsonDocumentWriter.WriteCard(context, card));
                documents.Add(JsonDocumentWriter.WriteExamplesDocument(context, card));
            }

            documents.Add(JsonDocumentWriter.WriteExampleDocument(context, catalog.Find("guide")!, "run"));
            documents.Add(JsonDocumentWriter.WriteError(context.Options, "not-found", "Message.", null, null));
            documents.Add(JsonDocumentWriter.WriteError(context.Options, "usage-error", "Message."));
        }

        return documents;
    }

    /// <summary>
    ///     Counts the JSON null values and the objects in a JSON element, recursively.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The number of null values.</returns>
    private static int CountNulls(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => 1,
            JsonValueKind.Object => element.EnumerateObject().Sum(property => CountNulls(property.Value)),
            JsonValueKind.Array => element.EnumerateArray().Sum(CountNulls),
            _ => 0,
        };
    }

    /// <summary>
    ///     Proves that no document ever contains a JSON null, and that all of them are valid JSON on one line.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_EveryDocument_HasNoNullAndIsOneValidLine()
    {
        // Act
        var documents = EveryDocument();

        // Assert
        Assert.NotEmpty(documents);
        foreach (var json in documents)
        {
            using var document = JsonDocument.Parse(json);
            Assert.Equal(0, CountNulls(document.RootElement));
            Assert.DoesNotContain('\n', json);
            Assert.DoesNotContain('\r', json);
            Assert.Equal(1, document.RootElement.GetProperty("helpSchemaVersion").GetInt32());
            Assert.False(json.StartsWith(' ') || json.EndsWith(' '));
        }
    }

    /// <summary>
    ///     Proves that every document is ASCII only, whichever characters the cards contain.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_NonAsciiText_IsEscapedAsUnicodeEscapes()
    {
        // Arrange: accents, a symbol, and a character beyond the basic plane (a surrogate pair)
        var title = "Caf\u00E9 \u2615 \uD83D\uDE00";
        var catalog = CatalogBuilding.FromFiles(("a.md", SourceReading.Card(title: title, summary: "Sum.", body: "Bod\u00FC.")));
        var context = RenderContext.Create(catalog, new HelpRenderOptions { Format = HelpFormat.Json });

        // Act
        var json = JsonDocumentWriter.WriteCard(context, catalog.Find("a")!);

        // Assert: ASCII only, escapes in upper-case hexadecimal, and the text survives a round trip
        Assert.All(json, character => Assert.True(character is >= ' ' and < '\u007F'));
        Assert.Contains("Caf\\u00E9 \\u2615 \\uD83D\\uDE00", json, StringComparison.Ordinal);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(title, document.RootElement.GetProperty("title").GetString());
        Assert.Equal("Bod\u00FC.", document.RootElement.GetProperty("body").GetString());
    }

    /// <summary>
    ///     Proves that quotes, backticks, apostrophes, and angle brackets are readable and that control characters are
    ///     escaped as JSON requires.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_SpecialCharacters_AreEscapedOnlyWhenJsonRequiresIt()
    {
        // Act
        var json = JsonDocumentWriter.WriteError(null, "usage-error", "Say \"hi\" to 'x' with `y` <z> & \\ \t end");

        // Assert
        Assert.Contains("\"message\":\"Say \\\"hi\\\" to 'x' with `y` <z> & \\\\ \\t end\"", json, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the keys of the card document follow the order of the contract.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_WriteCard_Keys_FollowTheContractOrder()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var context = RenderContext.Create(catalog, new HelpRenderOptions { Format = HelpFormat.Json });

        // Act
        var keys = TopLevelKeys(JsonDocumentWriter.WriteCard(context, catalog.Find("guide")!));

        // Assert
        Assert.Equal(
            [
                "helpSchemaVersion", "ok", "kind", "id", "title", "summary", "aliases", "tags", "source", "culture", "body",
                "children", "seeAlso", "examples", "links",
            ],
            keys);
    }

    /// <summary>
    ///     Proves that the command is written only for the command link style and that tool, version, and usage appear
    ///     only when they were supplied.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_OptionalKeys_AreLeftOutWhenAbsent()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var plain = RenderContext.Create(catalog, new HelpRenderOptions { Format = HelpFormat.Json });
        var command = RenderContext.Create(catalog, new HelpRenderOptions { Format = HelpFormat.Json, LinkStyle = HelpLinkStyle.Command });

        // Act
        var plainIndex = JsonDocumentWriter.WriteIndex(plain, 0, null);
        var commandIndex = JsonDocumentWriter.WriteIndex(command, 0, null);

        // Assert: the simple catalog has a version but no tool name, and no usage lines
        Assert.DoesNotContain("\"command\"", plainIndex, StringComparison.Ordinal);
        Assert.Contains("\"command\":\"simple help guide\"", commandIndex, StringComparison.Ordinal);
        Assert.DoesNotContain("\"tool\"", plainIndex, StringComparison.Ordinal);
        Assert.DoesNotContain("\"usage\"", plainIndex, StringComparison.Ordinal);
        Assert.Contains("\"version\":\"2.5\"", plainIndex, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that <c>exitCode</c> and <c>expect</c> are appended to an example object when the command states them,
    ///     in the card, the examples, and the example documents.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_Example_ExitCodeAndExpect_AreAppendedWhenSet()
    {
        // Arrange
        var catalog = CatalogBuilding.Runnable();
        var context = RenderContext.Create(catalog, new HelpRenderOptions { Format = HelpFormat.Json });
        var check = catalog.Find("check")!;

        // Act
        var card = JsonDocumentWriter.WriteCard(context, check);
        var example = JsonDocumentWriter.WriteExampleDocument(context, check, "missing");

        // Assert: exit code first, then the fragments, after the content
        Assert.Contains(
            "\"content\":\"demo check absent.yaml\",\"exitCode\":2,\"expect\":[\"Cannot open absent.yaml\"]}",
            card,
            StringComparison.Ordinal);
        Assert.Contains(
            "\"content\":\"demo check check/config.yaml\",\"expect\":[\"Checked 1 file\",\"No problems\"]}",
            card,
            StringComparison.Ordinal);
        Assert.EndsWith("\"content\":\"demo check absent.yaml\",\"exitCode\":2,\"expect\":[\"Cannot open absent.yaml\"]}}", example, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the default exit code and an empty expectation are left out, so documents of examples that
    ///     state no result do not change.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_Example_DefaultExitCode_IsOmitted()
    {
        // Arrange
        var catalog = CatalogBuilding.Runnable();
        var context = RenderContext.Create(catalog, new HelpRenderOptions { Format = HelpFormat.Json });

        // Act
        var json = JsonDocumentWriter.WriteExampleDocument(context, catalog.Find("check")!, "config");

        // Assert
        Assert.DoesNotContain("exitCode", json, StringComparison.Ordinal);
        Assert.DoesNotContain("expect", json, StringComparison.Ordinal);
        Assert.DoesNotContain("null", json, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the index respects the depth limit and reports the depth of each card.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_WriteIndex_MaxDepth_LimitsCardsAndReportsDepth()
    {
        // Arrange
        var context = RenderContext.Create(CatalogBuilding.Simple(), new HelpRenderOptions { Format = HelpFormat.Json });

        // Act
        using var all = JsonDocument.Parse(JsonDocumentWriter.WriteIndex(context, int.MaxValue, null));
        using var roots = JsonDocument.Parse(JsonDocumentWriter.WriteIndex(context, 0, null));

        // Assert
        var depths = all.RootElement.GetProperty("cards").EnumerateArray().Select(card => card.GetProperty("depth").GetInt32());
        Assert.Equal([0, 1, 1, 0, 1], depths);
        Assert.Equal(2, roots.RootElement.GetProperty("cards").GetArrayLength());
    }

    /// <summary>
    ///     Proves that the links of a card list every visible target of the body in order, with the optional parts.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_WriteCard_Links_ListVisibleTargetsInBodyOrder()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var context = RenderContext.Create(catalog, new HelpRenderOptions { Format = HelpFormat.Json });

        // Act
        using var document = JsonDocument.Parse(JsonDocumentWriter.WriteCard(context, catalog.Find("guide")!));
        var links = document.RootElement.GetProperty("links").EnumerateArray().Select(link => link.GetRawText()).ToList();

        // Assert: absent external links are text and not links; the external link keeps its display text
        Assert.Equal(
            [
                "{\"cardId\":\"guide/setup\"}",
                "{\"cardId\":\"guide/usage\"}",
                "{\"exampleName\":\"run\",\"exampleCardId\":\"guide\"}",
                "{\"cardId\":\"guide\",\"exampleName\":\"settings\",\"exampleCardId\":\"guide\"}",
                "{\"cardId\":\"reference/limits\",\"text\":\"the limits card\"}",
            ],
            links);
    }

    /// <summary>
    ///     Proves that the culture of a card is the culture of the file that supplied its body.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_WriteCard_Culture_IsTheCultureOfTheBody()
    {
        // Arrange
        var catalog = CatalogBuilding.Create(
            CatalogBuilding.Fixture("i18n"),
            builder => builder.Culture(CatalogBuilding.Culture("de")));
        var context = RenderContext.Create(catalog, new HelpRenderOptions { Format = HelpFormat.Json });

        // Act
        var cultures = catalog.Cards
            .Select(card =>
            {
                using var document = JsonDocument.Parse(JsonDocumentWriter.WriteCard(context, card));
                return (card.Id, document.RootElement.GetProperty("culture").GetString());
            })
            .ToList();

        // Assert: the farewell card has a German body, the plain card falls back to the source language
        Assert.Contains(("farewell", "de"), cultures);
        Assert.Contains(("plain", "en"), cultures);
    }

    /// <summary>
    ///     Proves that an envelope callback replaces the top-level object and receives the kind and the field writer.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_JsonEnvelope_ReplacesTopLevelObject()
    {
        // Arrange: an envelope in the style of an application that has its own result format
        var kinds = new List<string>();
        var catalog = RenderSources.Examples();
        var options = new HelpRenderOptions
        {
            Format = HelpFormat.Json,
            JsonEnvelope = (kind, writeFields) =>
            {
                kinds.Add(kind);
                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("schemaVersion", 2);
                    writer.WriteBoolean("ok", true);
                    writer.WriteString("command", "help");
                    writeFields(writer);
                    writer.WriteEndObject();
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            },
        };

        // Act
        var json = catalog.Render(catalog.Find("requirements/tags")!, options);

        // Assert
        Assert.Equal(["card"], kinds);
        Assert.StartsWith("{\"schemaVersion\":2,\"ok\":true,\"command\":\"help\",\"id\":\"requirements/tags\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("helpSchemaVersion", json, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that non-ASCII text written through an envelope is escaped too.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_JsonEnvelope_NonAsciiOutput_IsEscaped()
    {
        // Arrange: an envelope that writes a non-ASCII member with a writer that does not escape it
        var options = new HelpRenderOptions
        {
            JsonEnvelope = (_, writeFields) =>
            {
                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(
                    stream,
                    new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
                {
                    writer.WriteStartObject();
                    writer.WriteString("name", "caf\u00E9");
                    writeFields(writer);
                    writer.WriteEndObject();
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            },
        };

        // Act
        var json = JsonDocumentWriter.WriteError(options, "usage-error", "m");

        // Assert
        Assert.StartsWith("{\"name\":\"caf\\u00E9\"", json, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that candidates appear only for not-found and ambiguous errors, and that the hint is optional.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_WriteError_Codes_ControlCandidatesAndHint()
    {
        // Act
        var notFound = JsonDocumentWriter.WriteError(null, "not-found", "m");
        var ambiguous = JsonDocumentWriter.WriteError(null, "ambiguous", "m", "h", ["a", "b"]);
        var usage = JsonDocumentWriter.WriteError(null, "usage-error", "m", null, ["ignored"]);
        var catalogError = JsonDocumentWriter.WriteError(null, "catalog-error", "m", "h");

        // Assert
        Assert.EndsWith("\"code\":\"not-found\",\"message\":\"m\",\"candidates\":[]}", notFound, StringComparison.Ordinal);
        Assert.EndsWith("\"code\":\"ambiguous\",\"message\":\"m\",\"hint\":\"h\",\"candidates\":[\"a\",\"b\"]}", ambiguous, StringComparison.Ordinal);
        Assert.EndsWith("\"code\":\"usage-error\",\"message\":\"m\"}", usage, StringComparison.Ordinal);
        Assert.EndsWith("\"code\":\"catalog-error\",\"message\":\"m\",\"hint\":\"h\"}", catalogError, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a body callback replaces the default members of an error document, inside the default envelope
    ///     and inside a custom envelope, and that the code is still checked.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_WriteError_WithBody_ReplacesTheMembersInsideTheEnvelope()
    {
        // Arrange
        static void Body(Utf8JsonWriter writer)
        {
            writer.WriteStartObject("error");
            writer.WriteString("code", "mine");
            writer.WriteEndObject();
        }

        var custom = new HelpRenderOptions
        {
            JsonEnvelope = (kind, writeFields) =>
            {
                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject();
                    writer.WriteString("kind", kind);
                    writeFields(writer);
                    writer.WriteEndObject();
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            },
        };

        // Act
        var plain = JsonDocumentWriter.WriteError(null, "not-found", "m", "h", ["a"], Body);
        var wrapped = JsonDocumentWriter.WriteError(custom, "usage-error", "m", null, null, Body);

        // Assert
        Assert.Equal("{\"helpSchemaVersion\":1,\"ok\":false,\"kind\":\"error\",\"error\":{\"code\":\"mine\"}}", plain);
        Assert.Equal("{\"kind\":\"error\",\"error\":{\"code\":\"mine\"}}", wrapped);
        Assert.Throws<ArgumentException>(() => JsonDocumentWriter.WriteError(null, "oops", "m", null, null, Body));
    }

    /// <summary>
    ///     Proves that an unknown error code and null arguments are rejected.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_WriteError_InvalidArguments_Throw()
    {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => JsonDocumentWriter.WriteError(null, "oops", "m"));
        Assert.Throws<ArgumentNullException>(() => JsonDocumentWriter.WriteError(null, null!, "m"));
        Assert.Throws<ArgumentNullException>(() => JsonDocumentWriter.WriteError(null, "usage-error", null!));
    }

    /// <summary>
    ///     Proves that an envelope callback that returns null is reported with a descriptive argument error.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_WriteError_EnvelopeReturningNull_ThrowsDescriptiveArgumentException()
    {
        // Arrange
        var options = new HelpRenderOptions { Format = HelpFormat.Json, JsonEnvelope = (_, _) => null! };

        // Act
        var exception = Assert.Throws<ArgumentException>(() => JsonDocumentWriter.WriteError(options, "usage-error", "m"));

        // Assert
        Assert.Contains("JsonEnvelope", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the documents do not depend on the current culture of the machine.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_EveryDocument_DoesNotDependOnCurrentCulture()
    {
        // Arrange
        var original = CultureInfo.CurrentCulture;
        var invariant = EveryDocument();
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            // Act
            var turkish = EveryDocument();

            // Assert
            Assert.Equal(invariant, turkish);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    ///     Proves that null arguments of the document writers are rejected.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_NullArguments_ThrowArgumentNullException()
    {
        // Arrange
        var catalog = RenderSources.Examples();
        var context = RenderContext.Create(catalog, null);
        var card = catalog.Find("requirements")!;

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => JsonDocumentWriter.WriteCard(null!, card));
        Assert.Throws<ArgumentNullException>(() => JsonDocumentWriter.WriteCard(context, null!));
        Assert.Throws<ArgumentNullException>(() => JsonDocumentWriter.WriteExamplesDocument(context, null!));
        Assert.Throws<ArgumentNullException>(() => JsonDocumentWriter.WriteIndex(null!, 0, null));
        Assert.Throws<ArgumentNullException>(() => JsonDocumentWriter.WriteSearch(context, null!, []));
        Assert.Throws<ArgumentNullException>(() => JsonDocumentWriter.WriteSearch(context, "q", null!));
        Assert.Throws<ArgumentNullException>(() => JsonDocumentWriter.WriteCardList(context, null!, [], []));
        Assert.Throws<ArgumentNullException>(() => JsonDocumentWriter.WriteCardList(context, [], null!, []));
        Assert.Throws<ArgumentNullException>(() => JsonDocumentWriter.WriteCardList(context, [], [], null!));
        Assert.Throws<ArgumentException>(() => JsonDocumentWriter.WriteExampleDocument(context, card, "missing"));
    }
}
