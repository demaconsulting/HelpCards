namespace DemaConsulting.HelpCards.Testing;

/// <summary>
///     What a line of a Markdown body is with respect to fenced code blocks.
/// </summary>
internal enum MarkdownLineKind
{
    /// <summary>A line outside any fenced block.</summary>
    Text,

    /// <summary>A line that opens a fenced block.</summary>
    FenceOpen,

    /// <summary>A line inside a fenced block.</summary>
    FenceBody,

    /// <summary>A line that closes a fenced block.</summary>
    FenceClose
}
