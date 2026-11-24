namespace DemaConsulting.HelpCards.Testing;

/// <summary>
///     One line of a Markdown body with its one-based number and its place in a fenced code block.
/// </summary>
/// <param name="Number">The one-based number of the line within the body.</param>
/// <param name="Text">The text of the line without its line feed.</param>
/// <param name="Kind">Whether the line is outside a fence, opens one, is inside one, or closes one.</param>
/// <param name="Info">
///     For a line that opens a fence, the text after the run of fence characters (the language), trimmed;
///     otherwise an empty string.
/// </param>
internal sealed record MarkdownLine(int Number, string Text, MarkdownLineKind Kind, string Info);
