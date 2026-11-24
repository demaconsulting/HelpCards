namespace DemaConsulting.HelpCards.Testing;

/// <summary>
///     The limits and switches of <see cref="HelpCardValidator"/>.
/// </summary>
/// <remarks>
///     <para>
///         Every property has a default that suits help read in a terminal, so a test usually passes no
///         options at all. The options are immutable: they are set once with an object initializer, and each
///         value is checked where it is set, so a nonsensical limit fails where it is written.
///     </para>
///     <para>
///         The limits count characters of the rendered text of a card, not bytes. Root cards (cards without a
///         parent) that have cards below them are index pages and get their own, smaller limit.
///     </para>
/// </remarks>
public sealed class HelpValidationOptions
{
    /// <summary>
    ///     The default limit of a card's rendered text.
    /// </summary>
    private const int DefaultMaxCardChars = 6000;

    /// <summary>
    ///     The default limit of the rendered text of a root card that has cards below it.
    /// </summary>
    private const int DefaultMaxRootIndexChars = 3500;

    /// <summary>
    ///     The default limit of a summary.
    /// </summary>
    private const int DefaultMaxSummaryChars = 100;

    /// <summary>
    ///     The default limit of the summary of a root card.
    /// </summary>
    private const int DefaultMaxRootSummaryChars = 60;

    /// <summary>
    ///     The backing store of <see cref="MaxCardChars"/>.
    /// </summary>
    private readonly int _maxCardChars = DefaultMaxCardChars;

    /// <summary>
    ///     The backing store of <see cref="MaxRootIndexChars"/>.
    /// </summary>
    private readonly int _maxRootIndexChars = DefaultMaxRootIndexChars;

    /// <summary>
    ///     The backing store of <see cref="MaxSummaryChars"/>.
    /// </summary>
    private readonly int _maxSummaryChars = DefaultMaxSummaryChars;

    /// <summary>
    ///     The backing store of <see cref="MaxRootSummaryChars"/>.
    /// </summary>
    private readonly int _maxRootSummaryChars = DefaultMaxRootSummaryChars;

    /// <summary>
    ///     The backing store of <see cref="MaxLineLength"/>.
    /// </summary>
    private readonly int? _maxLineLength;

    /// <summary>
    ///     The backing store of <see cref="Rules"/>.
    /// </summary>
    private readonly IReadOnlyList<IHelpCardRule> _rules = [];

    /// <summary>
    ///     Gets the most characters the rendered text of a card may have; 6000 by default.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is not positive.</exception>
    public int MaxCardChars
    {
        get => _maxCardChars;
        init => _maxCardChars = RequirePositive(value, nameof(MaxCardChars));
    }

    /// <summary>
    ///     Gets the most characters the rendered text of a root card that has cards below it may have; 3500 by default.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is not positive.</exception>
    public int MaxRootIndexChars
    {
        get => _maxRootIndexChars;
        init => _maxRootIndexChars = RequirePositive(value, nameof(MaxRootIndexChars));
    }

    /// <summary>
    ///     Gets the most characters a summary may have; 100 by default.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is not positive.</exception>
    public int MaxSummaryChars
    {
        get => _maxSummaryChars;
        init => _maxSummaryChars = RequirePositive(value, nameof(MaxSummaryChars));
    }

    /// <summary>
    ///     Gets the most characters the summary of a root card may have; 60 by default.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is not positive.</exception>
    public int MaxRootSummaryChars
    {
        get => _maxRootSummaryChars;
        init => _maxRootSummaryChars = RequirePositive(value, nameof(MaxRootSummaryChars));
    }

    /// <summary>
    ///     Gets the most characters a line of a card body may have, or <see langword="null"/> (the default) for no limit.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is not null and not positive.</exception>
    public int? MaxLineLength
    {
        get => _maxLineLength;
        init => _maxLineLength = value is null ? null : RequirePositive(value.Value, nameof(MaxLineLength));
    }

    /// <summary>
    ///     Gets the rules of the product that are applied to every card; none by default.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the value or one of its entries is null.</exception>
    public IReadOnlyList<IHelpCardRule> Rules
    {
        get => _rules;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            var copy = value.ToList();
            if (copy.Any(rule => rule is null))
            {
                throw new ArgumentNullException(nameof(Rules), "A rule must not be null.");
            }

            _rules = copy.AsReadOnly();
        }
    }

    /// <summary>
    ///     Gets a value that turns every link to an absent external source into an error; <see langword="false"/> by default.
    /// </summary>
    /// <remarks>
    ///     Creating a catalog never fails for an absent source, because a library may link to a companion that
    ///     is not installed. A product that wants complete help sets this option.
    /// </remarks>
    public bool RequireExternalSources { get; init; }

    /// <summary>
    ///     Gets a value that makes warnings fail <see cref="HelpCardValidator.AssertValid(Loading.HelpSource, HelpValidationOptions?)"/>; <see langword="false"/> by default.
    /// </summary>
    public bool WarningsAreErrors { get; init; }

    /// <summary>
    ///     Checks that a limit is positive.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="name">The name of the property.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is not positive.</exception>
    private static int RequirePositive(int value, string name)
    {
        return value > 0
            ? value
            : throw new ArgumentOutOfRangeException(name, value, "A limit must be greater than zero.");
    }
}
