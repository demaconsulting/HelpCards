namespace DemaConsulting.HelpCards.Composition;

/// <summary>
///     The outcome of looking up a reference to a card in the sources of a catalog.
/// </summary>
internal enum LookupKind
{
    /// <summary>Exactly one card was found.</summary>
    Found,

    /// <summary>No card has the id; the reference is a defect.</summary>
    NotFound,

    /// <summary>A bare id names a card of the referencing source and a card of an imported source.</summary>
    Ambiguous,

    /// <summary>The reference names a source that is not part of the catalog.</summary>
    SourceAbsent,

    /// <summary>The card failed to load or is left out; its problem is already reported.</summary>
    Suppressed
}
