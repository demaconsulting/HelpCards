namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     The settings of the knowledge tool that gives an AI agent access to a catalog.
/// </summary>
/// <remarks>
///     <para>
///         Every setting has a default. The values are checked when the tool is created with
///         <see cref="HelpAgentTools.Create"/>, not when they are set.
///     </para>
///     <para>
///         A host that registers the tool under a name of its own, such as <c>canvasnet_help</c>, sets
///         <see cref="Name"/>; the name is the only thing the host has to choose.
///     </para>
/// </remarks>
public sealed class HelpAgentToolOptions
{
    /// <summary>
    ///     Gets the name of the tool as the agent calls it; <c>help</c> unless set. It starts with a lower-case letter
    ///     and has up to 64 lower-case letters, digits, and underscores.
    /// </summary>
    public string Name { get; init; } = "help";

    /// <summary>
    ///     Gets the description the agent reads, or <see langword="null"/> to generate one that lists every source with its
    ///     summary and the three ways to call the tool.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    ///     Gets the most search results a call returns when the agent does not ask for fewer; 8 unless set, from 1 to 20.
    /// </summary>
    public int MaxResults { get; init; } = 8;

    /// <summary>
    ///     Gets the most characters an answer holds before it is cut at a line end; 16000 unless set, from 1000 to 200000.
    /// </summary>
    public int MaxOutputChars { get; init; } = 16000;

    /// <summary>
    ///     Gets the deepest level of the index an answer lists; 2 unless set, from 0 (only the roots) to 8.
    /// </summary>
    public int IndexDepth { get; init; } = 2;

    /// <summary>
    ///     Gets the name of the tool or product that the generated description names, or <see langword="null"/> to use
    ///     the tool name of the catalog, if it has one.
    /// </summary>
    public string? ToolName { get; init; }
}
