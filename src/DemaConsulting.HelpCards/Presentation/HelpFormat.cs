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
    /// <remarks>
    ///     The document kinds (<c>index</c>, <c>card</c>, <c>examples</c>, <c>example</c>, <c>search</c>, <c>cards</c>, and
    ///     <c>error</c>) and the fields of each are listed in the table on the page of the
    ///     <c>DemaConsulting.HelpCards.Presentation</c> namespace. With the default envelope every document starts with
    ///     <c>{"helpSchemaVersion":1,"ok":...,"kind":"..."}</c>, where <c>ok</c> is <c>true</c> except for the
    ///     <c>error</c> kind (<c>"ok":false</c>); a <c>HelpRenderOptions.JsonEnvelope</c> callback replaces that
    ///     envelope.
    /// </remarks>
    Json
}
