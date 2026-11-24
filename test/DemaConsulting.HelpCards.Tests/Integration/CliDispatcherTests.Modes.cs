using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Integration;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Integration;

/// <summary>
///     Unit tests for the answers of the modes of <see cref="CliDispatcher"/>, compared with golden files where the
///     whole document matters.
/// </summary>
public partial class CliDispatcherTests
{
    /// <summary>
    ///     Creates a harness over the twelve-card fixture.
    /// </summary>
    /// <returns>The harness.</returns>
    private static CliHarness BigHarness()
    {
        return new CliHarness(CatalogBuilding.Create(CatalogBuilding.Fixture("big")));
    }

    /// <summary>
    ///     Proves that the index lists the usage, the preamble, the sections, the roots, and the footer in text, and
    ///     matches the golden file.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_Index_WritesUsagePreambleSectionsTopicsAndFooter()
    {
        // Arrange
        var harness = new CliHarness(
            CatalogBuilding.Simple(),
            usage: ["tool [options]", "tool help [PATH]"],
            preamble: "Tool does things.",
            sections: [new HelpCliSection("Commands:", ["build  Build it"])]);

        // Act
        var code = harness.Run();

        // Assert
        Assert.Equal(0, code);
        GoldenFiles.AssertEqual("cli", "index.text.expected.txt", harness.Output.ToString().TrimEnd('\n'));
        Assert.Empty(harness.Error.ToString());
    }

    /// <summary>
    ///     Proves that the index without usage, preamble, or sections is the topics and the footer only.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_IndexWithoutExtras_IsTopicsAndFooter()
    {
        // Arrange
        var harness = new CliHarness(CatalogBuilding.Simple());

        // Act
        harness.Run();

        // Assert
        Assert.StartsWith("Help topics:\ntool help guide ", harness.Output.ToString(), StringComparison.Ordinal);
        Assert.EndsWith("\n\nRun 'tool help' for topics and examples.\n", harness.Output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("Usage:", harness.Output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that <c>--all</c> lists the whole tree and matches the golden file.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_IndexAll_ListsTheWholeTree()
    {
        // Arrange
        var harness = new CliHarness(CatalogBuilding.Simple());

        // Act
        harness.Run("--all");

        // Assert
        GoldenFiles.AssertEqual("cli", "index.all.expected.txt", harness.Output.ToString().TrimEnd('\n'));
    }

    /// <summary>
    ///     Proves that the JSON index carries the usage lines of the tool and matches the golden file.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_IndexInJson_CarriesUsageLinesAndMatchesGolden()
    {
        // Arrange
        var harness = new CliHarness(CatalogBuilding.Simple(), usage: ["tool [options]", "tool help [PATH]"]);

        // Act
        var code = harness.Run("--json");
        var without = new CliHarness(CatalogBuilding.Simple());
        without.Run("--format", "json");

        // Assert
        Assert.Equal(0, code);
        GoldenFiles.AssertEqual("cli", "index.expected.json", harness.Output.ToString().TrimEnd('\n'));
        Assert.DoesNotContain("\"usage\"", without.Output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a card is written with commands for its links and matches the golden file; Markdown has bold labels.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_Card_WritesTheCardWithCommandLinks()
    {
        // Arrange
        var text = new CliHarness(CatalogBuilding.Simple());
        var markdown = new CliHarness(CatalogBuilding.Simple());

        // Act
        text.Run("guide");
        markdown.Run("guide", "--format", "markdown");

        // Assert
        GoldenFiles.AssertEqual("cli", "card.text.expected.txt", text.Output.ToString().TrimEnd('\n'));
        Assert.Contains("**Tags:** tutorial, authoring", markdown.Output.ToString(), StringComparison.Ordinal);
        Assert.Contains("`tool help guide setup`", markdown.Output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a path may be written as words, with slashes, by an alias, and in any case.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_CardPath_AcceptsWordsSlashesAliasesAndCase()
    {
        // Arrange
        var outputs = new List<string>();

        // Act
        foreach (var arguments in new[] { new[] { "guide", "setup" }, ["guide/setup"], ["START", "Setup"], ["guide setup"] })
        {
            var harness = new CliHarness(CatalogBuilding.Simple());
            Assert.Equal(0, harness.Run(arguments));
            outputs.Add(harness.Output.ToString());
        }

        // Assert
        Assert.All(outputs, output => Assert.Equal(outputs[0], output));
        Assert.StartsWith("# Setup\n", outputs[0], StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that <c>--examples</c> prints the examples of the card in full, only lists the cards below it that have
    ///     examples, and matches the golden file.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_Examples_PrintsOwnExamplesAndListsDescendants()
    {
        // Arrange
        var harness = new CliHarness(CatalogBuilding.Simple());

        // Act
        var code = harness.Run("guide", "--examples");

        // Assert
        Assert.Equal(0, code);
        GoldenFiles.AssertEqual("cli", "examples.text.expected.txt", harness.Output.ToString().TrimEnd('\n'));
        Assert.DoesNotContain("tool --version", harness.Output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that <c>--example</c> prints exactly the content of one example, found in the card or below it, also in
    ///     the qualified form, and as a document in JSON.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_Example_PrintsExactlyTheContent()
    {
        // Arrange
        var own = new CliHarness(CatalogBuilding.Simple());
        var below = new CliHarness(CatalogBuilding.Simple());
        var qualified = new CliHarness(CatalogBuilding.Simple());
        var json = new CliHarness(CatalogBuilding.Simple());

        // Act
        var ownCode = own.Run("guide", "--example", "RUN");
        var belowCode = below.Run("guide", "--example", "check");
        var qualifiedCode = qualified.Run("guide", "--example", "guide/setup#check");
        var jsonCode = json.Run("guide", "--example", "run", "--json");

        // Assert
        Assert.Equal(0, ownCode + belowCode + qualifiedCode + jsonCode);
        Assert.Equal("simple run input.txt\n", own.Output.ToString());
        Assert.Equal("simple --version\n", below.Output.ToString());
        Assert.Equal("simple --version\n", qualified.Output.ToString());
        Assert.StartsWith("{\"helpSchemaVersion\":1,\"ok\":true,\"kind\":\"example\",\"id\":\"guide\",\"example\":{\"card\":\"guide\",\"name\":\"run\"", json.Output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that an example that does not exist is reported with the names of the examples that do.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_ExampleNotFound_ListsTheExamplesOfTheCardAndBelow()
    {
        // Arrange
        var harness = new CliHarness(CatalogBuilding.Simple());
        var json = new CliHarness(CatalogBuilding.Simple());
        var leaf = new CliHarness(CatalogBuilding.Simple());

        // Act
        var code = harness.Run("guide", "--example", "nope");
        var jsonCode = json.Run("guide", "--example", "nope", "--json");
        var leafCode = leaf.Run("guide/usage", "--example", "nope");

        // Assert
        Assert.Equal(2, code);
        Assert.Equal(
            "Error: The help card 'guide' has no example 'nope'.\n"
                + "Examples: run, settings, fragment, check. Run 'tool help guide --examples' to see them.\n",
            harness.Error.ToString());
        Assert.Equal(2, jsonCode);
        Assert.Contains("\"candidates\":[\"guide#run\",\"guide#settings\",\"guide#fragment\",\"guide/setup#check\"]", json.Output.ToString(), StringComparison.Ordinal);
        Assert.Equal(2, leafCode);
        Assert.Contains("'guide/usage' has no examples.", leaf.Error.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that an unknown card is reported with the closest name, the names under its parent, and the command
    ///     for their summaries, in text and in JSON, matching the golden files.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_UnknownCard_SuggestsAndListsTheNamesAtThatLevel()
    {
        // Arrange
        var text = new CliHarness(CatalogBuilding.Simple());
        var json = new CliHarness(CatalogBuilding.Simple());

        // Act
        var textCode = text.Run("guide", "usages");
        var jsonCode = json.Run("guide", "setups", "--json");

        // Assert
        Assert.Equal(2, textCode);
        GoldenFiles.AssertEqual("cli", "notfound.text.expected.txt", text.Error.ToString().TrimEnd('\n'));
        Assert.Empty(text.Output.ToString());
        Assert.Equal(2, jsonCode);
        GoldenFiles.AssertEqual("cli", "notfound.expected.json", json.Output.ToString().TrimEnd('\n'));
        Assert.Empty(json.Error.ToString());
    }

    /// <summary>
    ///     Proves that an unknown first word lists the topics, that no suggestion is written when nothing is close, and
    ///     that a card without children says so.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_UnknownCard_TopLevelFarNameAndLeafParent()
    {
        // Arrange
        var top = new CliHarness(CatalogBuilding.Simple());
        var leaf = new CliHarness(CatalogBuilding.Simple());
        var close = new CliHarness(CatalogBuilding.Simple());

        // Act
        top.Run("qqqqqqqq");
        leaf.Run("guide/usage/x");
        close.Run("guid");

        // Assert
        Assert.Equal(
            "Error: Unknown help card 'qqqqqqqq'.\nHelp topics: guide, reference. Run 'tool help' for their summaries.\n",
            top.Error.ToString());
        Assert.Equal(
            "Error: Unknown help card 'x' under 'guide/usage'.\n'guide/usage' has no cards below it.\n",
            leaf.Error.ToString());
        Assert.StartsWith("Error: Unknown help card 'guid'.\nDid you mean 'guide'? Help topics: guide, reference.", close.Error.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a search lists the matching cards with commands, in text and in JSON, matching the golden files,
    ///     and that no match is a sentence with exit code 0.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_Search_ListsRankedCards()
    {
        // Arrange
        var text = BigHarness();
        var json = BigHarness();
        var none = BigHarness();

        // Act
        var textCode = text.Run("--search", "build");
        var jsonCode = json.Run("--search", "build", "--json");
        var noneCode = none.Run("--search", "zebra");

        // Assert
        Assert.Equal(0, textCode + jsonCode + noneCode);
        GoldenFiles.AssertEqual("cli", "search.text.expected.txt", text.Output.ToString().TrimEnd('\n'));
        GoldenFiles.AssertEqual("cli", "search.expected.json", json.Output.ToString().TrimEnd('\n'));
        Assert.Equal("No help cards match 'zebra'.\n", none.Output.ToString());
    }

    /// <summary>
    ///     Proves that a search may be restricted by tags and by sources, which repeat.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_SearchWithTagsAndSources_RestrictsTheCards()
    {
        // Arrange
        var catalog = HelpCatalog.Create(builder => builder.AddSource(CatalogBuilding.Fixture("big")).AddSource(CatalogBuilding.LibA()));
        var tagged = new CliHarness(catalog);
        var sourced = new CliHarness(catalog);
        var restricted = new CliHarness(catalog);

        // Act
        tagged.Run("--search", "program", "--tag", "quality", "--tag", "RELEASE");
        sourced.Run("--search", "graph", "--source", "LIB-A", "--source", "big");
        restricted.Run("--search", "program", "--tag", "quality", "--json");

        // Assert
        Assert.Contains("tool help test coverage", tagged.Output.ToString(), StringComparison.Ordinal);
        Assert.Contains("tool help deploy production", tagged.Output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("tool help build compile", tagged.Output.ToString(), StringComparison.Ordinal);
        Assert.Contains("tool help graph", sourced.Output.ToString(), StringComparison.Ordinal);
        Assert.Contains("\"source\":\"big\"", restricted.Output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that <c>--tag</c> alone lists the cards that have any of the tags, in text and in JSON, matching the
    ///     golden file, and that an unknown tag lists the known ones.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_Tags_ListsCardsWithAnyTag()
    {
        // Arrange
        var json = BigHarness();
        var many = BigHarness();
        var unknown = BigHarness();
        var unknownJson = BigHarness();

        // Act
        var jsonCode = json.Run("--tag", "quality", "--json");
        many.Run("--tag", "quality", "--tag", "build");
        var unknownCode = unknown.Run("--tag", "nope");
        unknownJson.Run("--tag", "nope", "--json");

        // Assert
        Assert.Equal(0, jsonCode);
        GoldenFiles.AssertEqual("cli", "tags.expected.json", json.Output.ToString().TrimEnd('\n'));
        Assert.Contains("tool help build clean", many.Output.ToString(), StringComparison.Ordinal);
        Assert.Contains("tool help test unit", many.Output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("tool help deploy", many.Output.ToString(), StringComparison.Ordinal);
        Assert.Equal(2, unknownCode);
        GoldenFiles.AssertEqual("cli", "tag-unknown.expected.txt", unknown.Error.ToString().TrimEnd('\n'));
        Assert.Contains("\"candidates\":[\"build\",\"quality\",\"release\"]", unknownJson.Output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a catalog without any tag lists no cards for a tag and finds nothing for a tagged search.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_TagsInACatalogWithoutTags_FindNothing()
    {
        // Arrange
        var harness = new CliHarness(CatalogBuilding.FromIds("a", "b"));
        var search = new CliHarness(CatalogBuilding.FromIds("a", "b"));

        // Act
        var code = harness.Run("--tag", "x");
        search.Run("--search", "card", "--tag", "x");

        // Assert
        Assert.Equal(0, code);
        Assert.Equal("There are no help cards.\n", harness.Output.ToString());
        Assert.Equal("No help cards match 'card'.\n", search.Output.ToString());
    }

    /// <summary>
    ///     Proves that <c>--source</c> restricts the index, and that an unknown source lists the known ones.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_Source_RestrictsTheIndexAndReportsUnknownNames()
    {
        // Arrange
        var catalog = HelpCatalog.Create(builder => builder.AddSource(CatalogBuilding.LibA()).AddSource(CatalogBuilding.LibB()));
        var restricted = new CliHarness(catalog);
        var all = new CliHarness(catalog);
        var unknown = new CliHarness(catalog);
        var unknownJson = new CliHarness(catalog);

        // Act
        restricted.Run("--source", "lib-b");
        all.Run();
        var code = unknown.Run("--source", "lib-c");
        unknownJson.Run("--source", "lib-c", "--json");

        // Assert
        Assert.Contains("tool help chart", restricted.Output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("tool help graph", restricted.Output.ToString(), StringComparison.Ordinal);
        Assert.Contains("tool help graph", all.Output.ToString(), StringComparison.Ordinal);
        Assert.Equal(2, code);
        Assert.Equal("Error: Unknown help source 'lib-c'.\nKnown sources: lib-a, lib-b.\n", unknown.Error.ToString());
        Assert.Contains("\"code\":\"not-found\"", unknownJson.Output.ToString(), StringComparison.Ordinal);
        Assert.Contains("\"candidates\":[\"lib-a\",\"lib-b\"]", unknownJson.Output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the sources of a tag listing appear in the filter of the JSON document.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_TagsWithSource_WritesTheFilterToTheDocument()
    {
        // Arrange
        var harness = BigHarness();

        // Act
        harness.Run("--tag", "quality", "--source", "BIG", "--json");

        // Assert
        Assert.Contains("\"filter\":{\"tags\":[\"quality\"],\"sources\":[\"big\"]}", harness.Output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that an example name that matches several examples below one mount is ambiguous, lists the qualified
    ///     names to choose from, and that the qualified name selects one.
    /// </summary>
    [Fact]
    public void CliDispatcher_Run_ExampleNameInTwoRootsBelowAMount_IsAmbiguous()
    {
        // Arrange: two roots of one source have an example of the same name, which is allowed across roots
        const string example = "examples:\n  - name: x\n    kind: command\n    language: shell\n    content: tool x\n    description: An example.\n";
        var files = new[]
        {
            ("a.md", SourceReading.Card(title: "A", extraFrontMatter: example)),
            ("b.md", SourceReading.Card(title: "B", extraFrontMatter: example.Replace("tool x", "tool y", StringComparison.Ordinal))),
        };
        var source = HelpSource.FromFiles(SourceReading.Files(files), "lib");
        var catalog = HelpCatalog.Create(builder => builder.AddMountedSource(source, "m"));
        var ambiguous = new CliHarness(catalog);
        var json = new CliHarness(catalog);
        var chosen = new CliHarness(catalog);

        // Act
        var code = ambiguous.Run("m", "--example", "x");
        json.Run("m", "--example", "x", "--json");
        var chosenCode = chosen.Run("m", "--example", "m/b#x");

        // Assert
        Assert.Equal(2, code);
        Assert.Equal(
            "Error: The example name 'x' matches several examples.\nUse one of: m/a#x, m/b#x.\n",
            ambiguous.Error.ToString());
        Assert.Contains("\"code\":\"ambiguous\"", json.Output.ToString(), StringComparison.Ordinal);
        Assert.Contains("\"candidates\":[\"m/a#x\",\"m/b#x\"]", json.Output.ToString(), StringComparison.Ordinal);
        Assert.Equal(0, chosenCode);
        Assert.Equal("tool y\n", chosen.Output.ToString());
    }

    /// <summary>
    ///     Proves that <c>--example</c> prints the example of the card itself when a card below it has the same name,
    ///     and that the card below is chosen by its own command.
    /// </summary>
    [Fact]
    public void CliDispatcher_Example_OwnCardExample_BeatsDescendantWithSameName()
    {
        // Arrange: the parent and its child both have an example x
        const string example = "examples:\n  - name: x\n    kind: command\n    language: shell\n    content: tool x\n    description: An example.\n";
        var catalog = CatalogBuilding.FromFiles(
            ("a.md", SourceReading.Card(title: "A", extraFrontMatter: example)),
            ("a/b.md", SourceReading.Card(title: "B", extraFrontMatter: example.Replace("tool x", "tool y", StringComparison.Ordinal))));
        var parent = new CliHarness(catalog);
        var child = new CliHarness(catalog);

        // Act
        var parentCode = parent.Run("a", "--example", "x");
        var childCode = child.Run("a/b", "--example", "x");

        // Assert
        Assert.Equal((0, 0), (parentCode, childCode));
        Assert.Equal("tool x\n", parent.Output.ToString());
        Assert.Equal("tool y\n", child.Output.ToString());
    }
}
