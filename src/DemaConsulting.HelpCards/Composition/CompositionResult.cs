using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Composition;

/// <summary>
///     The outcome of composing a catalog: the frozen data when there is no error, and every problem found.
/// </summary>
/// <remarks>
///     The result lets the catalog throw one exception for all errors and lets the validator report problems
///     without any exception. The record is immutable.
/// </remarks>
/// <param name="Data">The frozen data, or <see langword="null"/> when at least one error was found.</param>
/// <param name="Errors">Every error in display order; empty when <paramref name="Data"/> is not null.</param>
/// <param name="Problems">Every problem of every severity in display order.</param>
internal sealed record CompositionResult(
    CatalogData? Data,
    IReadOnlyList<HelpProblem> Errors,
    IReadOnlyList<HelpProblem> Problems);
