using DemaConsulting.HelpCards.Integration;

namespace DemaConsulting.HelpCards.Tests.Integration;

/// <summary>
///     Unit tests for <see cref="AgentArgumentReader"/>: every row of the argument table of the knowledge tool.
/// </summary>
public class AgentArgumentReaderTests
{
    /// <summary>
    ///     Proves that no arguments, empty text, white space, JSON null, and an empty object all mean the index.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \n")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData(" { } ")]
    public void AgentArgumentReader_Read_NothingToAskFor_IsTheEmptyArguments(string? json)
    {
        // Arrange / Act
        var result = AgentArgumentReader.Read(json);

        // Assert
        Assert.Null(result.Error);
        Assert.Equal(new AgentArguments(null, null, null, null, null, null), result.Arguments);
    }

    /// <summary>
    ///     Proves that every valid property is read, text is trimmed, and a null, empty, or white-space text is absent.
    /// </summary>
    [Fact]
    public void AgentArgumentReader_Read_ValidProperties_AreReadAndBlankTextIsAbsent()
    {
        // Arrange
        const string full = "{\"topic\":\" svg \",\"example\":\"a#b\",\"query\":\"words here\",\"source\":\"s\",\"tag\":\"t\",\"maxResults\":20}";
        const string blank = "{\"topic\":null,\"example\":\"\",\"query\":\"  \",\"source\":null,\"maxResults\":null}";

        // Act
        var read = AgentArgumentReader.Read(full);
        var absent = AgentArgumentReader.Read(blank);

        // Assert
        Assert.Equal(new AgentArguments("svg", "a#b", "words here", "s", "t", 20), read.Arguments);
        Assert.Equal(new AgentArguments(null, null, null, null, null, null), absent.Arguments);
    }

    /// <summary>
    ///     Proves that the last of a repeated property counts.
    /// </summary>
    [Fact]
    public void AgentArgumentReader_Read_RepeatedProperty_LastCounts()
    {
        // Arrange / Act
        var result = AgentArgumentReader.Read("{\"topic\":\"a\",\"topic\":\"b\"}");

        // Assert
        Assert.Equal("b", result.Arguments!.Topic);
    }

    /// <summary>
    ///     Proves that text that is not JSON is reported with a reason and an example of what to send.
    /// </summary>
    [Theory]
    [InlineData("{")]
    [InlineData("topic")]
    [InlineData("{\"topic\":}")]
    [InlineData("{'topic':'x'}")]
    [InlineData("{\"topic\":\"x\",}")]
    public void AgentArgumentReader_Read_InvalidJson_ExplainsAndShowsAnExample(string json)
    {
        // Arrange / Act
        var result = AgentArgumentReader.Read(json);

        // Assert
        Assert.Null(result.Arguments);
        Assert.StartsWith("The arguments are not valid JSON (", result.Error, StringComparison.Ordinal);
        Assert.EndsWith("). Send an object such as {\"topic\":\"svg\"}.", result.Error, StringComparison.Ordinal);
        Assert.DoesNotContain("Path:", result.Error, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that JSON that is not an object is rejected.
    /// </summary>
    [Theory]
    [InlineData("[]")]
    [InlineData("[{\"topic\":\"x\"}]")]
    [InlineData("\"topic\"")]
    [InlineData("5")]
    [InlineData("true")]
    public void AgentArgumentReader_Read_NotAnObject_IsRejected(string json)
    {
        // Arrange / Act
        var result = AgentArgumentReader.Read(json);

        // Assert
        Assert.Equal("The arguments must be a JSON object.", result.Error);
    }

    /// <summary>
    ///     Proves that the first unknown property in ordinal order is named, with the valid names, before types are checked.
    /// </summary>
    [Fact]
    public void AgentArgumentReader_Read_UnknownProperty_NamesTheFirstInOrdinalOrder()
    {
        // Arrange / Act
        var result = AgentArgumentReader.Read("{\"topic\":5,\"zebra\":1,\"Alpha\":2,\"beta\":3}");

        // Assert
        Assert.Equal("Unknown argument 'Alpha'. Valid arguments: topic, example, query, source, tag, maxResults.", result.Error);
    }

    /// <summary>
    ///     Proves that a text property that is not a string, and a maximum that is not an integer from 1 to 20, are
    ///     reported by name.
    /// </summary>
    [Theory]
    [InlineData("{\"topic\":5}", "The argument 'topic' must be a string.")]
    [InlineData("{\"example\":true}", "The argument 'example' must be a string.")]
    [InlineData("{\"query\":[\"a\"]}", "The argument 'query' must be a string.")]
    [InlineData("{\"source\":{}}", "The argument 'source' must be a string.")]
    [InlineData("{\"tag\":1.5}", "The argument 'tag' must be a string.")]
    [InlineData("{\"maxResults\":\"5\"}", "The argument 'maxResults' must be an integer from 1 to 20.")]
    [InlineData("{\"maxResults\":0}", "The argument 'maxResults' must be an integer from 1 to 20.")]
    [InlineData("{\"maxResults\":21}", "The argument 'maxResults' must be an integer from 1 to 20.")]
    [InlineData("{\"maxResults\":-1}", "The argument 'maxResults' must be an integer from 1 to 20.")]
    [InlineData("{\"maxResults\":2.5}", "The argument 'maxResults' must be an integer from 1 to 20.")]
    [InlineData("{\"maxResults\":5.0}", "The argument 'maxResults' must be an integer from 1 to 20.")]
    [InlineData("{\"maxResults\":true}", "The argument 'maxResults' must be an integer from 1 to 20.")]
    [InlineData("{\"maxResults\":99999999999}", "The argument 'maxResults' must be an integer from 1 to 20.")]
    public void AgentArgumentReader_Read_WrongTypes_AreReportedByName(string json, string message)
    {
        // Arrange / Act
        var result = AgentArgumentReader.Read(json);

        // Assert
        Assert.Null(result.Arguments);
        Assert.Equal(message, result.Error);
    }

    /// <summary>
    ///     Proves that the limits of <c>maxResults</c> are valid and that the first wrong type of the table order is reported.
    /// </summary>
    [Fact]
    public void AgentArgumentReader_Read_MaxResultsLimits_AreInclusiveAndTopicIsCheckedFirst()
    {
        // Arrange / Act
        var low = AgentArgumentReader.Read("{\"maxResults\":1}");
        var high = AgentArgumentReader.Read("{\"maxResults\":20}");
        var both = AgentArgumentReader.Read("{\"maxResults\":0,\"tag\":1,\"topic\":2}");

        // Assert
        Assert.Equal(1, low.Arguments!.MaxResults);
        Assert.Equal(20, high.Arguments!.MaxResults);
        Assert.Equal("The argument 'topic' must be a string.", both.Error);
    }

    /// <summary>
    ///     Proves that a very deeply nested document is reported as invalid JSON, not thrown.
    /// </summary>
    [Fact]
    public void AgentArgumentReader_Read_TooDeepJson_IsReportedAsInvalid()
    {
        // Arrange
        var json = new string('[', 200) + new string(']', 200);

        // Act
        var result = AgentArgumentReader.Read(json);

        // Assert
        Assert.StartsWith("The arguments are not valid JSON (", result.Error, StringComparison.Ordinal);
    }
}
