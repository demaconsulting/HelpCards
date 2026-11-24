using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Presentation;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Catalog;

/// <summary>
///     Unit tests for the <c>Render</c> members of <see cref="HelpCatalog"/>.
/// </summary>
public partial class HelpCatalogTests
{
    /// <summary>
    ///     Proves that rendering a card without options writes the text document and that null options are the defaults.
    /// </summary>
    [Fact]
    public void HelpCatalog_Render_DefaultOptions_WritesTextDocument()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var guide = catalog.Find("guide")!;

        // Act
        var withoutOptions = catalog.Render(guide);
        var withDefaults = catalog.Render(guide, new HelpRenderOptions());

        // Assert
        Assert.Equal(withDefaults, withoutOptions);
        Assert.StartsWith("# Guide\n\nHow to set up and use the tool.\nAlso known as: start\nTags: tutorial, authoring\n", withoutOptions, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the format of the options selects text, Markdown, or JSON.
    /// </summary>
    [Fact]
    public void HelpCatalog_Render_Format_SelectsTextMarkdownOrJson()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var guide = catalog.Find("guide")!;

        // Act
        var markdown = catalog.Render(guide, new HelpRenderOptions { Format = HelpFormat.Markdown });
        var json = catalog.Render(guide, new HelpRenderOptions { Format = HelpFormat.Json });

        // Assert
        Assert.Contains("**Tags:** tutorial, authoring", markdown, StringComparison.Ordinal);
        Assert.StartsWith("{\"helpSchemaVersion\":1,\"ok\":true,\"kind\":\"card\",\"id\":\"guide\"", json, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a null card is an argument error and that a card hidden in the catalog cannot be rendered.
    /// </summary>
    [Fact]
    public void HelpCatalog_Render_NullOrHiddenCard_ThrowsArgumentException()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var view = catalog.Without(["reference"]);
        var reference = catalog.Find("reference")!;

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => catalog.Render(null!));
        Assert.Throws<ArgumentNullException>(() => catalog.RenderExamples(null!));
        Assert.Throws<ArgumentNullException>(() => catalog.RenderExample(null!, "run"));
        Assert.Throws<ArgumentNullException>(() => catalog.FormatBody(null!));
        Assert.Throws<ArgumentException>(() => view.Render(reference));
        Assert.Throws<ArgumentException>(() => view.RenderExamples(reference));
        Assert.Throws<ArgumentException>(() => view.RenderExample(reference, "run"));
        Assert.Throws<ArgumentException>(() => view.FormatBody(reference));
    }

    /// <summary>
    ///     Proves that a list of cards is mapped to the rendering view: a visible card of another view is listed and a
    ///     card that the view hides is rejected in every format.
    /// </summary>
    [Fact]
    public void HelpCatalog_RenderCardList_CardsOfOtherView_AreMappedOrRejected()
    {
        // Arrange: the view hides 'reference', which the full catalog shows
        var catalog = CatalogBuilding.Simple();
        var view = catalog.Without(["reference"]);
        var json = new HelpRenderOptions { Format = HelpFormat.Json };
        var guide = catalog.Find("guide")!;
        var reference = catalog.Find("reference")!;

        // Act / Assert: a card that the view shows is listed whichever view it came from
        Assert.Equal(view.RenderCardList(view.Cards), view.RenderCardList(catalog.Cards.Where(card => card.Id != "reference").ToList()));
        Assert.Equal(catalog.RenderCardList([guide]), view.RenderCardList([guide]));

        // Act / Assert: a hidden card is rejected, by the public and by the internal overload
        Assert.Throws<ArgumentException>(() => view.RenderCardList([reference]));
        Assert.Throws<ArgumentException>(() => view.RenderCardList([guide, reference], json));
        Assert.Throws<ArgumentException>(() => view.RenderCardList([reference], [], [], null));
    }

    /// <summary>
    ///     Proves that a card is rendered with the rules of the view that renders it, not of the view it came from.
    /// </summary>
    [Fact]
    public void HelpCatalog_Render_CardOfOtherView_UsesTheRulesOfTheRenderingView()
    {
        // Arrange: in the view the card <c>guide/usage</c> is hidden, so links to it become its title
        var catalog = CatalogBuilding.Simple();
        var view = catalog.Without(["guide/usage"]);

        // Act
        var full = catalog.Render(catalog.Find("guide")!);
        var reduced = view.Render(catalog.Find("guide")!);

        // Assert
        Assert.Contains("and then read the card `guide/usage`.", full, StringComparison.Ordinal);
        Assert.Contains("and then read Usage.", reduced, StringComparison.Ordinal);
        Assert.DoesNotContain("- guide/usage", reduced, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the index lists the cards, limits the depth, and rejects a negative depth.
    /// </summary>
    [Fact]
    public void HelpCatalog_RenderIndex_DepthAndFormats_AreHonored()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act
        var all = catalog.RenderIndex();
        var roots = catalog.RenderIndex(null, 0);
        var json = catalog.RenderIndex(new HelpRenderOptions { Format = HelpFormat.Json }, 0);

        // Assert
        Assert.Equal(5, all.Split('\n').Length);
        Assert.Equal(2, roots.Split('\n').Length);
        Assert.StartsWith("{\"helpSchemaVersion\":1,\"ok\":true,\"kind\":\"index\"", json, StringComparison.Ordinal);
        Assert.Throws<ArgumentOutOfRangeException>(() => catalog.RenderIndex(null, -1));
    }

    /// <summary>
    ///     Proves that the internal index overload writes the usage lines of the tool into the JSON document.
    /// </summary>
    [Fact]
    public void HelpCatalog_RenderIndex_UsageLines_AreWrittenForJsonOnly()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act
        var json = catalog.RenderIndex(new HelpRenderOptions { Format = HelpFormat.Json }, 0, ["tool [options]"]);
        var text = catalog.RenderIndex(null, 0, ["tool [options]"]);

        // Assert
        Assert.Contains("\"usage\":[\"tool [options]\"]", json, StringComparison.Ordinal);
        Assert.DoesNotContain("tool [options]", text, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the examples of a card and one example are rendered, in text and in JSON.
    /// </summary>
    [Fact]
    public void HelpCatalog_RenderExamplesAndExample_TextAndJson_AreWritten()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var guide = catalog.Find("guide")!;

        // Act
        var examples = catalog.RenderExamples(guide);
        var example = catalog.RenderExample(guide, "run");
        var json = catalog.RenderExample(guide, "Run", new HelpRenderOptions { Format = HelpFormat.Json });

        // Assert
        Assert.StartsWith("# Guide: examples\n\n## run [command]\n", examples, StringComparison.Ordinal);
        Assert.Equal("simple run input.txt", example);
        Assert.Contains("\"kind\":\"example\",\"id\":\"guide\",\"example\":{\"card\":\"guide\",\"name\":\"run\"", json, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => catalog.RenderExample(guide, "missing"));
        Assert.Throws<ArgumentException>(() => catalog.RenderExample(guide, " "));
    }

    /// <summary>
    ///     Proves that search results and card lists are rendered and that null arguments are rejected.
    /// </summary>
    [Fact]
    public void HelpCatalog_RenderSearchAndCardList_Documents_AreWritten()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        IReadOnlyList<HelpSearchResult> results = [new("guide", "Guide", "How to set up and use the tool.", "simple", 80, ["title"])];
        var json = new HelpRenderOptions { Format = HelpFormat.Json };

        // Act
        var search = catalog.RenderSearch("guide", results);
        var searchJson = catalog.RenderSearch("guide", results, json);
        var list = catalog.RenderCardList([catalog.Find("reference")!]);
        var listJson = catalog.RenderCardList([catalog.Find("reference")!], json);

        // Assert
        Assert.Equal("guide  How to set up and use the tool.", search);
        Assert.Contains("\"kind\":\"search\",\"query\":\"guide\",\"results\":[{\"id\":\"guide\"", searchJson, StringComparison.Ordinal);
        Assert.Equal("reference  Facts about limits and options.", list);
        Assert.Contains("\"filter\":{\"tags\":[],\"sources\":[]},\"cards\":[{\"id\":\"reference\"", listJson, StringComparison.Ordinal);
        Assert.Throws<ArgumentNullException>(() => catalog.RenderSearch(null!, results));
        Assert.Throws<ArgumentNullException>(() => catalog.RenderSearch("q", null!));
        Assert.Throws<ArgumentNullException>(() => catalog.RenderCardList(null!));
        Assert.Throws<ArgumentNullException>(() => catalog.RenderCardList([null!]));
    }

    /// <summary>
    ///     Proves that the internal card list overload writes the filter of the JSON document.
    /// </summary>
    [Fact]
    public void HelpCatalog_RenderCardList_Filter_IsWrittenForJson()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act
        var json = catalog.RenderCardList(
            catalog.Cards.Where(card => card.Tags.Contains("reference")).ToList(),
            ["reference"],
            ["simple"],
            new HelpRenderOptions { Format = HelpFormat.Json });

        // Assert
        Assert.Contains("\"filter\":{\"tags\":[\"reference\"],\"sources\":[\"simple\"]}", json, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the formatted body follows the link style, ignores the format, and equals the plain body by default.
    /// </summary>
    [Fact]
    public void HelpCatalog_FormatBody_LinkStyle_IsAppliedWhateverTheFormat()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var guide = catalog.Find("guide")!;

        // Act
        var plain = catalog.FormatBody(guide);
        var command = catalog.FormatBody(guide, new HelpRenderOptions { Format = HelpFormat.Json, LinkStyle = HelpLinkStyle.Command });
        var custom = catalog.FormatBody(guide, new HelpRenderOptions { LinkFormatter = link => $"<{link.CardId ?? link.ExampleName}>" });

        // Assert
        Assert.Equal(guide.Body, plain);
        Assert.Contains("`simple help guide setup`", command, StringComparison.Ordinal);
        Assert.Contains("Start with <guide/setup> and then read <guide/usage>.", custom, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the link callback is called only for visible targets of the rendered view.
    /// </summary>
    [Fact]
    public void HelpCatalog_Render_LinkFormatter_IsCalledOnlyForVisibleTargets()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple().Without(["guide/usage"]);
        var calls = new List<string>();
        var options = new HelpRenderOptions
        {
            LinkFormatter = link =>
            {
                calls.Add(link.CardId ?? "(local)");
                return "X";
            },
        };

        // Act
        var text = catalog.Render(catalog.Find("guide")!, options);

        // Assert
        Assert.DoesNotContain("guide/usage", calls);
        Assert.Contains("guide/setup", calls);
        Assert.Contains("and then read Usage.", text, StringComparison.Ordinal);
    }
}
