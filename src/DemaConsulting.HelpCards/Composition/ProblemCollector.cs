using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Composition;

/// <summary>
///     Collects the problems of a catalog build and hands them out in the one deterministic order.
/// </summary>
/// <remarks>
///     <para>
///         Every step of composition reports problems here instead of throwing, so one build finds every defect.
///         The order is the source registration index, the file path (ordinal), the line, and the rule id
///         (ordinal); problems that are equal in all of these keep the order in which they were found. An identical
///         problem reported twice is kept once, so a defect that two passes both notice is not reported twice.
///     </para>
///     <para>
///         A collector belongs to one build and is not safe for concurrent use.
///     </para>
/// </remarks>
internal sealed class ProblemCollector
{
    /// <summary>
    ///     The problems with the registration index of their source, in the order found.
    /// </summary>
    private readonly List<(int SourceIndex, HelpProblem Problem)> _items = [];

    /// <summary>
    ///     Gets a value that says whether at least one error was collected.
    /// </summary>
    public bool HasErrors => _items.Any(item => item.Problem.Severity == HelpSeverity.Error);

    /// <summary>
    ///     Adds one problem.
    /// </summary>
    /// <param name="sourceIndex">The registration index of the source the problem belongs to.</param>
    /// <param name="problem">The problem. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="problem"/> is null.</exception>
    public void Add(int sourceIndex, HelpProblem problem)
    {
        ArgumentNullException.ThrowIfNull(problem);
        _items.Add((sourceIndex, problem));
    }

    /// <summary>
    ///     Adds several problems of one source.
    /// </summary>
    /// <param name="sourceIndex">The registration index of the source the problems belong to.</param>
    /// <param name="problems">The problems. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="problems"/> is null.</exception>
    public void AddRange(int sourceIndex, IEnumerable<HelpProblem> problems)
    {
        ArgumentNullException.ThrowIfNull(problems);
        foreach (var problem in problems)
        {
            Add(sourceIndex, problem);
        }
    }

    /// <summary>
    ///     Gets the collected problems in display order.
    /// </summary>
    /// <param name="severity">Only problems of this severity, or <see langword="null"/> for all of them.</param>
    /// <returns>The problems without duplicates, ordered by source, file, line, and rule.</returns>
    public IReadOnlyList<HelpProblem> GetOrdered(HelpSeverity? severity = null)
    {
        return _items
            .Where(item => severity is null || item.Problem.Severity == severity)
            .Distinct()
            .OrderBy(item => item.SourceIndex)
            .ThenBy(item => item.Problem.File, StringComparer.Ordinal)
            .ThenBy(item => item.Problem.Line)
            .ThenBy(item => item.Problem.RuleId, StringComparer.Ordinal)
            .Select(item => item.Problem)
            .ToList();
    }
}
