using System.Text;

namespace DemaConsulting.HelpCards.Loading;

/// <summary>
///     The seam through which the loader lists and reads the files of one help source.
/// </summary>
/// <remarks>
///     Every source kind (assembly resources, a directory, or in-memory text) hides behind this class so the
///     rest of the loader never knows where bytes come from, and so tests can count or fail calls. A provider
///     is created without any I/O; listing and reading happen only when a catalog is built. Paths are relative
///     to the help root, use <c>/</c> as the separator, and are listed in <see cref="StringComparer.Ordinal"/>
///     order so every build sees the same sequence. Implementations are safe for concurrent use.
/// </remarks>
internal abstract class HelpFileProvider
{
    /// <summary>
    ///     The encoding used to read every file: UTF-8 that rejects invalid bytes.
    /// </summary>
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    /// <summary>
    ///     Lists the files of the source.
    /// </summary>
    /// <returns>The relative <c>/</c>-separated paths in ordinal order.</returns>
    /// <exception cref="IOException">Thrown when the files cannot be listed.</exception>
    public abstract IReadOnlyList<string> ListFiles();

    /// <summary>
    ///     Reads one file as text.
    /// </summary>
    /// <param name="path">A path returned by <see cref="ListFiles"/>.</param>
    /// <returns>The UTF-8 text of the file with a byte order mark removed.</returns>
    /// <exception cref="IOException">Thrown when the file cannot be read.</exception>
    /// <exception cref="ArgumentException">Thrown when the path was not listed or the bytes are not UTF-8.</exception>
    public abstract string ReadText(string path);

    /// <summary>
    ///     Decodes the bytes of a file as strict UTF-8 and removes a leading byte order mark.
    /// </summary>
    /// <remarks>
    ///     Strict decoding turns invalid bytes into an error that is reported as a read problem; silently
    ///     replacing them with a substitute character would hide a damaged card.
    /// </remarks>
    /// <param name="bytes">The raw bytes of the file.</param>
    /// <returns>The decoded text.</returns>
    /// <exception cref="ArgumentException">Thrown when the bytes are not valid UTF-8.</exception>
    protected static string DecodeUtf8(byte[] bytes)
    {
        // A UTF-8 byte order mark is three bytes that are not part of the content
        var offset = bytes is [0xEF, 0xBB, 0xBF, ..] ? 3 : 0;
        return StrictUtf8.GetString(bytes, offset, bytes.Length - offset);
    }

    /// <summary>
    ///     Sorts paths in ordinal order into a new read-only list.
    /// </summary>
    /// <param name="paths">The paths to sort.</param>
    /// <returns>The sorted paths.</returns>
    protected static IReadOnlyList<string> SortOrdinal(IEnumerable<string> paths)
    {
        var sorted = paths.ToList();
        sorted.Sort(StringComparer.Ordinal);
        return sorted;
    }
}
