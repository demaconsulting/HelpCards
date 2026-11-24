namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     Provides the files of a source from a directory on disk.
/// </summary>
/// <remarks>
///     This provider exists for authoring and tests: it lets an author edit cards and see the result without
///     rebuilding the assembly. The directory is resolved and enumerated only when a catalog is built, so
///     constructing the provider does no I/O. Every listing and every read goes to the file system, so
///     the provider reflects the current disk state. It never follows a path outside the directory because
///     a read refuses any path that resolves outside the directory (the loader only asks for listed paths).
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
