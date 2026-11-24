using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Catalog;

/// <summary>
///     Unit tests for <see cref="ExampleResolver"/>.
/// </summary>
public class ExampleResolverTests
{
    /// <summary>
    ///     Builds the front matter of a card with command examples of the given names.
    /// </summary>
    /// <param name="names">The example names.</param>
    /// <returns>The front-matter lines.</returns>
    private static string Examples(params string[] names)
    {
        var lines = names.Select(name => $"  - name: {name}\n    kind: command\n    language: shell\n    content: run {name}\n    description: Example {name}.\n");
        return "examples:\n" + string.Concat(lines);
    }

    /// <summary>
    ///     Gets the scope of a tree where <c>a</c>, <c>a/b</c>, <c>a/c</c>, <c>d</c>, <c>e</c>, and <c>e/f</c> hold examples
    ///     and the name <c>shared</c> is used in the two scopes <c>d</c> and <c>e</c>.
    /// </summary>
    /// <returns>The scope.</returns>
    private static CatalogScope TreeScope()
    {
        return CatalogScope.All(CatalogBuilding.Compose(
            ("a.md", SourceReading.Card(extraFrontMatter: Examples("own"))),
            ("a/b.md", SourceReading.Card(extraFrontMatter: Examples("deep"))),
            ("a/c.md", SourceReading.Card(extraFrontMatter: Examples("other"))),
            ("d.md", SourceReading.Card(extraFrontMatter: Examples("shared"))),
            ("e.md", SourceReading.Card(extraFrontMatter: Examples("shared"))),
            ("e/f.md", SourceReading.Card(extraFrontMatter: Examples("unrelated")))).Data!);
    }

    /// <summary>
    ///     Proves that a subtree search finds a card's own example, ignoring case.
    /// </summary>
    [Fact]
    public void ExampleResolver_FindInSubtree_OwnExample_IsFoundIgnoringCase()
    {
        // Arrange
        var scope = TreeScope();

        // Act
        var found = ExampleResolver.FindInSubtree(scope.Data.Find("a")!, "OWN", scope.GetChildren);

        // Assert
        Assert.Equal("a", found?.CardId);
        Assert.Equal("own", found?.Example.Name);
    }

    /// <summary>
    ///     Proves that a subtree search goes through the children in order and ignores other trees.
    /// </summary>
    [Fact]
    public void ExampleResolver_FindInSubtree_Children_AreSearchedAndOtherTreesAreNot()
    {
        // Arrange
        var scope = TreeScope();
        var root = scope.Data.Find("a")!;

        // Act / Assert
        Assert.Equal("a/b", ExampleResolver.FindInSubtree(root, "deep", scope.GetChildren)?.CardId);
        Assert.Equal("a/c", ExampleResolver.FindInSubtree(root, "other", scope.GetChildren)?.CardId);
        Assert.Null(ExampleResolver.FindInSubtree(root, "unrelated", scope.GetChildren));
        Assert.Null(ExampleResolver.FindInSubtree(scope.Data.Find("a/b")!, "own", scope.GetChildren));
    }

    /// <summary>
    ///     Proves that the children function decides which cards are searched.
    /// </summary>
    [Fact]
    public void ExampleResolver_FindInSubtree_ChildrenFunction_DecidesWhatIsSearched()
    {
        // Arrange
        var scope = TreeScope();

        // Act
        var found = ExampleResolver.FindInSubtree(scope.Data.Find("a")!, "deep", _ => []);

        // Assert
        Assert.Null(found);
    }

    /// <summary>
    ///     Proves that a search of a whole scope returns every match in depth-first order.
    /// </summary>
    [Fact]
    public void ExampleResolver_FindAll_NameOnly_ReturnsEveryMatchInOrder()
    {
        // Arrange / Act
        var all = ExampleResolver.FindAll(TreeScope(), "shared", null);

        // Assert
        Assert.Equal(["d", "e"], all.Select(reference => reference.CardId));
    }

    /// <summary>
    ///     Proves that the card form and the scope id both restrict a search, together by intersection.
    /// </summary>
    [Fact]
    public void ExampleResolver_FindAll_CardFormAndScope_RestrictByIntersection()
    {
        // Arrange
        var scope = TreeScope();

        // Act / Assert
        Assert.Equal(["d"], ExampleResolver.FindAll(scope, "d#shared", null).Select(reference => reference.CardId));
        Assert.Equal(["e"], ExampleResolver.FindAll(scope, "shared", "e").Select(reference => reference.CardId));
        Assert.Equal(["e/f"], ExampleResolver.FindAll(scope, "e#unrelated", "e/f").Select(reference => reference.CardId));
        Assert.Empty(ExampleResolver.FindAll(scope, "d#shared", "e"));
        Assert.Empty(ExampleResolver.FindAll(scope, "unknown#shared", null));
        Assert.Empty(ExampleResolver.FindAll(scope, "shared", "unknown"));
    }

    /// <summary>
    ///     Proves that hidden cards contribute no examples and that a hidden scope card yields nothing.
    /// </summary>
    [Fact]
    public void ExampleResolver_FindAll_HiddenCards_ContributeNothing()
    {
        // Arrange
        var scope = TreeScope().Restrict(node => node.Id != "e");

        // Act / Assert
        Assert.Equal(["d"], ExampleResolver.FindAll(scope, "shared", null).Select(reference => reference.CardId));
        Assert.Empty(ExampleResolver.FindAll(scope, "e#shared", null));
        Assert.Equal(["e/f"], ExampleResolver.FindAll(scope, "unrelated", null).Select(reference => reference.CardId));
    }
}
