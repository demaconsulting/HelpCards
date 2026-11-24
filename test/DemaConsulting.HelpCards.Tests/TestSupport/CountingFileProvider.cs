using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Tests.TestSupport;

/// <summary>
///     A file provider that counts how often the loader lists and reads files.
/// </summary>
/// <remarks>
///     Tests use it to prove that creating a source performs no I/O and that a catalog build reads each file
///     once. It wraps in-memory files and can be told to fail for chosen paths so read-failure handling can
///     be tested without touching the file system. Counters are updated atomically.
/// </remarks>
internal sealed class CountingFileProvider : HelpFileProvider
{
    /// <summary>
    ///     The files that back the provider.
    /// </summary>
    private readonly MemoryFileProvider _inner;

    /// <summary>
    ///     The paths whose read fails.
    /// </summary>
    private readonly HashSet<string> _failingReads;

    /// <summary>
    ///     Whether listing fails.
    /// </summary>
    private readonly bool _failList;

    /// <summary>
    ///     The number of list calls.
    /// </summary>
    private int _listCount;

    /// <summary>
    ///     The number of read calls.
    /// </summary>
    private int _readCount;

    /// <summary>
    ///     Creates a counting provider.
    /// </summary>
    /// <param name="files">The files that back the provider.</param>
    /// <param name="failingReads">The paths whose read throws an <see cref="IOException"/>.</param>
    /// <param name="failList">Whether listing throws an <see cref="IOException"/>.</param>
    public CountingFileProvider(
        IReadOnlyDictionary<string, string> files,
        IEnumerable<string>? failingReads = null,
        bool failList = false)
    {
        _inner = new MemoryFileProvider(files);
        _failingReads = new HashSet<string>(failingReads ?? [], StringComparer.Ordinal);
        _failList = failList;
    }

    /// <summary>
    ///     Gets the number of list calls so far.
    /// </summary>
    public int ListCount => Volatile.Read(ref _listCount);

    /// <summary>
    ///     Gets the number of read calls so far.
    /// </summary>
    public int ReadCount => Volatile.Read(ref _readCount);

    /// <summary>
    ///     Lists the files and counts the call.
    /// </summary>
    /// <returns>The paths in ordinal order.</returns>
    public override IReadOnlyList<string> ListFiles()
    {
        Interlocked.Increment(ref _listCount);
        return _failList ? throw new IOException("The listing failed on purpose.") : _inner.ListFiles();
    }

    /// <summary>
    ///     Reads a file and counts the call.
    /// </summary>
    /// <param name="path">The path to read.</param>
    /// <returns>The text of the file.</returns>
    public override string ReadText(string path)
    {
        Interlocked.Increment(ref _readCount);
        return _failingReads.Contains(path) ? throw new IOException("The read failed on purpose.") : _inner.ReadText(path);
    }
}
