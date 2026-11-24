namespace DemaConsulting.HelpCards.Presentation;

/// <summary>
///     The output format of a rendering.
/// </summary>
public enum HelpFormat
{
    /// <summary>
    ///     Plain text, wrapped to the configured width. This is the default.
    /// </summary>
    Text,

    /// <summary>
    ///     Markdown with the same layout as the text format, never wrapped, with bold labels.
    /// </summary>
    Markdown,

    /// <summary>
    ///     One JSON document on one line, as defined by the JSON contract with <c>helpSchemaVersion</c> 1.
    /// </summary>
    Json
}
