using System.Reflection;

namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     Provides the files of a source from the embedded resources of an assembly.
/// </summary>
/// <remarks>
///     The packaged MSBuild targets embed every card under the logical name
///     <c>{prefix}/{relative path}</c>, and MSBuild writes the relative path with the separator of the build
///     machine, so listing converts <c>\</c> to <c>/</c> and remembers the real resource name for each
///     converted path. The prefix defaults to <c>{AssemblyName}.Help</c> and is resolved at the first listing,
///     so constructing the provider does no I/O. The first listing is cached because the resource set of a
///     loaded assembly never changes; the cache is created under a lock and then only read.
/// </remarks>
internal sealed class AssemblyFileProvider : HelpFileProvider
{
    /// <summary>
    ///     The assembly that holds the resources.
    /// </summary>
    private readonly Assembly _assembly;

    /// <summary>
    ///     The resource prefix as given, or <see langword="null"/> for the default.
    /// </summary>
    private readonly string? _prefix;

    /// <summary>
    ///     Guards the creation of <see cref="_resources"/>.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    ///     The relative path to real resource name map, or <see langword="null"/> before the first listing.
    /// </summary>
    private Dictionary<string, string>? _resources;

    /// <summary>
    ///     Creates a provider without touching the assembly.
    /// </summary>
    /// <param name="assembly">The assembly that holds the resources. Must not be null.</param>
    /// <param name="prefix">The resource prefix, or <see langword="null"/> for <c>{AssemblyName}.Help</c>.</param>
    public AssemblyFileProvider(Assembly assembly, string? prefix)
    {
        _assembly = assembly;
        _prefix = prefix;
    }

    /// <summary>
    ///     Lists the resources whose names start with the prefix and a <c>/</c>.
    /// </summary>
    /// <returns>The relative paths in ordinal order.</returns>
    public override IReadOnlyList<string> ListFiles()
    {
        return SortOrdinal(GetResources().Keys);
    }

    /// <summary>
    ///     Reads one embedded resource as text.
    /// </summary>
    /// <param name="path">A path returned by <see cref="ListFiles"/>.</param>
    /// <returns>The text of the resource.</returns>
    /// <exception cref="ArgumentException">Thrown when the path is not an embedded resource.</exception>
    public override string ReadText(string path)
    {
        if (!GetResources().TryGetValue(path, out var resourceName))
        {
            throw new ArgumentException($"The resource '{path}' is not embedded in the assembly.", nameof(path));
        }

        // Read the whole resource so the strict decoder sees every byte at once
        using var stream = _assembly.GetManifestResourceStream(resourceName)
            ?? throw new IOException($"The resource '{resourceName}' could not be opened.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return DecodeUtf8(buffer.ToArray());
    }

    /// <summary>
    ///     Gets the path to resource name map, creating it on first use.
    /// </summary>
    /// <returns>The map, keyed by <c>/</c>-separated relative path.</returns>
    private Dictionary<string, string> GetResources()
    {
        lock (_gate)
        {
            return _resources ??= BuildResourceMap();
        }
    }

    /// <summary>
    ///     Builds the map from the manifest resource names of the assembly.
    /// </summary>
    /// <returns>The map, keyed by <c>/</c>-separated relative path.</returns>
    private Dictionary<string, string> BuildResourceMap()
    {
        // The default prefix is the assembly name followed by ".Help"; trailing separators are not part of it
        var prefix = (_prefix ?? $"{_assembly.GetName().Name}.Help").TrimEnd('/', '\\') + "/";
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in _assembly.GetManifestResourceNames())
        {
            // Normalize the separator MSBuild wrote, then keep only resources below the prefix
            var normalized = name.Replace('\\', '/');
            if (normalized.StartsWith(prefix, StringComparison.Ordinal) && normalized.Length > prefix.Length)
            {
                map[normalized[prefix.Length..]] = name;
            }
        }

        return map;
    }
}
