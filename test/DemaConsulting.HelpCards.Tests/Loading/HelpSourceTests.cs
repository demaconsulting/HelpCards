using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Loading;

/// <summary>
///     Unit tests for <see cref="HelpSource"/>.
/// </summary>
public class HelpSourceTests
{
    /// <summary>
    ///     Proves that a null assembly is rejected.
    /// </summary>
    [Fact]
    public void HelpSource_FromAssembly_NullAssembly_ThrowsArgumentNullException()
    {
        // Arrange / Act / Assert: a null assembly cannot hold cards
        Assert.Throws<ArgumentNullException>(() => HelpSource.FromAssembly(null!));
    }

    /// <summary>
    ///     Proves that an empty resource prefix is rejected.
    /// </summary>
    [Fact]
    public void HelpSource_FromAssembly_EmptyPrefix_ThrowsArgumentException()
    {
        // Arrange / Act / Assert: an empty prefix would match every resource
        Assert.Throws<ArgumentException>(() => HelpSource.FromAssembly(typeof(HelpSourceTests).Assembly, " "));
    }

    /// <summary>
    ///     Proves that a source created from an assembly reads the embedded cards through the default prefix.
    /// </summary>
    [Fact]
    public void HelpSource_FromAssembly_DefaultPrefix_ListsEmbeddedFiles()
    {
        // Arrange: the source-less project that embeds a default Help folder
        var assembly = System.Reflection.Assembly.Load("DemaConsulting.HelpCards.Tests.EmbeddingDefault");

        // Act: create the source and list its files through its provider
        var source = HelpSource.FromAssembly(assembly);

        // Assert: the default folder is found without any prefix argument
        Assert.Equal(["_source.md", "first.md"], source.Provider.ListFiles());
    }

    /// <summary>
    ///     Proves that an empty or white-space directory is rejected.
    /// </summary>
    [Fact]
    public void HelpSource_FromDirectory_EmptyDirectory_ThrowsArgumentException()
    {
        // Arrange / Act / Assert: no directory is not a directory
        Assert.Throws<ArgumentException>(() => HelpSource.FromDirectory(string.Empty));
    }

    /// <summary>
    ///     Proves that a source created from a directory reads the files on disk.
    /// </summary>
    [Fact]
    public void HelpSource_FromDirectory_ExistingDirectory_ReadsFilesOnDisk()
    {
        // Arrange: a directory with one card
        using var directory = new TemporaryDirectory();
        directory.Write("card.md", "text");

        // Act: create the source and read through its provider
        var source = HelpSource.FromDirectory(directory.FullPath);

        // Assert: the file is listed and readable
        Assert.Equal(["card.md"], source.Provider.ListFiles());
        Assert.Equal("text", source.Provider.ReadText("card.md"));
    }

    /// <summary>
    ///     Proves that creating a source from a missing directory does not fail until a catalog is built.
    /// </summary>
    [Fact]
    public void HelpSource_FromDirectory_MissingDirectory_DoesNotThrowAtCreation()
    {
        // Arrange / Act: create a source for a directory that does not exist
        var source = HelpSource.FromDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

        // Assert: creation succeeded; the failure is deferred to reading
        Assert.NotNull(source);
    }

    /// <summary>
    ///     Proves that a null file dictionary is rejected.
    /// </summary>
    [Fact]
    public void HelpSource_FromFiles_NullFiles_ThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => HelpSource.FromFiles(null!));
    }

    /// <summary>
    ///     Proves that two paths that differ only in separator are rejected as duplicates.
    /// </summary>
    [Fact]
    public void HelpSource_FromFiles_PathsEqualAfterSeparatorNormalization_ThrowsArgumentException()
    {
        // Arrange: the same file written with both separators
        var files = new Dictionary<string, string> { ["a/b.md"] = "x", ["a\\b.md"] = "y" };

        // Act / Assert: the duplicate is refused
        Assert.Throws<ArgumentException>(() => HelpSource.FromFiles(files));
    }

    /// <summary>
    ///     Proves that a null text is rejected.
    /// </summary>
    [Fact]
    public void HelpSource_FromFiles_NullText_ThrowsArgumentException()
    {
        // Arrange: a file without text
        var files = new Dictionary<string, string> { ["a.md"] = null! };

        // Act / Assert
        Assert.Throws<ArgumentException>(() => HelpSource.FromFiles(files));
    }

    /// <summary>
    ///     Proves that backslashes in paths are stored as forward slashes.
    /// </summary>
    [Fact]
    public void HelpSource_FromFiles_BackslashPath_IsStoredWithForwardSlash()
    {
        // Arrange / Act: create a source with a Windows-style path
        var source = HelpSource.FromFiles(new Dictionary<string, string> { ["a\\b.md"] = "x" });

        // Assert: the path is listed with '/'
        Assert.Equal(["a/b.md"], source.Provider.ListFiles());
    }

    /// <summary>
    ///     Proves that the source does not change when the caller changes the dictionary afterwards.
    /// </summary>
    [Fact]
    public void HelpSource_FromFiles_DictionaryChangedAfterCreation_SourceUnchanged()
    {
        // Arrange: a source created from a dictionary that is then changed
        var files = new Dictionary<string, string> { ["a.md"] = "x" };
        var source = HelpSource.FromFiles(files);
        files["b.md"] = "y";

        // Act: list the files
        var listed = source.Provider.ListFiles();

        // Assert: only the original file is there
        Assert.Equal(["a.md"], listed);
    }

    /// <summary>
    ///     Proves that the name given at creation is kept.
    /// </summary>
    [Fact]
    public void HelpSource_FromFiles_WithName_KeepsName()
    {
        // Arrange / Act
        var source = HelpSource.FromFiles(new Dictionary<string, string>(), "lib");

        // Assert
        Assert.Equal("lib", source.Name);
    }

    /// <summary>
    ///     Proves that <c>WithName</c> returns a new source and leaves the original without a name.
    /// </summary>
    [Fact]
    public void HelpSource_WithName_ReturnsNewSourceAndKeepsOriginal()
    {
        // Arrange: a source without a name
        var original = HelpSource.FromFiles(new Dictionary<string, string>());

        // Act: set a name
        var named = original.WithName("lib");

        // Assert: the original is unchanged and the copy shares the provider
        Assert.Null(original.Name);
        Assert.Equal("lib", named.Name);
        Assert.NotSame(original, named);
        Assert.Same(original.Provider, named.Provider);
    }

    /// <summary>
    ///     Proves that an empty name is rejected.
    /// </summary>
    [Fact]
    public void HelpSource_WithName_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var source = HelpSource.FromFiles(new Dictionary<string, string>());

        // Act / Assert
        Assert.Throws<ArgumentException>(() => source.WithName(string.Empty));
    }

    /// <summary>
    ///     Proves that <c>WithTokens</c> returns a new source and leaves the original without tokens.
    /// </summary>
    [Fact]
    public void HelpSource_WithTokens_ReturnsNewSourceAndKeepsOriginal()
    {
        // Arrange: a source without tokens
        var original = HelpSource.FromFiles(new Dictionary<string, string>());

        // Act: set tokens
        var withTokens = original.WithTokens(new Dictionary<string, string> { ["maxDpi"] = "600" });

        // Assert: only the copy has them
        Assert.Empty(original.Tokens);
        Assert.Equal("600", withTokens.Tokens["maxDpi"]);
    }

    /// <summary>
    ///     Proves that a second call replaces the tokens of the first.
    /// </summary>
    [Fact]
    public void HelpSource_WithTokens_CalledTwice_ReplacesEarlierTokens()
    {
        // Arrange: a source with one token
        var first = HelpSource.FromFiles(new Dictionary<string, string>())
            .WithTokens(new Dictionary<string, string> { ["a"] = "1" });

        // Act: set different tokens
        var second = first.WithTokens(new Dictionary<string, string> { ["b"] = "2" });

        // Assert: only the new token remains
        Assert.Equal(["b"], second.Tokens.Keys);
    }

    /// <summary>
    ///     Proves that the caller's token dictionary is copied.
    /// </summary>
    [Fact]
    public void HelpSource_WithTokens_DictionaryChangedAfterCall_SourceUnchanged()
    {
        // Arrange: tokens that the caller changes after the call
        var tokens = new Dictionary<string, string> { ["a"] = "1" };
        var source = HelpSource.FromFiles(new Dictionary<string, string>()).WithTokens(tokens);
        tokens["a"] = "changed";

        // Act / Assert: the source still has the original value
        Assert.Equal("1", source.Tokens["a"]);
    }

    /// <summary>
    ///     Proves that a token name with characters other than letters and digits is rejected.
    /// </summary>
    [Fact]
    public void HelpSource_WithTokens_InvalidName_ThrowsArgumentException()
    {
        // Arrange
        var source = HelpSource.FromFiles(new Dictionary<string, string>());

        // Act / Assert: a dash is not allowed in a token name
        Assert.Throws<ArgumentException>(() => source.WithTokens(new Dictionary<string, string> { ["max-dpi"] = "1" }));
    }

    /// <summary>
    ///     Proves that a null token value is rejected.
    /// </summary>
    [Fact]
    public void HelpSource_WithTokens_NullValue_ThrowsArgumentException()
    {
        // Arrange
        var source = HelpSource.FromFiles(new Dictionary<string, string>());

        // Act / Assert
        Assert.Throws<ArgumentException>(() => source.WithTokens(new Dictionary<string, string> { ["a"] = null! }));
    }

    /// <summary>
    ///     Proves that a null token dictionary is rejected.
    /// </summary>
    [Fact]
    public void HelpSource_WithTokens_NullTokens_ThrowsArgumentNullException()
    {
        // Arrange
        var source = HelpSource.FromFiles(new Dictionary<string, string>());

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => source.WithTokens(null!));
    }

    /// <summary>
    ///     Proves that imports are kept in declaration order and the original is unchanged.
    /// </summary>
    [Fact]
    public void HelpSource_Imports_TwoImports_KeepsDeclarationOrderAndOriginal()
    {
        // Arrange: three sources
        var importer = HelpSource.FromFiles(new Dictionary<string, string>(), "app");
        var first = HelpSource.FromFiles(new Dictionary<string, string>(), "first");
        var second = HelpSource.FromFiles(new Dictionary<string, string>(), "second");

        // Act: import both
        var imported = importer.Imports(first).Imports(second);

        // Assert: the copy lists the imports in order and the original has none
        Assert.Equal(["first", "second"], imported.ImportedSources.Select(s => s.Name));
        Assert.Empty(importer.ImportedSources);
    }

    /// <summary>
    ///     Proves that a source cannot import itself.
    /// </summary>
    [Fact]
    public void HelpSource_Imports_Itself_ThrowsArgumentException()
    {
        // Arrange
        var source = HelpSource.FromFiles(new Dictionary<string, string>());

        // Act / Assert
        Assert.Throws<ArgumentException>(() => source.Imports(source));
    }

    /// <summary>
    ///     Proves that importing null is rejected.
    /// </summary>
    [Fact]
    public void HelpSource_Imports_Null_ThrowsArgumentNullException()
    {
        // Arrange
        var source = HelpSource.FromFiles(new Dictionary<string, string>());

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => source.Imports(null!));
    }

    /// <summary>
    ///     Proves that creating a source and changing it with the <c>With</c> methods never lists or reads files.
    /// </summary>
    [Fact]
    public void HelpSource_Construction_AllFactoriesAndWithMethods_PerformNoIo()
    {
        // Arrange: a provider that counts every list and read call
        var provider = new CountingFileProvider(new Dictionary<string, string> { ["a.md"] = "x" });

        // Act: create a source and apply every With method and an import
        var source = HelpSource.FromProvider(provider, "lib")
            .WithName("other")
            .WithTokens(new Dictionary<string, string> { ["a"] = "1" })
            .Imports(HelpSource.FromProvider(new CountingFileProvider(new Dictionary<string, string>())));

        // Assert: nothing was listed or read
        Assert.NotNull(source);
        Assert.Equal(0, provider.ListCount);
        Assert.Equal(0, provider.ReadCount);
    }
}
