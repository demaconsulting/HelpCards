using System.Buffers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DemaConsulting.HelpCards.Presentation;

/// <summary>
///     Writes the JSON documents of contract version 1: one document per call, on one line, ASCII only, with the keys
///     in a fixed order and never a JSON <c>null</c>.
/// </summary>
/// <remarks>
///     <para>
///         Optional values are left out and arrays are always present, possibly empty. The default envelope is
///         <c>{"helpSchemaVersion":1,"ok":true,"kind":"..."}</c> followed by the fields of the document; a
///         <see cref="HelpRenderOptions.JsonEnvelope"/> callback replaces the whole top-level object and receives an
///         action that writes the fields. Characters that are not ASCII are written as <c>\uXXXX</c> whichever
///         envelope is used, so the output is byte-identical on every machine.
///     </para>
///     <para>
///         The writer is stateless and uses invariant formatting only. This file holds the envelope, the character
///         escaping, and the error document; the other parts hold the documents for cards and for lists.
///     </para>
/// </remarks>
internal static partial class JsonDocumentWriter
{
    /// <summary>
    ///     The version of the JSON contract that every document announces.
    /// </summary>
    public const int SchemaVersion = 1;

    /// <summary>
    ///     The error code for a wrong command line or arguments.
    /// </summary>
    public const string UsageErrorCode = "usage-error";

    /// <summary>
    ///     The error code for a card or example that does not exist.
    /// </summary>
    public const string NotFoundCode = "not-found";

    /// <summary>
    ///     The error code for a name that matches several cards.
    /// </summary>
    public const string AmbiguousCode = "ambiguous";

    /// <summary>
    ///     The error code for a help source that failed to load.
    /// </summary>
    public const string CatalogErrorCode = "catalog-error";

    /// <summary>
    ///     The first character that is written as an escape sequence.
    /// </summary>
    private const char FirstEscapedCharacter = '\u007F';

    /// <summary>
    ///     Writes an error document.
    /// </summary>
    /// <remarks>
    ///     The candidates are written only for the codes <c>not-found</c> and <c>ambiguous</c>, and then always, as
    ///     an array that is empty when there is no candidate. There is no public member for this document: the
    ///     command-line helper and the agent tool are its callers.
    /// </remarks>
    /// <param name="options">The options, or <see langword="null"/> for the defaults.</param>
    /// <param name="code">One of <c>usage-error</c>, <c>not-found</c>, <c>ambiguous</c>, or <c>catalog-error</c>.</param>
    /// <param name="message">The message. Must not be null.</param>
    /// <param name="hint">A hint for the reader, or <see langword="null"/> to leave it out.</param>
    /// <param name="candidates">The global ids that could have been meant, or <see langword="null"/> for none.</param>
    /// <param name="writeBody">
    ///     Writes the body of the error in place of the default one (the same <paramref name="code"/>, message, hint,
    ///     and candidates) into the open object, or <see langword="null"/> for the default body.
    /// </param>
    /// <returns>The document.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="code"/> is not a defined error code, or when the <c>JsonEnvelope</c> callback of
    ///     the options returns null.
    /// </exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
    public static string WriteError(
        HelpRenderOptions? options,
        string code,
        string message,
        string? hint = null,
        IReadOnlyList<string>? candidates = null,
        Action<Utf8JsonWriter>? writeBody = null)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(message);

        // Only the four codes of the contract exist, so a typo cannot reach a reader of the document
        if (code is not (UsageErrorCode or NotFoundCode or AmbiguousCode or CatalogErrorCode))
        {
            throw new ArgumentException($"'{code}' is not a help error code.", nameof(code));
        }

        // A tool with its own error shape writes the body itself; the envelope stays the one of the options
        if (writeBody is not null)
        {
            return WriteDocument(options ?? new HelpRenderOptions(), "error", false, writeBody);
        }

        return WriteDocument(options ?? new HelpRenderOptions(), "error", false, writer =>
        {
            writer.WriteString("code", code);
            writer.WriteString("message", message);
            if (hint is not null)
            {
                writer.WriteString("hint", hint);
            }

            if (code is NotFoundCode or AmbiguousCode)
            {
                WriteStrings(writer, "candidates", candidates ?? []);
            }
        });
    }

    /// <summary>
    ///     Writes a document: the envelope and the fields of one kind.
    /// </summary>
    /// <param name="options">The options of the call.</param>
    /// <param name="kind">The kind of the document.</param>
    /// <param name="ok">Whether the document reports success.</param>
    /// <param name="writeFields">Writes the fields of the document into an open object.</param>
    /// <returns>The document on one line, ASCII only, without a trailing line feed.</returns>
    private static string WriteDocument(HelpRenderOptions options, string kind, bool ok, Action<Utf8JsonWriter> writeFields)
    {
        // A custom envelope owns the whole top-level object; the library only supplies the fields
        var envelope = options.JsonEnvelope;
        if (envelope is not null)
        {
            var custom = envelope(kind, writeFields)
                ?? throw new ArgumentException("The JsonEnvelope callback returned null instead of a document.", nameof(options));
            return EscapeNonAscii(custom);
        }

        // The relaxed encoder leaves quotes, backticks, and angle brackets readable; non-ASCII is escaped below
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(
            buffer,
            new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("helpSchemaVersion", SchemaVersion);
            writer.WriteBoolean("ok", ok);
            writer.WriteString("kind", kind);
            writeFields(writer);
            writer.WriteEndObject();
        }

        return EscapeNonAscii(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    ///     Replaces every character outside printable ASCII with a <c>\uXXXX</c> escape.
    /// </summary>
    /// <remarks>
    ///     Characters outside ASCII occur only inside JSON strings, where the escape is equivalent. Surrogate pairs
    ///     are escaped as two units, which is how JSON writes characters beyond the basic plane.
    /// </remarks>
    /// <param name="json">The JSON text.</param>
    /// <returns>The same JSON in ASCII.</returns>
    private static string EscapeNonAscii(string json)
    {
        if (!json.Any(character => character >= FirstEscapedCharacter))
        {
            return json;
        }

        var builder = new StringBuilder(json.Length + 16);
        foreach (var character in json)
        {
            if (character >= FirstEscapedCharacter)
            {
                builder.Append("\\u").Append(((int)character).ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    ///     Writes an array of strings; the array is always written, even when it is empty.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="name">The property name.</param>
    /// <param name="values">The strings.</param>
    private static void WriteStrings(Utf8JsonWriter writer, string name, IEnumerable<string> values)
    {
        writer.WriteStartArray(name);
        foreach (var value in values)
        {
            writer.WriteStringValue(value);
        }

        writer.WriteEndArray();
    }

    /// <summary>
    ///     Writes the <c>command</c> property when the link style writes commands.
    /// </summary>
    /// <param name="context">The settings of the call.</param>
    /// <param name="writer">The writer.</param>
    /// <param name="id">The global id of the card the command prints.</param>
    private static void WriteCommand(RenderContext context, Utf8JsonWriter writer, string id)
    {
        if (context.UsesCommands)
        {
            writer.WriteString("command", LinkFormatter.Command(context, id));
        }
    }
}
