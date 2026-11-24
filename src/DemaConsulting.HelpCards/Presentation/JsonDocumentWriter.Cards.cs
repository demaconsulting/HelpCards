using System.Text.Json;
using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Presentation;

/// <summary>
///     The documents for one card: <c>card</c>, <c>examples</c>, and <c>example</c>.
/// </summary>
internal static partial class JsonDocumentWriter
{
    /// <summary>
    ///     Writes the <c>card</c> document.
    /// </summary>
    /// <remarks>
    ///     The body is the Markdown body with its links written in the link style of the options. The examples always
    ///     carry their content, because a JSON reader asks for data; <see cref="HelpRenderOptions.IncludeTags"/> and
    ///     <see cref="HelpRenderOptions.IncludeExampleContent"/> shape only the text formats.
    /// </remarks>
    /// <param name="context">The settings of the call. Must not be null.</param>
    /// <param name="card">The card, as the catalog view of the call shows it. Must not be null.</param>
    /// <returns>The document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static string WriteCard(RenderContext context, HelpCard card)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(card);

        return WriteDocument(context.Options, "card", true, writer =>
        {
            writer.WriteString("id", card.Id);
            writer.WriteString("title", card.Title);
            writer.WriteString("summary", card.Summary);
            WriteStrings(writer, "aliases", card.Aliases);
            WriteStrings(writer, "tags", card.Tags);
            writer.WriteString("source", card.SourceName);
            writer.WriteString("culture", card.Culture);
            writer.WriteString("body", LinkFormatter.FormatBody(context, card));
            WriteCardReferences(context, writer, "children", context.Catalog.GetChildren(card));
            WriteCardReferences(context, writer, "seeAlso", context.Catalog.GetSeeAlso(card));
            WriteExamples(writer, "examples", card.Examples);
            WriteLinks(writer, card);
        });
    }

    /// <summary>
    ///     Writes the <c>examples</c> document: the examples of the card and the cards below it that have examples.
    /// </summary>
    /// <param name="context">The settings of the call. Must not be null.</param>
    /// <param name="card">The card, as the catalog view of the call shows it. Must not be null.</param>
    /// <returns>The document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static string WriteExamplesDocument(RenderContext context, HelpCard card)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(card);

        return WriteDocument(context.Options, "examples", true, writer =>
        {
            writer.WriteString("id", card.Id);
            WriteExamples(writer, "examples", card.Examples);
            writer.WriteStartArray("cards");
            foreach (var below in context.DescendantsWithExamples(card))
            {
                writer.WriteStartObject();
                writer.WriteString("id", below.Id);
                writer.WriteString("summary", below.Summary);
                WriteCommand(context, writer, below.Id);
                WriteStrings(writer, "examples", below.Examples.Select(example => example.Name));
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        });
    }

    /// <summary>
    ///     Writes the <c>example</c> document for one example of a card or of the cards below it.
    /// </summary>
    /// <param name="context">The settings of the call. Must not be null.</param>
    /// <param name="card">The card to search from, as the catalog view of the call shows it. Must not be null.</param>
    /// <param name="name">The example name. Must not be null or white space.</param>
    /// <returns>The document, with the id of the requested card and the card that owns the example.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when there is no such example.</exception>
    public static string WriteExampleDocument(RenderContext context, HelpCard card, string name)
    {
        ArgumentNullException.ThrowIfNull(context);
        var reference = context.RequireExample(card, name);

        return WriteDocument(context.Options, "example", true, writer =>
        {
            writer.WriteString("id", card.Id);
            writer.WritePropertyName("example");
            WriteExample(writer, reference.CardId, reference.Example);
        });
    }

    /// <summary>
    ///     Writes an array of card references: the id, title, summary, and command of each card.
    /// </summary>
    /// <param name="context">The settings of the call.</param>
    /// <param name="writer">The writer.</param>
    /// <param name="name">The property name.</param>
    /// <param name="cards">The cards.</param>
    private static void WriteCardReferences(
        RenderContext context,
        Utf8JsonWriter writer,
        string name,
        IReadOnlyList<HelpCard> cards)
    {
        writer.WriteStartArray(name);
        foreach (var card in cards)
        {
            writer.WriteStartObject();
            writer.WriteString("id", card.Id);
            writer.WriteString("title", card.Title);
            writer.WriteString("summary", card.Summary);
            WriteCommand(context, writer, card.Id);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    /// <summary>
    ///     Writes an array of examples without the card they belong to.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="name">The property name.</param>
    /// <param name="examples">The examples.</param>
    private static void WriteExamples(Utf8JsonWriter writer, string name, IReadOnlyList<HelpExample> examples)
    {
        writer.WriteStartArray(name);
        foreach (var example in examples)
        {
            WriteExample(writer, null, example);
        }

        writer.WriteEndArray();
    }

    /// <summary>
    ///     Writes one example as an object.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="cardId">The id of the card that owns the example, or <see langword="null"/> to leave it out.</param>
    /// <param name="example">The example.</param>
    private static void WriteExample(Utf8JsonWriter writer, string? cardId, HelpExample example)
    {
        writer.WriteStartObject();
        if (cardId is not null)
        {
            writer.WriteString("card", cardId);
        }

        writer.WriteString("name", example.Name);
        writer.WriteString("kind", example.Kind);
        writer.WriteString("language", example.Language);
        if (example.FileName is not null)
        {
            writer.WriteString("fileName", example.FileName);
        }

        writer.WriteString("description", example.Description);
        writer.WriteString("content", example.Content);
        writer.WriteEndObject();
    }

    /// <summary>
    ///     Writes the <c>links</c> array: the visible link targets of the body in body order.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="card">The card whose body is described.</param>
    private static void WriteLinks(Utf8JsonWriter writer, HelpCard card)
    {
        writer.WriteStartArray("links");
        foreach (var link in card.BodyPieces.Select(piece => piece.Link).OfType<HelpLink>())
        {
            writer.WriteStartObject();
            WriteOptionalString(writer, "cardId", link.CardId);
            WriteOptionalString(writer, "exampleName", link.ExampleName);
            WriteOptionalString(writer, "exampleCardId", link.ExampleCardId);
            WriteOptionalString(writer, "text", link.Text);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    /// <summary>
    ///     Writes a string property only when it has a value, so no JSON <c>null</c> is ever written.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The value, or <see langword="null"/> to write nothing.</param>
    private static void WriteOptionalString(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is not null)
        {
            writer.WriteString(name, value);
        }
    }
}
