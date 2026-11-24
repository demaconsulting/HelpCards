using DemaConsulting.HelpCards.Composition;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Composition;

/// <summary>
///     Unit tests for <see cref="Mounter"/>: global ids, claiming ids, generated mount roots, and root names.
/// </summary>
public class MounterTests
{
    /// <summary>
    ///     Reads sources into a registry and places their cards as the pipeline does.
    /// </summary>
    /// <param name="registrations">The registrations.</param>
    /// <returns>The registry with trees placed.</returns>
    private static SourceRegistry Read(params (HelpCards.Loading.HelpSource Source, bool Mounted, string? Mount)[] registrations)
    {
        var registry = SourceRegistry.Read(
            registrations.Select(item => new SourceRegistration(item.Source, item.Mounted, item.Mount)).ToList(),
            System.Globalization.CultureInfo.InvariantCulture,
            null,
            null);
        foreach (var entry in registry.Entries)
        {
            var roots = entry.Definition.Cards.Where(card => !card.LocalId.Contains('/', StringComparison.Ordinal)).ToList();
            entry.Tree = new SourceTree(
                entry.Definition.Cards.ToDictionary(card => card.LocalId, StringComparer.Ordinal),
                new Dictionary<string, IReadOnlyList<HelpCards.Loading.CardDefinition>> { [string.Empty] = roots },
                new HashSet<string>());
        }

        return registry;
    }

    /// <summary>
    ///     Proves that a flat source keeps local ids and a mounted source prefixes them with its mount.
    /// </summary>
    [Fact]
    public void Mounter_GlobalId_FlatAndMounted_PrefixesOnlyMountedSources()
    {
        // Arrange
        var registry = Read(
            (CatalogBuilding.Named("flat", ("a.md", SourceReading.Card())), false, null),
            (CatalogBuilding.Named("lib", ("a.md", SourceReading.Card())), true, null),
            (CatalogBuilding.Named("other", ("a.md", SourceReading.Card())), true, "o"));

        // Act
        var ids = registry.Entries.Select(entry => Mounter.GlobalId(entry, "a/b")).ToList();

        // Assert
        Assert.Equal(["a/b", "lib/a/b", "o/a/b"], ids);
        Assert.Throws<ArgumentNullException>(() => Mounter.GlobalId(null!, "a"));
        Assert.Throws<ArgumentNullException>(() => Mounter.GlobalId(registry.Entries[0], null!));
    }

    /// <summary>
    ///     Proves that every global id is claimed by its source and the owners are returned.
    /// </summary>
    [Fact]
    public void Mounter_Register_DistinctIds_ReturnsOwners()
    {
        // Arrange
        var registry = Read(
            (CatalogBuilding.Named("flat", ("a.md", SourceReading.Card())), false, null),
            (CatalogBuilding.Named("lib", ("a.md", SourceReading.Card())), true, null));
        var problems = new ProblemCollector();

        // Act
        var owners = Mounter.Register(registry, problems);

        // Assert
        Assert.Equal(["a", "lib", "lib/a"], owners.Keys.Order(StringComparer.Ordinal));
        Assert.Equal("lib", owners["lib/a"].Name);
        Assert.Empty(problems.GetOrdered());
    }

    /// <summary>
    ///     Proves that the later source loses a contested id and is told which source and file hold it.
    /// </summary>
    [Fact]
    public void Mounter_Register_ContestedIdAndMount_DropsTheLaterHolder()
    {
        // Arrange
        var registry = Read(
            (CatalogBuilding.Named("one", ("m.md", SourceReading.Card())), false, null),
            (CatalogBuilding.Named("two", ("b.md", SourceReading.Card())), true, "m"),
            (CatalogBuilding.Named("three", ("m.md", SourceReading.Card())), false, null),
            (CatalogBuilding.Named("four", ("c.md", SourceReading.Card())), true, "Bad"));
        var problems = new ProblemCollector();

        // Act
        Mounter.Register(registry, problems);

        // Assert: two is excluded (its mount is taken), three drops its card, four is excluded (invalid mount)
        Assert.Equal([false, true, false, true], registry.Entries.Select(entry => entry.Excluded));
        Assert.Equal(["m"], registry.Entries[2].Dropped);
        Assert.Equal(
            [("two", "collision"), ("three", "collision"), ("four", "mount-invalid")],
            problems.GetOrdered().Select(problem => (problem.SourceName, problem.RuleId)));
        Assert.Contains("the source 'one' ('m.md')", problems.GetOrdered()[0].Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the cards below a dropped card claim no ids, so they cannot cause collisions for other sources.
    /// </summary>
    [Fact]
    public void Mounter_Register_DroppedCard_DoesNotClaimItsDescendants()
    {
        // Arrange: 'two' loses 'a' to 'one'; its 'a/b' must not reserve the id that 'three' uses
        var registry = Read(
            (CatalogBuilding.Named("one", ("a.md", SourceReading.Card())), false, null),
            (CatalogBuilding.Named("two", ("a.md", SourceReading.Card()), ("a/b.md", SourceReading.Card()), ("a/b/c.md", SourceReading.Card())), false, null),
            (CatalogBuilding.Named("three", ("a/b.md", SourceReading.Card())), false, null));
        var problems = new ProblemCollector();

        // Act
        var owners = Mounter.Register(registry, problems);

        // Assert: only the card 'a' is reported, 'a/b' is owned by 'three', and 'a/b/c' is claimed by nobody
        var problem = Assert.Single(problems.GetOrdered());
        Assert.Equal(("two", "collision"), (problem.SourceName, problem.RuleId));
        Assert.Equal(["a"], registry.Entries[1].Dropped);
        Assert.Empty(registry.Entries[2].Dropped);
        Assert.Equal("three", owners["a/b"].Name);
        Assert.False(owners.ContainsKey("a/b/c"));
    }

    /// <summary>
    ///     Proves that the generated root card takes its text from the source file and has the roots as children.
    /// </summary>
    [Fact]
    public void Mounter_CreateRoot_MountedSource_TakesTextFromSourceFileAndListsRoots()
    {
        // Arrange
        var source = CatalogBuilding.Named(
            "lib",
            ("_source.md", "---\ntitle: Library\nsummary: The library.\naliases:\n  - li\nx-kind: demo\n---\nLanding."),
            ("a.md", SourceReading.Card()),
            ("a/b.md", SourceReading.Card()));
        var registry = Read((source, true, "mnt"));

        // Act
        var root = Mounter.CreateRoot(registry.Entries[0]);

        // Assert
        Assert.Equal(("mnt", "mnt", "lib", "Library", "The library."), (root.Id, root.Name, root.SourceName, root.Title, root.Summary));
        Assert.Null(root.ParentId);
        Assert.Equal(["mnt/a"], root.ChildIds);
        Assert.Equal(["li"], root.Aliases);
        Assert.Equal("demo", root.Extensions["x-kind"]);
        Assert.Empty(root.Tags);
        Assert.Empty(root.Examples);
        Assert.Throws<ArgumentNullException>(() => Mounter.CreateRoot(null!));
    }

    /// <summary>
    ///     Proves that a root name used by two sources is a collision, a flat repeat is left to the sibling check, and
    ///     a mount alias that repeats the mount is a name duplicate.
    /// </summary>
    [Fact]
    public void Mounter_CheckRootNames_NamesAndAliases_ReportCollisionsAcrossSources()
    {
        // Arrange
        var registry = Read(
            (CatalogBuilding.Named("one", ("a.md", SourceReading.Card(extraFrontMatter: "aliases:\n  - dup\n")), ("b.md", SourceReading.Card(extraFrontMatter: "aliases:\n  - dup\n"))), false, null),
            (CatalogBuilding.Named("two", ("dup.md", SourceReading.Card())), false, null),
            (CatalogBuilding.Named("three", ("_source.md", "---\naliases:\n  - three\n---\n"), ("c.md", SourceReading.Card())), true, null));
        var problems = new ProblemCollector();

        // Act
        Mounter.CheckRootNames(registry, problems);

        // Assert
        Assert.Equal(
            [("two", "dup.md", "collision"), ("three", "_source.md", "name-duplicate")],
            problems.GetOrdered().Select(problem => (problem.SourceName, problem.File, problem.RuleId)));
        Assert.Throws<ArgumentNullException>(() => Mounter.CheckRootNames(null!, problems));
        Assert.Throws<ArgumentNullException>(() => Mounter.CheckRootNames(registry, null!));
        Assert.Throws<ArgumentNullException>(() => Mounter.Register(null!, problems));
        Assert.Throws<ArgumentNullException>(() => Mounter.Register(registry, null!));
    }

    /// <summary>
    ///     Proves that a mounted source without a usable name is excluded without a second problem: the missing name
    ///     is reported once by Loading, and the mount that would fall back to it is not reported again.
    /// </summary>
    [Fact]
    public void Mounter_Register_MountedSourceWithoutName_ExcludesSilently()
    {
        // Arrange
        var registry = Read(
            (HelpCards.Loading.HelpSource.FromFiles(SourceReading.Files(("a.md", SourceReading.Card()))), true, null),
            (CatalogBuilding.Named("lib", ("b.md", SourceReading.Card())), true, null));
        var problems = new ProblemCollector();

        // Act
        Mounter.Register(registry, problems);

        // Assert: Register adds nothing; the only problem is the loader's source-invalid
        Assert.Equal([true, false], registry.Entries.Select(entry => entry.Excluded));
        Assert.Empty(problems.GetOrdered());
        Assert.Equal(["source-invalid"], registry.Problems.Select(item => item.Problem.RuleId));
    }

    /// <summary>
    ///     Proves that a mount alias that a flat root alias used first is a collision as well as the other way round.
    /// </summary>
    [Fact]
    public void Mounter_CheckRootNames_FlatAliasBeforeMountAlias_ReportsCollisionOnTheMount()
    {
        // Arrange
        var registry = Read(
            (CatalogBuilding.Named("flat", ("a.md", SourceReading.Card(extraFrontMatter: "aliases:\n  - shared\n"))), false, null),
            (CatalogBuilding.Named("lib", ("_source.md", "---\naliases:\n  - shared\n---\n"), ("b.md", SourceReading.Card())), true, null));
        var problems = new ProblemCollector();

        // Act
        Mounter.CheckRootNames(registry, problems);

        // Assert
        Assert.Equal(
            [("lib", "_source.md", "collision")],
            problems.GetOrdered().Select(problem => (problem.SourceName, problem.File, problem.RuleId)));
    }
}
