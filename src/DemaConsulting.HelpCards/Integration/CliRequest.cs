using System.Globalization;
using DemaConsulting.HelpCards.Presentation;

namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     A help command after its arguments were read and checked.
/// </summary>
/// <remarks>
///     The record is immutable. Option values were already validated by the parser, so the dispatcher only acts on them.
/// </remarks>
/// <param name="Mode">What the command asks for.</param>
/// <param name="Path">The words of the card path; empty when there is none.</param>
/// <param name="Format">The output format.</param>
/// <param name="Width">The width the user gave, or <see langword="null"/> for the default.</param>
/// <param name="AutoWidth">Whether the user asked for the width of the console.</param>
/// <param name="Name">The query of a search or the name of an example; <see langword="null"/> otherwise.</param>
/// <param name="Tags">The tags to list or search within; empty for none.</param>
/// <param name="Sources">The source names to restrict to; empty for none.</param>
/// <param name="All">Whether the index lists the whole tree.</param>
/// <param name="Culture">The culture of the language option, or <see langword="null"/> for none.</param>
internal sealed record CliRequest(
    CliMode Mode,
    IReadOnlyList<string> Path,
    HelpFormat Format,
    int? Width,
    bool AutoWidth,
    string? Name,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Sources,
    bool All,
    CultureInfo? Culture);
