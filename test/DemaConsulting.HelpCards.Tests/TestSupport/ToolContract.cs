using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DemaConsulting.HelpCards.Integration;

namespace DemaConsulting.HelpCards.Tests.TestSupport;

/// <summary>
///     The JSON contract of an adopting tool that has its own envelope and error shape, as a tool with an established
///     <c>--json</c> contract writes it, shared by the tests of the command-line helper.
/// </summary>
/// <remarks>
///     The contract is <c>{"schemaVersion":2,"ok":...,"command":"help","kind":...,...}</c> for every document, with an
///     error nested as <c>"error":{"code","message","hint"}</c>, and the exit code 1 for every failure.
/// </remarks>
internal static class ToolContract
{
    /// <summary>
    ///     Writes the top-level object of the tool: its members first, then the fields of the document.
    /// </summary>
    /// <param name="kind">The kind of the document.</param>
    /// <param name="writeFields">Writes the fields of the kind into the open object.</param>
    /// <returns>The document.</returns>
    public static string Envelope(string kind, Action<Utf8JsonWriter> writeFields)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", 2);
            writer.WriteBoolean("ok", kind != "error");
            writer.WriteString("command", "help");
            writer.WriteString("kind", kind);
            writeFields(writer);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    ///     Writes the error of the tool as the nested object <c>error</c>.
    /// </summary>
    /// <param name="writer">The writer, inside the open object of the document.</param>
    /// <param name="error">The error to write.</param>
    public static void ErrorBody(Utf8JsonWriter writer, HelpCliError error)
    {
        writer.WriteStartObject("error");
        writer.WriteString("code", error.Code);
        writer.WriteString("message", error.Message);
        if (error.Hint is not null)
        {
            writer.WriteString("hint", error.Hint);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    ///     Gets the exit codes of the tool: 1 for every failure.
    /// </summary>
    public static HelpCliExitCodes ExitCodes => new() { UsageError = 1, InternalError = 1 };
}
