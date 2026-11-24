namespace DemaConsulting.HelpCards.Tests.TestSupport;

/// <summary>
///     A temporary directory that is deleted when disposed.
/// </summary>
/// <remarks>
///     Tests use it to prove that directory sources read what is on disk. Each instance gets a unique
///     folder below the system temporary folder, so tests that run in parallel never share files.
/// </remarks>
internal sealed class TemporaryDirectory : IDisposable
{
    /// <summary>
    ///     Creates the directory.
    /// </summary>
    public TemporaryDirectory()
    {
        FullPath = Path.Combine(Path.GetTempPath(), "help-cards-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(FullPath);
    }

    /// <summary>
    ///     Gets the full path of the directory.
    /// </summary>
    public string FullPath { get; }

    /// <summary>
    ///     Writes a UTF-8 file without a byte order mark, creating folders as needed.
    /// </summary>
    /// <param name="relativePath">The <c>/</c>-separated path below the directory.</param>
    /// <param name="text">The text.</param>
    public void Write(string relativePath, string text)
    {
        var full = Path.Combine(FullPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, text);
    }

    /// <summary>
    ///     Deletes the directory and everything in it.
    /// </summary>
    public void Dispose()
    {
        Directory.Delete(FullPath, true);
    }
}
