namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     A reference to a card as written in <c>see-also</c> or <c>extends</c>: <c>[source:]localId</c>.
/// </summary>
/// <remarks>
///     The reference is syntax only. Which source and card it finally names is decided over the whole
///     catalog, where bare references resolve first in the referencing source and then in its imports.
/// </remarks>
/// <param name="SourceName">The declared source name, or <see langword="null"/> for a bare reference.</param>
/// <param name="LocalId">The local card id.</param>
/// <param name="File">The path of the file the reference was written in, inside the source.</param>
/// <param name="Line">The one-based line of the file.</param>
internal sealed record ReferenceDefinition(string? SourceName, string LocalId, string File, int Line);
