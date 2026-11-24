namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     The severity of a <see cref="HelpProblem"/>.
/// </summary>
/// <remarks>
///     The severity decides what a problem means for the caller: an <see cref="Error"/> makes catalog
///     creation fail, a <see cref="Warning"/> is a quality finding that only the validator reports, and an
///     <see cref="Info"/> problem is a note that never fails anything. The numeric order is
///     <see cref="Info"/> (0) before <see cref="Warning"/> (1) before <see cref="Error"/> (2).
/// </remarks>
public enum HelpSeverity
{
    /// <summary>
    ///     A note about a deliberate degradation, such as a link to a source that is not present.
    /// </summary>
    Info,

    /// <summary>
    ///     A quality finding that does not prevent a catalog from being built.
    /// </summary>
    Warning,

    /// <summary>
    ///     A defect that makes catalog creation fail.
    /// </summary>
    Error
}
