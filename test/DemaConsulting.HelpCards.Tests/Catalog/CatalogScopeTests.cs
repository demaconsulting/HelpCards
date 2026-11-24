using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Catalog;

/// <summary>
///     Unit tests for <see cref="CatalogScope"/>.
/// </summary>
public class CatalogScopeTests
{
    /// <summary>
    ///     Gets the data of a small tree: <c>a</c> with <c>a/b</c> and <c>a/c</c>, and <c>d</c>.
    /// </summary>
    /// <returns>The data.</returns>
    private static CatalogData TreeData()
    {
        return CatalogBuilding.Compose(
            ("a.md", SourceReading.Card()),
            ("a/b.md", SourceReading.Card()),
            ("a/c.md", SourceReading.Card()),
            ("d.md", SourceReading.Card())).Data!;
    }

    /// <summary>
    ///     Proves that the scope of everything shows every card with its real tree.
    /// </summary>
    [Fact]
    public void CatalogScope_All_ShowsEveryCardWithRealTree()
    {
        // Arrange / Act
        var scope = CatalogScope.All(TreeData());

        // Assert
        Assert.True(scope.ShowsEverything);
        Assert.Equal(["a", "a/b", "a/c", "d"], scope.Cards.Select(node => node.Id));
        Assert.Equal(["a", "d"], scope.Roots.Select(node => node.Id));
        Assert.Equal(["a/b", "a/c"], scope.GetChildren(scope.Data.Find("a")!).Select(node => node.Id));
        Assert.Equal("a", scope.GetParentId(scope.Data.Find("a/b")!));
        Assert.Null(scope.GetParentId(scope.Data.Find("a")!));
    }

    /// <summary>
    ///     Proves that a restriction hides cards and lifts their visible children.
    /// </summary>
    [Fact]
    public void CatalogScope_Restrict_HiddenParent_LiftsChildrenToRoots()
    {
        // Arrange
        var scope = CatalogScope.All(TreeData());

        // Act
        var restricted = scope.Restrict(node => node.Id != "a");

        // Assert
        Assert.False(restricted.ShowsEverything);
        Assert.False(restricted.IsVisible("a"));
        Assert.Equal(["a/b", "a/c", "d"], restricted.Roots.Select(node => node.Id));
        Assert.Empty(restricted.GetChildren(restricted.Data.Find("a")!));
        Assert.Null(restricted.GetParentId(restricted.Data.Find("a/b")!));
    }

    /// <summary>
    ///     Proves that restricting twice gives the intersection and leaves the earlier scopes unchanged.
    /// </summary>
    [Fact]
    public void CatalogScope_Restrict_Twice_IsIntersectionAndImmutable()
    {
        // Arrange
        var all = CatalogScope.All(TreeData());
        var first = all.Restrict(node => node.Id != "a/b");

        // Act
        var second = first.Restrict(node => node.Id != "d");

        // Assert
        Assert.Equal(["a", "a/c"], second.Cards.Select(node => node.Id));
        Assert.Equal(["a", "a/c", "d"], first.Cards.Select(node => node.Id));
        Assert.Equal(4, all.Cards.Count);
    }

    /// <summary>
    ///     Proves that a restriction is called once per visible card, in depth-first order.
    /// </summary>
    [Fact]
    public void CatalogScope_Restrict_Predicate_RunsOncePerVisibleCardInOrder()
    {
        // Arrange
        var scope = CatalogScope.All(TreeData()).Restrict(node => node.Id != "a/c");
        var seen = new List<string>();

        // Act
        _ = scope.Restrict(node =>
        {
            seen.Add(node.Id);
            return true;
        });

        // Assert: the hidden card is not offered again
        Assert.Equal(["a", "a/b", "d"], seen);
    }

    /// <summary>
    ///     Proves that null arguments are rejected.
    /// </summary>
    [Fact]
    public void CatalogScope_NullArguments_ThrowArgumentNullException()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => CatalogScope.All(null!));
        Assert.Throws<ArgumentNullException>(() => CatalogScope.All(TreeData()).Restrict(null!));
    }
}
