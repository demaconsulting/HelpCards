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
    private const int DefaultMaxRootSummaryChars = 80;

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
    ///     The backing store of <see cref="MaxFenceLineLength"/>.
    /// </summary>
    private readonly int? _maxFenceLineLength;

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
    ///     Gets the most characters the summary of a root card may have; 80 by default.
    /// </summary>
    /// <remarks>
    ///     The summary of a root card is shown in the topic index next to a command label. At the default width of 78
    ///     and a label such as <c>reqstream help requirements-file</c> (32 characters) the summary has 44 columns on
    ///     each line, so 80 characters fit on two lines, which is the most the index should spend on one topic.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is not positive.</exception>
    public int MaxRootSummaryChars
    {
        get => _maxRootSummaryChars;
        init => _maxRootSummaryChars = RequirePositive(value, nameof(MaxRootSummaryChars));
    }

    /// <summary>
    ///     Gets the most characters a line of a card body may have, or <see langword="null"/> (the default) for no limit.
    ///     Lines inside fenced code blocks use <see cref="MaxFenceLineLength"/> when it is set.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is not null and not positive.</exception>
    public int? MaxLineLength
    {
        get => _maxLineLength;
        init => _maxLineLength = value is null ? null : RequirePositive(value.Value, nameof(MaxLineLength));
    }

    /// <summary>
    ///     Gets the most characters a line inside a fenced code block may have, or <see langword="null"/> (the default)
    ///     to check those lines against <see cref="MaxLineLength"/> like every other line.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A line is inside a fence when it opens the fence, is between the fences, or closes the fence, which is the
    ///         same classification that the <c>fence-language</c> and <c>fence-unclosed</c> rules use; the lines of an
    ///         unclosed fence up to the end of the body count as inside it. When this limit is set, it replaces
    ///         <see cref="MaxLineLength"/> for those lines; every other line is still checked against
    ///         <see cref="MaxLineLength"/>. Violations are reported under the <c>line-length</c> rule.
    ///     </para>
    ///     <para>
    ///         Code and command samples are usually wider than prose, so a typical setting is <c>MaxLineLength = 80</c>
    ///         with <c>MaxFenceLineLength = 120</c>. When only this option is set, the lines inside fences are checked and
    ///         the lines outside fences are not.
    ///     </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is not null and not positive.</exception>
    public int? MaxFenceLineLength
    {
        get => _maxFenceLineLength;
        init => _maxFenceLineLength = value is null ? null : RequirePositive(value.Value, nameof(MaxFenceLineLength));
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
