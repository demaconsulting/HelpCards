using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Presentation;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Presentation;

/// <summary>
///     Unit tests for <see cref="TextRenderer"/>: the card document in text and Markdown, the golden files of the
///     <c>simple</c> fixture, wrapping, sections, and link styles. The example documents and the lists are tested in
///     the other parts of this class.
/// </summary>
public partial class TextRendererTests
{
    /// <summary>
    ///     Creates a small tree of in-memory cards used by several tests: card <c>a</c> with aliases, tags, a body link,
    ///     a command example, and a child, and card <c>c</c> that refers to <c>a</c>.
    /// </summary>
    /// <returns>The catalog; its source and tool are named <c>test</c>.</returns>
    private static HelpCatalog Tree()
    {
        var a = SourceReading.Card(
            title: "A",
            summary: "The first card.",
            extraFrontMatter: "aliases:\n  - alpha\n  - first\ntags:\n  - t1\n  - t2\n"
                + "examples:\n  - name: run\n    kind: command\n    language: shell\n    content: tool run\n"
                + "    description: Runs it.\n",
            body: "Body of A. See [[a/b]].");
        return CatalogBuilding.FromFiles(
            ("a.md", a),
            ("a/b.md", SourceReading.Card(title: "B", summary: "The child.", body: "Child body.")),
            ("c.md", SourceReading.Card(title: "C", summary: "Related.", extraFrontMatter: "see-also:\n  - a\n")));
    }

    /// <summary>
    ///     Proves that the guide card of the simple fixture renders as the text golden file.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderCard_SimpleGuide_Text_EqualsGolden()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var context = RenderContext.Create(catalog, null);

        // Act
        var text = TextRenderer.RenderCard(context, catalog.Find("guide")!);

        // Assert
        GoldenFiles.AssertEqual("guide.text.expected.txt", text);
    }

    /// <summary>
    ///     Proves that the guide card renders as the Markdown golden file.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderCard_SimpleGuide_Markdown_EqualsGolden()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var context = RenderContext.Create(catalog, new HelpRenderOptions { Format = HelpFormat.Markdown });

        // Act
        var text = TextRenderer.RenderCard(context, catalog.Find("guide")!);

        // Assert
        GoldenFiles.AssertEqual("guide.markdown.expected.txt", text);
    }

    /// <summary>
    ///     Proves that the command link style renders the guide card as its golden file, with the two hint lines.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderCard_SimpleGuide_CommandStyle_EqualsGolden()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var context = RenderContext.Create(catalog, new HelpRenderOptions { LinkStyle = HelpLinkStyle.Command });

        // Act
        var text = TextRenderer.RenderCard(context, catalog.Find("guide")!);

        // Assert
        GoldenFiles.AssertEqual("guide.command.expected.txt", text);
    }

    /// <summary>
    ///     Proves that the Markdown link style renders the guide card as its golden file.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderCard_SimpleGuide_MarkdownLinkStyle_EqualsGolden()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var context = RenderContext.Create(
            catalog,
            new HelpRenderOptions { Format = HelpFormat.Markdown, LinkStyle = HelpLinkStyle.Markdown });

        // Act
        var text = TextRenderer.RenderCard(context, catalog.Find("guide")!);

        // Assert
        GoldenFiles.AssertEqual("guide.markdown-links.expected.txt", text);
    }

    /// <summary>
    ///     Proves that the example content is written in fences when it is asked for, as the golden file records.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderCard_SimpleGuide_ExampleContent_EqualsGolden()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var context = RenderContext.Create(
            catalog,
            new HelpRenderOptions { IncludeExampleContent = true, LinkStyle = HelpLinkStyle.Command });

        // Act
        var text = TextRenderer.RenderCard(context, catalog.Find("guide")!);

        // Assert
        GoldenFiles.AssertEqual("guide.content.expected.txt", text);
    }

    /// <summary>
    ///     Proves that a narrow width wraps the generated lines and leaves the authored body as it is.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderCard_SimpleGuide_NarrowWidth_EqualsGolden()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var context = RenderContext.Create(catalog, new HelpRenderOptions { Width = 30 });

        // Act
        var text = TextRenderer.RenderCard(context, catalog.Find("guide")!);

        // Assert
        GoldenFiles.AssertEqual("guide.narrow.expected.txt", text);
    }

    /// <summary>
    ///     Proves the exact layout of a card with aliases, tags, a body, children, and examples.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderCard_FullCard_WritesTheSectionsInOrder()
    {
        // Arrange
        var catalog = Tree();
        var context = RenderContext.Create(catalog, null);

        // Act
        var text = TextRenderer.RenderCard(context, catalog.Find("a")!);

        // Assert
        Assert.Equal(
            "# A\n\nThe first card.\nAlso known as: alpha, first\nTags: t1, t2\n\nBody of A. See the card `a/b`.\n\n"
            + "## Cards\n\n- a/b\n  The child.\n\n## Examples\n\n- run [command]: Runs it.",
            text);
    }

    /// <summary>
    ///     Proves that a card without aliases, tags, children, examples, and related cards has only the header and body.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderCard_PlainCard_OmitsEmptySections()
    {
        // Arrange
        var catalog = Tree();
        var context = RenderContext.Create(catalog, null);

        // Act
        var text = TextRenderer.RenderCard(context, catalog.Find("a/b")!);

        // Assert
        Assert.Equal("# B\n\nThe child.\n\nChild body.", text);
    }

    /// <summary>
    ///     Proves that a card without a body has no body block and no stray blank lines.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderCard_EmptyBody_WritesNoBodyBlock()
    {
        // Arrange
        var catalog = CatalogBuilding.FromFiles(("a.md", SourceReading.Card(title: "A", summary: "Summary.", body: string.Empty)));
        var context = RenderContext.Create(catalog, null);

        // Act
        var text = TextRenderer.RenderCard(context, catalog.Find("a")!);

        // Assert
        Assert.Equal("# A\n\nSummary.", text);
    }

    /// <summary>
    ///     Proves that the related cards are listed in a see-also section and that tags can be switched off.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderCard_SeeAlsoAndTagsOption_AreRespected()
    {
        // Arrange
        var catalog = Tree();
        var context = RenderContext.Create(catalog, new HelpRenderOptions { IncludeTags = false });

        // Act
        var related = TextRenderer.RenderCard(context, catalog.Find("c")!);
        var withoutTags = TextRenderer.RenderCard(context, catalog.Find("a")!);

        // Assert
        Assert.EndsWith("\n\n## See also\n\n- a\n  The first card.", related, StringComparison.Ordinal);
        Assert.DoesNotContain("Tags:", withoutTags, StringComparison.Ordinal);
        Assert.Contains("Also known as: alpha, first", withoutTags, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the Markdown format writes bold labels and does not wrap, with the layout of the text format.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderCard_Markdown_WritesBoldLabelsAndDoesNotWrap()
    {
        // Arrange
        var longSummary = string.Join(' ', Enumerable.Repeat("word", 40));
        var catalog = CatalogBuilding.FromFiles(
            ("a.md", SourceReading.Card(title: "A", summary: longSummary, extraFrontMatter: "aliases:\n  - one\ntags:\n  - t1\n")));
        var context = RenderContext.Create(catalog, new HelpRenderOptions { Format = HelpFormat.Markdown });

        // Act
        var text = TextRenderer.RenderCard(context, catalog.Find("a")!);

        // Assert
        Assert.Equal($"# A\n\n{longSummary}\n**Also known as:** one\n**Tags:** t1\n\nBody text.", text);
    }

    /// <summary>
    ///     Proves that generated lines wrap at the width with a hanging indent and the body is never reflowed.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderCard_Width_WrapsGeneratedLinesOnly()
    {
        // Arrange
        var body = "An authored line that is deliberately much longer than the width of twenty characters.";
        var catalog = CatalogBuilding.FromFiles(
            ("a.md", SourceReading.Card(
                title: "A",
                summary: "A summary that needs two lines.",
                extraFrontMatter: "aliases:\n  - alpha\n  - beta\n  - gamma\n",
                body: body)));
        var context = RenderContext.Create(catalog, new HelpRenderOptions { Width = 20 });

        // Act
        var text = TextRenderer.RenderCard(context, catalog.Find("a")!);

        // Assert
        Assert.Equal(
            $"# A\n\nA summary that needs\ntwo lines.\nAlso known as:\n  alpha, beta, gamma\n\n{body}",
            text);
    }

    /// <summary>
    ///     Proves that the command style writes the hint lines only when the card has examples.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderCard_CommandStyle_HintsOnlyWithExamples()
    {
        // Arrange
        var catalog = Tree();
        var context = RenderContext.Create(catalog, new HelpRenderOptions { LinkStyle = HelpLinkStyle.Command });

        // Act
        var withExamples = TextRenderer.RenderCard(context, catalog.Find("a")!);
        var without = TextRenderer.RenderCard(context, catalog.Find("a/b")!);

        // Assert
        Assert.EndsWith(
            "- run [command]: Runs it.\n\nPrint them in full with: test help a --examples, or one alone with: test help\n"
            + "  a --example <name>.",
            withExamples,
            StringComparison.Ordinal);
        Assert.DoesNotContain("Print", without, StringComparison.Ordinal);
        Assert.Contains("- test help a b\n  The child.", withExamples, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the fence of example content is one backtick longer than the longest run in the content.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderCard_ExampleContentWithBackticks_UsesALongerFence()
    {
        // Arrange: a snippet whose content holds a run of three backticks
        var card = SourceReading.Card(
            title: "A",
            summary: "Summary.",
            extraFrontMatter: "examples:\n  - name: fenced\n    kind: snippet\n    language: markdown\n    file: f.md.txt\n"
                + "    description: Holds a fence.\n");
        var catalog = CatalogBuilding.FromFiles(("a.md", card), ("f.md.txt", "Use ```code``` here"));
        var context = RenderContext.Create(catalog, new HelpRenderOptions { IncludeExampleContent = true });

        // Act
        var text = TextRenderer.RenderCard(context, catalog.Find("a")!);

        // Assert
        Assert.EndsWith("### fenced [snippet] f.md.txt\n\nHolds a fence.\n\n````markdown\nUse ```code``` here\n````", text, StringComparison.Ordinal);
        Assert.Equal("```", TextRenderer.CreateFence("plain"));
        Assert.Equal("`````", TextRenderer.CreateFence("a ```` b"));
    }

    /// <summary>
    ///     Proves that rendering has LF line endings, no trailing line feed, no tabs, and no escape characters.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderCard_EveryCardAndFormat_UsesLfOnlyAndNoTrailingNewline()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act / Assert
        foreach (var format in new[] { HelpFormat.Text, HelpFormat.Markdown })
        {
            var context = RenderContext.Create(catalog, new HelpRenderOptions { Format = format, LinkStyle = HelpLinkStyle.Command });
            foreach (var card in catalog.Cards)
            {
                var text = TextRenderer.RenderCard(context, card);
                Assert.DoesNotContain('\r', text);
                Assert.DoesNotContain('\u001b', text);
                Assert.False(text.EndsWith('\n'));
                Assert.All(text.Split('\n'), line => Assert.Equal(line.TrimEnd(), line));
            }
        }
    }

    /// <summary>
    ///     Proves that null arguments are rejected.
    /// </summary>
    [Fact]
    public void TextRenderer_RenderCard_NullArguments_ThrowArgumentNullException()
    {
        // Arrange
        var catalog = Tree();
        var context = RenderContext.Create(catalog, null);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => TextRenderer.RenderCard(null!, catalog.Find("a")!));
        Assert.Throws<ArgumentNullException>(() => TextRenderer.RenderCard(context, null!));
    }
}
