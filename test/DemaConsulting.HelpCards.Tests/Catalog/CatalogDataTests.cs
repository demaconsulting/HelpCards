using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Catalog;

/// <summary>
///     Unit tests for <see cref="CatalogData"/>.
/// </summary>
public class CatalogDataTests
{
    /// <summary>
    ///     Gets the frozen data of the simple fixture.
    /// </summary>
    /// <returns>The data.</returns>
    private static CatalogData SimpleData()
    {
        return CatalogBuilding.Compose(CatalogBuilding.Fixture("simple", new Dictionary<string, string> { ["limit"] = "42" })).Data!;
    }

    /// <summary>
    ///     Proves that the data holds the cards in depth-first order and the ids of the roots.
    /// </summary>
    [Fact]
    public void CatalogData_Cards_SimpleFixture_AreDepthFirstWithRootIds()
    {
        // Arrange / Act
        var data = SimpleData();

        // Assert
        Assert.Equal(["guide", "guide/setup", "guide/usage", "reference", "reference/limits"], data.Cards.Select(node => node.Id));
        Assert.Equal(["guide", "reference"], data.RootIds);
    }

    /// <summary>
    ///     Proves that a lookup by id ignores case and finds nothing for an unknown id.
    /// </summary>
    [Fact]
    public void CatalogData_Find_Id_IgnoresCaseAndReturnsNullWhenUnknown()
    {
        // Arrange
        var data = SimpleData();

        // Act / Assert
        Assert.Equal("guide/setup", data.Find("GUIDE/Setup")?.Id);
        Assert.Null(data.Find("guide/none"));
    }

    /// <summary>
    ///     Proves that tag names are the distinct tags of all cards in ordinal order.
    /// </summary>
    [Fact]
    public void CatalogData_TagNames_AllCards_AreDistinctAndOrdered()
    {
        // Arrange / Act
        var data = SimpleData();

        // Assert
        Assert.Equal(["authoring", "reference", "tutorial"], data.TagNames);
    }

    /// <summary>
    ///     Proves that the tool name, tool version, and vocabulary are kept as configured, and are null when not set.
    /// </summary>
    [Fact]
    public void CatalogData_Settings_AsConfigured_AreKept()
    {
        // Arrange
        var source = CatalogBuilding.Fixture("simple", new Dictionary<string, string> { ["limit"] = "42" });

        // Act
        var plain = CatalogBuilding.Compose(source).Data!;
        var configured = CatalogBuilding.Compose(
            source,
            builder => builder.ToolName("tool").ToolVersion("1.0").TagVocabulary(["tutorial", "authoring", "reference"])).Data!;

        // Assert
        Assert.Null(plain.ToolName);
        Assert.Null(plain.ToolVersion);
        Assert.Null(plain.AllowedTags);
        Assert.Equal(("tool", "1.0"), (configured.ToolName, configured.ToolVersion));
        Assert.Equal(["authoring", "reference", "tutorial"], configured.AllowedTags!.Order(StringComparer.Ordinal));
    }

    /// <summary>
    ///     Proves that the lists of the data cannot be changed through a cast.
    /// </summary>
    [Fact]
    public void CatalogData_Lists_AreReadOnly()
    {
        // Arrange
        var data = SimpleData();

        // Act / Assert
        Assert.True(((ICollection<CardNode>)data.Cards).IsReadOnly);
        Assert.True(((ICollection<string>)data.RootIds).IsReadOnly);
        Assert.True(((ICollection<HelpSourceInfo>)data.Sources).IsReadOnly);
        Assert.True(((ICollection<HelpProblem>)data.Diagnostics).IsReadOnly);
        Assert.True(((ICollection<string>)data.TagNames).IsReadOnly);
    }

    /// <summary>
    ///     Proves that a node carries resolved global ids in its body and see-also list.
    /// </summary>
    [Fact]
    public void CatalogData_Node_ResolvedBody_HoldsGlobalIds()
    {
        // Arrange / Act
        var guide = SimpleData().Find("guide")!;

        // Assert
        Assert.Equal(["reference/limits"], guide.SeeAlsoIds);
        var links = guide.Body.Where(part => part.Link is not null).Select(part => part.Link!).ToList();
        Assert.Equal(["guide/setup", "guide/usage", null, "guide", "reference/limits"], links.Select(link => link.CardId));
        Assert.Equal("guide", links[2].ExampleCardId);
    }

    /// <summary>
    ///     Proves that the freeze helpers copy their input so a later change of the input has no effect.
    /// </summary>
    [Fact]
    public void CardNode_Freeze_Input_IsCopied()
    {
        // Arrange
        var list = new List<string> { "a" };
        var map = new Dictionary<string, string> { ["x-k"] = "v" };

        // Act
        var frozenList = CardNode.Freeze(list);
        var frozenMap = CardNode.Freeze(map);
        list.Add("b");
        map["x-other"] = "w";

        // Assert
        Assert.Equal(["a"], frozenList);
        Assert.Single(frozenMap);
    }
}
