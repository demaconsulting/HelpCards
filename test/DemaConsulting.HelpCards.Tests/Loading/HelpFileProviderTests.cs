using System.Reflection;
using System.Text;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Loading;

/// <summary>
///     Unit tests for <see cref="HelpFileProvider"/> and its assembly, directory, and in-memory implementations.
/// </summary>
public class HelpFileProviderTests
{
    /// <summary>
    ///     The resource prefix of the embedding fixture in this test assembly.
    /// </summary>
    private const string FixturePrefix = "DemaConsulting.HelpCards.Tests.Help/embedding/Help";

    /// <summary>
    ///     The character that a byte order mark decodes to.
    /// </summary>
    private static readonly string ByteOrderMark = char.ConvertFromUtf32(0xFEFF);

    /// <summary>
    ///     Proves that the in-memory provider lists paths in ordinal order, not insertion order.
    /// </summary>
    [Fact]
    public void MemoryFileProvider_ListFiles_UnsortedInput_ReturnsOrdinalOrder()
    {
        // Arrange: files added out of order, including an upper-case path
        var provider = new MemoryFileProvider(new Dictionary<string, string>
        {
            ["b.md"] = "b",
            ["B.md"] = "B",
            ["a/z.md"] = "z",
            ["a.md"] = "a"
        });

        // Act: list the files
        var files = provider.ListFiles();

        // Assert: ordinal order puts upper-case before lower-case and '.' before '/'
        Assert.Equal(["B.md", "a.md", "a/z.md", "b.md"], files);
    }

    /// <summary>
    ///     Proves that the in-memory provider copies its input so later changes by the caller do not leak in.
    /// </summary>
    [Fact]
    public void MemoryFileProvider_Constructor_SourceDictionaryChanged_ProviderUnaffected()
    {
        // Arrange: a provider over a dictionary that the caller then changes
        var files = new Dictionary<string, string> { ["a.md"] = "first" };
        var provider = new MemoryFileProvider(files);
        files["a.md"] = "second";
        files["b.md"] = "added";

        // Act: read through the provider
        var text = provider.ReadText("a.md");

        // Assert: the provider still sees the original single file
        Assert.Equal("first", text);
        Assert.Equal(["a.md"], provider.ListFiles());
    }

    /// <summary>
    ///     Proves that a byte order mark character at the start of in-memory text is removed.
    /// </summary>
    [Fact]
    public void MemoryFileProvider_ReadText_LeadingByteOrderMark_IsRemoved()
    {
        // Arrange: text that starts with the byte order mark character
        var provider = new MemoryFileProvider(new Dictionary<string, string> { ["a.md"] = ByteOrderMark + "text" });

        // Act: read it
        var text = provider.ReadText("a.md");

        // Assert: the mark is gone
        Assert.Equal("text", text);
    }

    /// <summary>
    ///     Proves that reading a path that is not in the source is an argument error.
    /// </summary>
    [Fact]
    public void MemoryFileProvider_ReadText_UnknownPath_ThrowsArgumentException()
    {
        // Arrange: an empty provider
        var provider = new MemoryFileProvider(new Dictionary<string, string>());

        // Act / Assert: reading any path fails
        Assert.Throws<ArgumentException>(() => provider.ReadText("missing.md"));
    }

    /// <summary>
    ///     Proves that the assembly provider lists exactly the resources below the prefix, relative to it.
    /// </summary>
    [Fact]
    public void AssemblyFileProvider_ListFiles_ExplicitPrefix_ReturnsRelativePathsBelowPrefix()
    {
        // Arrange: the embedding fixture of this test assembly
        var provider = new AssemblyFileProvider(typeof(HelpFileProviderTests).Assembly, FixturePrefix);

        // Act: list the files
        var files = provider.ListFiles();

        // Assert: paths are relative, use '/', and are in ordinal order
        Assert.Equal(["_source.md", "guide.md", "guide/diagram.mmd", "guide/setup.md"], files);
    }

    /// <summary>
    ///     Proves that a trailing separator on the prefix is ignored.
    /// </summary>
    [Fact]
    public void AssemblyFileProvider_ListFiles_PrefixWithTrailingSlash_ReturnsSameFiles()
    {
        // Arrange: the same prefix with a trailing slash
        var provider = new AssemblyFileProvider(typeof(HelpFileProviderTests).Assembly, FixturePrefix + "/");

        // Act: list the files
        var files = provider.ListFiles();

        // Assert: the result is the same four files
        Assert.Equal(4, files.Count);
    }

    /// <summary>
    ///     Proves that the default prefix is the assembly name followed by <c>.Help</c>.
    /// </summary>
    [Fact]
    public void AssemblyFileProvider_ListFiles_NullPrefix_UsesAssemblyNameHelp()
    {
        // Arrange: the source-less project that embeds a default Help folder
        var assembly = Assembly.Load("DemaConsulting.HelpCards.Tests.EmbeddingDefault");
        var provider = new AssemblyFileProvider(assembly, null);

        // Act: list the files
        var files = provider.ListFiles();

        // Assert: the default folder content is found
        Assert.Equal(["_source.md", "first.md"], files);
    }

    /// <summary>
    ///     Proves that a prefix that matches no resource yields an empty list instead of an error.
    /// </summary>
    [Fact]
    public void AssemblyFileProvider_ListFiles_UnknownPrefix_ReturnsEmpty()
    {
        // Arrange: a prefix that no resource uses
        var provider = new AssemblyFileProvider(typeof(HelpFileProviderTests).Assembly, "No.Such.Prefix");

        // Act: list the files
        var files = provider.ListFiles();

        // Assert: nothing is found
        Assert.Empty(files);
    }

    /// <summary>
    ///     Proves that an embedded resource is read as its authored UTF-8 text.
    /// </summary>
    [Fact]
    public void AssemblyFileProvider_ReadText_EmbeddedFile_ReturnsAuthoredText()
    {
        // Arrange: the provider for the embedding fixture
        var provider = new AssemblyFileProvider(typeof(HelpFileProviderTests).Assembly, FixturePrefix);

        // Act: read a nested card
        var text = provider.ReadText("guide/setup.md");

        // Assert: the text is the card as authored
        Assert.StartsWith("---\ntitle: Setup", text.Replace("\r\n", "\n", StringComparison.Ordinal), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that reading a path that is not an embedded resource is an argument error.
    /// </summary>
    [Fact]
    public void AssemblyFileProvider_ReadText_UnknownPath_ThrowsArgumentException()
    {
        // Arrange: the provider for the embedding fixture
        var provider = new AssemblyFileProvider(typeof(HelpFileProviderTests).Assembly, FixturePrefix);

        // Act / Assert: an unlisted path fails
        Assert.Throws<ArgumentException>(() => provider.ReadText("missing.md"));
    }

    /// <summary>
    ///     Proves that the assembly provider removes a byte order mark, keeps non-ASCII text, converts a backslash in
    ///     a resource name to a slash, and refuses bytes that are not valid UTF-8.
    /// </summary>
    [Fact]
    public void AssemblyFileProvider_ReadText_ByteOrderMarkAndInvalidBytes_RemovesMarkAndRefusesInvalid()
    {
        // Arrange: a resource with a mark, one written with a backslash as MSBuild does on Windows, and a damaged one
        var assembly = new FakeResourceAssembly("Fake", new Dictionary<string, byte[]>
        {
            ["Fake.Help/mark.md"] = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("café")],
            ["Fake.Help/nested\\card.md"] = Encoding.UTF8.GetBytes("nested"),
            ["Fake.Help/bad.md"] = [0x61, 0xFF, 0x62],
            ["Other.Help/ignored.md"] = Encoding.UTF8.GetBytes("ignored")
        });
        var provider = new AssemblyFileProvider(assembly, null);

        // Act / Assert: the default prefix lists three files, the mark is gone, and invalid bytes fail
        Assert.Equal(["bad.md", "mark.md", "nested/card.md"], provider.ListFiles());
        Assert.Equal("café", provider.ReadText("mark.md"));
        Assert.Equal("nested", provider.ReadText("nested/card.md"));
        Assert.ThrowsAny<ArgumentException>(() => provider.ReadText("bad.md"));
    }

    /// <summary>
    ///     Proves that the directory provider lists nested files with <c>/</c> separators in ordinal order.
    /// </summary>
    [Fact]
    public void DirectoryFileProvider_ListFiles_NestedFiles_ReturnsRelativeOrdinalPaths()
    {
        // Arrange: a temporary directory with a nested file
        using var directory = new TemporaryDirectory();
        directory.Write("b.md", "b");
        directory.Write("a/c.md", "c");
        directory.Write("a.md", "a");
        var provider = new DirectoryFileProvider(directory.FullPath);

        // Act: list the files
        var files = provider.ListFiles();

        // Assert: relative paths use '/' and are ordered
        Assert.Equal(["a.md", "a/c.md", "b.md"], files);
    }

    /// <summary>
    ///     Proves that the directory provider reads UTF-8 text and removes a byte order mark.
    /// </summary>
    [Fact]
    public void DirectoryFileProvider_ReadText_Utf8WithByteOrderMark_RemovesMark()
    {
        // Arrange: a file written with a UTF-8 byte order mark and a non-ASCII character
        using var directory = new TemporaryDirectory();
        File.WriteAllBytes(Path.Combine(directory.FullPath, "a.md"), [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("café")]);
        var provider = new DirectoryFileProvider(directory.FullPath);

        // Act: read it
        var text = provider.ReadText("a.md");

        // Assert: the mark is gone and the character survives
        Assert.Equal("café", text);
    }

    /// <summary>
    ///     Proves that bytes that are not valid UTF-8 are rejected instead of being replaced silently.
    /// </summary>
    [Fact]
    public void DirectoryFileProvider_ReadText_InvalidUtf8_ThrowsArgumentException()
    {
        // Arrange: a file with a byte that cannot start a UTF-8 sequence
        using var directory = new TemporaryDirectory();
        File.WriteAllBytes(Path.Combine(directory.FullPath, "a.md"), [0x61, 0xFF, 0x62]);
        var provider = new DirectoryFileProvider(directory.FullPath);

        // Act / Assert: strict decoding fails (the decoder exception is an argument exception)
        Assert.ThrowsAny<ArgumentException>(() => provider.ReadText("a.md"));
    }

    /// <summary>
    ///     Proves that listing a directory that does not exist fails with a directory error.
    /// </summary>
    [Fact]
    public void DirectoryFileProvider_ListFiles_MissingDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange: a path that does not exist
        var provider = new DirectoryFileProvider(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

        // Act / Assert: listing fails
        Assert.Throws<DirectoryNotFoundException>(() => provider.ListFiles());
    }

    /// <summary>
    ///     Proves that a path that climbs out of the directory cannot be read.
    /// </summary>
    [Fact]
    public void DirectoryFileProvider_ReadText_PathOutsideDirectory_ThrowsArgumentException()
    {
        // Arrange: a file next to the help directory and a provider for the directory
        using var directory = new TemporaryDirectory();
        var provider = new DirectoryFileProvider(directory.FullPath);

        // Act / Assert: a parent-relative path and an absolute path elsewhere are refused
        Assert.Throws<ArgumentException>(() => provider.ReadText("../outside.md"));
        Assert.Throws<ArgumentException>(() => provider.ReadText(Path.Combine(Path.GetTempPath(), "outside.md")));
    }
}
