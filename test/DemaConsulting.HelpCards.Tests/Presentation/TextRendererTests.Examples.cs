using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Presentation;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Presentation;

/// <summary>
///     The tests of the example documents of <see cref="TextRenderer"/>.
/// </summary>
public partial class TextRendererTests
{
    /// <summary>
    ///     Creates a tree in which a card has an example of its own and a child that has two examples.
    /// </summary>
    /// <returns>The catalog; its source and tool are named <c>test</c>.</returns>
    private static HelpCatalog ExampleTree()
    {
        var parent = SourceReading.Card(
            title: "Parent",
            summary: "The parent.",
            extraFrontMatter: "examples:\n  - name: own\n    kind: command\n    language: shell\n    content: tool own\n"
                + "    description: The own example.\n");
        var child = SourceReading.Card(
            title: "Child",
            summary: "The child.",
            extraFrontMatter: "examples:\n  - name: one\n    kind: command\n    language: shell\n    content: tool one\n"
                + "    description: First.\n  - name: two\n    kind: source\n    language: yaml\n    file: two.yaml\n"
                + "    description: Second.\n");
        return CatalogBuilding.FromFiles(
            ("parent.md", parent),
            ("parent/child.md", child),
            ("parent/two.yaml", "key: value\nother: 2"),
            ("empty.md", SourceReading.Card(title: "Empty", summary: "No examples.")));
    }

    /// <summary>
    ///     Proves that the examples document of the simple guide renders as its golden file in the command style.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderExamples_SimpleGuide_CommandStyle_EqualsGolden()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var context = RenderContext.Create(catalog, new HelpRenderOptions { LinkStyle = HelpLinkStyle.Command });

        // Act
        var text = TextRenderer.RenderExamples(context, catalog.Find("guide")!);

        // Assert
        GoldenFiles.AssertEqual("examples.guide.expected.txt", text);
    }

    /// <summary>
    ///     Proves the exact layout: the own examples in full, the hint for commands, and the cards below that have
    ///     examples with their names, without their contents.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderExamples_OwnExamplesAndDescendants_WritesTheLayout()
    {
        // Arrange
        var catalog = ExampleTree();
        var context = RenderContext.Create(catalog, new HelpRenderOptions { LinkStyle = HelpLinkStyle.Command });

        // Act
        var text = TextRenderer.RenderExamples(context, catalog.Find("parent")!);

        // Assert
        Assert.Equal(
            "# Parent: examples\n\n## own [command]\n\nThe own example.\n\n```shell\ntool own\n```\n\n"
            + "Print one alone with: test help parent --example <name>\n\n"
            + "## Cards below with examples\n\n- test help parent child --examples\n  Examples: one, two",
            text);
        Assert.DoesNotContain("key: value", text, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the plain style has no command hint and lists the descendants by id.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderExamples_PlainStyle_HasNoHintAndListsIds()
    {
        // Arrange
        var catalog = ExampleTree();
        var context = RenderContext.Create(catalog, null);

        // Act
        var text = TextRenderer.RenderExamples(context, catalog.Find("parent")!);

        // Assert
        Assert.Equal(
            "# Parent: examples\n\n## own [command]\n\nThe own example.\n\n```shell\ntool own\n```\n\n"
            + "## Cards below with examples\n\n- parent/child\n  Examples: one, two",
            text);
    }

    /// <summary>
    ///     Proves that a source example names its file in the heading and shows the content of the file.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderExamples_SourceExample_ShowsFileNameAndContent()
    {
        // Arrange
        var catalog = ExampleTree();
        var context = RenderContext.Create(catalog, new HelpRenderOptions { Format = HelpFormat.Markdown });

        // Act
        var text = TextRenderer.RenderExamples(context, catalog.Find("parent/child")!);

        // Assert
        Assert.Contains("## two [source] two.yaml\n\nSecond.\n\n```yaml\nkey: value\nother: 2\n```", text, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a card without examples of its own says so.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderExamples_NoOwnExamples_SaysSo()
    {
        // Arrange
        var catalog = ExampleTree();
        var context = RenderContext.Create(catalog, null);

        // Act
        var text = TextRenderer.RenderExamples(context, catalog.Find("empty")!);

        // Assert
        Assert.Equal("# Empty: examples\n\nThis card has no examples of its own.", text);
    }

    /// <summary>
    ///     Proves that one example is rendered as exactly its content for the text and Markdown formats.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderExample_TextAndMarkdown_ReturnExactlyTheContent()
    {
        // Arrange
        var catalog = ExampleTree();
        var parent = catalog.Find("parent")!;

        // Act
        var own = TextRenderer.RenderExample(RenderContext.Create(catalog, null), parent, "own");
        var below = TextRenderer.RenderExample(
            RenderContext.Create(catalog, new HelpRenderOptions { Format = HelpFormat.Markdown }),
            parent,
            "TWO");

        // Assert
        Assert.Equal("tool own", own);
        Assert.Equal("key: value\nother: 2", below);
    }

    /// <summary>
    ///     Proves that an example that does not exist is an argument error.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderExample_UnknownName_ThrowsArgumentException()
    {
        // Arrange
        var catalog = ExampleTree();
        var context = RenderContext.Create(catalog, null);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => TextRenderer.RenderExample(context, catalog.Find("empty")!, "own"));
        Assert.Throws<ArgumentNullException>(() => TextRenderer.RenderExamples(context, null!));
        Assert.Throws<ArgumentNullException>(() => TextRenderer.RenderExamples(null!, catalog.Find("empty")!));
    }
}
