using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Testing;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Testing;

/// <summary>
///     Unit tests for <see cref="HelpExamples"/>.
/// </summary>
public class HelpExamplesTests
{
    /// <summary>
    ///     Proves that every example is listed with its card in catalog order and in the order of the card.
    /// </summary>
    [Fact]
    public void HelpExamples_Enumerate_NoKinds_ListsEveryExampleInCatalogOrder()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act
        var examples = HelpExamples.Enumerate(catalog).Select(pair => $"{pair.Card.Id}#{pair.Example.Name}").ToList();

        // Assert
        var expected = catalog.Cards.SelectMany(card => card.Examples.Select(example => $"{card.Id}#{example.Name}")).ToList();
        Assert.Equal(["guide#run", "guide#settings", "guide#fragment"], examples.Take(3));
        Assert.Equal(expected, examples);
    }

    /// <summary>
    ///     Proves that only the requested kinds are listed, that the kinds are compared exactly, and that an empty
    ///     list of kinds lists nothing.
    /// </summary>
    [Fact]
    public void HelpExamples_Enumerate_Kinds_FiltersByKind()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act
        var commands = HelpExamples.Enumerate(catalog, [HelpExample.CommandKind]).ToList();
        var sourcesAndSnippets = HelpExamples.Enumerate(catalog, [HelpExample.SourceKind, HelpExample.SnippetKind]).ToList();
        var unknown = HelpExamples.Enumerate(catalog, ["Command"]).ToList();
        var none = HelpExamples.Enumerate(catalog, []).ToList();

        // Assert
        Assert.NotEmpty(commands);
        Assert.All(commands, pair => Assert.Equal(HelpExample.CommandKind, pair.Example.Kind));
        Assert.All(sourcesAndSnippets, pair => Assert.NotEqual(HelpExample.CommandKind, pair.Example.Kind));
        Assert.Equal(HelpExamples.Enumerate(catalog).Count(), commands.Count + sourcesAndSnippets.Count);
        Assert.Empty(unknown);
        Assert.Empty(none);
    }

    /// <summary>
    ///     Proves that the examples of a view are the examples of its visible cards only.
    /// </summary>
    [Fact]
    public void HelpExamples_Enumerate_View_ListsVisibleCardsOnly()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple().Without(["guide"]);

        // Act
        var examples = HelpExamples.Enumerate(catalog).ToList();

        // Assert
        Assert.DoesNotContain(examples, pair => pair.Card.Id == "guide");
    }

    /// <summary>
    ///     Proves that a null catalog or a null kind is an argument error at the call, not at enumeration.
    /// </summary>
    [Fact]
    public void HelpExamples_Enumerate_InvalidArguments_ThrowAtTheCall()
    {
        // Arrange
        var catalog = CatalogBuilding.Simple();

        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => HelpExamples.Enumerate(null!));
        Assert.Throws<ArgumentNullException>(() => HelpExamples.Enumerate(catalog, [HelpExample.CommandKind, null!]));
    }
}
