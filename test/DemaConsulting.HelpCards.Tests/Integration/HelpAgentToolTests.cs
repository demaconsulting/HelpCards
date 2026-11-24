using System.Text.Json;
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Integration;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Integration;

/// <summary>
///     Unit tests for <see cref="HelpAgentTool"/> and <see cref="HelpAgentTools"/>: creation, the schema, the output
///     budget, and the boundary at which exceptions become messages.
/// </summary>
public class HelpAgentToolTests
{
    /// <summary>
    ///     The schema of the specification, written out a second time so that a change of the constant is noticed.
    /// </summary>
    private const string ExpectedSchema =
        "{\"type\":\"object\",\"properties\":{\"topic\":{\"type\":\"string\",\"description\":\"Card id or path such as svg/framed-figure; names and aliases work, case does not matter.\"},\"example\":{\"type\":\"string\",\"description\":\"Example name, or card-id#name when several cards have an example of that name. With topic, only that card and the cards below it are searched.\"},\"query\":{\"type\":\"string\",\"description\":\"Words to search for in titles, summaries, tags, aliases, and ids.\"},\"source\":{\"type\":\"string\",\"description\":\"Restrict the index or the search to one source (library) name.\"},\"tag\":{\"type\":\"string\",\"description\":\"Restrict the index or the search to cards with this tag.\"},\"maxResults\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":20,\"description\":\"The most search results to return.\"}},\"additionalProperties\":false}";

    /// <summary>
    ///     Proves that the schema is exactly the text of the specification, on one line, and valid JSON.
    /// </summary>
    [Fact]
    public void HelpAgentTool_ParametersSchema_IsTheSpecifiedTextByteForByte()
    {
        // Arrange
        var tool = HelpAgentTools.Create(CatalogBuilding.Simple());

        // Act
        var schema = tool.ParametersSchema;
        using var document = JsonDocument.Parse(schema);

        // Assert
        Assert.Equal(ExpectedSchema, schema);
        Assert.Equal(System.Text.Encoding.UTF8.GetBytes(ExpectedSchema), System.Text.Encoding.UTF8.GetBytes(schema));
        Assert.DoesNotContain('\n', schema);
        Assert.False(document.RootElement.GetProperty("additionalProperties").GetBoolean());
    }

    /// <summary>
    ///     Proves that the tool is named <c>help</c> unless the options say otherwise, and that a host can choose a name
    ///     such as <c>canvasnet_help</c>.
    /// </summary>
    [Fact]
    public void HelpAgentTools_Create_Name_DefaultsToHelpAndAcceptsAHostName()
    {
        // Arrange / Act
        var plain = HelpAgentTools.Create(CatalogBuilding.Simple());
        var named = HelpAgentTools.Create(CatalogBuilding.Simple(), new HelpAgentToolOptions { Name = "canvasnet_help" });
        var longest = HelpAgentTools.Create(CatalogBuilding.Simple(), new HelpAgentToolOptions { Name = "a" + new string('9', 63) });

        // Assert
        Assert.Equal("help", plain.Name);
        Assert.Equal("canvasnet_help", named.Name);
        Assert.Equal(64, longest.Name.Length);
    }

    /// <summary>
    ///     Proves that invalid names, ranges, and descriptions are rejected where the tool is created.
    /// </summary>
    [Fact]
    public void HelpAgentTools_Create_InvalidArguments_Throw()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => HelpAgentTools.Create(null!));
        foreach (var name in new[] { string.Empty, "Help", "1help", "_help", "help-me", "help me", "a" + new string('b', 64) })
        {
            Assert.Throws<ArgumentException>(() => HelpAgentTools.Create(catalog, new HelpAgentToolOptions { Name = name }));
        }

        Assert.Throws<ArgumentException>(() => HelpAgentTools.Create(catalog, new HelpAgentToolOptions { Description = " " }));
        Assert.Throws<ArgumentException>(() => HelpAgentTools.Create(catalog, new HelpAgentToolOptions { ToolName = " " }));
        Assert.Throws<ArgumentOutOfRangeException>(() => HelpAgentTools.Create(catalog, new HelpAgentToolOptions { MaxResults = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => HelpAgentTools.Create(catalog, new HelpAgentToolOptions { MaxResults = 21 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => HelpAgentTools.Create(catalog, new HelpAgentToolOptions { MaxOutputChars = 999 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => HelpAgentTools.Create(catalog, new HelpAgentToolOptions { MaxOutputChars = 200001 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => HelpAgentTools.Create(catalog, new HelpAgentToolOptions { IndexDepth = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => HelpAgentTools.Create(catalog, new HelpAgentToolOptions { IndexDepth = 9 }));
        Assert.NotNull(HelpAgentTools.Create(catalog, new HelpAgentToolOptions { MaxResults = 20, MaxOutputChars = 1000, IndexDepth = 0 }));
        Assert.NotNull(HelpAgentTools.Create(catalog, new HelpAgentToolOptions { MaxResults = 1, MaxOutputChars = 200000, IndexDepth = 8 }));
    }

    /// <summary>
    ///     Proves that the generated description names the tool, lists every source with its summary, and gives the three
    ///     ways to call it, and that a given description is used as it is.
    /// </summary>
    [Fact]
    public void HelpAgentTools_Create_Description_ListsSourcesAndUsageOrIsTheGivenText()
    {
        // Arrange
        var catalog = HelpCatalog.Create(builder => builder
            .AddSource(CatalogBuilding.LibA())
            .AddSource(CatalogBuilding.LibB())
            .ToolName("charts"));

        // Act
        var generated = HelpAgentTools.Create(catalog).Description;
        var named = HelpAgentTools.Create(catalog, new HelpAgentToolOptions { ToolName = "Chart Suite" }).Description;
        var given = HelpAgentTools.Create(catalog, new HelpAgentToolOptions { Description = "Mine." }).Description;

        // Assert
        Assert.StartsWith("Searchable help cards for charts. Sources:\n- lib-a: Graph and shape cards of the first fixture library.\n- lib-b: Chart cards of the second fixture library.\n", generated, StringComparison.Ordinal);
        Assert.Contains("no arguments for the index", generated, StringComparison.Ordinal);
        Assert.Contains("topic to read a card", generated, StringComparison.Ordinal);
        Assert.Contains("query to search", generated, StringComparison.Ordinal);
        Assert.StartsWith("Searchable help cards for Chart Suite.", named, StringComparison.Ordinal);
        Assert.Equal("Mine.", given);
    }

    /// <summary>
    ///     Proves that an answer over the limit is cut at the last line end at or before it, with the number of omitted
    ///     characters, and that an answer within the limit is untouched.
    /// </summary>
    [Fact]
    public void HelpAgentTool_Invoke_LongAnswer_IsCutAtALineEndWithTheOmittedCount()
    {
        // Arrange: a card whose body is many short lines
        var body = string.Join("\n", Enumerable.Range(1, 400).Select(number => $"Line number {number} of the body."));
        var catalog = CatalogBuilding.FromFiles(("long.md", SourceReading.Card(title: "Long", body: body)));
        var full = HelpAgentTools.Create(catalog, new HelpAgentToolOptions { MaxOutputChars = 200000 }).Invoke("{\"topic\":\"long\"}");
        var limited = HelpAgentTools.Create(catalog, new HelpAgentToolOptions { MaxOutputChars = 1000 });

        // Act
        var cut = limited.Invoke("{\"topic\":\"long\"}");

        // Assert
        var marker = cut.IndexOf("\n\n[Truncated: ", StringComparison.Ordinal);
        var kept = cut[..marker];
        Assert.True(kept.Length <= 1000);
        Assert.StartsWith(kept, full, StringComparison.Ordinal);
        Assert.Equal('\n', full[kept.Length]);
        Assert.Equal(
            $"\n\n[Truncated: {full.Length - kept.Length} characters omitted. Ask for a narrower topic or an example.]",
            cut[marker..]);
        Assert.Equal(full[..kept.Length], kept);
        Assert.True(full.LastIndexOf('\n', 1000) == kept.Length);
        Assert.DoesNotContain("[Truncated", full, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a single line longer than the limit is cut at the limit without splitting a surrogate pair.
    /// </summary>
    [Fact]
    public void HelpAgentTool_Invoke_LongSingleLine_IsCutAtTheLimitWithoutSplittingAPair()
    {
        // Arrange: the answer is one long line of characters outside the basic plane
        var catalog = CatalogBuilding.Simple();
        var tool = new HelpAgentTool("help", "d", _ => new string('a', 999) + "\U0001F600" + new string('b', 100), 1000);

        // Act
        var cut = tool.Invoke(null);

        // Assert
        var kept = cut[..cut.IndexOf("\n\n[Truncated: ", StringComparison.Ordinal)];
        Assert.Equal(999, kept.Length);
        Assert.Contains("102 characters omitted", cut, StringComparison.Ordinal);
        Assert.NotNull(catalog);
    }

    /// <summary>
    ///     Proves that the recoverable exceptions become a message with the name of the exception type only.
    /// </summary>
    [Fact]
    public void HelpAgentTool_Invoke_RecoverableExceptions_BecomeAMessageWithoutTheirText()
    {
        // Arrange
        var problem = new HelpProblem(HelpSeverity.Error, "syntax", "s", "f.md", 1, "secret text");
        Exception[] exceptions =
        [
            new HelpCatalogException([problem]),
            new ArgumentException("secret text"),
            NullException("x"),
            RangeException("x"),
            new InvalidOperationException("secret text"),
            new KeyNotFoundException("secret text"),
            new FormatException("secret text"),
            new JsonException("secret text"),
        ];

        // Act / Assert
        foreach (var exception in exceptions)
        {
            var tool = new HelpAgentTool("help", "d", _ => throw exception, 1000);
            Assert.Equal($"The help tool could not answer ({exception.GetType().Name}).", tool.Invoke("{}"));
        }
    }

    /// <summary>
    ///     Creates a null-argument exception with a message that must never reach an agent.
    /// </summary>
    /// <param name="name">The name of the parameter of the exception.</param>
    /// <returns>The exception.</returns>
    private static ArgumentNullException NullException(string name)
    {
        return new ArgumentNullException(name, "secret text");
    }

    /// <summary>
    ///     Creates an out-of-range exception with a message that must never reach an agent.
    /// </summary>
    /// <param name="name">The name of the parameter of the exception.</param>
    /// <returns>The exception.</returns>
    private static ArgumentOutOfRangeException RangeException(string name)
    {
        return new ArgumentOutOfRangeException(name, "secret text");
    }

    /// <summary>
    ///     Proves that out-of-memory and cancellation conditions are not caught, as the contract says.
    /// </summary>
    [Fact]
    public void HelpAgentTool_Invoke_NonRecoverableExceptions_Propagate()
    {
        // Arrange
        var memory = new HelpAgentTool("help", "d", _ => throw new OutOfMemoryException(), 1000);
        var cancelled = new HelpAgentTool("help", "d", _ => throw new OperationCanceledException(), 1000);

        // Act / Assert
        Assert.Throws<OutOfMemoryException>(() => memory.Invoke(null));
        Assert.Throws<OperationCanceledException>(() => cancelled.Invoke(null));
    }

    /// <summary>
    ///     Proves that arguments that are not JSON text at all, including a lone surrogate, never make the tool throw.
    /// </summary>
    [Theory]
    [InlineData("\uD800")]
    [InlineData("{\"topic\":\"\uD800\"}")]
    [InlineData("\0")]
    [InlineData("{\"topic\":\"a\"")]
    public void HelpAgentTool_Invoke_HostileText_NeverThrows(string text)
    {
        // Arrange
        var tool = HelpAgentTools.Create(CatalogBuilding.Simple());

        // Act
        var answer = tool.Invoke(text);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(answer));
    }

    /// <summary>
    ///     Proves that using the tool reads no file, however many calls are made and from however many threads.
    /// </summary>
    [Fact]
    public void HelpAgentTool_Invoke_ManyCallsAndThreads_ReadNoFileAndAgree()
    {
        // Arrange
        var files = new CountingFileProvider(SourceReading.Files(
            ("a.md", SourceReading.Card(title: "Alpha", summary: "The first card.")),
            ("b.md", SourceReading.Card(title: "Beta", summary: "The second card."))));
        var catalog = CatalogBuilding.Create(HelpSource.FromProvider(files, "test"));
        var tool = HelpAgentTools.Create(catalog);
        var (lists, reads) = (files.ListCount, files.ReadCount);
        var expected = new[] { tool.Invoke(null), tool.Invoke("{\"topic\":\"a\"}"), tool.Invoke("{\"query\":\"second\"}") };
        var differing = 0;

        // Act
        Parallel.For(0, 200, _ =>
        {
            var actual = new[] { tool.Invoke(null), tool.Invoke("{\"topic\":\"a\"}"), tool.Invoke("{\"query\":\"second\"}") };
            if (!expected.SequenceEqual(actual))
            {
                Interlocked.Increment(ref differing);
            }
        });

        // Assert
        Assert.Equal(0, differing);
        Assert.Equal((lists, reads), (files.ListCount, files.ReadCount));
    }
}
