namespace DemaConsulting.HelpCards.Catalog;

/// <summary>
///     The exception thrown when a help source or a composition of sources is defective.
/// </summary>
/// <remarks>
///     <para>
///         Catalog creation collects every defect it can find and throws this one exception afterwards, so an
///         author sees all problems at once. <see cref="Problems"/> holds every error in the deterministic order
///         of the failure model; the message is the display text of the first problem followed by the number of
///         further ones. Bad arguments never produce this exception; they throw
///         <see cref="ArgumentException"/> or <see cref="ArgumentNullException"/>.
///     </para>
///     <para>
///         The exception derives from <see cref="InvalidOperationException"/> so callers that catch the broader
///         type also see it. It is immutable and therefore safe to share between threads.
///     </para>
/// </remarks>
public sealed class HelpCatalogException : InvalidOperationException
{
    /// <summary>
    ///     Creates the exception for a list of errors.
    /// </summary>
    /// <param name="problems">The errors, in display order. Must not be null or empty.</param>
    internal HelpCatalogException(IReadOnlyList<HelpProblem> problems)
        : base(CreateMessage(problems))
    {
        Problems = CardNode.Freeze(problems);
    }

    /// <summary>
    ///     Gets every error that was found, in the order source, file, line, and rule.
    /// </summary>
    public IReadOnlyList<HelpProblem> Problems { get; }

    /// <summary>
    ///     Writes the message: the first problem and how many more there are.
    /// </summary>
    /// <param name="problems">The errors.</param>
    /// <returns>The message.</returns>
    private static string CreateMessage(IReadOnlyList<HelpProblem> problems)
    {
        ArgumentNullException.ThrowIfNull(problems);
        if (problems.Count == 0)
        {
            throw new ArgumentException("At least one problem is required.", nameof(problems));
        }

        var first = problems[0].ToDisplayText();
        return problems.Count == 1 ? first : $"{first} (and {problems.Count - 1} more)";
    }
}
