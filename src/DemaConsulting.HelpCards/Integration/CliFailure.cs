using DemaConsulting.HelpCards.Presentation;

namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     A help command that failed: the code of the error document, the text for a reader, and the names that could have
///     been meant.
/// </summary>
/// <remarks>
///     The record is immutable. Usage errors, unknown names, and ambiguous names are usage failures and a defective
///     catalog is an internal failure; the exit codes of the two come from <see cref="HelpCliExitCodes"/>, which are 2
///     and 70 unless the tool sets them.
/// </remarks>
/// <param name="Code">One of <c>usage-error</c>, <c>not-found</c>, <c>ambiguous</c>, or <c>catalog-error</c>.</param>
/// <param name="Message">The message.</param>
/// <param name="Hint">The hint, or <see langword="null"/>.</param>
/// <param name="Candidates">The names that could have been meant, or <see langword="null"/> for none.</param>
internal sealed record CliFailure(string Code, string Message, string? Hint, IReadOnlyList<string>? Candidates)
{
    /// <summary>
    ///     Gets the exit code of the failure under a mapping of exit codes.
    /// </summary>
    /// <param name="exitCodes">The exit codes of the helper.</param>
    /// <returns>The internal error code for a catalog error, else the usage error code.</returns>
    public int GetExitCode(HelpCliExitCodes exitCodes)
    {
        return Code == JsonDocumentWriter.CatalogErrorCode ? exitCodes.InternalError : exitCodes.UsageError;
    }
}
