namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     One example of a help card: a command line, a complete file, or a fragment, shown as data.
/// </summary>
/// <remarks>
///     An example is never run or saved by the library. Its <see cref="Content"/> is final: line endings are
///     normalized to LF and trailing line feeds are removed. Unless the example sets <c>tokens: false</c>, tokens
///     are replaced and <c>\{{</c> escapes are resolved; with <c>tokens: false</c> the content, including the
///     content of a data file, is verbatim and may contain <c>{{...}}</c>. The type is immutable and
///     therefore safe for concurrent use. Instances are created by the catalog and cannot be created by callers.
///     In a card file an example is an item of the front-matter key <c>examples</c> with the keys <c>name</c>,
///     <c>kind</c>, <c>language</c>, <c>description</c>, <c>file</c>, <c>content</c>, <c>save-as</c> (schema 2),
///     <c>tokens</c>, <c>exit</c> and <c>expect</c>. <see cref="FileName"/> is the name to save a <c>source</c> or <c>snippet</c> as: the
///     <c>save-as</c> of schema 2 (by default the last segment of its <c>file</c>), or the <c>file</c> of schema 1; a
///     <c>command</c> has none. <c>exit</c> is <see cref="ExitCode"/> and <c>expect</c> is <see cref="Expect"/>, and only
///     a <c>command</c> has them. The complete grammar is on the page of the <c>DemaConsulting.HelpCards.Loading</c>
///     namespace.
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
    /// <param name="fileName">The save-as name of a source or snippet, or <see langword="null"/> for a command.</param>
    /// <param name="description">The one-line description.</param>
    /// <param name="content">The normalized content, verbatim when the example sets <c>tokens: false</c>.</param>
    /// <param name="sourceName">The name of the source the example comes from.</param>
    /// <param name="exitCode">The exit code a command is expected to end with, from 0 to 255.</param>
    /// <param name="expect">The fragments the output of a command must contain; <see langword="null"/> for none.</param>
    internal HelpExample(
        string name,
        string kind,
        string language,
        string? fileName,
        string description,
        string content,
        string sourceName,
        int exitCode = 0,
        IReadOnlyList<string>? expect = null)
    {
        Name = name;
        Kind = kind;
        Language = language;
        FileName = fileName;
        Description = description;
        Content = content;
        SourceName = sourceName;
        ExitCode = exitCode;
        Expect = expect is null || expect.Count == 0 ? [] : Array.AsReadOnly(expect.ToArray());
    }

    /// <summary>
    ///     Gets the name of the example, unique within its card, as used in <c>--example</c> and links.
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
    ///     Gets the file name to save a source or snippet as (the save-as name), or <see langword="null"/> for a command.
    /// </summary>
    public string? FileName { get; }

    /// <summary>
    ///     Gets the one-line description of the example, in the culture of the catalog where translated.
    /// </summary>
    public string Description { get; }

    /// <summary>
    ///     Gets the content: the command line, or the text of the file or fragment.
    /// </summary>
    /// <remarks>
    ///     Tokens are replaced and <c>\{{</c> escapes are resolved, unless the example sets <c>tokens: false</c>;
    ///     then the content is verbatim and may contain <c>{{...}}</c>.
    /// </remarks>
    public string Content { get; }

    /// <summary>
    ///     Gets the name of the source the example comes from.
    /// </summary>
    public string SourceName { get; }

    /// <summary>
    ///     Gets the exit code a command example is expected to end with, from 0 to 255.
    /// </summary>
    /// <remarks>
    ///     It is 0 for a command without an <c>exit</c> key and for every example that is not a command. The library
    ///     never runs a command; a test runs it and compares the result (see
    ///     <c>DemaConsulting.HelpCards.Testing.HelpExampleRunner</c>).
    /// </remarks>
    public int ExitCode { get; }

    /// <summary>
    ///     Gets the fragments the output of a command example must contain, in the order they are written.
    /// </summary>
    /// <remarks>
    ///     The list is empty when the example has no <c>expect</c> key and for every example that is not a command.
    ///     Every fragment is one line of text after token replacement; <c>tokens: false</c> does not affect it.
    /// </remarks>
    public IReadOnlyList<string> Expect { get; }
}
