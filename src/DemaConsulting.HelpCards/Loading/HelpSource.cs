using System.Reflection;

namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     Describes where the help cards of one library or tool come from, without reading them.
/// </summary>
/// <remarks>
///     <para>
///         A source is the unit a library exposes to its adopters: one place for the cards, an optional
///         declared name, the values of the <c>{{tokens}}</c> its cards use, and the sources it imports. It is
///         deliberately cheap: creating a source performs no I/O, so a library can keep one in a static field
///         without slowing its start-up. Files are listed and read only when a catalog is built.
///     </para>
///     <para>
///         A source is immutable. Every <c>With...</c> and <see cref="Imports"/> call returns a new source and
///         leaves the original untouched, so a shared source can never be changed by an adopter. The public
///         signatures use only base class library types so the type can appear in the API of any library.
///         Instances are safe for concurrent use.
///     </para>
/// </remarks>
public sealed class HelpSource
{
    /// <summary>
    ///     The tokens of a source that has none.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> NoTokens =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    ///     The sources this source imports, in declaration order.
    /// </summary>
    private readonly IReadOnlyList<HelpSource> _imports;

    /// <summary>
    ///     Creates a source.
    /// </summary>
    /// <param name="provider">Lists and reads the files of the source.</param>
    /// <param name="name">The declared name, or <see langword="null"/> when the source declares it in <c>_source.md</c>.</param>
    /// <param name="tokens">The token values of the source.</param>
    /// <param name="imports">The imported sources in declaration order.</param>
    private HelpSource(
        HelpFileProvider provider,
        string? name,
        IReadOnlyDictionary<string, string> tokens,
        IReadOnlyList<HelpSource> imports)
    {
        Provider = provider;
        Name = name;
        Tokens = tokens;
        _imports = imports;
    }

    /// <summary>
    ///     Gets the object that lists and reads the files of the source.
    /// </summary>
    internal HelpFileProvider Provider { get; }

    /// <summary>
    ///     Gets the name set with <see cref="WithName"/>, or <see langword="null"/> when none was set.
    /// </summary>
    internal string? Name { get; }

    /// <summary>
    ///     Gets the token values supplied with <see cref="WithTokens"/>.
    /// </summary>
    internal IReadOnlyDictionary<string, string> Tokens { get; }

    /// <summary>
    ///     Gets the sources this source imports, in declaration order.
    /// </summary>
    internal IReadOnlyList<HelpSource> ImportedSources => _imports;

    /// <summary>
    ///     Creates a source from the embedded resources of an assembly.
    /// </summary>
    /// <remarks>
    ///     The resources are the files that the packaged MSBuild targets embed under the logical name prefix.
    ///     The assembly is not inspected until a catalog is built.
    /// </remarks>
    /// <param name="assembly">The assembly that holds the cards. Must not be null.</param>
    /// <param name="resourcePrefix">
    ///     The logical name prefix of the resources, or <see langword="null"/> for <c>{AssemblyName}.Help</c>.
    ///     A trailing <c>/</c> is ignored. Must not be empty or white space when given.
    /// </param>
    /// <returns>The new source.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assembly"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="resourcePrefix"/> is empty or white space.</exception>
    public static HelpSource FromAssembly(Assembly assembly, string? resourcePrefix = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        if (resourcePrefix is not null && string.IsNullOrWhiteSpace(resourcePrefix))
        {
            throw new ArgumentException("The resource prefix must not be empty.", nameof(resourcePrefix));
        }

        return Create(new AssemblyFileProvider(assembly, resourcePrefix), null);
    }

    /// <summary>
    ///     Creates a source from text held in memory.
    /// </summary>
    /// <remarks>
    ///     The dictionary is copied, so later changes by the caller do not affect the source. Separators in
    ///     the keys may be <c>/</c> or <c>\</c>; both are stored as <c>/</c>.
    /// </remarks>
    /// <param name="files">The UTF-8 text of each file by path under the help root. Must not be null.</param>
    /// <param name="name">The declared name of the source, or <see langword="null"/> to take it from <c>_source.md</c>.</param>
    /// <returns>The new source.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="files"/> is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when a path or text is null, a path is empty, two paths are the same after separator
    ///     normalization, or <paramref name="name"/> is empty or white space.
    /// </exception>
    public static HelpSource FromFiles(IReadOnlyDictionary<string, string> files, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(files);

        // Copy with normalized separators so the caller cannot change the source afterwards
        var copy = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (path, text) in files)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A file path must not be empty.", nameof(files));
            }

            if (text is null)
            {
                throw new ArgumentException($"The text of '{path}' must not be null.", nameof(files));
            }

            if (!copy.TryAdd(path.Replace('\\', '/'), text))
            {
                throw new ArgumentException($"The path '{path}' is given twice.", nameof(files));
            }
        }

        var source = Create(new MemoryFileProvider(copy), null);
        return name is null ? source : source.WithName(name);
    }

    /// <summary>
    ///     Creates a source that reads through the given provider.
    /// </summary>
    /// <remarks>
    ///     This is the seam that lets tests count or fail provider calls; it is not part of the public API.
    /// </remarks>
    /// <param name="provider">The provider. Must not be null.</param>
    /// <param name="name">The declared name, or <see langword="null"/>.</param>
    /// <returns>The new source.</returns>
    internal static HelpSource FromProvider(HelpFileProvider provider, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return Create(provider, name);
    }

    /// <summary>
    ///     Returns a copy of this source with a declared name.
    /// </summary>
    /// <remarks>
    ///     A name given here takes precedence over a <c>name</c> in <c>_source.md</c>. The grammar of the
    ///     name is checked when the catalog is built, where a violation is reported as a problem together
    ///     with the other defects of the source.
    /// </remarks>
    /// <param name="name">The name of the source. Must not be null, empty, or white space.</param>
    /// <returns>A new source; this source is unchanged.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null, empty, or white space.</exception>
    public HelpSource WithName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new HelpSource(Provider, name, Tokens, _imports);
    }

    /// <summary>
    ///     Returns a copy of this source with the values of the <c>{{tokens}}</c> its cards use.
    /// </summary>
    /// <remarks>
    ///     The tokens replace any tokens set earlier. The dictionary is copied. The names <c>tool</c> and
    ///     <c>version</c> are built in and are reported as <c>token-redefined</c> when the catalog is built.
    /// </remarks>
    /// <param name="tokens">The token values by name. Names use letters and digits only. Must not be null.</param>
    /// <returns>A new source; this source is unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tokens"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a name is not made of letters and digits or a value is null.</exception>
    public HelpSource WithTokens(IReadOnlyDictionary<string, string> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        var copy = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, value) in tokens)
        {
            if (!TokenResolver.IsValidTokenName(name))
            {
                throw new ArgumentException(
                    $"The token name '{name}' must be one or more ASCII letters and digits.",
                    nameof(tokens));
            }

            copy[name] = value ?? throw new ArgumentException($"The value of the token '{name}' must not be null.", nameof(tokens));
        }

        return new HelpSource(Provider, Name, copy, _imports);
    }

    /// <summary>
    ///     Returns a copy of this source that declares it belongs to the same product as another source.
    /// </summary>
    /// <remarks>
    ///     An importing source sees the tokens and the local card ids of the imported source. The imported
    ///     source must be added to the catalog first; the order, existence, and cycles of imports are checked
    ///     when the catalog is built.
    /// </remarks>
    /// <param name="imported">The source to import. Must not be null and must not be this source.</param>
    /// <returns>A new source; this source is unchanged.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="imported"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="imported"/> is this source.</exception>
    public HelpSource Imports(HelpSource imported)
    {
        ArgumentNullException.ThrowIfNull(imported);
        if (ReferenceEquals(imported, this))
        {
            throw new ArgumentException("A source cannot import itself.", nameof(imported));
        }

        return new HelpSource(Provider, Name, Tokens, [.. _imports, imported]);
    }

    /// <summary>
    ///     Creates a source with no tokens and no imports.
    /// </summary>
    /// <param name="provider">The provider of the files.</param>
    /// <param name="name">The declared name, or <see langword="null"/>.</param>
    /// <returns>The new source.</returns>
    private static HelpSource Create(HelpFileProvider provider, string? name)
    {
        return new HelpSource(provider, name, NoTokens, []);
    }
}
