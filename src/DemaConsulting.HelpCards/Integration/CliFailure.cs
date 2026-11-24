namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     A help command that failed: the code of the error document, the text for a reader, and the names that could have
///     been meant.
/// </summary>
/// <remarks>
///     The record is immutable. Usage errors, unknown names, and ambiguous names exit with 2, and a defective catalog
///     with 70.
/// </remarks>
/// <param name="Code">One of <c>usage-error</c>, <c>not-found</c>, <c>ambiguous</c>, or <c>catalog-error</c>.</param>
/// <param name="Message">The message.</param>
/// <param name="Hint">The hint, or <see langword="null"/>.</param>
/// <param name="Candidates">The names that could have been meant, or <see langword="null"/> for none.</param>
internal sealed record CliFailure(string Code, string Message, string? Hint, IReadOnlyList<string>? Candidates)
{
    /// <summary>
    ///     The exit code of a command that failed because the catalog could not be created.
    /// </summary>
    public const int CatalogErrorExitCode = 70;

    /// <summary>
    ///     The exit code of a command that failed because of its arguments.
    /// </summary>
    public const int UsageExitCode = 2;

    /// <summary>
    ///     Gets the exit code of the failure.
    /// </summary>
    public int ExitCode => Code == "catalog-error" ? CatalogErrorExitCode : UsageExitCode;
}
