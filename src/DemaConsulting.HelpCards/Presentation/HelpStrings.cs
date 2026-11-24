namespace DemaConsulting.HelpCards.Presentation;

/// <summary>
///     The English chrome of a rendering: headings, labels, and hint sentences.
/// </summary>
/// <remarks>
///     Every word that the library writes around authored content is defined here and nowhere else, so chrome is one
///     seam: it is English in this release, it is never localized, and JSON keys do not use it. The strings are
///     constants and functions of their arguments only, so they cannot depend on the culture of the machine.
/// </remarks>
internal static class HelpStrings
{
    /// <summary>
    ///     The heading text of the section that lists the children of a card.
    /// </summary>
    public const string CardsHeading = "Cards";

    /// <summary>
    ///     The heading text of the section that lists the examples of a card.
    /// </summary>
    public const string ExamplesHeading = "Examples";

    /// <summary>
    ///     The heading text of the section that lists the related cards of a card.
    /// </summary>
    public const string SeeAlsoHeading = "See also";

    /// <summary>
    ///     The heading text of the section of an examples document that lists the cards below with examples.
    /// </summary>
    public const string ExamplesBelowHeading = "Cards below with examples";

    /// <summary>
    ///     The label before the aliases of a card.
    /// </summary>
    public const string AliasesLabel = "Also known as";

    /// <summary>
    ///     The label before the tags of a card.
    /// </summary>
    public const string TagsLabel = "Tags";

    /// <summary>
    ///     The label before the example names of a card below.
    /// </summary>
    public const string ExampleNamesLabel = "Examples";

    /// <summary>
    ///     The text written for an examples document of a card that has no examples of its own.
    /// </summary>
    public const string NoOwnExamples = "This card has no examples of its own.";

    /// <summary>
    ///     The text written for a list of cards that is empty.
    /// </summary>
    public const string NoCards = "There are no help cards.";

    /// <summary>
    ///     Gets the second-level heading line of a section, such as <c>## Cards</c>.
    /// </summary>
    /// <param name="title">The heading text.</param>
    /// <returns>The heading line without a line break.</returns>
    public static string Heading(string title)
    {
        return $"## {title}";
    }

    /// <summary>
    ///     Gets the title line of an examples document.
    /// </summary>
    /// <param name="cardTitle">The title of the card.</param>
    /// <returns>The line without a line break.</returns>
    public static string ExamplesTitle(string cardTitle)
    {
        return $"# {cardTitle}: examples";
    }

    /// <summary>
    ///     Gets the hint that tells how to print the examples of a card in full or one alone.
    /// </summary>
    /// <param name="command">The command that prints the card.</param>
    /// <returns>The sentence.</returns>
    public static string PrintExamplesHint(string command)
    {
        return $"Print them in full with: {command} --examples, or one alone with: {command} --example <name>.";
    }

    /// <summary>
    ///     Gets the hint that tells how to print one example alone.
    /// </summary>
    /// <param name="command">The command that prints the card.</param>
    /// <returns>The sentence.</returns>
    public static string PrintOneExampleHint(string command)
    {
        return $"Print one alone with: {command} --example <name>";
    }

    /// <summary>
    ///     Gets the line that tells the exit code a command example is expected to end with.
    /// </summary>
    /// <param name="exitCode">The expected exit code.</param>
    /// <returns>The line without a line break.</returns>
    public static string ExpectedExitCode(int exitCode)
    {
        return string.Create(System.Globalization.CultureInfo.InvariantCulture, $"Expected exit code: {exitCode}");
    }

    /// <summary>
    ///     Gets the line that tells one fragment the output of a command example is expected to contain.
    /// </summary>
    /// <param name="fragment">The expected fragment of output.</param>
    /// <returns>The line without a line break.</returns>
    public static string ExpectedOutputContains(string fragment)
    {
        return $"Expected output contains: {fragment}";
    }

    /// <summary>
    ///     Gets the text written for a search that found nothing.
    /// </summary>
    /// <param name="query">The query that was searched for.</param>
    /// <returns>The sentence.</returns>
    public static string NoResults(string query)
    {
        return $"No help cards match '{query}'.";
    }

    /// <summary>
    ///     Gets the label of a labeled line, in plain text or in Markdown with a bold label.
    /// </summary>
    /// <param name="label">The label without a colon.</param>
    /// <param name="markdown">Whether the label is written in bold for Markdown.</param>
    /// <returns>The label with its colon, such as <c>Tags:</c> or <c>**Tags:**</c>.</returns>
    public static string Label(string label, bool markdown)
    {
        return markdown ? $"**{label}:**" : $"{label}:";
    }
}
