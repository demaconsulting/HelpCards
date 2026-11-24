using System.Globalization;
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Composition;

/// <summary>
///     Configures the sources and settings of a catalog; the catalog callback receives one.
/// </summary>
/// <remarks>
///     <para>
///         A builder is created only by <see cref="HelpCatalog.Create"/> (and by the validator) and is meant to
///         be used inside the configuration callback. Every method returns the builder so calls can be chained.
///         Argument errors throw <see cref="ArgumentException"/> or <see cref="ArgumentNullException"/> at once;
///         defects of the sources are found when the catalog is created and are reported together.
///     </para>
///     <para>
///         Several sources compose into one catalog; see <see cref="AddSource"/> and <see cref="AddMountedSource"/>.
///         A builder is not safe for concurrent use. The public signatures use only base class library types and
///         <see cref="HelpSource"/>.
///     </para>
/// </remarks>
public sealed partial class HelpCatalogBuilder
{
    /// <summary>
    ///     The registered sources, flat or mounted, in registration order.
    /// </summary>
    private readonly List<SourceRegistration> _registrations = [];

    /// <summary>
    ///     The sources of the registrations in registration order, for the validator.
    /// </summary>
    private readonly List<HelpSource> _sources = [];

    /// <summary>
    ///     The culture whose translations are used.
    /// </summary>
    private CultureInfo _culture = CultureInfo.InvariantCulture;

    /// <summary>
    ///     The name of the tool, or <see langword="null"/> when none was set.
    /// </summary>
    private string? _toolName;

    /// <summary>
    ///     The version of the tool, or <see langword="null"/> when none was set.
    /// </summary>
    private string? _toolVersion;

    /// <summary>
    ///     The closed tag vocabulary, or <see langword="null"/> when tags are free-form.
    /// </summary>
    private HashSet<string>? _allowedTags;

    /// <summary>
    ///     The tags of which every card needs at least one; empty when none are required.
    /// </summary>
    private HashSet<string> _requiredTags = new(StringComparer.Ordinal);

    /// <summary>
    ///     The expected global ids of the roots, or <see langword="null"/> when any roots are accepted.
    /// </summary>
    private HashSet<string>? _expectedRoots;

    /// <summary>
    ///     Creates an empty builder.
    /// </summary>
    internal HelpCatalogBuilder()
    {
    }

    /// <summary>
    ///     Gets the registered sources in registration order, for the validator.
    /// </summary>
    internal IReadOnlyList<HelpSource> Sources => _sources;

    /// <summary>
    ///     Registers a source whose cards are added to the catalog without a mount.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The source is not read here; its files are read when the catalog is created. The cards of a flat
    ///         source keep their local ids as global ids, so its roots become roots of the catalog.
    ///     </para>
    ///     <para>
    ///         Registration order is significant: it orders the roots, orders overlays, and decides which sources
    ///         may be imported, because a source can only import sources that were added before it.
    ///     </para>
    /// </remarks>
    /// <param name="source">The source. Must not be null.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    public HelpCatalogBuilder AddSource(HelpSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _registrations.Add(new SourceRegistration(source, false, null));
        _sources.Add(source);
        return this;
    }

    /// <summary>
    ///     Registers a source whose cards are mounted below a generated root card.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The catalog gets a root card whose id is the mount; its title, summary, aliases, and landing text
    ///         come from the source's <c>_source.md</c> and its children are the roots of the source. The global
    ///         id of each card of the source is <c>mount/localId</c>. A mount renames only global ids: cards keep
    ///         referring to each other by local id.
    ///     </para>
    ///     <para>
    ///         An invalid mount, or one that another mount or a flat root already uses, is reported when the
    ///         catalog is created.
    ///     </para>
    /// </remarks>
    /// <param name="source">The source. Must not be null.</param>
    /// <param name="mount">
    ///     The mount, one segment, or <see langword="null"/> for the declared name of the source. Must not be
    ///     empty or white space when given.
    /// </param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="mount"/> is empty or white space.</exception>
    public HelpCatalogBuilder AddMountedSource(HelpSource source, string? mount = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (mount is not null && string.IsNullOrWhiteSpace(mount))
        {
            throw new ArgumentException("The mount must not be empty.", nameof(mount));
        }

        _registrations.Add(new SourceRegistration(source, true, mount));
        _sources.Add(source);
        return this;
    }
    /// <summary>
    ///     Sets the culture whose translations are used.
    /// </summary>
    /// <remarks>
    ///     The default is the invariant culture, which selects the neutral files only. The ambient UI culture is
    ///     never used.
    /// </remarks>
    /// <param name="culture">The culture. Must not be null.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="culture"/> is null.</exception>
    public HelpCatalogBuilder Culture(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        _culture = culture;
        return this;
    }

    /// <summary>
    ///     Sets the name of the tool, which is the value of the built-in <c>{{tool}}</c> token.
    /// </summary>
    /// <remarks>
    ///     Without a name, <c>{{tool}}</c> is the name of the first source.
    /// </remarks>
    /// <param name="name">The tool name. Must not be null, empty, or white space.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null, empty, or white space.</exception>
    public HelpCatalogBuilder ToolName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _toolName = name;
        return this;
    }

    /// <summary>
    ///     Sets the version of the tool, which is the value of the built-in <c>{{version}}</c> token.
    /// </summary>
    /// <remarks>
    ///     Without a version, <c>{{version}}</c> is empty.
    /// </remarks>
    /// <param name="version">The version text. Must not be null.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="version"/> is null.</exception>
    public HelpCatalogBuilder ToolVersion(string version)
    {
        ArgumentNullException.ThrowIfNull(version);
        _toolVersion = version;
        return this;
    }

    /// <summary>
    ///     Closes the set of tags cards may use and optionally requires every card to have one of some tags.
    /// </summary>
    /// <remarks>
    ///     A card with a tag that is not allowed fails with <c>tag-unknown</c>; a card with none of the required
    ///     tags fails with <c>tag-required</c>. The allowed set also closes the tags that the catalog filters
    ///     accept.
    /// </remarks>
    /// <param name="allowed">The allowed tags, each a segment. Must not be null.</param>
    /// <param name="requireOneOf">The tags of which every card needs one; each must be allowed. May be null.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="allowed"/> is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when a tag is not a valid segment or a required tag is not in the allowed set.
    /// </exception>
    public HelpCatalogBuilder TagVocabulary(IEnumerable<string> allowed, IEnumerable<string>? requireOneOf = null)
    {
        ArgumentNullException.ThrowIfNull(allowed);
        var allowedSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tag in allowed)
        {
            RequireSegment(tag, nameof(allowed));
            allowedSet.Add(tag);
        }

        var requiredSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tag in requireOneOf ?? [])
        {
            RequireSegment(tag, nameof(requireOneOf));
            if (!allowedSet.Contains(tag))
            {
                throw new ArgumentException($"The required tag '{tag}' is not in the allowed tags.", nameof(requireOneOf));
            }

            requiredSet.Add(tag);
        }

        _allowedTags = allowedSet;
        _requiredTags = requiredSet;
        return this;
    }

    /// <summary>
    ///     Requires the roots of the catalog to be exactly the given ids.
    /// </summary>
    /// <remarks>
    ///     A difference in either direction fails the build with <c>roots-mismatch</c>. Root ids are single
    ///     segments.
    /// </remarks>
    /// <param name="ids">The expected global ids of the roots. Must not be null.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ids"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an id is not a single valid segment.</exception>
    public HelpCatalogBuilder ExpectedRoots(IEnumerable<string> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in ids)
        {
            RequireSegment(id, nameof(ids));
            set.Add(id);
        }

        _expectedRoots = set;
        return this;
    }

    /// <summary>
    ///     Checks that a value is a valid segment.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="parameterName">The parameter the value came from.</param>
    /// <exception cref="ArgumentException">Thrown when the value is not a segment.</exception>
    private static void RequireSegment(string? value, string parameterName)
    {
        if (!CardReader.IsSegment(value))
        {
            throw new ArgumentException(
                $"'{value}' is not valid; use lower-case letters and digits with single dashes (1 to 48 characters).",
                parameterName);
        }
    }
}
