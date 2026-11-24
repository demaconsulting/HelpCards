namespace DemaConsulting.HelpCards.Tests.TestSupport;

/// <summary>
///     Reads the golden output files of the <c>render</c> fixture.
/// </summary>
/// <remarks>
///     A golden file holds exactly the output of a rendering followed by one line feed, so that the file ends with a
///     newline like every other file of the repository while the rendering itself has none. The files are embedded
///     in the test assembly with the other fixtures, use LF line endings only, and are compared exactly.
/// </remarks>
internal static class GoldenFiles
{
    /// <summary>
    ///     The resource prefix under which the golden files are embedded.
    /// </summary>
    private const string Prefix = "DemaConsulting.HelpCards.Tests.Help/render/";

    /// <summary>
    ///     Asserts that a rendering equals a golden file exactly.
    /// </summary>
    /// <param name="name">The file name inside the <c>render</c> fixture.</param>
    /// <param name="actual">The rendering.</param>
    public static void AssertEqual(string name, string actual)
    {
        Assert.Equal(Read(name), actual);
    }

    /// <summary>
    ///     Reads a golden file as the rendering it records.
    /// </summary>
    /// <param name="name">The file name inside the <c>render</c> fixture, such as <c>guide.text.expected.txt</c>.</param>
    /// <returns>The recorded rendering, without the line feed that ends the file.</returns>
    public static string Read(string name)
    {
        var assembly = typeof(GoldenFiles).Assembly;

        // MSBuild writes the logical name with the separators of the build machine, so compare normalized names
        var resource = assembly.GetManifestResourceNames()
            .Single(candidate => string.Equals(candidate.Replace('\\', '/'), Prefix + name, StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"The golden file '{name}' could not be opened.");
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();

        // The file must end with exactly one line feed; anything else is a broken golden file
        if (!text.EndsWith('\n') || text.EndsWith("\n\n", StringComparison.Ordinal) || text.Contains('\r', StringComparison.Ordinal))
        {
            throw new InvalidDataException($"The golden file '{name}' must use LF and end with one line feed.");
        }

        return text[..^1];
    }
}
