using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Presentation;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Presentation;

/// <summary>
///     Unit tests for <see cref="HelpRenderOptions"/>, <see cref="HelpFormat"/>, <see cref="HelpLinkStyle"/>, and the
///     internal <see cref="RenderContext"/> that resolves them for one call.
/// </summary>
public class HelpRenderOptionsTests
{
    /// <summary>
    ///     Proves that an options object without settings has the documented defaults.
    /// </summary>
    [Fact]
    public void HelpRenderOptions_Defaults_AreTheDocumentedValues()
    {
        // Arrange / Act
        var options = new HelpRenderOptions();

        // Assert
        Assert.Equal(HelpFormat.Text, options.Format);
        Assert.Equal(HelpLinkStyle.Plain, options.LinkStyle);
        Assert.Null(options.CommandTemplate);
        Assert.Null(options.LinkFormatter);
        Assert.Equal(78, options.Width);
        Assert.True(options.IncludeTags);
        Assert.False(options.IncludeExampleContent);
        Assert.Null(options.JsonEnvelope);
    }

    /// <summary>
    ///     Proves that every setting that is given is read back unchanged.
    /// </summary>
    [Fact]
    public void HelpRenderOptions_Initialization_StoresEverySetting()
    {
        // Arrange
        Func<HelpLink, string> formatter = link => link.CardId ?? string.Empty;
        Func<string, Action<System.Text.Json.Utf8JsonWriter>, string> envelope = (kind, _) => kind;

        // Act
        var options = new HelpRenderOptions
        {
            Format = HelpFormat.Json,
            LinkStyle = HelpLinkStyle.Markdown,
            CommandTemplate = "{tool} man {path}",
            LinkFormatter = formatter,
            Width = 40,
            IncludeTags = false,
            IncludeExampleContent = true,
            JsonEnvelope = envelope,
        };

        // Assert
        Assert.Equal(HelpFormat.Json, options.Format);
        Assert.Equal(HelpLinkStyle.Markdown, options.LinkStyle);
        Assert.Equal("{tool} man {path}", options.CommandTemplate);
        Assert.Same(formatter, options.LinkFormatter);
        Assert.Equal(40, options.Width);
        Assert.False(options.IncludeTags);
        Assert.True(options.IncludeExampleContent);
        Assert.Same(envelope, options.JsonEnvelope);
    }

    /// <summary>
    ///     Proves that zero and the two limits of the width range are accepted.
    /// </summary>
    [Fact]
    public void HelpRenderOptions_Width_AllowedValues_AreAccepted()
    {
        // Act / Assert: zero switches wrapping off, 20 and 500 are the limits of the range
        foreach (var width in new[] { 0, 20, 78, 500 })
        {
            Assert.Equal(width, new HelpRenderOptions { Width = width }.Width);
        }
    }

    /// <summary>
    ///     Proves that a width outside zero and the range of 20 to 500 is an argument error.
    /// </summary>
    [Fact]
    public void HelpRenderOptions_Width_OutOfRange_ThrowsArgumentException()
    {
        // Act / Assert: the range error is an argument error
        foreach (var width in new[] { -1, 1, 19, 501 })
        {
            Assert.ThrowsAny<ArgumentException>(() => new HelpRenderOptions { Width = width });
        }
    }

    /// <summary>
    ///     Proves that numbers that name no format or link style are rejected where they are set.
    /// </summary>
    [Fact]
    public void HelpRenderOptions_UndefinedEnumerationValues_ThrowArgumentException()
    {
        // Act / Assert
        Assert.ThrowsAny<ArgumentException>(() => new HelpRenderOptions { Format = (HelpFormat)7 });
        Assert.ThrowsAny<ArgumentException>(() => new HelpRenderOptions { LinkStyle = (HelpLinkStyle)7 });
    }

    /// <summary>
    ///     Proves that a command template of white space is rejected and that null selects the default.
    /// </summary>
    [Fact]
    public void HelpRenderOptions_CommandTemplate_WhiteSpace_ThrowsArgumentException()
    {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => new HelpRenderOptions { CommandTemplate = "  " });
        Assert.Null(new HelpRenderOptions { CommandTemplate = null }.CommandTemplate);
    }

    /// <summary>
    ///     Proves that the context takes the tool name from the builder, and otherwise from the first source.
    /// </summary>
    [Fact]
    public void RenderContext_ToolName_BuilderThenSourceName()
    {
        // Arrange
        var named = CatalogBuilding.Create(
            DemaConsulting.HelpCards.Loading.HelpSource.FromFiles(
                SourceReading.Files(("a.md", SourceReading.Card())),
                "lib"),
            builder => builder.ToolName("custom").ToolVersion("3.1"));
        var unnamed = CatalogBuilding.FromIds("a");

        // Act
        var fromBuilder = RenderContext.Create(named, null);
        var fromSource = RenderContext.Create(unnamed, null);

        // Assert
        Assert.Equal("custom", fromBuilder.ToolName);
        Assert.Equal("3.1", fromBuilder.ToolVersion);
        Assert.Equal("test", fromSource.ToolName);
        Assert.Null(fromSource.ToolVersion);
    }

    /// <summary>
    ///     Proves that Markdown never wraps and that the text format wraps at the width of the options.
    /// </summary>
    [Fact]
    public void RenderContext_WrapWidth_FollowsFormatAndWidth()
    {
        // Arrange
        var catalog = CatalogBuilding.FromIds("a");

        // Act
        var text = RenderContext.Create(catalog, new HelpRenderOptions { Width = 30 });
        var markdown = RenderContext.Create(catalog, new HelpRenderOptions { Format = HelpFormat.Markdown, Width = 30 });
        var unwrapped = RenderContext.Create(catalog, new HelpRenderOptions { Width = 0 });

        // Assert
        Assert.Equal(30, text.WrapWidth);
        Assert.Equal(0, markdown.WrapWidth);
        Assert.Equal(0, unwrapped.WrapWidth);
        Assert.True(markdown.IsMarkdown);
        Assert.False(text.UsesCommands);
    }

    /// <summary>
    ///     Proves that the context lists the visible cards depth first with their depth, down to a depth.
    /// </summary>
    [Fact]
    public void RenderContext_Flatten_ListsCardsDepthFirstDownToDepth()
    {
        // Arrange
        var context = RenderContext.Create(CatalogBuilding.Simple(), null);

        // Act
        var all = context.Flatten(int.MaxValue).Select(entry => (entry.Card.Id, entry.Depth)).ToList();
        var roots = context.Flatten(0).Select(entry => entry.Card.Id).ToList();

        // Assert
        Assert.Equal(
            [("guide", 0), ("guide/setup", 1), ("guide/usage", 1), ("reference", 0), ("reference/limits", 1)],
            all);
        Assert.Equal(["guide", "reference"], roots);
    }

    /// <summary>
    ///     Proves that the context finds the cards below a card that have examples of their own.
    /// </summary>
    [Fact]
    public void RenderContext_DescendantsWithExamples_ListsOnlyCardsWithExamples()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var context = RenderContext.Create(catalog, null);

        // Act
        var fromGuide = context.DescendantsWithExamples(catalog.Find("guide")!).Select(card => card.Id).ToList();
        var fromReference = context.DescendantsWithExamples(catalog.Find("reference")!);

        // Assert
        Assert.Equal(["guide/setup"], fromGuide);
        Assert.Empty(fromReference);
    }

    /// <summary>
    ///     Proves that the context maps a card of another view to the card of its own view and rejects hidden cards.
    /// </summary>
    [Fact]
    public void RenderContext_Own_OtherViewCard_IsMappedAndHiddenCardIsRejected()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var view = catalog.Without(["reference"]);
        var context = RenderContext.Create(view, null);

        // Act
        var own = context.Own(catalog.Find("guide")!);

        // Assert
        Assert.Same(view.Find("guide"), own);
        Assert.Throws<ArgumentException>(() => context.Own(catalog.Find("reference")!));
        Assert.Throws<ArgumentNullException>(() => context.Own(null!));
    }

    /// <summary>
    ///     Proves that an example is found in the subtree and that a missing example is an argument error.
    /// </summary>
    [Fact]
    public void RenderContext_RequireExample_FindsSubtreeExampleOrThrows()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();
        var context = RenderContext.Create(catalog, null);
        var guide = catalog.Find("guide")!;

        // Act
        var found = context.RequireExample(guide, "CHECK");

        // Assert
        Assert.Equal("guide/setup", found.CardId);
        Assert.Throws<ArgumentException>(() => context.RequireExample(guide, "missing"));
        Assert.Throws<ArgumentException>(() => context.RequireExample(guide, " "));
    }

    /// <summary>
    ///     Proves that creating a context without a catalog is an argument error.
    /// </summary>
    [Fact]
    public void RenderContext_Create_NullCatalog_ThrowsArgumentNullException()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => RenderContext.Create(null!, null));
    }
}
