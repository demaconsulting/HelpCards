namespace DemaConsulting.HelpCards.Integration;

/// <summary>
///     The exit codes of a command-line help helper, for a tool whose contract uses other numbers than 0, 2, and 70.
/// </summary>
/// <remarks>
///     <para>
///         The defaults are the contract of the helper: 0 for success, 2 for wrong arguments and for a name that matches
///         nothing or several things, and 70 for a catalog that is defective or could not be created. A tool that exits
///         with 1 for every failure sets <see cref="UsageError"/> and <see cref="InternalError"/> to 1. The mapping does
///         not change the exit code of a <see cref="HelpCliAnswer"/>, which the tool chose itself.
///     </para>
///     <para>
///         Every value is checked when it is set and must be from 0 to 255, so an invalid number fails where it is
///         written. The object is immutable after initialization and safe to share.
///     </para>
/// </remarks>
public sealed class HelpCliExitCodes
{
    /// <summary>
    ///     The smallest exit code that is allowed.
    /// </summary>
    internal const int MinimumExitCode = 0;

    /// <summary>
    ///     The largest exit code that is allowed.
    /// </summary>
    internal const int MaximumExitCode = 255;

    /// <summary>
    ///     The exit code of a command that succeeded, unless set.
    /// </summary>
    internal const int DefaultSuccess = 0;

    /// <summary>
    ///     The exit code of a command that failed because of its arguments, unless set.
    /// </summary>
    internal const int DefaultUsageError = 2;

    /// <summary>
    ///     The exit code of a command that failed because the catalog could not be created, unless set.
    /// </summary>
    internal const int DefaultInternalError = 70;

    /// <summary>
    ///     The success code, checked when set.
    /// </summary>
    private readonly int _success = DefaultSuccess;

    /// <summary>
    ///     The usage error code, checked when set.
    /// </summary>
    private readonly int _usageError = DefaultUsageError;

    /// <summary>
    ///     The internal error code, checked when set.
    /// </summary>
    private readonly int _internalError = DefaultInternalError;

    /// <summary>
    ///     Gets the exit code of a command that was answered; 0 by default.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on initialization with a value outside 0 to 255.</exception>
    public int Success
    {
        get => _success;
        init => _success = Check(value);
    }

    /// <summary>
    ///     Gets the exit code of a command that failed because of its arguments, or because a card, example, tag, or
    ///     source was not found or not unique (the error codes <c>usage-error</c>, <c>not-found</c>, and
    ///     <c>ambiguous</c>); 2 by default.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on initialization with a value outside 0 to 255.</exception>
    public int UsageError
    {
        get => _usageError;
        init => _usageError = Check(value);
    }

    /// <summary>
    ///     Gets the exit code of a command that failed because the catalog is defective or could not be created (the
    ///     error code <c>catalog-error</c>); 70 by default.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on initialization with a value outside 0 to 255.</exception>
    public int InternalError
    {
        get => _internalError;
        init => _internalError = Check(value);
    }

    /// <summary>
    ///     Checks that an exit code is from 0 to 255.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is outside 0 to 255.</exception>
    private static int Check(int value)
    {
        // Operating systems keep only the low byte of an exit code, so anything else cannot be reported faithfully
        if (value is < MinimumExitCode or > MaximumExitCode)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"The exit code must be between {MinimumExitCode} and {MaximumExitCode}.");
        }

        return value;
    }
}
