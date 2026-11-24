using System.Text.Json;
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Presentation;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Presentation;

/// <summary>
///     Unit tests for <see cref="JsonDocumentWriter"/>: the seven documents of the contract, written exactly as the
///     specification shows them.
/// </summary>
public partial class JsonDocumentWriterTests
{
    /// <summary>
    ///     The options that write commands, as the command-line helper does.
    /// </summary>
    private static readonly HelpRenderOptions CommandJson = new() { Format = HelpFormat.Json, LinkStyle = HelpLinkStyle.Command };

    /// <summary>
    ///     Proves that the <c>index</c> document equals the fixture of the contract, with tool, version, and usage.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_WriteIndex_ContractFixture_IsWrittenVerbatim()
    {
        // Arrange
        var context = RenderContext.Create(RenderSources.Index(), CommandJson);

        // Act
        var json = JsonDocumentWriter.WriteIndex(context, int.MaxValue, ["reqstream [options]"]);

        // Assert
        Assert.Equal(
            "{\"helpSchemaVersion\":1,\"ok\":true,\"kind\":\"index\",\"tool\":\"reqstream\",\"version\":\"1.4.0\","
            + "\"usage\":[\"reqstream [options]\"],\"sources\":[{\"name\":\"reqstream\",\"title\":\"reqstream\","
            + "\"summary\":\"Help cards of reqstream.\"}],\"cards\":[{\"id\":\"requirements\",\"title\":\"Requirements files\","
            + "\"summary\":\"How to write requirements.\",\"aliases\":[\"req\"],\"tags\":[\"authoring\"],"
            + "\"source\":\"reqstream\",\"depth\":0,\"command\":\"reqstream help requirements\"}]}",
            json);
    }

    /// <summary>
    ///     Proves that the <c>card</c> document equals the fixture of the contract.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_WriteCard_ContractFixture_IsWrittenVerbatim()
    {
        // Arrange
        var catalog = RenderSources.Card();
        var context = RenderContext.Create(catalog, CommandJson);

        // Act
        var json = JsonDocumentWriter.WriteCard(context, catalog.Find("requirements")!);

        // Assert
        Assert.Equal(
            "{\"helpSchemaVersion\":1,\"ok\":true,\"kind\":\"card\",\"id\":\"requirements\",\"title\":\"Requirements files\","
            + "\"summary\":\"How to write requirements.\",\"aliases\":[\"req\"],\"tags\":[\"authoring\"],"
            + "\"source\":\"reqstream\",\"culture\":\"en\",\"body\":\"Write one file per section.\\n\\nSee `reqstream help tests`.\","
            + "\"children\":[{\"id\":\"requirements/tags\",\"title\":\"Tags\",\"summary\":\"Root tags.\","
            + "\"command\":\"reqstream help requirements tags\"}],\"seeAlso\":[],\"examples\":[{\"name\":\"check\","
            + "\"kind\":\"command\",\"language\":\"shell\",\"description\":\"Check traceability.\","
            + "\"content\":\"reqstream --requirements requirements.yaml --enforce\"}],\"links\":[{\"cardId\":\"tests\"}]}",
            json);
    }

    /// <summary>
    ///     Proves that the <c>examples</c> document equals the fixture of the contract.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_WriteExamplesDocument_ContractFixture_IsWrittenVerbatim()
    {
        // Arrange
        var catalog = RenderSources.Examples();
        var context = RenderContext.Create(catalog, CommandJson);

        // Act
        var json = JsonDocumentWriter.WriteExamplesDocument(context, catalog.Find("requirements")!);

        // Assert
        Assert.Equal(
            "{\"helpSchemaVersion\":1,\"ok\":true,\"kind\":\"examples\",\"id\":\"requirements\",\"examples\":[{\"name\":\"check\","
            + "\"kind\":\"command\",\"language\":\"shell\",\"description\":\"Check traceability.\","
            + "\"content\":\"reqstream --enforce\"}],\"cards\":[{\"id\":\"requirements/tags\",\"summary\":\"Root tags.\","
            + "\"command\":\"reqstream help requirements tags\",\"examples\":[\"root-tag\"]}]}",
            json);
    }

    /// <summary>
    ///     Proves that the <c>example</c> document equals the fixture of the contract.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_WriteExampleDocument_ContractFixture_IsWrittenVerbatim()
    {
        // Arrange
        var catalog = RenderSources.Examples();
        var context = RenderContext.Create(catalog, CommandJson);

        // Act
        var json = JsonDocumentWriter.WriteExampleDocument(context, catalog.Find("requirements")!, "root-tag");

        // Assert
        Assert.Equal(
            "{\"helpSchemaVersion\":1,\"ok\":true,\"kind\":\"example\",\"id\":\"requirements\",\"example\":{\"card\":\"requirements/tags\","
            + "\"name\":\"root-tag\",\"kind\":\"source\",\"language\":\"yaml\",\"fileName\":\"tags.yaml\","
            + "\"description\":\"A root tag.\",\"content\":\"root-tags: [system]\"}}",
            json);
    }

    /// <summary>
    ///     Proves that the <c>search</c> document equals the fixture of the contract.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_WriteSearch_ContractFixture_IsWrittenVerbatim()
    {
        // Arrange
        var context = RenderContext.Create(RenderSources.Examples(), CommandJson);
        IReadOnlyList<HelpSearchResult> results =
        [
            new("requirements/tags", "Tags", "Root tags.", "reqstream", 90, ["id", "title"]),
        ];

        // Act
        var json = JsonDocumentWriter.WriteSearch(context, "tags", results);

        // Assert
        Assert.Equal(
            "{\"helpSchemaVersion\":1,\"ok\":true,\"kind\":\"search\",\"query\":\"tags\",\"results\":[{\"id\":\"requirements/tags\","
            + "\"title\":\"Tags\",\"summary\":\"Root tags.\",\"source\":\"reqstream\",\"score\":90,"
            + "\"matchedOn\":[\"id\",\"title\"],\"command\":\"reqstream help requirements tags\"}]}",
            json);
    }

    /// <summary>
    ///     Proves that the <c>cards</c> document equals the fixture of the contract.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_WriteCardList_ContractFixture_IsWrittenVerbatim()
    {
        // Arrange
        var catalog = RenderSources.Index();
        var context = RenderContext.Create(catalog, CommandJson);

        // Act
        var json = JsonDocumentWriter.WriteCardList(context, [catalog.Find("requirements")!], ["authoring"], []);

        // Assert
        Assert.Equal(
            "{\"helpSchemaVersion\":1,\"ok\":true,\"kind\":\"cards\",\"filter\":{\"tags\":[\"authoring\"],\"sources\":[]},"
            + "\"cards\":[{\"id\":\"requirements\",\"title\":\"Requirements files\",\"summary\":\"How to write requirements.\","
            + "\"tags\":[\"authoring\"],\"source\":\"reqstream\",\"command\":\"reqstream help requirements\"}]}",
            json);
    }

    /// <summary>
    ///     Proves that the error document equals the fixture of the contract.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_WriteError_ContractFixture_IsWrittenVerbatim()
    {
        // Act
        var json = JsonDocumentWriter.WriteError(
            null,
            "not-found",
            "Unknown help card 'tag' under 'requirements'.",
            "Did you mean 'tags'? Cards under 'requirements': tags.",
            ["requirements/tags"]);

        // Assert
        Assert.Equal(
            "{\"helpSchemaVersion\":1,\"ok\":false,\"kind\":\"error\",\"code\":\"not-found\","
            + "\"message\":\"Unknown help card 'tag' under 'requirements'.\","
            + "\"hint\":\"Did you mean 'tags'? Cards under 'requirements': tags.\",\"candidates\":[\"requirements/tags\"]}",
            json);
    }

    /// <summary>
    ///     Proves that the guide card of the simple fixture renders as the JSON golden file.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_WriteCard_SimpleGuide_EqualsGolden()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var context = RenderContext.Create(catalog, new HelpRenderOptions { Format = HelpFormat.Json });

        // Act
        var json = JsonDocumentWriter.WriteCard(context, catalog.Find("guide")!);

        // Assert
        GoldenFiles.AssertEqual("guide.expected.json", json);
    }

    /// <summary>
    ///     Proves that the index of the simple fixture renders as the JSON golden file, with the version only.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_WriteIndex_SimpleFixture_EqualsGolden()
    {
        // Arrange
        var context = RenderContext.Create(CatalogBuilding.Simple(), new HelpRenderOptions { Format = HelpFormat.Json });

        // Act
        var json = JsonDocumentWriter.WriteIndex(context, int.MaxValue, null);

        // Assert
        GoldenFiles.AssertEqual("index.expected.json", json);
    }

    /// <summary>
    ///     Proves that the examples document of the simple guide renders as the JSON golden file.
    /// </summary>
    [Fact]
    public void JsonDocumentWriter_WriteExamplesDocument_SimpleGuide_EqualsGolden()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var context = RenderContext.Create(catalog, new HelpRenderOptions { Format = HelpFormat.Json, LinkStyle = HelpLinkStyle.Command });

        // Act
        var json = JsonDocumentWriter.WriteExamplesDocument(context, catalog.Find("guide")!);

        // Assert
        GoldenFiles.AssertEqual("examples.guide.expected.json", json);
    }

    /// <summary>
    ///     Parses a document and returns its property names at the top level in order.
    /// </summary>
    /// <param name="json">The document.</param>
    /// <returns>The names.</returns>
    private static string[] TopLevelKeys(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateObject().Select(property => property.Name).ToArray();
    }
}
