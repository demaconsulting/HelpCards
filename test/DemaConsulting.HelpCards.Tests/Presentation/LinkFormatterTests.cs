using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Presentation;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Presentation;

/// <summary>
///     Unit tests for <see cref="LinkFormatter"/>: the three link styles, the command template, card labels, and the
///     callback of the caller.
/// </summary>
public class LinkFormatterTests
{
    /// <summary>
    ///     The card id that contains the links of the tests.
    /// </summary>
    private const string Containing = "guide";

    /// <summary>
    ///     Creates a context over a catalog whose tool is named <c>tool</c>.
    /// </summary>
    /// <param name="options">The options of the call.</param>
    /// <returns>The context.</returns>
    private static RenderContext Context(HelpRenderOptions? options)
    {
        var catalog = CatalogBuilding.Create(
            HelpSource.FromFiles(SourceReading.Files(("a.md", SourceReading.Card())), "lib"),
            builder => builder.ToolName("tool"));
        return RenderContext.Create(catalog, options);
    }

    /// <summary>
    ///     Creates the link to a card.
    /// </summary>
    /// <returns>The link to <c>a/b</c> titled <c>Bee</c>.</returns>
    private static HelpLink CardLink() => new("a/b", null, null, "Bee", null, null);

    /// <summary>
    ///     Creates the link to an example of another card.
    /// </summary>
    /// <returns>The link to the example <c>x</c> of <c>a/b</c>.</returns>
    private static HelpLink ExampleLink() => new("a/b", "x", "a/b", "Bee", null, null);

    /// <summary>
    ///     Creates the short link to an example of the containing card.
    /// </summary>
    /// <returns>The link <c>[[#x]]</c> whose example belongs to the containing card.</returns>
    private static HelpLink LocalLink() => new(null, "x", Containing, "Guide", null, null);

    /// <summary>
    ///     Creates the external link to a card of a source.
    /// </summary>
    /// <returns>The link to <c>s/a/b</c> with the display text <c>the text</c>.</returns>
    private static HelpLink ExternalLink() => new("s/a/b", null, null, "Bee", "the text", "s");

    /// <summary>
    ///     Proves that the plain style names a card, an example, a local example, and an external card in words.
    /// </summary>
    [Fact]
    public void LinkFormatter_FormatLink_PlainStyle_WritesWords()
    {
        // Arrange
        var context = Context(null);

        // Act / Assert
        Assert.Equal("the card `a/b`", LinkFormatter.FormatLink(context, CardLink(), Containing));
        Assert.Equal("the example `x` of the card `a/b`", LinkFormatter.FormatLink(context, ExampleLink(), Containing));
        Assert.Equal("the example `x`", LinkFormatter.FormatLink(context, LocalLink(), Containing));
        Assert.Equal("the card `s/a/b`", LinkFormatter.FormatLink(context, ExternalLink(), Containing));
    }

    /// <summary>
    ///     Proves that the command style writes the command that prints the target.
    /// </summary>
    [Fact]
    public void LinkFormatter_FormatLink_CommandStyle_WritesCommands()
    {
        // Arrange
        var context = Context(new HelpRenderOptions { LinkStyle = HelpLinkStyle.Command });

        // Act / Assert
        Assert.Equal("`tool help a b`", LinkFormatter.FormatLink(context, CardLink(), Containing));
        Assert.Equal("`tool help a b --example x`", LinkFormatter.FormatLink(context, ExampleLink(), Containing));
        Assert.Equal("`--example x`", LinkFormatter.FormatLink(context, LocalLink(), Containing));
        Assert.Equal("`tool help s a b`", LinkFormatter.FormatLink(context, ExternalLink(), Containing));
    }

    /// <summary>
    ///     Proves that the Markdown style writes Markdown links, with the display text of an external link.
    /// </summary>
    [Fact]
    public void LinkFormatter_FormatLink_MarkdownStyle_WritesMarkdownLinks()
    {
        // Arrange
        var context = Context(new HelpRenderOptions { LinkStyle = HelpLinkStyle.Markdown });

        // Act / Assert
        Assert.Equal("[Bee](a/b)", LinkFormatter.FormatLink(context, CardLink(), Containing));
        Assert.Equal("[x of Bee](a/b#x)", LinkFormatter.FormatLink(context, ExampleLink(), Containing));
        Assert.Equal("[x](#x)", LinkFormatter.FormatLink(context, LocalLink(), Containing));
        Assert.Equal("[the text](s/a/b)", LinkFormatter.FormatLink(context, ExternalLink(), Containing));
    }

    /// <summary>
    ///     Proves that a local example of another card than the containing one is written as a full example link.
    /// </summary>
    [Fact]
    public void LinkFormatter_FormatLink_LocalExampleOfOtherCard_UsesTheExampleForm()
    {
        // Arrange: the example of the link belongs to a card below the containing card
        var link = new HelpLink(null, "x", "guide/setup", "Setup", null, null);

        // Act / Assert
        Assert.Equal(
            "the example `x` of the card `guide/setup`",
            LinkFormatter.FormatLink(Context(null), link, Containing));
        Assert.Equal(
            "`tool help guide setup --example x`",
            LinkFormatter.FormatLink(Context(new HelpRenderOptions { LinkStyle = HelpLinkStyle.Command }), link, Containing));
        Assert.Equal(
            "[x of Setup](guide/setup#x)",
            LinkFormatter.FormatLink(Context(new HelpRenderOptions { LinkStyle = HelpLinkStyle.Markdown }), link, Containing));
    }

    /// <summary>
    ///     Proves that a callback replaces every style and receives the link it is asked to write.
    /// </summary>
    [Fact]
    public void LinkFormatter_FormatLink_Callback_OverridesLinkStyle()
    {
        // Arrange
        var seen = new List<HelpLink>();
        var context = Context(new HelpRenderOptions
        {
            LinkStyle = HelpLinkStyle.Command,
            LinkFormatter = link =>
            {
                seen.Add(link);
                return $"<{link.CardId}>";
            },
        });
        var link = CardLink();

        // Act
        var text = LinkFormatter.FormatLink(context, link, Containing);

        // Assert
        Assert.Equal("<a/b>", text);
        Assert.Same(link, Assert.Single(seen));
    }

    /// <summary>
    ///     Proves that a callback that returns null is written as empty text, for a link and for a label.
    /// </summary>
    [Fact]
    public void LinkFormatter_FormatLinkAndLabel_CallbackReturnsNull_WritesEmptyText()
    {
        // Arrange
        var context = Context(new HelpRenderOptions { LinkFormatter = _ => null! });

        // Act
        var link = LinkFormatter.FormatLink(context, CardLink(), Containing);
        var label = LinkFormatter.FormatLabel(context, "a/b", "Bee", "lib");

        // Assert
        Assert.Equal(string.Empty, link);
        Assert.Equal(string.Empty, label);
    }

    /// <summary>
    ///     Proves that the command template is filled in with the tool and the path with spaces for slashes.
    /// </summary>
    [Fact]
    public void LinkFormatter_Command_Template_ReplacesToolAndPath()
    {
        // Arrange
        var defaultContext = Context(null);
        var custom = Context(new HelpRenderOptions { CommandTemplate = "{tool} --man {path} --plain" });

        // Act / Assert
        Assert.Equal("tool help a b c", LinkFormatter.Command(defaultContext, "a/b/c"));
        Assert.Equal("tool --man a b --plain", LinkFormatter.Command(custom, "a/b"));
    }

    /// <summary>
    ///     Proves that card labels are the id, the command, a Markdown link, or what the callback writes.
    /// </summary>
    [Fact]
    public void LinkFormatter_FormatLabel_EveryStyleAndCallback_WritesTheLabel()
    {
        // Arrange
        HelpLink? seen = null;
        var callback = Context(new HelpRenderOptions
        {
            LinkFormatter = link =>
            {
                seen = link;
                return "custom";
            },
        });

        // Act / Assert
        Assert.Equal("a/b", LinkFormatter.FormatLabel(Context(null), "a/b", "Bee", "s"));
        Assert.Equal(
            "tool help a b",
            LinkFormatter.FormatLabel(Context(new HelpRenderOptions { LinkStyle = HelpLinkStyle.Command }), "a/b", "Bee", "s"));
        Assert.Equal(
            "[Bee](a/b)",
            LinkFormatter.FormatLabel(Context(new HelpRenderOptions { LinkStyle = HelpLinkStyle.Markdown }), "a/b", "Bee", "s"));
        Assert.Equal("custom", LinkFormatter.FormatLabel(callback, "a/b", "Bee", "s"));
        Assert.Equal(("a/b", "Bee", "s"), (seen!.CardId, seen.Title, seen.SourceName));
    }

    /// <summary>
    ///     Proves that the body of the simple card is written in the command style from its parsed pieces.
    /// </summary>
    [Fact]
    public void LinkFormatter_FormatBody_CommandStyle_ReplacesEveryVisibleLink()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var context = RenderContext.Create(catalog, new HelpRenderOptions { LinkStyle = HelpLinkStyle.Command });

        // Act
        var body = LinkFormatter.FormatBody(context, catalog.Find("guide")!);

        // Assert: local example, example link, external card, and absent external text
        Assert.Contains("Start with `simple help guide setup` and then read `simple help guide usage`.", body, StringComparison.Ordinal);
        Assert.Contains("Run it with `--example run` or see `simple help guide --example settings` for", body, StringComparison.Ordinal);
        Assert.Contains("`simple help reference limits`", body, StringComparison.Ordinal);
        Assert.Contains("the thing", body, StringComparison.Ordinal);
        Assert.DoesNotContain("[[other", body, StringComparison.Ordinal);
        Assert.Contains("`[[x]]`", body, StringComparison.Ordinal);
        Assert.DoesNotContain("[[guide", body, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the plain style of the formatter equals the body that the card writes for itself.
    /// </summary>
    [Fact]
    public void LinkFormatter_FormatBody_PlainStyle_EqualsCardBody()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var context = RenderContext.Create(catalog, null);

        // Act / Assert: every card of the fixture, so every kind of link is covered
        foreach (var card in catalog.Cards)
        {
            Assert.Equal(card.Body, LinkFormatter.FormatBody(context, card));
        }
    }

    /// <summary>
    ///     Proves that null arguments are rejected.
    /// </summary>
    [Fact]
    public void LinkFormatter_NullArguments_ThrowArgumentNullException()
    {
        // Arrange
        var context = Context(null);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => LinkFormatter.FormatBody(null!, null!));
        Assert.Throws<ArgumentNullException>(() => LinkFormatter.FormatBody(context, null!));
        Assert.Throws<ArgumentNullException>(() => LinkFormatter.FormatLink(context, null!, Containing));
        Assert.Throws<ArgumentNullException>(() => LinkFormatter.FormatLabel(null!, "a", "A", "s"));
        Assert.Throws<ArgumentNullException>(() => LinkFormatter.Command(context, null!));
    }
}
