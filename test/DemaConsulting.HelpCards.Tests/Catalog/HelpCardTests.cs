using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Catalog;

/// <summary>
///     Unit tests for <see cref="HelpCard"/> and the model types that belong to it: examples, links, source
///     descriptions, and search results.
/// </summary>
public class HelpCardTests
{
    /// <summary>
    ///     Proves that a card exposes the values of its file and its place in the tree.
    /// </summary>
    [Fact]
    public void HelpCard_Properties_Root_ExposeFileValuesAndPlace()
    {
        // Arrange / Act
        var guide = CatalogBuilding.Simple().Find("guide")!;

        // Assert
        Assert.Equal("guide", guide.Id);
        Assert.Equal("guide", guide.LocalId);
        Assert.Equal("guide", guide.Name);
        Assert.Null(guide.ParentId);
        Assert.Equal("simple", guide.SourceName);
        Assert.Equal("Guide", guide.Title);
        Assert.Equal("How to set up and use the tool.", guide.Summary);
        Assert.Equal(["tutorial", "authoring"], guide.Tags);
        Assert.Equal(["start"], guide.Aliases);
        Assert.Equal("en", guide.Culture);
        Assert.Equal(["guide/setup", "guide/usage"], guide.ChildIds);
        Assert.Equal(["reference/limits"], guide.SeeAlsoIds);
        Assert.Empty(guide.Extensions);
    }

    /// <summary>
    ///     Proves that a child card names its parent and has a name that is the last segment of its id.
    /// </summary>
    [Fact]
    public void HelpCard_Properties_Child_NamesParentAndLastSegment()
    {
        // Arrange / Act
        var limits = CatalogBuilding.Simple().Find("reference limits")!;

        // Assert
        Assert.Equal("reference/limits", limits.Id);
        Assert.Equal("limits", limits.Name);
        Assert.Equal("reference", limits.ParentId);
        Assert.Empty(limits.ChildIds);
    }

    /// <summary>
    ///     Proves that extension keys are kept on the card.
    /// </summary>
    [Fact]
    public void HelpCard_Extensions_XKey_IsPreserved()
    {
        // Arrange
        var catalog = CatalogBuilding.FromFiles(("a.md", SourceReading.Card(extraFrontMatter: "x-audience: agent\n")));

        // Act
        var extensions = catalog.Find("a")!.Extensions;

        // Assert
        Assert.Equal("agent", Assert.Single(extensions).Value);
        Assert.Equal("x-audience", Assert.Single(extensions).Key);
    }

    /// <summary>
    ///     Proves that examples expose their kind, language, file name, description, content, and source.
    /// </summary>
    [Fact]
    public void HelpExample_Properties_Examples_ExposeResolvedValues()
    {
        // Arrange / Act
        var examples = CatalogBuilding.Simple().Find("guide")!.Examples;

        // Assert: the command has no file name and its token is replaced; the source example reads its file
        var run = examples[0];
        Assert.Equal(("run", HelpExample.CommandKind, "shell", null, "simple"), (run.Name, run.Kind, run.Language, run.FileName, run.SourceName));
        Assert.Equal("simple run input.txt", run.Content);
        Assert.Equal("Run the tool on a file.", run.Description);
        var settings = examples[1];
        Assert.Equal(HelpExample.SourceKind, settings.Kind);
        Assert.Equal("guide/settings.yaml", settings.FileName);
        Assert.Equal("---\nlimit: 42\nmode: fast", settings.Content);
        var fragment = examples[2];
        Assert.Equal(HelpExample.SnippetKind, fragment.Kind);
        Assert.Equal("limit: 42\nmode: fast", fragment.Content);
    }

    /// <summary>
    ///     Proves that the body writes every link in the plain style with global ids.
    /// </summary>
    [Fact]
    public void HelpCard_Body_SimpleFixtureGuide_WritesLinksInPlainStyle()
    {
        // Arrange / Act
        var body = CatalogBuilding.Simple().Find("guide")!.Body;

        // Assert: card, local example, example of a card, external link, absent external link, code span, tokens
        Assert.Equal(
            "Start with the card `guide/setup` and then read the card `guide/usage`.\n"
            + "Run it with the example `run` or see the example `settings` of the card `guide` for the settings file.\n"
            + "The limits are described in the card `reference/limits`, and\n"
            + "the thing lives in another library.\n"
            + "A code span such as `[[x]]` is not a link.\n"
            + "Version 2.5 of simple accepts at most 42 items.",
            body);
    }

    /// <summary>
    ///     Proves that the body is computed once and shared by every reader.
    /// </summary>
    [Fact]
    public void HelpCard_Body_ReadTwice_ReturnsTheSameString()
    {
        // Arrange
        var card = CatalogBuilding.Simple().Find("guide")!;

        // Act / Assert
        Assert.Same(card.Body, card.Body);
    }

    /// <summary>
    ///     Proves that an example in a descendant is written with its owner, whether the link names the card or not.
    /// </summary>
    [Fact]
    public void HelpCard_Body_ExampleOfDescendant_IsWrittenWithItsOwner()
    {
        // Arrange
        var catalog = CatalogBuilding.FromFiles(
            ("a.md", SourceReading.Card(body: "Here [[a#deep]] and here [[#deep]].")),
            ("a/b.md", SourceReading.Card(extraFrontMatter: "examples:\n  - name: deep\n    kind: command\n    language: shell\n    content: run\n    description: Deep.\n", body: "See [[#deep]].")));

        // Act / Assert: the owner is not the containing card in the first card; it is in the second
        Assert.Equal("Here the example `deep` of the card `a/b` and here the example `deep` of the card `a/b`.", catalog.Find("a")!.Body);
        Assert.Equal("See the example `deep`.", catalog.Find("a/b")!.Body);
    }

    /// <summary>
    ///     Proves that a link to a hidden card is written as its title and a link to a hidden example as "an example".
    /// </summary>
    [Fact]
    public void HelpCard_Body_LinksToHiddenTargets_AreWrittenAsTitleAndAnExample()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act
        var withoutSetup = catalog.Without(["guide/setup"]).Find("guide")!.Body;
        var withoutGuide = catalog.Without(["guide"]);

        // Assert: the card link becomes the title; the example link of the hidden owner becomes "an example"
        Assert.Contains("Start with Setup and then", withoutSetup, StringComparison.Ordinal);
        Assert.Contains("read the card `guide/usage`", withoutSetup, StringComparison.Ordinal);
        Assert.Contains("Look at Guide for", withoutGuide.Find("reference")!.Body, StringComparison.Ordinal);
        Assert.Contains("The setting is shown in an example.", withoutGuide.Find("reference/limits")!.Body, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the link pieces carry global ids, titles, and the display text of external links.
    /// </summary>
    [Fact]
    public void HelpCard_BodyPieces_Links_CarryIdsTitlesAndText()
    {
        // Arrange / Act
        var links = CatalogBuilding.Simple().Find("guide")!.BodyPieces
            .Where(piece => piece.Link is not null)
            .Select(piece => piece.Link!)
            .ToList();

        // Assert: five visible links in body order; the link to the absent source became text
        Assert.Equal(5, links.Count);
        Assert.Equal(("guide/setup", null, null, "Setup", null, null), Describe(links[0]));
        Assert.Equal(("guide/usage", null, null, "Usage", null, null), Describe(links[1]));
        Assert.Equal((null, "run", "guide", "Guide", null, null), Describe(links[2]));
        Assert.Equal(("guide", "settings", "guide", "Guide", null, null), Describe(links[3]));
        Assert.Equal(("reference/limits", null, null, "Limits", "the limits card", "simple"), Describe(links[4]));
    }

    /// <summary>
    ///     Proves that adjacent text, including the text of dropped links, is merged into one piece.
    /// </summary>
    [Fact]
    public void HelpCard_BodyPieces_AdjacentText_IsMerged()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple().Without(["guide/setup", "guide/usage"]);

        // Act
        var pieces = catalog.Find("guide")!.BodyPieces;

        // Assert: no two text pieces follow each other
        for (var i = 1; i < pieces.Count; i++)
        {
            Assert.False(pieces[i - 1].Text is not null && pieces[i].Text is not null);
        }

        Assert.StartsWith("Start with Setup and then read Usage.", pieces[0].Text, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a source description holds its values and compares by value.
    /// </summary>
    [Fact]
    public void HelpSourceInfo_Equality_SameValues_AreEqual()
    {
        // Arrange
        var first = new HelpSourceInfo("a", null, "A", "Summary.", "en", 1);
        var second = new HelpSourceInfo("a", null, "A", "Summary.", "en", 1);

        // Act / Assert
        Assert.Equal(first, second);
        Assert.NotEqual(first, second with { Mount = "m" });
    }

    /// <summary>
    ///     Proves that a search result is a plain value that holds its fields and compares by value.
    /// </summary>
    [Fact]
    public void HelpSearchResult_Constructor_Fields_AreExposed()
    {
        // Arrange
        var matched = new[] { "id", "title" };

        // Act
        var result = new HelpSearchResult("a/b", "Title", "Summary.", "src", 90, matched);

        // Assert
        Assert.Equal(("a/b", "Title", "Summary.", "src", 90), (result.Id, result.Title, result.Summary, result.SourceName, result.Score));
        Assert.Equal(matched, result.MatchedOn);
        Assert.Equal(result, result with { });
        Assert.NotEqual(result, result with { Score = 1 });

        // Equal values held in different list instances are equal; a different list is not
        var same = new HelpSearchResult("a/b", "Title", "Summary.", "src", 90, new List<string> { "id", "title" });
        Assert.Equal(result, same);
        Assert.Equal(result.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(result, result with { MatchedOn = new[] { "id" } });
    }

    /// <summary>
    ///     Proves that an example reference holds the card id and the example and compares by value.
    /// </summary>
    [Fact]
    public void HelpExampleReference_Constructor_Fields_AreExposed()
    {
        // Arrange
        var example = CatalogBuilding.Simple().Find("guide")!.Examples[0];

        // Act
        var reference = new HelpExampleReference("guide", example);

        // Assert
        Assert.Equal("guide", reference.CardId);
        Assert.Same(example, reference.Example);
        Assert.Equal(reference, new HelpExampleReference("guide", example));
    }

    /// <summary>
    ///     Describes a link as a tuple so tests can compare it in one assertion.
    /// </summary>
    /// <param name="link">The link.</param>
    /// <returns>The card id, example name, example card id, title, text, and source name.</returns>
    private static (string?, string?, string?, string?, string?, string?) Describe(HelpLink link)
    {
        return (link.CardId, link.ExampleName, link.ExampleCardId, link.Title, link.Text, link.SourceName);
    }

    /// <summary>
    ///     Proves that a card names its file, the file that supplied its body, and the line of the body, and that a
    ///     selected translation supplies its own file and line.
    /// </summary>
    [Fact]
    public void HelpCard_Site_NeutralAndTranslatedCards_NameFileBodyFileAndLine()
    {
        // Arrange
        var source = CatalogBuilding.Fixture("i18n");

        // Act
        var neutral = CatalogBuilding.Create(source).Find("welcome")!;
        var german = CatalogBuilding.Create(source, builder => builder.Culture(CatalogBuilding.Culture("de"))).Find("welcome")!;

        // Assert
        Assert.Equal(("welcome.md", "welcome.md", 13), (neutral.File, neutral.BodyFile, neutral.BodyLine));
        Assert.Equal(("welcome.md", "welcome.de.md", 10), (german.File, german.BodyFile, german.BodyLine));
    }
}
