namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     One example of a card, with its text resolved.
/// </summary>
/// <remarks>
///     An example is data, never code that runs. <paramref name="Content"/> is already normalized (LF line
///     endings, trailing line feeds removed, tokens replaced) and comes either from the inline
///     <c>content</c> or from the data file next to the card. Descriptions of a selected translation
///     have already replaced the neutral descriptions. A command may state the result it is expected to give
///     (<paramref name="ExitCode"/> and <paramref name="Expect"/>); the library never runs the command.
/// </remarks>
/// <param name="Name">The example name, a segment.</param>
/// <param name="Kind">The kind: <c>command</c>, <c>source</c>, or <c>snippet</c>.</param>
/// <param name="Language">The language of the content, one lower-case word.</param>
/// <param name="FileName">
///     The file name to save a source or snippet as (<c>save-as</c> in schema 2, <c>file</c> in schema 1), or
///     <see langword="null"/> for a command.
/// </param>
/// <param name="Description">The one-line description.</param>
/// <param name="Content">The normalized content.</param>
/// <param name="ExitCode">The exit code a command is expected to end with, from 0 to 255; 0 when not stated or not a command.</param>
/// <param name="Expect">The fragments the output of a command must contain; empty when not stated or not a command.</param>
/// <param name="File">The path of the card or overlay file the example is written in.</param>
/// <param name="Line">The one-based line of the example in that file.</param>
internal sealed record ExampleDefinition(
    string Name,
    string Kind,
    string Language,
    string? FileName,
    string Description,
    string Content,
    int ExitCode,
    IReadOnlyList<string> Expect,
    string File,
    int Line);
