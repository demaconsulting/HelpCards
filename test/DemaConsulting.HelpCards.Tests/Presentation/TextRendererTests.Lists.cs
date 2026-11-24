using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Presentation;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Presentation;

/// <summary>
///     The tests of the index, card list, and search documents of <see cref="TextRenderer"/>.
/// </summary>
public partial class TextRendererTests
{
    /// <summary>
    ///     Proves that the index of the simple fixture renders as its text golden file.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderIndex_SimpleFixture_Text_EqualsGolden()
    {
        // Arrange
        var context = RenderContext.Create(CatalogBuilding.Simple(), null);

        // Act
        var text = TextRenderer.RenderIndex(context, int.MaxValue);

        // Assert
        GoldenFiles.AssertEqual("index.text.expected.txt", text);
    }

    /// <summary>
    ///     Proves that the index of the simple fixture renders as its Markdown golden file.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderIndex_SimpleFixture_Markdown_EqualsGolden()
    {
        // Arrange
        var context = RenderContext.Create(CatalogBuilding.Simple(), new HelpRenderOptions { Format = HelpFormat.Markdown });

        // Act
        var text = TextRenderer.RenderIndex(context, int.MaxValue);

        // Assert
        GoldenFiles.AssertEqual("index.markdown.expected.txt", text);
    }

    /// <summary>
    ///     Proves that the index in the command style shows command paths, as its golden file records.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderIndex_SimpleFixture_CommandStyle_EqualsGolden()
    {
        // Arrange
        var context = RenderContext.Create(CatalogBuilding.Simple(), new HelpRenderOptions { LinkStyle = HelpLinkStyle.Command, Width = 50 });

        // Act
        var text = TextRenderer.RenderIndex(context, int.MaxValue);

        // Assert
        GoldenFiles.AssertEqual("index.command.expected.txt", text);
    }

    /// <summary>
    ///     Proves the layout of the index: two spaces per level, labels padded to the widest, and summaries wrapped
    ///     at the hanging indent.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderIndex_Tree_PadsLabelsAndIndentsByDepth()
    {
        // Arrange
        var catalog = Tree();
        var context = RenderContext.Create(catalog, null);

        // Act
        var text = TextRenderer.RenderIndex(context, int.MaxValue);

        // Assert
        Assert.Equal("a      The first card.\n  a/b  The child.\nc      Related.", text);
    }

    /// <summary>
    ///     Proves that a depth limit of zero lists only the roots and a limit of one adds their children.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderIndex_MaxDepth_LimitsTheLevels()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var context = RenderContext.Create(catalog, null);

        // Act
        var roots = TextRenderer.RenderIndex(context, 0);
        var one = TextRenderer.RenderIndex(context, 1);

        // Assert
        Assert.Equal(2, roots.Split('\n').Length);
        Assert.Equal(5, one.Split('\n').Length);
        Assert.StartsWith("guide ", roots, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that summaries wrap at the hanging indent of the label column.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderIndex_LongSummary_WrapsAtHangingIndent()
    {
        // Arrange
        var catalog = CatalogBuilding.FromFiles(
            ("a.md", SourceReading.Card(title: "A", summary: "one two three four five six seven")));
        var context = RenderContext.Create(catalog, new HelpRenderOptions { Width = 20 });

        // Act
        var text = TextRenderer.RenderIndex(context, int.MaxValue);

        // Assert
        Assert.Equal("a  one two three\n   four five six\n   seven", text);
    }

    /// <summary>
    ///     Proves that an index without cards says so instead of being empty.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderIndex_NoVisibleCards_WritesTheEmptySentence()
    {
        // Arrange
        var context = RenderContext.Create(CatalogBuilding.Simple().Where(_ => false), null);

        // Act
        var text = TextRenderer.RenderIndex(context, int.MaxValue);

        // Assert
        Assert.Equal("There are no help cards.", text);
    }

    /// <summary>
    ///     Proves that a list of cards has one line per card without indentation, in the order given.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderCardList_Cards_WritesOneLineEach()
    {
        // Arrange
        var catalog = Tree();
        var context = RenderContext.Create(catalog, null);
        var cards = new[] { catalog.Find("c")!, catalog.Find("a/b")! };

        // Act
        var text = TextRenderer.RenderCardList(context, cards);

        // Assert
        Assert.Equal("c    Related.\na/b  The child.", text);
        Assert.Equal("There are no help cards.", TextRenderer.RenderCardList(context, []));
    }

    /// <summary>
    ///     Proves that search results are written like a list, with commands in the command style, and that no results
    ///     name the query.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderSearch_Results_WritesLabelsAndSummaries()
    {
        // Arrange
        var catalog = Tree();
        IReadOnlyList<HelpSearchResult> results =
        [
            new("a/b", "B", "The child.", "test", 90, ["id", "title"]),
            new("c", "C", "Related.", "test", 40, ["summary"]),
        ];
        var plain = RenderContext.Create(catalog, null);
        var command = RenderContext.Create(catalog, new HelpRenderOptions { LinkStyle = HelpLinkStyle.Command });

        // Act / Assert
        Assert.Equal("a/b  The child.\nc    Related.", TextRenderer.RenderSearch(plain, "child", results));
        Assert.Equal("test help a b  The child.\ntest help c    Related.", TextRenderer.RenderSearch(command, "child", results));
        Assert.Equal("No help cards match 'zzz'.", TextRenderer.RenderSearch(plain, "zzz", []));
    }

    /// <summary>
    ///     Proves that null arguments of the list renderers are rejected.
    /// </summary>
    [Fact]
    public void TextRenderer_Lists_NullArguments_ThrowArgumentNullException()
    {
        // Arrange
        var context = RenderContext.Create(Tree(), null);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => TextRenderer.RenderIndex(null!, 0));
        Assert.Throws<ArgumentNullException>(() => TextRenderer.RenderCardList(context, null!));
        Assert.Throws<ArgumentNullException>(() => TextRenderer.RenderSearch(context, null!, []));
        Assert.Throws<ArgumentNullException>(() => TextRenderer.RenderSearch(context, "q", null!));
    }
}
