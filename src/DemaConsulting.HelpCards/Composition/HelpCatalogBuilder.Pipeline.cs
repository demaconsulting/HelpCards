using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Composition;

/// <summary>
///     The one composition pipeline of the catalog builder.
/// </summary>
public sealed partial class HelpCatalogBuilder
{
    /// <summary>
    ///     Runs the composition pipeline over the configured source.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The steps run in this order: validate the builder input (1), read the files and cards of the
    ///         source (2 and 3), place the cards and check the tree (4 and 5), apply overlays (6, reserved),
    ///         resolve references over the final tree (7), check uniqueness and expected roots (8), and freeze the
    ///         data (9). Each step runs on what survived the earlier ones, so a file that could not be read
    ///         contributes nothing and references to it are not reported. Defects never throw; they are returned
    ///         as problems.
    ///     </para>
    ///     <para>
    ///         Reading performs the file I/O of the source. The method is meant to be called once per builder.
    ///     </para>
    /// </remarks>
    /// <returns>The frozen data when there is no error, and every problem found.</returns>
    /// <exception cref="ArgumentException">Thrown when no source was added.</exception>
    internal CompositionResult Compose()
    {
        // Step 1: the only input that can be invalid after the argument checks is an empty builder
        if (_sources.Count == 0)
        {
            throw new ArgumentException("Add a source with AddSource before creating the catalog.");
        }

        var problems = new ProblemCollector();
        var found = new List<HelpProblem>();
        var source = _sources[0];

        // Steps 2 and 3: list and read the files, then build the card definitions
        var definition = CardReader.Read(source, _culture, name => CreateTokenScope(source, name));
        problems.AddRange(0, definition.Problems);

        // Steps 4 and 5: place the cards, order siblings, and check the tree and the tags
        var tree = PlaceCards(definition, found);
        CheckTags(definition, tree, found);
        var nodes = CreateNodes(tree);

        // Step 6 (apply overlays) is reserved: this release reads overlays but does not merge them
        ApplyOverlays(definition);

        // Steps 7 and 8: resolve references and check uniqueness over the final tree
        var validator = new ReferenceValidator(
            definition.Name,
            nodes,
            tree.Suppressed,
            new HashSet<string>(StringComparer.Ordinal) { definition.Name },
            found);
        var resolved = ResolveReferences(tree, nodes, validator);
        CheckUniqueness(tree, validator);
        CheckExpectedRoots(definition, tree, found);
        problems.AddRange(0, found);

        // Step 9: freeze the data when no error was found
        var errors = problems.GetOrdered(HelpSeverity.Error);
        var all = problems.GetOrdered();
        if (errors.Count > 0)
        {
            return new CompositionResult(null, errors, all);
        }

        return new CompositionResult(Freeze(definition, tree, resolved, problems.GetOrdered(HelpSeverity.Info)), errors, all);
    }

    /// <summary>
    ///     Builds the token scope of a source once its name is known.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="name">The resolved name of the source.</param>
    /// <returns>The token scope with the built-in tokens.</returns>
    private TokenResolver CreateTokenScope(HelpSource source, string name)
    {
        return TokenResolver.Create(name, source.Tokens, [], _toolName ?? name, _toolVersion ?? string.Empty);
    }

    /// <summary>
    ///     Applies overlays to the cards they extend (pipeline step 6).
    /// </summary>
    /// <remarks>
    ///     This release composes one source and does not merge overlays: the files are read and validated by the
    ///     loader, and this step is the single place where merging will be added. Until then an overlay has no
    ///     effect on the catalog.
    /// </remarks>
    /// <param name="definition">The definition of the source, whose overlays would be applied.</param>
    private static void ApplyOverlays(SourceDefinition definition)
    {
        _ = definition.Overlays;
    }

    /// <summary>
    ///     Creates the nodes of the tree with children and examples in place and bodies not yet resolved.
    /// </summary>
    /// <param name="tree">The placed cards.</param>
    /// <returns>The nodes by global id.</returns>
    private static Dictionary<string, CardNode> CreateNodes(SourceTree tree)
    {
        var nodes = new Dictionary<string, CardNode>(StringComparer.Ordinal);
        foreach (var card in tree.Cards.Values)
        {
            nodes[card.LocalId] = new CardNode(
                card.LocalId,
                card.LocalId,
                SiblingOrder.NameOf(card),
                ParentOf(card.LocalId),
                card.SourceName,
                card.Title,
                card.Summary,
                CardNode.Freeze(card.Tags.Select(tag => tag.Value)),
                CardNode.Freeze(card.Aliases.Select(alias => alias.Value)),
                card.Culture,
                CardNode.Freeze(tree.Children[card.LocalId].Select(child => child.LocalId)),
                [],
                CardNode.Freeze(card.Examples.Select(example => new HelpExample(
                    example.Name,
                    example.Kind,
                    example.Language,
                    example.FileName,
                    example.Description,
                    example.Content,
                    card.SourceName))),
                CardNode.Freeze(card.Extensions),
                card.File,
                card.BodyFile,
                card.BodyLine,
                []);
        }

        return nodes;
    }

    /// <summary>
    ///     Resolves the links and see-also entries of every card (pipeline step 7).
    /// </summary>
    /// <param name="tree">The placed cards.</param>
    /// <param name="nodes">The nodes without bodies.</param>
    /// <param name="validator">The validator that resolves references.</param>
    /// <returns>The final nodes by global id.</returns>
    private static Dictionary<string, CardNode> ResolveReferences(
        SourceTree tree,
        Dictionary<string, CardNode> nodes,
        ReferenceValidator validator)
    {
        var resolved = new Dictionary<string, CardNode>(StringComparer.Ordinal);
        foreach (var card in tree.Cards.Values.OrderBy(card => card.LocalId, StringComparer.Ordinal))
        {
            resolved[card.LocalId] = nodes[card.LocalId] with
            {
                Body = CardNode.Freeze(validator.ResolveBody(card)),
                SeeAlsoIds = CardNode.Freeze(validator.ResolveSeeAlso(card))
            };
        }

        return resolved;
    }

    /// <summary>
    ///     Checks that example names, and the names and aliases of siblings, are unique (pipeline step 8).
    /// </summary>
    /// <param name="tree">The placed cards.</param>
    /// <param name="validator">The validator that performs the checks.</param>
    private static void CheckUniqueness(SourceTree tree, ReferenceValidator validator)
    {
        validator.CheckExampleNames(tree.Cards.Values.OrderBy(card => card.LocalId, StringComparer.Ordinal));
        foreach (var siblings in tree.Children.Values)
        {
            validator.CheckSiblingNames(siblings);
        }
    }

    /// <summary>
    ///     Checks the roots against the expected roots, when expected roots were set (pipeline step 8).
    /// </summary>
    /// <remarks>
    ///     The check is skipped when the source itself could not be read (no name, an unsupported schema, or a
    ///     failed listing), because every root would then be missing. Roots whose file failed to load count as
    ///     present, so one broken root file yields one problem.
    /// </remarks>
    /// <param name="definition">The definition of the source.</param>
    /// <param name="tree">The placed cards.</param>
    /// <param name="problems">Receives the <c>roots-mismatch</c> problem.</param>
    private void CheckExpectedRoots(SourceDefinition definition, SourceTree tree, ICollection<HelpProblem> problems)
    {
        var unreadable = definition.Problems.Any(problem =>
            problem.RuleId is RuleIds.SchemaUnsupported or RuleIds.SourceInvalid
            || (problem.RuleId == RuleIds.SourceRead && problem.File.Length == 0));
        if (_expectedRoots is null || unreadable)
        {
            return;
        }

        var actual = tree.Roots.Select(SiblingOrder.NameOf)
            .Concat(tree.Suppressed.Where(id => !id.Contains('/', StringComparison.Ordinal)))
            .ToHashSet(StringComparer.Ordinal);
        var missing = _expectedRoots.Where(id => !actual.Contains(id)).Order(StringComparer.Ordinal).ToList();
        var unexpected = actual.Where(id => !_expectedRoots.Contains(id)).Order(StringComparer.Ordinal).ToList();
        if (missing.Count == 0 && unexpected.Count == 0)
        {
            return;
        }

        var message = "The root cards differ from the expected roots";
        message += missing.Count > 0 ? $"; missing: {string.Join(", ", missing)}" : string.Empty;
        message += unexpected.Count > 0 ? $"; unexpected: {string.Join(", ", unexpected)}" : string.Empty;
        problems.Add(HelpProblem.CreateError(RuleIds.RootsMismatch, definition.Name, string.Empty, 0, message + "."));
    }

    /// <summary>
    ///     Builds the immutable catalog data (pipeline step 9).
    /// </summary>
    /// <param name="definition">The definition of the source.</param>
    /// <param name="tree">The placed cards.</param>
    /// <param name="resolved">The final nodes by global id.</param>
    /// <param name="diagnostics">The informational problems.</param>
    /// <returns>The data.</returns>
    private CatalogData Freeze(
        SourceDefinition definition,
        SourceTree tree,
        Dictionary<string, CardNode> resolved,
        IReadOnlyList<HelpProblem> diagnostics)
    {
        var ordered = new List<CardNode>();
        var rootIds = tree.Roots.Select(root => root.LocalId).ToList();
        foreach (var rootId in rootIds)
        {
            Visit(rootId);
        }

        var info = new HelpSourceInfo(definition.Name, null, definition.Title, definition.Summary, definition.Language, definition.Schema);
        return new CatalogData(_culture, ordered, rootIds, [info], diagnostics, _toolName, _toolVersion, _allowedTags);

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
