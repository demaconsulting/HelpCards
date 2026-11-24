using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Composition;
using DemaConsulting.HelpCards.Loading;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Composition;

/// <summary>
///     Unit tests for <see cref="SiblingOrder"/>.
/// </summary>
public class SiblingOrderTests
{
    /// <summary>
    ///     Reads cards with the given order values into definitions.
    /// </summary>
    /// <param name="cards">The name of each card and its order value, or <see langword="null"/> for none.</param>
    /// <returns>The definitions in the order of the arguments.</returns>
    private static IReadOnlyList<CardDefinition> Cards(params (string Name, int? Order)[] cards)
    {
        var files = cards.Select(card => (
            $"{card.Name}.md",
            SourceReading.Card(extraFrontMatter: card.Order is null ? string.Empty : $"order: {card.Order}\n")));
        var definition = SourceReading.Read(SourceReading.Files(files.ToArray()));
        return cards.Select(card => definition.Cards.Single(read => read.LocalId == card.Name)).ToList();
    }

    /// <summary>
    ///     Orders cards with a list of children and returns the names and the problems.
    /// </summary>
    /// <param name="siblings">The cards.</param>
    /// <param name="listed">The names in the children list.</param>
    /// <param name="parentId">The local id of the parent, or <see langword="null"/> for roots.</param>
    /// <param name="suppressed">The suppressed ids.</param>
    /// <returns>The ordered names and the problems.</returns>
    private static (List<string> Names, List<HelpProblem> Problems) Order(
        IReadOnlyList<CardDefinition> siblings,
        string[] listed,
        string? parentId = null,
        params string[] suppressed)
    {
        var problems = new List<HelpProblem>();
        var located = listed.Select((name, index) => new LocatedValue(name, "_source.md", index + 3)).ToList();
        var ordered = SiblingOrder.Order(siblings, parentId, located, suppressed.ToHashSet(StringComparer.Ordinal), "test", problems);
        return (ordered.Select(SiblingOrder.NameOf).ToList(), problems);
    }

    /// <summary>
    ///     Proves that cards with an order value come first by ascending value, then the others, ties by name.
    /// </summary>
    [Fact]
    public void SiblingOrder_Order_OrderValues_AscendingThenUnorderedByName()
    {
        // Arrange
        var cards = Cards(("y", 5), ("x", 5), ("z", null), ("w", 1), ("v", -3), ("u", null));

        // Act
        var (names, problems) = Order(cards, []);

        // Assert
        Assert.Equal(["v", "w", "x", "y", "u", "z"], names);
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that the children list puts cards first in the listed order and the rest follow in the normal order.
    /// </summary>
    [Fact]
    public void SiblingOrder_Order_ChildrenList_GoesFirstInListedOrder()
    {
        // Arrange
        var cards = Cards(("w", 1), ("x", 5), ("y", null), ("z", null));

        // Act
        var (names, problems) = Order(cards, ["z", "x"]);

        // Assert
        Assert.Equal(["z", "x", "w", "y"], names);
        Assert.Empty(problems);
    }

    /// <summary>
    ///     Proves that names tie-break in ordinal order, not culture order.
    /// </summary>
    [Fact]
    public void SiblingOrder_Order_NameTies_UseOrdinalOrder()
    {
        // Arrange: dash sorts before a digit in ordinal order
        var cards = Cards(("a1", null), ("a-b", null), ("a0", null));

        // Act
        var (names, _) = Order(cards, []);

        // Assert
        Assert.Equal(["a-b", "a0", "a1"], names);
    }

    /// <summary>
    ///     Proves that a repeated entry is reported at its own line and the card is placed once.
    /// </summary>
    [Fact]
    public void SiblingOrder_Order_RepeatedEntry_ReportsChildrenDuplicate()
    {
        // Arrange
        var cards = Cards(("x", null), ("y", null));

        // Act
        var (names, problems) = Order(cards, ["y", "x", "y"]);

        // Assert
        Assert.Equal(["y", "x"], names);
        var problem = Assert.Single(problems);
        Assert.Equal(("children-duplicate", "_source.md", 5), (problem.RuleId, problem.File, problem.Line));
    }

    /// <summary>
    ///     Proves that an entry that is not a direct child is reported, unless the card it names failed to load.
    /// </summary>
    [Fact]
    public void SiblingOrder_Order_UnknownEntry_ReportsChildrenUnknownUnlessSuppressed()
    {
        // Arrange
        var cards = Cards(("x", null));

        // Act
        var unknown = Order(cards, ["nope"]);
        var suppressedRoot = Order(cards, ["nope"], null, "nope");
        var suppressedChild = Order(cards, ["nope"], "p", "p/nope");
        var unsuppressedChild = Order(cards, ["nope"], "p", "nope");

        // Assert
        Assert.Equal("children-unknown", Assert.Single(unknown.Problems).RuleId);
        Assert.Empty(suppressedRoot.Problems);
        Assert.Empty(suppressedChild.Problems);
        Assert.Single(unsuppressedChild.Problems);
        Assert.Equal(["x"], unknown.Names);
    }

    /// <summary>
    ///     Proves that the name of a card is the last segment of its id.
    /// </summary>
    [Fact]
    public void SiblingOrder_NameOf_NestedCard_IsLastSegment()
    {
        // Arrange
        var definition = SourceReading.Read(SourceReading.Files(("a.md", SourceReading.Card()), ("a/b.md", SourceReading.Card())));

        // Act / Assert
        Assert.Equal(["a", "b"], definition.Cards.Select(SiblingOrder.NameOf));
    }

    /// <summary>
    ///     Proves that null arguments are rejected.
    /// </summary>
    [Fact]
    public void SiblingOrder_Order_NullArguments_ThrowArgumentNullException()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => SiblingOrder.Order(null!, null, [], new HashSet<string>(), "test", []));
        Assert.Throws<ArgumentNullException>(() => SiblingOrder.Order([], null, null!, new HashSet<string>(), "test", []));
        Assert.Throws<ArgumentNullException>(() => SiblingOrder.Order([], null, [], null!, "test", []));
        Assert.Throws<ArgumentNullException>(() => SiblingOrder.Order([], null, [], new HashSet<string>(), "test", null!));
    }
}
