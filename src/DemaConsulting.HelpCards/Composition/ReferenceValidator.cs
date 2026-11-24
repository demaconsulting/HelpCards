using System.Text;
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Composition;

/// <summary>
///     Checks the final tree of a source: it resolves every link and see-also entry, and checks that names are unique.
/// </summary>
/// <remarks>
///     <para>
///         Resolution runs once over the finished tree, so a forward reference is as good as a backward one. A
///         <c>[[id]]</c> link and a see-also entry must name a card of the source; <c>[[id#name]]</c> must name an
///         example in the subtree of that card; <c>[[#name]]</c> must name an example in the subtree of the card the
///         link is written in. An external link whose source is registered resolves like a local link; one whose
///         source is not present degrades to its display text and leaves an informational problem. A reference to a
///         file that failed to load, or to a card that had no place in the tree, is not reported again.
///     </para>
///     <para>
///         The validator also checks that example names are unique within a scope (the source and the first
///         segment of the owning card's id) and that the names and aliases of siblings and of roots are unique,
///         ignoring case. A validator belongs to one build and is not safe for concurrent use.
///     </para>
/// </remarks>
internal sealed class ReferenceValidator
{
    /// <summary>
    ///     The name of the source being validated.
    /// </summary>
    private readonly string _sourceName;

    /// <summary>
    ///     The nodes of the tree by global id.
    /// </summary>
    private readonly IReadOnlyDictionary<string, CardNode> _nodes;

    /// <summary>
    ///     The ids of files that failed to load or had no place in the tree.
    /// </summary>
    private readonly IReadOnlySet<string> _suppressed;

    /// <summary>
    ///     The declared names of the registered sources.
    /// </summary>
    private readonly IReadOnlySet<string> _registeredSources;

    /// <summary>
    ///     Receives the problems found.
    /// </summary>
    private readonly ICollection<HelpProblem> _problems;

    /// <summary>
    ///     Creates a validator for one source.
    /// </summary>
    /// <param name="sourceName">The name of the source.</param>
    /// <param name="nodes">The nodes of the tree by global id, with children and examples in place.</param>
    /// <param name="suppressedIds">The ids of files that failed to load or had no place in the tree.</param>
    /// <param name="registeredSources">The declared names of the registered sources.</param>
    /// <param name="problems">Receives every problem found.</param>
    public ReferenceValidator(
        string sourceName,
        IReadOnlyDictionary<string, CardNode> nodes,
        IReadOnlySet<string> suppressedIds,
        IReadOnlySet<string> registeredSources,
        ICollection<HelpProblem> problems)
    {
        _sourceName = sourceName;
        _nodes = nodes;
        _suppressed = suppressedIds;
        _registeredSources = registeredSources;
        _problems = problems;
    }

    /// <summary>
    ///     Resolves the body links of a card.
    /// </summary>
    /// <param name="card">The definition of the card.</param>
    /// <returns>The body with every link resolved to global ids; unresolvable links are left out.</returns>
    public IReadOnlyList<ResolvedPart> ResolveBody(CardDefinition card)
    {
        var containing = _nodes[card.LocalId];
        var parts = new List<ResolvedPart>();
        foreach (var part in card.Body)
        {
            var resolved = part.Link is null
                ? new ResolvedPart(part.Text, null)
                : ResolveLink(part.Link, containing, card.BodyFile);
            if (resolved is not null)
            {
                parts.Add(resolved);
            }
        }

        return parts;
    }

    /// <summary>
    ///     Resolves the see-also entries of a card.
    /// </summary>
    /// <param name="card">The definition of the card.</param>
    /// <returns>The global ids of the related cards in the order written, without duplicates.</returns>
    public IReadOnlyList<string> ResolveSeeAlso(CardDefinition card)
    {
        var ids = new List<string>();
        foreach (var reference in card.SeeAlso)
        {
            if (reference.SourceName is not null && !_registeredSources.Contains(reference.SourceName))
            {
                ReportAbsentSource(reference.SourceName, reference.File, reference.Line);
            }
            else if (_nodes.TryGetValue(reference.LocalId, out var target))
            {
                if (!ids.Contains(target.Id, StringComparer.Ordinal))
                {
                    ids.Add(target.Id);
                }
            }
            else if (!_suppressed.Contains(reference.LocalId))
            {
                _problems.Add(HelpProblem.CreateError(
                    RuleIds.SeeAlsoTarget,
                    _sourceName,
                    reference.File,
                    reference.Line,
                    $"The 'see-also' entry '{Describe(reference)}' names no card; check the id."));
            }
        }

        return ids;
    }

    /// <summary>
    ///     Checks that example names are unique within each scope.
    /// </summary>
    /// <remarks>
    ///     The scope of an example is the source and the first segment of the id of the card that owns it. The
    ///     second use of a name in a scope is reported at its own file and line.
    /// </remarks>
    /// <param name="cards">The cards in ordinal id order.</param>
    public void CheckExampleNames(IEnumerable<CardDefinition> cards)
    {
        var first = new Dictionary<(string Scope, string Name), ExampleDefinition>();
        foreach (var card in cards)
        {
            var scope = card.LocalId.Split('/')[0];
            foreach (var example in card.Examples)
            {
                if (first.TryGetValue((scope, example.Name), out var original))
                {
                    _problems.Add(HelpProblem.CreateError(
                        RuleIds.DuplicateExample,
                        _sourceName,
                        example.File,
                        example.Line,
                        $"The example name '{example.Name}' is already used at line {original.Line} of '{original.File}'; "
                        + $"example names must be unique within '{scope}' and the cards below it."));
                }
                else
                {
                    first[(scope, example.Name)] = example;
                }
            }
        }
    }

    /// <summary>
    ///     Checks that the names and aliases of cards that share a parent are unique, ignoring case.
    /// </summary>
    /// <param name="siblings">The cards that share a parent, or the roots of the source, in order.</param>
    public void CheckSiblingNames(IReadOnlyList<CardDefinition> siblings)
    {
        var used = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var card in siblings)
        {
            var name = SiblingOrder.NameOf(card);
            Claim(used, name, $"the name of the card '{card.LocalId}'", card, card.File, 0, "name");
            foreach (var alias in card.Aliases)
            {
                var normalized = alias.Value.Normalize(NormalizationForm.FormC);
                Claim(used, normalized, $"an alias of the card '{card.LocalId}'", card, alias.File, alias.Line, "alias");
            }
        }
    }

    /// <summary>
    ///     Records a name or alias as used, or reports it when a sibling already uses it.
    /// </summary>
    /// <param name="used">The names used so far and who uses them.</param>
    /// <param name="key">The name or alias.</param>
    /// <param name="description">Who uses it, for the message.</param>
    /// <param name="card">The card the name or alias belongs to.</param>
    /// <param name="file">The file to report.</param>
    /// <param name="line">The line to report.</param>
    /// <param name="kind">The word <c>name</c> or <c>alias</c>, for the message.</param>
    private void Claim(Dictionary<string, string> used, string key, string description, CardDefinition card, string file, int line, string kind)
    {
        if (used.TryGetValue(key, out var owner))
        {
            _problems.Add(HelpProblem.CreateError(
                RuleIds.NameDuplicate,
                _sourceName,
                file,
                line,
                $"The {kind} '{key}' of the card '{card.LocalId}' is already {owner}; "
                + "names and aliases must be unique among siblings, ignoring case."));
        }
        else
        {
            used[key] = description;
        }
    }

    /// <summary>
    ///     Resolves one link of a body.
    /// </summary>
    /// <param name="link">The parsed link.</param>
    /// <param name="containing">The card the link is written in.</param>
    /// <param name="file">The file the body came from, for problems.</param>
    /// <returns>The resolved part, or <see langword="null"/> when the link cannot be resolved.</returns>
    private ResolvedPart? ResolveLink(ParsedLink link, CardNode containing, string file)
    {
        // A link to a source that is not present degrades to its display text
        if (link.SourceName is not null && !_registeredSources.Contains(link.SourceName))
        {
            ReportAbsentSource(link.SourceName, file, link.Line);
            return new ResolvedPart(link.Text, null);
        }

        var target = containing;
        if (link.CardId is not null)
        {
            if (!_nodes.TryGetValue(link.CardId, out var named))
            {
                if (!_suppressed.Contains(link.CardId))
                {
                    Report(RuleIds.LinkTarget, file, link.Line, $"The link '{Describe(link)}' names no card; check the id.");
                }

                return null;
            }

            target = named;
        }

        if (link.ExampleName is null)
        {
            return new ResolvedPart(null, new ResolvedLink(target.Id, null, null, link.SourceName, link.Text));
        }

        var found = ExampleResolver.FindInSubtree(target, link.ExampleName, ChildrenOf);
        if (found is null)
        {
            Report(
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
    /// <param name="sourceName">The name of the absent source.</param>
    /// <param name="file">The file the link was written in.</param>
    /// <param name="line">The line of the link.</param>
    private void ReportAbsentSource(string sourceName, string file, int line)
    {
        _problems.Add(HelpProblem.CreateInfo(
            RuleIds.ExternalSourceAbsent,
            _sourceName,
            file,
            line,
            $"The source '{sourceName}' is not part of this catalog; the link is written as its display text."));
    }

    /// <summary>
    ///     Records an error at a file and line.
    /// </summary>
    /// <param name="ruleId">The rule that was broken.</param>
    /// <param name="file">The file.</param>
    /// <param name="line">The line.</param>
    /// <param name="message">The message.</param>
    private void Report(string ruleId, string file, int line, string message)
    {
        _problems.Add(HelpProblem.CreateError(ruleId, _sourceName, file, line, message));
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
