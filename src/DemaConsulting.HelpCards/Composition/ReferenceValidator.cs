using System.Text;
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Composition;

/// <summary>
///     Checks the final tree of a catalog: it resolves every link and see-also entry, and checks that names are unique.
/// </summary>
/// <remarks>
///     <para>
///         Resolution runs once over the finished tree, so a forward reference is as good as a backward one. A
///         bare <c>[[id]]</c> link and a see-also entry name a card of the source the text is written in, or of a
///         source it imports; <c>[[source:id|text]]</c> names a card of that source; <c>[[id#name]]</c> must name
///         an example in the subtree of that card; <c>[[#name]]</c> must name an example in the subtree of the card
///         the link ends up in, which for an overlay body is the card the overlay extends. A link to a source that
///         is not part of the catalog degrades to its display text and leaves an informational problem; a link to
///         a present source with no such card is an error. A reference to a file that failed to load, or to a card
///         that had no place in the tree, is not reported again.
///     </para>
///     <para>
///         The validator also checks that example names are unique within a scope (the source and the first
///         segment of the id of the card the example belongs to, counting the examples that overlays add) and that
///         the names and aliases of siblings are unique, ignoring case. A validator belongs to one build and is not
///         safe for concurrent use.
///     </para>
/// </remarks>
internal sealed class ReferenceValidator
{
    /// <summary>
    ///     The registry that finds the cards references name.
    /// </summary>
    private readonly SourceRegistry _registry;

    /// <summary>
    ///     The nodes of the tree by global id.
    /// </summary>
    private readonly IReadOnlyDictionary<string, CardNode> _nodes;

    /// <summary>
    ///     Receives the problems found.
    /// </summary>
    private readonly ProblemCollector _problems;

    /// <summary>
    ///     Creates a validator for one build.
    /// </summary>
    /// <param name="registry">The registry that finds the cards references name.</param>
    /// <param name="nodes">The nodes of the tree by global id, with children and examples in place.</param>
    /// <param name="problems">Receives every problem found.</param>
    public ReferenceValidator(SourceRegistry registry, IReadOnlyDictionary<string, CardNode> nodes, ProblemCollector problems)
    {
        _registry = registry;
        _nodes = nodes;
        _problems = problems;
    }

    /// <summary>
    ///     Resolves the body of a card, including the bodies that overlays append.
    /// </summary>
    /// <param name="build">The card.</param>
    /// <returns>
    ///     The body with every link resolved to global ids and the pieces separated by a blank line; unresolvable
    ///     links are left out.
    /// </returns>
    public IReadOnlyList<ResolvedPart> ResolveBody(CardBuild build)
    {
        var parts = new List<ResolvedPart>();
        foreach (var segment in build.Bodies)
        {
            var resolved = ResolveParts(segment.Parts, segment.File, segment.Entry, build.Node!);
            if (resolved.Count == 0)
            {
                continue;
            }

            // Overlay bodies are appended after a blank line; an empty card body needs no separator
            if (parts.Count > 0)
            {
                parts.Add(new ResolvedPart("\n\n", null));
            }

            parts.AddRange(resolved);
        }

        return parts;
    }

    /// <summary>
    ///     Resolves the landing body of a mounted source.
    /// </summary>
    /// <param name="entry">The mounted source.</param>
    /// <param name="root">The generated root card the body belongs to.</param>
    /// <returns>The body with every link resolved to global ids.</returns>
    public IReadOnlyList<ResolvedPart> ResolveLanding(SourceEntry entry, CardNode root)
    {
        return ResolveParts(entry.Definition.Body, entry.Definition.File, entry, root);
    }

    /// <summary>
    ///     Resolves the see-also entries of a card, including those that overlays append.
    /// </summary>
    /// <param name="build">The card.</param>
    /// <returns>The global ids of the related cards in the order written, without duplicates.</returns>
    public IReadOnlyList<string> ResolveSeeAlso(CardBuild build)
    {
        var ids = new List<string>();
        foreach (var (reference, entry) in build.SeeAlso.Select(item => (item.Reference, item.Entry)))
        {
            var lookup = _registry.Lookup(entry, reference.SourceName, reference.LocalId);
            switch (lookup.Kind)
            {
                case LookupKind.SourceAbsent:
                    ReportAbsentSource(entry, reference.SourceName!, reference.File, reference.Line, "the see-also entry is left out");
                    break;
                case LookupKind.Found:
                    if (!ids.Contains(lookup.Build!.GlobalId, StringComparer.Ordinal))
                    {
                        ids.Add(lookup.Build.GlobalId);
                    }

                    break;
                case LookupKind.NotFound:
                    Report(
                        entry,
                        RuleIds.SeeAlsoTarget,
                        reference.File,
                        reference.Line,
                        $"The 'see-also' entry '{Describe(reference)}' names no card; check the id.");
                    break;
                case LookupKind.Ambiguous:
                    Report(
                        entry,
                        RuleIds.AmbiguousReference,
                        reference.File,
                        reference.Line,
                        $"The 'see-also' entry '{Describe(reference)}' is ambiguous: {lookup.Detail}; write 'source:{reference.LocalId}' to choose.");
                    break;
                default:
                    // The target failed to load or is left out; that defect is already reported
                    break;
            }
        }

        return ids;
    }

    /// <summary>
    ///     Checks that example names are unique within each scope.
    /// </summary>
    /// <remarks>
    ///     The scope of an example is the source of the card that owns it and the first segment of the local id of
    ///     that card; examples that overlays add count for the card they extend. The second use of a name in a scope
    ///     is reported at its own file and line.
    /// </remarks>
    /// <param name="builds">The cards, in the order that decides which use of a name is the first.</param>
    public void CheckExampleNames(IEnumerable<CardBuild> builds)
    {
        var first = new Dictionary<(string Source, string Scope, string Name), ExampleDefinition>();
        foreach (var build in builds)
        {
            var scope = build.Definition.LocalId.Split('/')[0];
            foreach (var (example, entry) in build.Examples.Select(item => (item.Definition, item.Entry)))
            {
                var key = (build.Entry.Name, scope, example.Name);
                if (first.TryGetValue(key, out var original))
                {
                    Report(
                        entry,
                        RuleIds.DuplicateExample,
                        example.File,
                        example.Line,
                        $"The example name '{example.Name}' is already used at line {original.Line} of '{original.File}'; "
                        + $"example names must be unique within '{scope}' and the cards below it.");
                }
                else
                {
                    first[key] = example;
                }
            }
        }
    }

    /// <summary>
    ///     Checks that the names and aliases of cards that share a parent are unique, ignoring case.
    /// </summary>
    /// <param name="entry">The source of the cards.</param>
    /// <param name="siblings">The cards that share a parent, or the roots of the source, in order.</param>
    public void CheckSiblingNames(SourceEntry entry, IReadOnlyList<CardDefinition> siblings)
    {
        var used = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var card in siblings)
        {
            var name = SiblingOrder.NameOf(card);
            Claim(entry, used, name, $"the name of the card '{card.LocalId}'", card, card.File, 0, "name");
            foreach (var alias in card.Aliases)
            {
                var normalized = alias.Value.Normalize(NormalizationForm.FormC);
                Claim(entry, used, normalized, $"an alias of the card '{card.LocalId}'", card, alias.File, alias.Line, "alias");
            }
        }
    }

    /// <summary>
    ///     Records a name or alias as used, or reports it when a sibling already uses it.
    /// </summary>
    /// <param name="entry">The source of the cards.</param>
    /// <param name="used">The names used so far and who uses them.</param>
    /// <param name="key">The name or alias.</param>
    /// <param name="description">Who uses it, for the message.</param>
    /// <param name="card">The card the name or alias belongs to.</param>
    /// <param name="file">The file to report.</param>
    /// <param name="line">The line to report.</param>
    /// <param name="kind">The word <c>name</c> or <c>alias</c>, for the message.</param>
    private void Claim(SourceEntry entry, Dictionary<string, string> used, string key, string description, CardDefinition card, string file, int line, string kind)
    {
        if (used.TryGetValue(key, out var owner))
        {
            Report(
                entry,
                RuleIds.NameDuplicate,
                file,
                line,
                $"The {kind} '{key}' of the card '{card.LocalId}' is already {owner}; "
                + "names and aliases must be unique among siblings, ignoring case.");
        }
        else
        {
            used[key] = description;
        }
    }

    /// <summary>
    ///     Resolves the links of one body.
    /// </summary>
    /// <param name="parts">The parts of the body.</param>
    /// <param name="file">The file the body came from, for problems.</param>
    /// <param name="entry">The source whose scope resolves the links.</param>
    /// <param name="containing">The card the body ends up in.</param>
    /// <returns>The resolved parts; unresolvable links are left out.</returns>
    private List<ResolvedPart> ResolveParts(IReadOnlyList<BodyPart> parts, string file, SourceEntry entry, CardNode containing)
    {
        var resolved = new List<ResolvedPart>();
        foreach (var part in parts)
        {
            var result = part.Link is null
                ? new ResolvedPart(part.Text, null)
                : ResolveLink(part.Link, containing, file, entry);
            if (result is not null)
            {
                resolved.Add(result);
            }
        }

        return resolved;
    }

    /// <summary>
    ///     Resolves one link of a body.
    /// </summary>
    /// <param name="link">The parsed link.</param>
    /// <param name="containing">The card the link ends up in.</param>
    /// <param name="file">The file the body came from, for problems.</param>
    /// <param name="entry">The source whose scope resolves the link.</param>
    /// <returns>The resolved part, or <see langword="null"/> when the link cannot be resolved.</returns>
    private ResolvedPart? ResolveLink(ParsedLink link, CardNode containing, string file, SourceEntry entry)
    {
        var target = containing;
        if (link.CardId is not null)
        {
            var lookup = _registry.Lookup(entry, link.SourceName, link.CardId);
            switch (lookup.Kind)
            {
                case LookupKind.SourceAbsent:
                    // A link to a source that is not present degrades to its display text
                    ReportAbsentSource(entry, link.SourceName!, file, link.Line, "the link is written as its display text");
                    return new ResolvedPart(link.Text, null);
                case LookupKind.Found:
                    target = lookup.Build!.Node!;
                    break;
                case LookupKind.NotFound:
                    Report(entry, RuleIds.LinkTarget, file, link.Line, $"The link '{Describe(link)}' names no card; check the id.");
                    return null;
                case LookupKind.Ambiguous:
                    Report(
                        entry,
                        RuleIds.AmbiguousReference,
                        file,
                        link.Line,
                        $"The link '{Describe(link)}' is ambiguous: {lookup.Detail}; write 'source:{link.CardId}' to choose.");
                    return null;
                default:
                    return null;
            }
        }

        if (link.ExampleName is null)
        {
            return new ResolvedPart(null, new ResolvedLink(target.Id, null, null, link.SourceName, link.Text));
        }

        var found = ExampleResolver.FindInSubtree(target, link.ExampleName, ChildrenOf);
        if (found is null)
        {
            Report(
                entry,
                RuleIds.ExampleTarget,
                file,
                link.Line,
                $"The link '{Describe(link)}' names an example that is not in the card '{target.Id}' or the cards below it.");
            return null;
        }

        var cardId = link.CardId is null ? null : target.Id;
        return new ResolvedPart(null, new ResolvedLink(cardId, found.Example.Name, found.CardId, link.SourceName, link.Text));
    }

    /// <summary>
    ///     Gets the children of a card in the tree.
    /// </summary>
    /// <param name="node">The card.</param>
    /// <returns>The children in sibling order.</returns>
    private IReadOnlyList<CardNode> ChildrenOf(CardNode node)
    {
        return node.ChildIds.Select(id => _nodes[id]).ToList();
    }

    /// <summary>
    ///     Records the informational note for a link or reference to a source that is not present.
    /// </summary>
    /// <param name="entry">The source whose file contains the reference.</param>
    /// <param name="sourceName">The name of the absent source.</param>
    /// <param name="file">The file the link was written in.</param>
    /// <param name="line">The line of the link.</param>
    /// <param name="consequence">What happens to the reference, for the message.</param>
    private void ReportAbsentSource(SourceEntry entry, string sourceName, string file, int line, string consequence)
    {
        _problems.Add(entry.Index, HelpProblem.CreateInfo(
            RuleIds.ExternalSourceAbsent,
            entry.Name,
            file,
            line,
            $"The source '{sourceName}' is not part of this catalog; {consequence}."));
    }

    /// <summary>
    ///     Records an error at a file and line.
    /// </summary>
    /// <param name="entry">The source whose file contains the defect.</param>
    /// <param name="ruleId">The rule that was broken.</param>
    /// <param name="file">The file.</param>
    /// <param name="line">The line.</param>
    /// <param name="message">The message.</param>
    private void Report(SourceEntry entry, string ruleId, string file, int line, string message)
    {
        _problems.Add(entry.Index, HelpProblem.CreateError(ruleId, entry.Name, file, line, message));
    }

    /// <summary>
    ///     Writes a link the way it was written in the card.
    /// </summary>
    /// <param name="link">The link.</param>
    /// <returns>The link text including the brackets.</returns>
    private static string Describe(ParsedLink link)
    {
        var builder = new StringBuilder("[[");
        if (link.SourceName is not null)
        {
            builder.Append(link.SourceName).Append(':');
        }

        builder.Append(link.CardId);
        if (link.ExampleName is not null)
        {
            builder.Append('#').Append(link.ExampleName);
        }

        if (link.Text is not null)
        {
            builder.Append('|').Append(link.Text);
        }

        return builder.Append("]]").ToString();
    }

    /// <summary>
    ///     Writes a reference the way it was written in the card.
    /// </summary>
    /// <param name="reference">The reference.</param>
    /// <returns>The reference text.</returns>
    private static string Describe(ReferenceDefinition reference)
    {
        return reference.SourceName is null ? reference.LocalId : $"{reference.SourceName}:{reference.LocalId}";
    }
}
