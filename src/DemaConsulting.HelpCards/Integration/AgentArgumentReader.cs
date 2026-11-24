using System.Text.Json;

namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     Reads the JSON arguments of a call of the knowledge tool and checks them against the schema.
/// </summary>
/// <remarks>
///     <para>
///         Nothing in the arguments can make the reader throw: text that is missing, not JSON, not an object, has an
///         unknown property, or has a value of the wrong type becomes a plain-text message that says what to send
///         instead. A property that is <c>null</c>, empty, or white space counts as absent. When a property is repeated
///         the last one counts. Argument text is only ever compared with names; it is never used as a file path.
///     </para>
///     <para>
///         The reader is static and stateless and therefore safe for concurrent use.
///     </para>
/// </remarks>
internal static class AgentArgumentReader
{
    /// <summary>
    ///     The names of the text properties, in the order in which their types are checked.
    /// </summary>
    private static readonly string[] TextProperties = ["topic", "example", "query", "source", "tag"];

    /// <summary>
    ///     The name of the property that holds the most search results.
    /// </summary>
    private const string MaxResultsProperty = "maxResults";

    /// <summary>
    ///     The least value of <c>maxResults</c>.
    /// </summary>
    private const int MinimumMaxResults = 1;

    /// <summary>
    ///     The most value of <c>maxResults</c>.
    /// </summary>
    private const int MaximumMaxResults = 20;

    /// <summary>
    ///     Reads the arguments of a call.
    /// </summary>
    /// <param name="argumentsJson">The arguments as JSON text; <see langword="null"/>, empty, or white space for none.</param>
    /// <returns>The arguments or a message for the agent.</returns>
    public static AgentReadResult Read(string? argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return new AgentReadResult(Empty(), null);
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(argumentsJson);
        }
        catch (JsonException exception)
        {
            return Fail($"The arguments are not valid JSON ({Describe(exception)}). Send an object such as {{\"topic\":\"svg\"}}.");
        }

        using (document)
        {
            return document.RootElement.ValueKind switch
            {
                JsonValueKind.Null => new AgentReadResult(Empty(), null),
                JsonValueKind.Object => ReadObject(document.RootElement),
                _ => Fail("The arguments must be a JSON object."),
            };
        }
    }

    /// <summary>
    ///     Creates arguments that ask for nothing, which means the index.
    /// </summary>
    /// <returns>The arguments.</returns>
    private static AgentArguments Empty()
    {
        return new AgentArguments(null, null, null, null, null, null);
    }

    /// <summary>
    ///     Creates a failed result.
    /// </summary>
    /// <param name="message">The message for the agent.</param>
    /// <returns>The result.</returns>
    private static AgentReadResult Fail(string message)
    {
        return new AgentReadResult(null, message);
    }

    /// <summary>
    ///     Gives the reason of a JSON failure without the position, which only a person editing a file could use.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The reason, without a final period.</returns>
    private static string Describe(JsonException exception)
    {
        var message = exception.Message;
        var path = message.IndexOf(" Path:", StringComparison.Ordinal);
        return (path >= 0 ? message[..path] : message).TrimEnd('.', ' ');
    }

    /// <summary>
    ///     Reads the properties of the arguments object.
    /// </summary>
    /// <param name="root">The object.</param>
    /// <returns>The arguments or a message for the agent.</returns>
    private static AgentReadResult ReadObject(JsonElement root)
    {
        // The last of a repeated property counts
        var properties = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in root.EnumerateObject())
        {
            properties[property.Name] = property.Value;
        }

        var unknown = properties.Keys
            .Where(name => !TextProperties.Contains(name, StringComparer.Ordinal) && name != MaxResultsProperty)
            .Order(StringComparer.Ordinal)
            .FirstOrDefault();
        if (unknown is not null)
        {
            return Fail($"Unknown argument '{unknown}'. Valid arguments: {string.Join(", ", TextProperties)}, {MaxResultsProperty}.");
        }

        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var name in TextProperties)
        {
            var text = ReadText(properties, name, out var error);
            if (error is not null)
            {
                return Fail(error);
            }

            values[name] = text;
        }

        var maxResults = ReadMaxResults(properties, out var maxError);
        return maxError is not null
            ? Fail(maxError)
            : new AgentReadResult(
                new AgentArguments(values["topic"], values["example"], values["query"], values["source"], values["tag"], maxResults),
                null);
    }

    /// <summary>
    ///     Reads one text property.
    /// </summary>
    /// <param name="properties">The properties of the object.</param>
    /// <param name="name">The name of the property.</param>
    /// <param name="error">Receives the message when the value is not text.</param>
    /// <returns>The trimmed text, or <see langword="null"/> when the property is absent, null, or white space.</returns>
    private static string? ReadText(Dictionary<string, JsonElement> properties, string name, out string? error)
    {
        error = null;
        if (!properties.TryGetValue(name, out var element) || element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            error = $"The argument '{name}' must be a string.";
            return null;
        }

        var text = element.GetString()?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    /// <summary>
    ///     Reads the <c>maxResults</c> property.
    /// </summary>
    /// <param name="properties">The properties of the object.</param>
    /// <param name="error">Receives the message when the value is not an integer from 1 to 20.</param>
    /// <returns>The value, or <see langword="null"/> when the property is absent or null.</returns>
    private static int? ReadMaxResults(Dictionary<string, JsonElement> properties, out string? error)
    {
        error = null;
        if (!properties.TryGetValue(MaxResultsProperty, out var element) || element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.Number
            && element.TryGetInt32(out var value)
            && value is >= MinimumMaxResults and <= MaximumMaxResults)
        {
            return value;
        }

        error = $"The argument '{MaxResultsProperty}' must be an integer from {MinimumMaxResults} to {MaximumMaxResults}.";
        return null;
    }
}
