namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     A named block of lines that a tool adds to the help index, such as a list of its commands.
/// </summary>
/// <remarks>
///     The heading is written as given, so it carries any colon the tool wants. The lines are indented by two spaces.
///     The record is immutable and compares by value, except that the list compares by reference.
/// </remarks>
/// <param name="Heading">The heading line.</param>
/// <param name="Lines">The lines below the heading.</param>
public sealed record HelpCliSection(string Heading, IReadOnlyList<string> Lines);
