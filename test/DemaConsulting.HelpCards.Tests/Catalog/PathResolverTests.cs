using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Catalog;

/// <summary>
///     Unit tests for <see cref="PathResolver"/>.
/// </summary>
public class PathResolverTests
{
    /// <summary>
    ///     Gets the scope of a small tree where <c>a</c> has the alias <c>first</c> and <c>a/b</c> the alias <c>second</c>.
    /// </summary>
    /// <returns>The scope.</returns>
    private static CatalogScope TreeScope()
    {
        return CatalogScope.All(CatalogBuilding.Compose(
            ("a.md", SourceReading.Card(extraFrontMatter: "aliases:\n  - first\n")),
            ("a/b.md", SourceReading.Card(extraFrontMatter: "aliases:\n  - second\n")),
            ("c.md", SourceReading.Card())).Data!);
    }

    /// <summary>
    ///     Proves that segments match names and aliases ignoring case and may be separated by slashes or white space.
    /// </summary>
    [Fact]
    public void PathResolver_Find_Forms_ReturnsCard()
    {
        // Arrange
        var scope = TreeScope();
        (string Path, string ExpectedId)[] cases =
        [
            ("a/b", "a/b"), ("First Second", "a/b"), ("a\tSECOND", "a/b"), ("/a//b/", "a/b"), ("c", "c")
        ];

        // Act / Assert
        foreach (var (path, expectedId) in cases)
        {
            Assert.Equal(expectedId, PathResolver.Find(scope, path)?.Id);
        }
    }

    /// <summary>
    ///     Proves that a failure names the parent, the segment, and the valid names at that level.
    /// </summary>
    [Fact]
    public void PathResolver_Resolve_UnknownSegment_ReportsParentSegmentAndNames()
    {
        // Arrange / Act
        var match = PathResolver.Resolve(TreeScope(), "a x");

        // Assert
        Assert.Null(match.Card);
        Assert.Equal("a", match.ParentId);
        Assert.Equal("x", match.UnknownSegment);
        Assert.Equal(["b"], match.ValidNames);
    }

    /// <summary>
    ///     Proves that an empty path names nothing and lists the root names.
    /// </summary>
    [Fact]
    public void PathResolver_Resolve_EmptyPath_ListsRoots()
    {
        // Arrange / Act
        var match = PathResolver.Resolve(TreeScope(), "  ");

        // Assert
        Assert.Null(match.Card);
        Assert.Null(match.ParentId);
        Assert.Null(match.UnknownSegment);
        Assert.Equal(["a", "c"], match.ValidNames);
    }

    /// <summary>
    ///     Proves that a hidden target is reported like an unknown segment and a hidden parent is walked through.
    /// </summary>
    [Fact]
    public void PathResolver_Resolve_HiddenTarget_IsReportedAsUnknownAndHiddenParentIsWalkedThrough()
    {
        // Arrange
        var scope = TreeScope().Restrict(node => node.Id != "a");

        // Act
        var hiddenTarget = PathResolver.Resolve(scope, "a");
        var throughHidden = PathResolver.Resolve(scope, "a b");

        // Assert
        Assert.Null(hiddenTarget.Card);
        Assert.Equal("a", hiddenTarget.UnknownSegment);
        Assert.Equal(["c"], hiddenTarget.ValidNames);
        Assert.Equal("a/b", throughHidden.Card?.Id);
    }

    /// <summary>
    ///     Proves that a hidden nested target reports its real parent and the visible siblings.
    /// </summary>
    [Fact]
    public void PathResolver_Resolve_HiddenNestedTarget_ReportsRealParent()
    {
        // Arrange
        var scope = TreeScope().Restrict(node => node.Id != "a/b");

        // Act
        var match = PathResolver.Resolve(scope, "a b");

        // Assert
        Assert.Null(match.Card);
        Assert.Equal("a", match.ParentId);
        Assert.Equal("b", match.UnknownSegment);
        Assert.Empty(match.ValidNames);
    }

    /// <summary>
    ///     Proves that an alias of a translation is matched in its normalized form, whichever form the path uses.
    /// </summary>
    [Fact]
    public void PathResolver_Find_TranslationAliasInDecomposedForm_MatchesNormalizedAlias()
    {
        // Arrange: the translation lists the alias with a combining accent (decomposed)
        var files = SourceReading.Files(
            ("a.md", SourceReading.Card()),
            ("a.de.md", SourceReading.Card(extraFrontMatter: "aliases:\n  - cafe\u0301\n")));
        var source = HelpSource.FromFiles(files, "test");
        var data = CatalogBuilding.Compose(source, builder => builder.Culture(CatalogBuilding.Culture("de"))).Data!;
        var scope = CatalogScope.All(data);

        // Act / Assert
        Assert.Equal("a", PathResolver.Find(scope, "caf\u00e9")?.Id);
        Assert.Equal("a", PathResolver.Find(scope, "cafe\u0301")?.Id);
    }

    /// <summary>
    ///     Proves that a failed walk suggests the closest name at the level that failed, and none when nothing is close.
    /// </summary>
    [Fact]
    public void PathResolver_Resolve_UnknownSegment_SuggestsClosestNameAtThatLevel()
    {
        // Arrange
        var scope = TreeScope();

        // Act
        var close = PathResolver.Resolve(scope, "a c");
        var far = PathResolver.Resolve(scope, "a qqqqqq");
        var empty = PathResolver.Resolve(scope, string.Empty);

        // Assert
        Assert.Equal("b", close.Suggestion);
        Assert.Null(far.Suggestion);
        Assert.Null(empty.Suggestion);
    }

    /// <summary>
    ///     Proves that null arguments are rejected.
    /// </summary>
    [Fact]
    public void PathResolver_NullArguments_ThrowArgumentNullException()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => PathResolver.Resolve(null!, "a"));
        Assert.Throws<ArgumentNullException>(() => PathResolver.Resolve(TreeScope(), null!));
    }
}
