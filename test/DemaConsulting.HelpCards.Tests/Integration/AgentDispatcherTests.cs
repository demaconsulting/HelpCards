using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Integration;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Integration;

/// <summary>
///     Unit tests for <see cref="AgentDispatcher"/>: which answer a call gets, and what the answers say when nothing,
///     or too much, was found.
/// </summary>
public class AgentDispatcherTests
{
    /// <summary>
    ///     Creates the tool over the simple fixture.
    /// </summary>
    /// <returns>The tool.</returns>
    private static HelpAgentTool SimpleTool()
    {
        return HelpAgentTools.Create(CatalogBuilding.Simple());
    }

    /// <summary>
    ///     Creates the tool over the twelve-card fixture.
    /// </summary>
    /// <param name="options">The options, or <see langword="null"/>.</param>
    /// <returns>The tool.</returns>
    private static HelpAgentTool BigTool(HelpAgentToolOptions? options = null)
    {
        return HelpAgentTools.Create(CatalogBuilding.Create(CatalogBuilding.Fixture("big")), options);
    }

    /// <summary>
    ///     Creates a catalog with two mounted sources that each have a card <c>x</c> and an example <c>go</c>.
    /// </summary>
    /// <returns>The catalog.</returns>
    private static HelpCatalog TwoMounts()
    {
        const string example = "examples:\n  - name: go\n    kind: command\n    language: shell\n    content: run {0}\n    description: Go.\n";
        HelpSource Make(string name) => HelpSource.FromFiles(
            SourceReading.Files(("x.md", SourceReading.Card(title: "X of " + name, extraFrontMatter: string.Format(System.Globalization.CultureInfo.InvariantCulture, example, name)))),
            name);
        return HelpCatalog.Create(builder => builder.AddMountedSource(Make("one")).AddMountedSource(Make("two")));
    }

    /// <summary>
    ///     Proves that every way of asking for nothing gives the same index, with the depth of the options.
    /// </summary>
    [Fact]
    public void AgentDispatcher_Answer_NoArguments_IsTheIndexToTheConfiguredDepth()
    {
        // Arrange
        var shallow = HelpAgentTools.Create(CatalogBuilding.Simple(), new HelpAgentToolOptions { IndexDepth = 0 });
        var deep = HelpAgentTools.Create(CatalogBuilding.Simple(), new HelpAgentToolOptions { IndexDepth = 1 });

        // Act
        var roots = shallow.Invoke(null);
        var withChildren = deep.Invoke("{}");

        // Assert
        Assert.StartsWith("Help index. Read a card with the topic argument", roots, StringComparison.Ordinal);
        Assert.Contains("guide  ", roots, StringComparison.Ordinal);
        Assert.DoesNotContain("guide/setup", roots, StringComparison.Ordinal);
        Assert.Contains("  guide/setup", withChildren, StringComparison.Ordinal);
        Assert.Equal(roots, shallow.Invoke("null"));
    }

    /// <summary>
    ///     Proves that several sources give one block each in the index, and that one source or tag restricts it to one
    ///     list.
    /// </summary>
    [Fact]
    public void AgentDispatcher_Answer_Index_ManySourcesAreBlocksAndRestrictionsAreOneList()
    {
        // Arrange
        var catalog = HelpCatalog.Create(builder => builder.AddSource(CatalogBuilding.Fixture("big")).AddSource(CatalogBuilding.LibA()));
        var tool = HelpAgentTools.Create(catalog);

        // Act
        var all = tool.Invoke(null);
        var source = tool.Invoke("{\"source\":\"LIB-A\"}");
        var tag = tool.Invoke("{\"tag\":\"quality\"}");

        // Assert
        Assert.Contains("## Big fixture\n\nA source of twelve cards for search tests.\n\n", all, StringComparison.Ordinal);
        Assert.Contains("## Library A\n\nGraph and shape cards", all, StringComparison.Ordinal);
        Assert.DoesNotContain("##", source, StringComparison.Ordinal);
        Assert.Contains("graph", source, StringComparison.Ordinal);
        Assert.DoesNotContain("deploy", source, StringComparison.Ordinal);
        Assert.Contains("test/coverage", tag, StringComparison.Ordinal);
        Assert.DoesNotContain("deploy", tag, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that an unknown source or tag in an index or a search says which names are valid.
    /// </summary>
    [Fact]
    public void AgentDispatcher_Answer_UnknownSourceOrTag_ListsTheValidOnes()
    {
        // Arrange
        var tool = BigTool();

        // Act
        var source = tool.Invoke("{\"source\":\"nope\"}");
        var tag = tool.Invoke("{\"tag\":\"nope\"}");
        var searchSource = tool.Invoke("{\"query\":\"x\",\"source\":\"nope\"}");
        var searchTag = tool.Invoke("{\"query\":\"x\",\"tag\":\"nope\"}");

        // Assert
        Assert.Equal("Unknown source 'nope'. Valid sources: big.", source);
        Assert.Equal("Unknown tag 'nope'. Valid tags: build, quality, release.", tag);
        Assert.Equal(source, searchSource);
        Assert.Equal(tag, searchTag);
    }

    /// <summary>
    ///     Proves that a tag in a catalog that has no tags at all is answered with a message, not with an empty list.
    /// </summary>
    [Fact]
    public void AgentDispatcher_Answer_TagInCatalogWithoutTags_SaysThereAreNone()
    {
        // Arrange
        var tool = HelpAgentTools.Create(CatalogBuilding.FromIds("a", "b"));

        // Act
        var index = tool.Invoke("{\"tag\":\"x\"}");
        var search = tool.Invoke("{\"tag\":\"x\",\"query\":\"a\"}");

        // Assert
        Assert.Equal("No cards have the tag 'x'; this help has no tags.", index);
        Assert.Equal(index, search);
    }

    /// <summary>
    ///     Proves that a topic gives the card in Markdown with plain links and the content of its examples.
    /// </summary>
    [Fact]
    public void AgentDispatcher_Answer_Topic_IsTheMarkdownCardWithExampleContent()
    {
        // Arrange
        var tool = SimpleTool();

        // Act
        var answer = tool.Invoke("{\"topic\":\"guide\"}");

        // Assert
        Assert.StartsWith("# Guide\n\nHow to set up and use the tool.\n**Also known as:** start\n**Tags:** tutorial, authoring\n", answer, StringComparison.Ordinal);
        Assert.Contains("Start with the card `guide/setup` and then read the card `guide/usage`.", answer, StringComparison.Ordinal);
        Assert.Contains("### run [command]", answer, StringComparison.Ordinal);
        Assert.Contains("```shell\nsimple run input.txt\n```", answer, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a topic may be a path, an alias, in any case, and with spaces.
    /// </summary>
    [Theory]
    [InlineData("guide/setup")]
    [InlineData("GUIDE/Setup")]
    [InlineData("start setup")]
    [InlineData("  guide setup  ")]
    public void AgentDispatcher_Answer_TopicForms_ResolveTheSameCard(string topic)
    {
        // Arrange
        var tool = SimpleTool();

        // Act
        var answer = tool.Invoke($"{{\"topic\":\"{topic}\"}}");

        // Assert
        Assert.StartsWith("# Setup\n", answer, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that an unqualified local id of a mounted source is found when it is unambiguous and that an ambiguous
    ///     one lists the global ids.
    /// </summary>
    [Fact]
    public void AgentDispatcher_Answer_LocalId_IsFoundWhenUnambiguousAndListedWhenNot()
    {
        // Arrange
        var mounted = HelpAgentTools.Create(HelpCatalog.Create(builder => builder.AddMountedSource(CatalogBuilding.Fixture("big"), "lib")));
        var two = HelpAgentTools.Create(TwoMounts());

        // Act
        var found = mounted.Invoke("{\"topic\":\"build/clean\"}");
        var global = mounted.Invoke("{\"topic\":\"lib/build/clean\"}");
        var ambiguous = two.Invoke("{\"topic\":\"x\"}");
        var qualified = two.Invoke("{\"topic\":\"one/x\"}");

        // Assert
        Assert.StartsWith("# Clean\n", found, StringComparison.Ordinal);
        Assert.Equal(found, global);
        Assert.Equal("The topic 'x' matches several cards: one/x, two/x. Ask for one of these ids.", ambiguous);
        Assert.StartsWith("# X of one\n", qualified, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that an unknown topic gets up to five suggestions and a pointer to the index.
    /// </summary>
    [Fact]
    public void AgentDispatcher_Answer_UnknownTopic_SuggestsAndPointsToTheIndex()
    {
        // Arrange
        var tool = BigTool();

        // Act
        var close = tool.Invoke("{\"topic\":\"built\"}");
        var far = tool.Invoke("{\"topic\":\"qqqqqqqq\"}");

        // Assert
        Assert.StartsWith("No help card matches 'built'. Did you mean: build", close, StringComparison.Ordinal);
        Assert.EndsWith(" Call with no arguments for the index.", close, StringComparison.Ordinal);
        Assert.Equal("No help card matches 'qqqqqqqq'. Call with no arguments for the index.", far);
    }

    /// <summary>
    ///     Proves that one example is answered with its description line and its content in a fence, and that a longer
    ///     fence protects content that has backticks.
    /// </summary>
    [Fact]
    public void AgentDispatcher_Answer_Example_IsTheDescriptionAndAFencedBlock()
    {
        // Arrange
        const string front = "examples:\n  - name: ticks\n    kind: snippet\n    language: markdown\n    file: ticks.txt\n    content: |\n      Use ``` to fence.\n    description: Shows a fence.\n";
        var ticks = HelpAgentTools.Create(CatalogBuilding.FromFiles(("t.md", SourceReading.Card(title: "T", extraFrontMatter: front))));
        var tool = SimpleTool();

        // Act
        var plain = tool.Invoke("{\"example\":\"run\"}");
        var file = tool.Invoke("{\"example\":\"settings\"}");
        var fenced = ticks.Invoke("{\"example\":\"ticks\"}");

        // Assert
        Assert.Equal("Example `run` of `guide` (command, shell): Run the tool on a file.\n\n```shell\nsimple run input.txt\n```", plain);
        Assert.StartsWith("Example `settings` of `guide` (source, yaml, guide/settings.yaml): A settings file with the limit.\n\n```yaml\n", file, StringComparison.Ordinal);
        Assert.Contains("````markdown\nUse ``` to fence.\n````", fenced, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the topic limits an example search to its card and the cards below it, and that the qualified name
    ///     works without a topic.
    /// </summary>
    [Fact]
    public void AgentDispatcher_Answer_ExampleWithTopic_SearchesTheSubtreeOnly()
    {
        // Arrange
        var tool = SimpleTool();

        // Act
        var below = tool.Invoke("{\"example\":\"check\",\"topic\":\"guide\"}");
        var outside = tool.Invoke("{\"example\":\"check\",\"topic\":\"reference\"}");
        var qualified = tool.Invoke("{\"example\":\"guide/setup#check\"}");
        var unknownTopic = tool.Invoke("{\"example\":\"check\",\"topic\":\"nope\"}");

        // Assert
        Assert.StartsWith("Example `check` of `guide/setup` (command, shell):", below, StringComparison.Ordinal);
        Assert.StartsWith("No example named 'check' was found under 'reference'.", outside, StringComparison.Ordinal);
        Assert.Equal(below, qualified);
        Assert.StartsWith("No help card matches 'nope'.", unknownTopic, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a missing example gets the closest example names, or card names when no example name is close.
    /// </summary>
    [Fact]
    public void AgentDispatcher_Answer_MissingExample_SuggestsClosestNames()
    {
        // Arrange
        var tool = SimpleTool();

        // Act
        var close = tool.Invoke("{\"example\":\"setting\"}");
        var card = tool.Invoke("{\"example\":\"guided\"}");
        var none = tool.Invoke("{\"example\":\"qqqqqqqq\"}");

        // Assert
        Assert.Equal("No example named 'setting' was found. Did you mean: guide#settings? Search with the query argument to find examples.", close);
        Assert.Equal("No example named 'guided' was found. Did you mean: guide? Search with the query argument to find examples.", card);
        Assert.Equal("No example named 'qqqqqqqq' was found. Search with the query argument to find examples.", none);
    }

    /// <summary>
    ///     Proves that an example name that several cards have lists the qualified names to choose from, and that a
    ///     qualified name chooses one.
    /// </summary>
    [Fact]
    public void AgentDispatcher_Answer_ExampleInSeveralCards_ListsTheChoices()
    {
        // Arrange
        var tool = HelpAgentTools.Create(TwoMounts());

        // Act
        var several = tool.Invoke("{\"example\":\"go\"}");
        var chosen = tool.Invoke("{\"example\":\"two/x#go\"}");

        // Assert
        Assert.Equal("The example name 'go' matches several examples. Use one of: one/x#go, two/x#go.", several);
        Assert.Contains("run two", chosen, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a topic that owns an example name answers with its own example although a card below it uses
    ///     the same name.
    /// </summary>
    [Fact]
    public void AgentDispatcher_Example_TopicOwnsName_ReturnsOwn()
    {
        // Arrange: the parent and its child both have an example go
        const string example = "examples:\n  - name: go\n    kind: command\n    language: shell\n    content: run {0}\n    description: Go.\n";
        var catalog = CatalogBuilding.FromFiles(
            ("p.md", SourceReading.Card(title: "P", extraFrontMatter: string.Format(System.Globalization.CultureInfo.InvariantCulture, example, "parent"))),
            ("p/c.md", SourceReading.Card(title: "C", extraFrontMatter: string.Format(System.Globalization.CultureInfo.InvariantCulture, example, "child"))));
        var tool = HelpAgentTools.Create(catalog);

        // Act
        var own = tool.Invoke("{\"example\":\"go\",\"topic\":\"p\"}");
        var child = tool.Invoke("{\"example\":\"go\",\"topic\":\"p/c\"}");
        var all = tool.Invoke("{\"example\":\"go\"}");

        // Assert
        Assert.StartsWith("Example `go` of `p` (command, shell):", own, StringComparison.Ordinal);
        Assert.Contains("run parent", own, StringComparison.Ordinal);
        Assert.Contains("run child", child, StringComparison.Ordinal);
        Assert.Equal("The example name 'go' matches several examples. Use one of: p#go, p/c#go.", all);
    }

    /// <summary>
    ///     Proves that a query lists the best cards, limited by the options and by <c>maxResults</c>, and that a query
    ///     without a match says so.
    /// </summary>
    [Fact]
    public void AgentDispatcher_Answer_Query_ListsTheBestCardsUpToTheLimit()
    {
        // Arrange
        var tool = BigTool();
        var small = BigTool(new HelpAgentToolOptions { MaxResults = 2 });

        // Act
        var answer = tool.Invoke("{\"query\":\"build\"}");
        var two = tool.Invoke("{\"query\":\"the\",\"maxResults\":2}");
        var byOptions = small.Invoke("{\"query\":\"the\"}");
        var byOverride = small.Invoke("{\"query\":\"the\",\"maxResults\":3}");
        var none = tool.Invoke("{\"query\":\"zebra\"}");

        // Assert
        Assert.StartsWith("Cards matching 'build':\n\nbuild ", answer, StringComparison.Ordinal);
        Assert.EndsWith("\n\nRead one with the topic argument.", answer, StringComparison.Ordinal);
        Assert.Equal(4, answer.Split('\n').Count(line => line.StartsWith("build", StringComparison.Ordinal)));
        Assert.Equal(2, CountResults(two));
        Assert.Equal(2, CountResults(byOptions));
        Assert.Equal(3, CountResults(byOverride));
        Assert.Equal("No cards match 'zebra'. Call with no arguments for the index.", none);
    }

    /// <summary>
    ///     Counts the result lines of a search answer.
    /// </summary>
    /// <param name="answer">The answer.</param>
    /// <returns>The number of results.</returns>
    private static int CountResults(string answer)
    {
        return answer.Split('\n').Skip(2).TakeWhile(line => line.Length > 0).Count();
    }

    /// <summary>
    ///     Proves that a search is restricted by source and tag.
    /// </summary>
    [Fact]
    public void AgentDispatcher_Answer_QueryWithSourceAndTag_IsRestricted()
    {
        // Arrange
        var catalog = HelpCatalog.Create(builder => builder.AddSource(CatalogBuilding.Fixture("big")).AddSource(CatalogBuilding.LibA()));
        var tool = HelpAgentTools.Create(catalog);

        // Act
        var tagged = tool.Invoke("{\"query\":\"program\",\"tag\":\"release\"}");
        var other = tool.Invoke("{\"query\":\"program\",\"source\":\"lib-a\"}");
        var graph = tool.Invoke("{\"query\":\"graph\",\"source\":\"LIB-A\"}");

        // Assert
        Assert.Contains("deploy/production", tagged, StringComparison.Ordinal);
        Assert.DoesNotContain("test/coverage", tagged, StringComparison.Ordinal);
        Assert.Equal("No cards match 'program'. Call with no arguments for the index.", other);
        Assert.StartsWith("Cards matching 'graph':\n\ngraph", graph, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a topic wins over a query and an example wins over both, and that the answer says what was
    ///     ignored in one last line.
    /// </summary>
    [Fact]
    public void AgentDispatcher_Answer_Conflicts_NameTheIgnoredQuery()
    {
        // Arrange
        var tool = SimpleTool();

        // Act
        var topic = tool.Invoke("{\"topic\":\"guide/setup\",\"query\":\"usage\"}");
        var example = tool.Invoke("{\"example\":\"run\",\"query\":\"usage\"}");
        var all = tool.Invoke("{\"example\":\"run\",\"topic\":\"guide\",\"query\":\"usage\"}");
        var noConflict = tool.Invoke("{\"topic\":\"guide/setup\",\"source\":\"simple\"}");

        // Assert
        Assert.StartsWith("# Setup\n", topic, StringComparison.Ordinal);
        Assert.EndsWith("\nIgnored 'query' because 'topic' was given.", topic, StringComparison.Ordinal);
        Assert.EndsWith("\nIgnored 'query' because 'example' was given.", example, StringComparison.Ordinal);
        Assert.StartsWith("Example `run`", all, StringComparison.Ordinal);
        Assert.EndsWith("\nIgnored 'query' because 'example' was given.", all, StringComparison.Ordinal);
        Assert.DoesNotContain("Ignored", noConflict, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that argument errors are answered as the table says, through the whole tool.
    /// </summary>
    [Fact]
    public void AgentDispatcher_Answer_ArgumentErrors_AreAnsweredAsText()
    {
        // Arrange
        var tool = SimpleTool();

        // Act / Assert
        Assert.StartsWith("The arguments are not valid JSON (", tool.Invoke("{"), StringComparison.Ordinal);
        Assert.Equal("The arguments must be a JSON object.", tool.Invoke("[1]"));
        Assert.Equal("Unknown argument 'shape'. Valid arguments: topic, example, query, source, tag, maxResults.", tool.Invoke("{\"shape\":1}"));
        Assert.Equal("The argument 'topic' must be a string.", tool.Invoke("{\"topic\":1}"));
        Assert.Equal("The argument 'maxResults' must be an integer from 1 to 20.", tool.Invoke("{\"maxResults\":0}"));
    }
}
