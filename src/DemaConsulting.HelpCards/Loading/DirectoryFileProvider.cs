namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     Provides the files of a directory on disk to the internal checks of the library.
/// </summary>
/// <remarks>
///     <para>
///         This provider is internal and has no public entry point: <c>HelpSource</c> offers no directory source.
///         The only product use is <c>HelpCardValidator.CompareToDirectory</c>, which lists the files of the
///         developer's own source folder at test time to compare them with the embedded resources; it never reads
///         their content. Constructing the provider does no I/O, and every listing and read goes to the file system
///         so that the provider reflects the current disk state.
///     </para>
///     <para>
///         A read validates its path lexically: a rooted path and a path that escapes the directory with
///         <c>..</c> are refused. Symbolic links and junctions are not resolved, so the directory must be a
///         folder of the developer's own repository and never an untrusted folder or a path from the input of a user.
///     </para>
/// </remarks>
internal sealed class DirectoryFileProvider : HelpFileProvider
{
    /// <summary>
    ///     The directory as given by the caller.
    /// </summary>
    private readonly string _directory;

    /// <summary>
    ///     Creates a provider without touching the file system.
    /// </summary>
    /// <param name="directory">The directory that holds the help files. Must not be null or empty.</param>
    public DirectoryFileProvider(string directory)
    {
        _directory = directory;
    }

    /// <summary>
    ///     Lists every file below the directory.
    /// </summary>
    /// <returns>The relative <c>/</c>-separated paths in ordinal order.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist.</exception>
    public override IReadOnlyList<string> ListFiles()
    {
        var root = Path.GetFullPath(_directory);
        var paths = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'));
        return SortOrdinal(paths);
    }

    /// <summary>
    ///     Reads one file below the directory as text.
    /// </summary>
    /// <param name="path">A path returned by <see cref="ListFiles"/>.</param>
    /// <returns>The text of the file.</returns>
    /// <exception cref="ArgumentException">Thrown when the path escapes the directory.</exception>
    public override string ReadText(string path)
    {
        var root = Path.GetFullPath(_directory);
        var full = Path.GetFullPath(Path.Combine(root, path));

        // Only paths below the directory are readable, so a crafted path cannot leave it
        var relative = Path.GetRelativePath(root, full);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            throw new ArgumentException($"The path '{path}' is outside the help directory.", nameof(path));
        }

        return DecodeUtf8(File.ReadAllBytes(full));
    }
}
