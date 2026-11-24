namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     One example of a help card: a command line, a complete file, or a fragment, shown as data.
/// </summary>
/// <remarks>
///     An example is never run or saved by the library. Its <see cref="Content"/> is final: line endings are
///     normalized to LF, trailing line feeds are removed, and tokens are replaced. The type is immutable and
///     therefore safe for concurrent use. Instances are created by the catalog and cannot be created by callers.
/// </remarks>
public sealed class HelpExample
{
    /// <summary>
    ///     The kind of an example that is one command line.
    /// </summary>
    public const string CommandKind = "command";

    /// <summary>
    ///     The kind of an example that is a complete file.
    /// </summary>
    public const string SourceKind = "source";

    /// <summary>
    ///     The kind of an example that is a fragment shown but never claimed to run alone.
    /// </summary>
    public const string SnippetKind = "snippet";

    /// <summary>
    ///     Creates an example.
    /// </summary>
    /// <param name="name">The example name, a segment.</param>
    /// <param name="kind">The kind, one of the three kind constants.</param>
    /// <param name="language">The language of the content, one lower-case word.</param>
    /// <param name="fileName">The file name to save the content as, or <see langword="null"/> for a command.</param>
    /// <param name="description">The one-line description.</param>
    /// <param name="content">The normalized content.</param>
    /// <param name="sourceName">The name of the source the example comes from.</param>
    internal HelpExample(
        string name,
        string kind,
        string language,
        string? fileName,
        string description,
        string content,
        string sourceName)
    {
        Name = name;
        Kind = kind;
        Language = language;
        FileName = fileName;
        Description = description;
        Content = content;
        SourceName = sourceName;
    }

    /// <summary>
    ///     Gets the name of the example, unique within its scope, as used in <c>--example</c> and links.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the kind: <see cref="CommandKind"/>, <see cref="SourceKind"/>, or <see cref="SnippetKind"/>.
    /// </summary>
    public string Kind { get; }

    /// <summary>
    ///     Gets the language of the content, such as <c>shell</c> or <c>yaml</c>.
    /// </summary>
    public string Language { get; }

    /// <summary>
    ///     Gets the file name to save a source or snippet as, or <see langword="null"/> for a command.
    /// </summary>
    public string? FileName { get; }

    /// <summary>
    ///     Gets the one-line description of the example, in the culture of the catalog where translated.
    /// </summary>
    public string Description { get; }

    /// <summary>
    ///     Gets the content: the command line, or the text of the file or fragment.
    /// </summary>
    public string Content { get; }

    /// <summary>
    ///     Gets the name of the source the example comes from.
    /// </summary>
    public string SourceName { get; }
}
