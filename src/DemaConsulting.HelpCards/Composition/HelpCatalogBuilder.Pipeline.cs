using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Composition;

/// <summary>
///     The one composition pipeline of the catalog builder.
/// </summary>
public sealed partial class HelpCatalogBuilder
{
    /// <summary>
    ///     Runs the composition pipeline over the configured sources.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The steps run in this order: register and read the sources and resolve their imports (1 to 3),
    ///         claim the global ids and place the cards of every source (4 and 5), apply overlays (6), resolve
    ///         references over the final tree (7), check uniqueness and expected roots (8), and freeze the data
    ///         (9). Each step runs on what survived the earlier ones, so a file that could not be read contributes
    ///         nothing and references to it are not reported. Defects never throw; they are returned as problems.
    ///     </para>
    ///     <para>
    ///         Reading performs the file I/O of the sources. The method is meant to be called once per builder.
    ///     </para>
    /// </remarks>
    /// <returns>The frozen data when there is no error, and every problem found.</returns>
    /// <exception cref="ArgumentException">Thrown when no source was added.</exception>
    internal CompositionResult Compose()
    {
        if (_registrations.Count == 0)
        {
            throw new ArgumentException("Add a source with AddSource or AddMountedSource before creating the catalog.");
        }

        var problems = new ProblemCollector();

        // Steps 1 to 3: register and read the sources in registration order
        var registry = SourceRegistry.Read(_registrations, _culture, _toolName, _toolVersion);
        foreach (var (sourceIndex, problem) in registry.Problems)
        {
            problems.Add(sourceIndex, problem);
        }

        // Steps 4 and 5: claim global ids, then place the cards of each source, order siblings, and check tags
        var owners = Mounter.Register(registry, problems);
        foreach (var entry in registry.Entries.Where(entry => !entry.Excluded))
        {
            PlaceEntry(entry, owners, problems);
        }

        Mounter.CheckRootNames(registry, problems);

        // Step 6: apply overlays, sources in registration order
        OverlayMerger.Apply(registry, problems);

        // Steps 7 and 8: resolve references and check uniqueness over the final tree
        var nodes = CreateNodes(registry);
        var validator = new ReferenceValidator(registry, nodes, problems);
        var resolved = ResolveReferences(registry, validator);
        CheckUniqueness(registry, validator);
        CheckExpectedRoots(registry, problems);

        // Step 9: freeze the data when no error was found
        var errors = problems.GetOrdered(HelpSeverity.Error);
        var all = problems.GetOrdered();
        if (errors.Count > 0)
        {
            return new CompositionResult(null, errors, all);
        }

        return new CompositionResult(Freeze(registry, resolved, problems.GetOrdered(HelpSeverity.Info)), errors, all);
    }

    /// <summary>
    ///     Places the cards of one source in their tree and checks the tags (pipeline steps 4 and 5).
    /// </summary>
    /// <param name="entry">The source.</param>
    /// <param name="owners">The source that owns each claimed global id.</param>
    /// <param name="problems">Receives the problems found.</param>
    private void PlaceEntry(SourceEntry entry, IReadOnlyDictionary<string, SourceEntry> owners, ProblemCollector problems)
    {
        var found = new List<HelpProblem>();
        entry.Tree = PlaceCards(entry, owners, found);
        CheckTags(entry, found);
        problems.AddRange(entry.Index, found);
        foreach (var (localId, definition) in entry.Tree.Cards)
        {
            entry.Cards[localId] = new CardBuild(entry, definition, Mounter.GlobalId(entry, localId));
        }
    }

    /// <summary>
    ///     Creates the nodes of the tree with children and examples in place and bodies not yet resolved.
    /// </summary>
    /// <remarks>
    ///     The nodes include the generated root card of every mounted source. They are created after overlays are
    ///     applied, so a title, summary, example, or extension key that an overlay changed is already in place.
    /// </remarks>
    /// <param name="registry">The registry whose sources are placed.</param>
    /// <returns>The nodes by global id.</returns>
    private static Dictionary<string, CardNode> CreateNodes(SourceRegistry registry)
    {
        var nodes = new Dictionary<string, CardNode>(StringComparer.Ordinal);
        foreach (var entry in registry.Entries.Where(entry => !entry.Excluded))
        {
            if (entry.Registration.Mounted)
            {
                entry.MountRoot = Mounter.CreateRoot(entry);
                nodes[entry.MountRoot.Id] = entry.MountRoot;
            }

            foreach (var build in entry.Cards.Values)
            {
                build.Node = CreateNode(entry, build);
                nodes[build.GlobalId] = build.Node;
            }
        }

        return nodes;
    }

    /// <summary>
    ///     Creates the node of one card without a body and without see-also ids.
    /// </summary>
    /// <param name="entry">The source of the card.</param>
    /// <param name="build">The card.</param>
    /// <returns>The node.</returns>
    private static CardNode CreateNode(SourceEntry entry, CardBuild build)
    {
        var card = build.Definition;
        var parentLocalId = ParentOf(card.LocalId);
        return new CardNode(
            build.GlobalId,
            card.LocalId,
            SiblingOrder.NameOf(card),
            parentLocalId is null ? entry.Mount : Mounter.GlobalId(entry, parentLocalId),
            entry.Name,
            build.Title,
            build.Summary,
            CardNode.Freeze(card.Tags.Select(tag => tag.Value)),
            CardNode.Freeze(card.Aliases.Select(alias => alias.Value)),
            card.Culture,
            CardNode.Freeze(entry.Tree.Children[card.LocalId].Select(child => Mounter.GlobalId(entry, child.LocalId))),
            [],
            CardNode.Freeze(build.Examples.Select(item => new HelpExample(
                item.Definition.Name,
                item.Definition.Kind,
                item.Definition.Language,
                item.Definition.FileName,
                item.Definition.Description,
                item.Definition.Content,
                item.Entry.Name,
                item.Definition.ExitCode,
                item.Definition.Expect))),
            CardNode.Freeze(build.Extensions),
            card.File,
            card.BodyFile,
            card.BodyLine,
            []);
    }

    /// <summary>
    ///     Resolves the links and see-also entries of every card (pipeline step 7).
    /// </summary>
    /// <param name="registry">The registry whose sources are placed.</param>
    /// <param name="validator">The validator that resolves references.</param>
    /// <returns>The final nodes by global id.</returns>
    private static Dictionary<string, CardNode> ResolveReferences(SourceRegistry registry, ReferenceValidator validator)
    {
        var resolved = new Dictionary<string, CardNode>(StringComparer.Ordinal);
        foreach (var entry in registry.Entries.Where(entry => !entry.Excluded))
        {
            if (entry.MountRoot is not null)
            {
                resolved[entry.MountRoot.Id] = entry.MountRoot with { Body = CardNode.Freeze(validator.ResolveLanding(entry, entry.MountRoot)) };
            }

            foreach (var build in entry.Cards.Values.OrderBy(build => build.GlobalId, StringComparer.Ordinal))
            {
                resolved[build.GlobalId] = build.Node! with
                {
                    Body = CardNode.Freeze(validator.ResolveBody(build)),
                    SeeAlsoIds = CardNode.Freeze(validator.ResolveSeeAlso(build))
                };
            }
        }

        return resolved;
    }

    /// <summary>
    ///     Checks that example names, and the names and aliases of siblings, are unique (pipeline step 8).
    /// </summary>
    /// <param name="registry">The registry whose sources are placed.</param>
    /// <param name="validator">The validator that performs the checks.</param>
    private static void CheckUniqueness(SourceRegistry registry, ReferenceValidator validator)
    {
        foreach (var entry in registry.Entries.Where(entry => !entry.Excluded))
        {
            validator.CheckExampleNames(entry.Cards.Values.OrderBy(build => build.Definition.LocalId, StringComparer.Ordinal));
            foreach (var siblings in entry.Tree.Children.Values)
            {
                validator.CheckSiblingNames(entry, siblings);
            }
        }
    }

    /// <summary>
    ///     Checks the roots against the expected roots, when expected roots were set (pipeline step 8).
    /// </summary>
    /// <remarks>
    ///     The check is skipped when a source could not be read (no name, an unsupported schema, or a failed
    ///     listing) or was left out of the catalog, because roots would then be missing for a reason that is
    ///     already reported. Roots whose file failed to load count as present, so one broken root file yields one
    ///     problem.
    /// </remarks>
    /// <param name="registry">The registry whose sources are placed.</param>
    /// <param name="problems">Receives the <c>roots-mismatch</c> problem.</param>
    private void CheckExpectedRoots(SourceRegistry registry, ProblemCollector problems)
    {
        var unreadable = registry.Entries.Any(entry =>
            entry.Excluded
            || entry.Definition.Problems.Any(problem =>
                problem.RuleId is RuleIds.SchemaUnsupported or RuleIds.SourceInvalid
                || (problem.RuleId == RuleIds.SourceRead && problem.File.Length == 0)));
        if (_expectedRoots is null || unreadable)
        {
            return;
        }

        var actual = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in registry.Entries)
        {
            if (entry.Mount is not null)
            {
                actual.Add(entry.Mount);
                continue;
            }

            actual.UnionWith(entry.Tree.Roots.Select(SiblingOrder.NameOf));
            actual.UnionWith(entry.Tree.Suppressed.Where(id => !id.Contains('/', StringComparison.Ordinal)));
        }

        var missing = _expectedRoots.Where(id => !actual.Contains(id)).Order(StringComparer.Ordinal).ToList();
        var unexpected = actual.Where(id => !_expectedRoots.Contains(id)).Order(StringComparer.Ordinal).ToList();
        if (missing.Count == 0 && unexpected.Count == 0)
        {
            return;
        }

        var message = "The root cards differ from the expected roots";
        message += missing.Count > 0 ? $"; missing: {string.Join(", ", missing)}" : string.Empty;
        message += unexpected.Count > 0 ? $"; unexpected: {string.Join(", ", unexpected)}" : string.Empty;
        problems.Add(0, HelpProblem.CreateError(RuleIds.RootsMismatch, registry.Entries[0].Name, string.Empty, 0, message + "."));
    }

    /// <summary>
    ///     Builds the immutable catalog data (pipeline step 9).
    /// </summary>
    /// <remarks>
    ///     The roots of the catalog are, in registration order, the mount root of each mounted source and the roots
    ///     of each flat source in sibling order; the cards follow depth first.
    /// </remarks>
    /// <param name="registry">The registry whose sources are placed.</param>
    /// <param name="resolved">The final nodes by global id.</param>
    /// <param name="diagnostics">The informational problems.</param>
    /// <returns>The data.</returns>
    private CatalogData Freeze(
        SourceRegistry registry,
        Dictionary<string, CardNode> resolved,
        IReadOnlyList<HelpProblem> diagnostics)
    {
        var ordered = new List<CardNode>();
        var rootIds = new List<string>();
        foreach (var entry in registry.Entries)
        {
            if (entry.Mount is not null)
            {
                rootIds.Add(entry.Mount);
                continue;
            }

            rootIds.AddRange(entry.Tree.Roots.Select(root => Mounter.GlobalId(entry, root.LocalId)));
        }

        foreach (var rootId in rootIds)
        {
            Visit(rootId);
        }

        var infos = registry.Entries
            .Select(entry => new HelpSourceInfo(
                entry.Name,
                entry.Mount,
                entry.Definition.Title,
                entry.Definition.Summary,
                entry.Definition.Language,
                entry.Definition.Schema))
            .ToList();
        return new CatalogData(_culture, ordered, rootIds, infos, diagnostics, _toolName, _toolVersion, _allowedTags);

        void Visit(string id)
        {
            var node = resolved[id];
            ordered.Add(node);
            foreach (var childId in node.ChildIds)
            {
                Visit(childId);
            }
        }
    }
}
