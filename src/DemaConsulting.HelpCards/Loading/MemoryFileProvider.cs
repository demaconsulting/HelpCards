namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     Provides the files of a source from text held in memory.
/// </summary>
/// <remarks>
///     In-memory sources make small cards easy to build in tests and in tools that generate help at run time.
///     The dictionary is copied when the provider is created, so later changes by the caller never leak into
///     a catalog. Listing and reading perform no I/O. The provider is immutable and safe for concurrent use.
/// </remarks>
internal sealed class MemoryFileProvider : HelpFileProvider
{
    /// <summary>
    ///     The files by <c>/</c>-separated relative path.
    /// </summary>
    private readonly Dictionary<string, string> _files;

    /// <summary>
    ///     Creates a provider over a copy of the given files.
    /// </summary>
    /// <param name="files">The files by relative path. Must not be null; keys must already use <c>/</c>.</param>
    public MemoryFileProvider(IReadOnlyDictionary<string, string> files)
    {
        _files = new Dictionary<string, string>(files, StringComparer.Ordinal);
    }

    /// <summary>
    ///     Lists the files.
    /// </summary>
    /// <returns>The relative paths in ordinal order.</returns>
    public override IReadOnlyList<string> ListFiles()
    {
        return SortOrdinal(_files.Keys);
    }

    /// <summary>
    ///     Reads one file.
    /// </summary>
    /// <param name="path">A path returned by <see cref="ListFiles"/>.</param>
    /// <returns>The text with a leading byte order mark character removed.</returns>
    /// <exception cref="ArgumentException">Thrown when the path is not one of the files.</exception>
    public override string ReadText(string path)
    {
        if (!_files.TryGetValue(path, out var text))
        {
            throw new ArgumentException($"The file '{path}' does not exist.", nameof(path));
        }

        return text.Length > 0 && text[0] == (char)0xFEFF ? text[1..] : text;
    }
}
