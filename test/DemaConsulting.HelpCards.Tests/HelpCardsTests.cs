using System.Reflection;

namespace DemaConsulting.HelpCards.Tests;

/// <summary>
///     System-level integration tests for the HelpCards system.
/// </summary>
/// <remarks>
///     The tests in this file prove user-visible capabilities of the whole library. Each test
///     name starts with the system name followed by the capability under test.
/// </remarks>
public class HelpCardsTests
{
    /// <summary>
    ///     The assembly name of the source-less project that embeds a default Help folder.
    /// </summary>
    private const string DefaultEmbeddingAssemblyName = "DemaConsulting.HelpCards.Tests.EmbeddingDefault";

    /// <summary>
    ///     Proves that the packaged build files embed a <c>Help</c> folder under the default logical
    ///     name prefix <c>{AssemblyName}.Help</c> when the project overrides nothing.
    /// </summary>
    [Fact]
    public void HelpCards_Embedding_DefaultDirectory_UsesAssemblyNamePrefix()
    {
        // Arrange: load the project that imports the packaged files and keeps every default
        var assembly = Assembly.Load(DefaultEmbeddingAssemblyName);

        // Act: list the embedded resources in the deterministic order the loader uses
        var names = assembly.GetManifestResourceNames()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        // Assert: every file of the Help folder is embedded under "{AssemblyName}.Help/"
        Assert.Equal(
            [
                $"{DefaultEmbeddingAssemblyName}.Help/_source.md",
                $"{DefaultEmbeddingAssemblyName}.Help/first.md"
            ],
            names);
    }

    /// <summary>
    ///     Proves that a project-body override of <c>HelpCardsDirectory</c> is honored by the packaged
    ///     targets: every file below the overridden folder is embedded, including nested folders and
    ///     data files, with forward slashes in the logical names.
    /// </summary>
    [Fact]
    public void HelpCards_Embedding_OverriddenDirectory_EmbedsEveryFileUnderPrefix()
    {
        // Arrange: this test project sets HelpCardsDirectory to "Fixtures" in its project body
        var assembly = typeof(HelpCardsTests).Assembly;
        const string prefix = "DemaConsulting.HelpCards.Tests.Help/embedding/Help/";

        // Act: select the resources of the embedding fixture in deterministic order, converting
        // the backslashes MSBuild writes for RecursiveDir exactly as the loader does
        var names = assembly.GetManifestResourceNames()
            .Select(name => name.Replace('\\', '/'))
            .Where(name => name.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        // Assert: nested folders and non-Markdown data files are embedded with "/" separators
        Assert.Equal(
            [
                prefix + "_source.md",
                prefix + "guide.md",
                prefix + "guide/diagram.mmd",
                prefix + "guide/setup.md"
            ],
            names);
    }

    /// <summary>
    ///     Proves that an embedded resource can be read back as UTF-8 text with the authored content.
    /// </summary>
    [Fact]
    public void HelpCards_Embedding_EmbeddedResource_ReadsAuthoredContent()
    {
        // Arrange: pick one embedded card of the overridden fixture directory
        var assembly = typeof(HelpCardsTests).Assembly;
        const string name = "DemaConsulting.HelpCards.Tests.Help/embedding/Help/guide/setup.md";

        // Act: find the resource whose normalized name matches (MSBuild writes backslashes on
        // Windows) and read it as text
        var resourceName = assembly.GetManifestResourceNames()
            .Single(candidate => candidate.Replace('\\', '/') == name);
        using var stream = assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
        var text = reader.ReadToEnd();

        // Assert: the card text is the authored file content
        Assert.Contains("title: Setup", text, StringComparison.Ordinal);
    }
}
