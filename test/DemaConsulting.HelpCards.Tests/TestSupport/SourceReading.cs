using System.Globalization;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Tests.TestSupport;

/// <summary>
///     Helpers that build in-memory sources and read them, shared by the loading tests.
/// </summary>
/// <remarks>
///     The helpers keep each test short: a test lists the files of a source as text, reads it with the same
///     entry point the catalog builder uses, and then asserts on the definitions and problems. Error sources
///     live in memory (not as checked-in files) so deliberately malformed cards never reach the repository
///     lint gates.
/// </remarks>
internal static class SourceReading
{
    /// <summary>
    ///     The value of the built-in <c>tool</c> token used by the read helpers.
    /// </summary>
    public const string ToolName = "demo";

    /// <summary>
    ///     The value of the built-in <c>version</c> token used by the read helpers.
    /// </summary>
    public const string ToolVersion = "1.2";

    /// <summary>
    ///     Builds a dictionary of files from path and text pairs.
    /// </summary>
    /// <param name="files">The path and text of each file.</param>
    /// <returns>The files by path.</returns>
    public static Dictionary<string, string> Files(params (string Path, string Text)[] files)
    {
        return files.ToDictionary(file => file.Path, file => file.Text, StringComparer.Ordinal);
    }

    /// <summary>
    ///     Reads an in-memory source the way the catalog builder does.
    /// </summary>
    /// <param name="files">The files of the source.</param>
    /// <param name="name">The declared name, or <see langword="null"/> to take it from <c>_source.md</c>.</param>
    /// <param name="culture">The requested culture; the invariant culture by default.</param>
    /// <param name="tokens">The tokens of the source, or <see langword="null"/> for none.</param>
    /// <returns>The definition of the source.</returns>
    public static SourceDefinition Read(
        Dictionary<string, string> files,
        string? name = "test",
        CultureInfo? culture = null,
        Dictionary<string, string>? tokens = null)
    {
        var source = HelpSource.FromFiles(files, name);
        if (tokens is not null)
        {
            source = source.WithTokens(tokens);
        }

        return Read(source, culture);
    }

    /// <summary>
    ///     Reads a source with the token scope of that source and the fixed built-in tokens.
    /// </summary>
    /// <param name="source">The source to read.</param>
    /// <param name="culture">The requested culture; the invariant culture by default.</param>
    /// <returns>The definition of the source.</returns>
    public static SourceDefinition Read(HelpSource source, CultureInfo? culture = null)
    {
        return CardReader.Read(
            source,
            culture ?? CultureInfo.InvariantCulture,
            name => TokenResolver.Create(name, source.Tokens, [], ToolName, ToolVersion));
    }

    /// <summary>
    ///     Builds a minimal valid card file.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <param name="summary">The summary.</param>
    /// <param name="extraFrontMatter">Additional front-matter lines, each ending with a line feed.</param>
    /// <param name="body">The body.</param>
    /// <returns>The text of the file.</returns>
    public static string Card(
        string title = "A card",
        string summary = "Summary of the card.",
        string extraFrontMatter = "",
        string body = "Body text.")
    {
        return $"---\ntitle: {title}\nsummary: {summary}\n{extraFrontMatter}---\n{body}\n";
    }
}
