using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Testing;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Testing;

/// <summary>
///     Unit tests for the comparison of embedded resources with a directory.
/// </summary>
public partial class HelpCardValidatorTests
{
    /// <summary>
    ///     Writes every file of a source into a directory.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="directory">The directory.</param>
    private static void Copy(HelpSource source, TemporaryDirectory directory)
    {
        foreach (var path in source.Provider.ListFiles())
        {
            directory.Write(path, source.Provider.ReadText(path));
        }
    }

    /// <summary>
    ///     Proves that embedded resources and a directory with the same files have no differences.
    /// </summary>
    [Fact]
    public void HelpCardValidator_CompareToDirectory_SameFiles_ReturnsNoProblems()
    {
        // Arrange
        var source = CatalogBuilding.Fixture("simple");
        using var directory = new TemporaryDirectory();
        Copy(source, directory);

        // Act
        var problems = HelpCardValidator.CompareToDirectory(source, directory.FullPath);

        // Assert
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that a file on disk that is not embedded, and an embedded file that is not on disk, are both
    ///     reported in path order as warnings.
    /// </summary>
    [Fact]
    public void HelpCardValidator_CompareToDirectory_DifferentFiles_ReportsBothDirections()
    {
        // Arrange
        var source = CatalogBuilding.Fixture("simple");
        using var directory = new TemporaryDirectory();
        Copy(source, directory);
        File.Delete(Path.Combine(directory.FullPath, "reference.md"));
        directory.Write("extra/new.md", "text");
        directory.Write("aaa.md", "text");

        // Act
        var problems = HelpCardValidator.CompareToDirectory(source, directory.FullPath);

        // Assert
        Assert.Equal(["aaa.md", "extra/new.md", "reference.md"], problems.Select(problem => problem.File));
        Assert.All(problems, problem =>
        {
            Assert.Equal(RuleIds.ResourcesMismatch, problem.RuleId);
            Assert.Equal(HelpSeverity.Warning, problem.Severity);
            Assert.Equal(0, problem.Line);
        });
        Assert.Contains("not embedded", problems[0].Message);
        Assert.Contains("not in the directory", problems[2].Message);
    }

    /// <summary>
    ///     Proves that a missing directory and invalid arguments are argument errors.
    /// </summary>
    [Fact]
    public void HelpCardValidator_CompareToDirectory_InvalidArguments_Throw()
    {
        // Arrange
        var source = CatalogBuilding.Fixture("simple");
        using var directory = new TemporaryDirectory();

        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => HelpCardValidator.CompareToDirectory(null!, directory.FullPath));
        Assert.Throws<ArgumentNullException>(() => HelpCardValidator.CompareToDirectory(source, null!));
        Assert.Throws<ArgumentException>(() => HelpCardValidator.CompareToDirectory(source, " "));
        Assert.Throws<ArgumentException>(() => HelpCardValidator.CompareToDirectory(source, Path.Combine(directory.FullPath, "missing")));
    }
}
