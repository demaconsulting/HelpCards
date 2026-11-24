using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Composition;

/// <summary>
///     Applies the overlays of every source to the cards they extend (pipeline step 6).
/// </summary>
/// <remarks>
///     <para>
///         <c>extends</c> is the only identity of an overlay; the path of the overlay file only has to be a valid
///         id that is unique in its source. Overlays are applied in the order of the sources in registration
///         order and, inside one source, by ordinal file path, so the result never depends on the machine. For
///         each overlay the title and summary replace those of the target (the last overlay wins), the body is
///         appended after a blank line, see-also entries are appended, examples are appended, and extension keys
///         replace those of the same name.
///     </para>
///     <para>
///         The target is a reference <c>[source:]localId</c>. A bare id is resolved like every bare reference: in
///         the overlay's own source first and then in its imports. A qualified target may name the overlay's own
///         source or one registered earlier. An optional overlay whose target source is not part of the catalog is
///         skipped and leaves an informational problem; every other unresolved target is an error. An overlay
///         cannot target a generated mount root, because mount roots have no local id. The class is static and
///         stateless and therefore safe for concurrent use.
///     </para>
/// </remarks>
internal static class OverlayMerger
{
    /// <summary>
    ///     Applies every overlay of every source.
    /// </summary>
    /// <param name="registry">The registry whose sources are placed. Must not be null.</param>
    /// <param name="problems">Receives the problems. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static void Apply(SourceRegistry registry, ProblemCollector problems)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(problems);

        foreach (var entry in registry.Entries.Where(entry => !entry.Excluded))
        {
            foreach (var overlay in entry.Definition.Overlays.OrderBy(overlay => overlay.File, StringComparer.Ordinal))
            {
                ApplyOverlay(registry, entry, overlay, problems);
            }
        }
    }

    /// <summary>
    ///     Applies one overlay, or reports why it cannot be applied.
    /// </summary>
    /// <param name="registry">The registry.</param>
    /// <param name="entry">The source of the overlay.</param>
    /// <param name="overlay">The overlay.</param>
    /// <param name="problems">Receives the problems.</param>
    private static void ApplyOverlay(SourceRegistry registry, SourceEntry entry, OverlayDefinition overlay, ProblemCollector problems)
    {
        var reference = overlay.Extends;
        if (reference.SourceName is not null && !IsUsableSource(registry, entry, reference.SourceName, overlay, problems))
        {
            return;
        }

        var lookup = registry.Lookup(entry, reference.SourceName, reference.LocalId);
        switch (lookup.Kind)
        {
            case LookupKind.Found:
                Merge(lookup.Build!, entry, overlay);
                break;
            case LookupKind.NotFound:
                Report(problems, entry, RuleIds.OverlayTarget, overlay, $"The overlay extends '{Describe(reference)}', which names no card; check the id. A generated mount root cannot be extended.");
                break;
            case LookupKind.Ambiguous:
                Report(problems, entry, RuleIds.AmbiguousReference, overlay, $"The overlay extends '{Describe(reference)}', but {lookup.Detail}; write 'source:{reference.LocalId}' to choose.");
                break;
            default:
                // The target failed to load or its source is excluded; that defect is already reported
                break;
        }
    }

    /// <summary>
    ///     Checks that the source an overlay names can be a target, and skips an optional overlay of an absent source.
    /// </summary>
    /// <param name="registry">The registry.</param>
    /// <param name="entry">The source of the overlay.</param>
    /// <param name="sourceName">The declared name of the target source.</param>
    /// <param name="overlay">The overlay.</param>
    /// <param name="problems">Receives the problems.</param>
    /// <returns><see langword="true"/> when the target can be looked up.</returns>
    private static bool IsUsableSource(
        SourceRegistry registry,
        SourceEntry entry,
        string sourceName,
        OverlayDefinition overlay,
        ProblemCollector problems)
    {
        var target = registry.FindByName(sourceName);
        if (target is null)
        {
            if (overlay.Optional)
            {
                problems.Add(entry.Index, HelpProblem.CreateInfo(
                    RuleIds.OverlayOptionalSkipped,
                    entry.Name,
                    overlay.File,
                    overlay.Extends.Line,
                    $"The optional overlay extends '{Describe(overlay.Extends)}', but the source '{sourceName}' is not part of this catalog; the overlay is skipped."));
            }
            else
            {
                Report(problems, entry, RuleIds.ExtendsTarget, overlay, $"The overlay extends '{Describe(overlay.Extends)}', but the source '{sourceName}' is not part of this catalog; add the source or mark the overlay 'optional: true'.");
            }

            return false;
        }

        if (target.Index > entry.Index)
        {
            Report(problems, entry, RuleIds.ExtendsTarget, overlay, $"The overlay extends '{Describe(overlay.Extends)}', but the source '{sourceName}' is added after '{entry.Name}'; add it first.");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Adds the content of an overlay to its target.
    /// </summary>
    /// <param name="target">The card the overlay extends.</param>
    /// <param name="entry">The source of the overlay.</param>
    /// <param name="overlay">The overlay.</param>
    private static void Merge(CardBuild target, SourceEntry entry, OverlayDefinition overlay)
    {
        target.Title = overlay.Title ?? target.Title;
        target.Summary = overlay.Summary ?? target.Summary;
        if (overlay.Body.Count > 0)
        {
            target.Bodies.Add(new CardBuild.BodySegment(overlay.Body, overlay.BodyFile, entry));
        }

        target.SeeAlso.AddRange(overlay.SeeAlso.Select(reference => new CardBuild.SeeAlsoEntry(reference, entry)));
        target.Examples.AddRange(overlay.Examples.Select(example => new CardBuild.ExampleEntry(example, entry)));
        foreach (var (key, value) in overlay.Extensions)
        {
            target.Extensions[key] = value;
        }
    }

    /// <summary>
    ///     Records an error of an overlay at its file and the line of its <c>extends</c>.
    /// </summary>
    /// <param name="problems">Receives the problem.</param>
    /// <param name="entry">The source of the overlay.</param>
    /// <param name="ruleId">The rule.</param>
    /// <param name="overlay">The overlay.</param>
    /// <param name="message">The message.</param>
    private static void Report(ProblemCollector problems, SourceEntry entry, string ruleId, OverlayDefinition overlay, string message)
    {
        problems.Add(entry.Index, HelpProblem.CreateError(ruleId, entry.Name, overlay.File, overlay.Extends.Line, message));
    }

    /// <summary>
    ///     Writes a reference the way it was written in the overlay.
    /// </summary>
    /// <param name="reference">The reference.</param>
    /// <returns>The reference text.</returns>
    private static string Describe(ReferenceDefinition reference)
    {
        return reference.SourceName is null ? reference.LocalId : $"{reference.SourceName}:{reference.LocalId}";
    }
}
