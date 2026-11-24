using System.Text;
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Composition;

/// <summary>
///     Gives the cards of every source their global ids, creates the generated root cards of mounted sources, and
///     finds the clashes that two sources can cause.
/// </summary>
/// <remarks>
///     <para>
///         A source added flat keeps its local ids as global ids. A mounted source gets a generated root card whose
///         id is the mount, and the global id of each of its cards is <c>mount/localId</c>. A mount renames only
///         global ids; authored references keep using local ids and declared source names.
///     </para>
///     <para>
///         Nothing shadows silently. Two cards with one global id, a mount equal to another mount or to a flat
///         root, and a root name or alias that two sources both use are <c>collision</c> problems that name both
///         sources and files; a mount that is not a valid segment is <c>mount-invalid</c>. The source or card that
///         comes later in registration order is left out, so one defect yields one problem. The class is static
///         and stateless and therefore safe for concurrent use.
///     </para>
/// </remarks>
internal static class Mounter
{
    /// <summary>
    ///     Gets the global id of a card of a source.
    /// </summary>
    /// <param name="entry">The source.</param>
    /// <param name="localId">The local id of the card.</param>
    /// <returns>The local id for a flat source, or <c>mount/localId</c> for a mounted one.</returns>
    public static string GlobalId(SourceEntry entry, string localId)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(localId);
        return entry.Mount is null ? localId : $"{entry.Mount}/{localId}";
    }

    /// <summary>
    ///     Claims the global ids of every source and reports clashes (pipeline step 4).
    /// </summary>
    /// <remarks>
    ///     Sources are visited in registration order. The first holder of an id keeps it. A source whose mount is
    ///     invalid or already taken is excluded as a whole; a card whose global id is taken is recorded in
    ///     <see cref="SourceEntry.Dropped"/> so that the tree placement leaves it, and the cards below it, out.
    /// </remarks>
    /// <param name="registry">The registry of the read sources. Must not be null.</param>
    /// <param name="problems">Receives the problems. Must not be null.</param>
    /// <returns>The source that owns each claimed global id.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static IReadOnlyDictionary<string, SourceEntry> Register(SourceRegistry registry, ProblemCollector problems)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(problems);

        var owners = new Dictionary<string, (SourceEntry Entry, string File)>(StringComparer.Ordinal);
        foreach (var entry in registry.Entries.Where(entry => !entry.Excluded))
        {
            if (entry.Registration.Mounted && !ClaimMount(entry, owners, problems))
            {
                continue;
            }

            foreach (var card in entry.Definition.Cards)
            {
                var id = GlobalId(entry, card.LocalId);
                if (owners.TryGetValue(id, out var holder))
                {
                    entry.Dropped.Add(card.LocalId);
                    problems.Add(entry.Index, HelpProblem.CreateError(
                        RuleIds.Collision,
                        entry.Name,
                        card.File,
                        0,
                        $"The id '{id}' of the card '{card.LocalId}' is already used by {Describe(holder.Entry, holder.File)}; "
                        + "change a mount or rename a card."));
                }
                else
                {
                    owners[id] = (entry, card.File);
                }
            }
        }

        return owners.ToDictionary(pair => pair.Key, pair => pair.Value.Entry, StringComparer.Ordinal);
    }

    /// <summary>
    ///     Creates the generated root card of a mounted source.
    /// </summary>
    /// <remarks>
    ///     The card takes its title, summary, aliases, culture, and extension keys from <c>_source.md</c>; it has
    ///     no tags and no examples. Its body is resolved later, with the other bodies.
    /// </remarks>
    /// <param name="entry">The mounted source, whose tree is placed. Must not be null.</param>
    /// <returns>The card, with the roots of the source as its children and no body.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is null.</exception>
    public static CardNode CreateRoot(SourceEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var definition = entry.Definition;
        var mount = entry.Mount!;
        return new CardNode(
            mount,
            mount,
            mount,
            null,
            definition.Name,
            definition.Title,
            definition.Summary,
            [],
            CardNode.Freeze(definition.Aliases.Select(alias => alias.Value)),
            definition.Culture,
            CardNode.Freeze(entry.Tree.Roots.Select(root => GlobalId(entry, root.LocalId))),
            [],
            [],
            CardNode.Freeze(definition.Extensions),
            definition.File,
            definition.File,
            definition.BodyLine,
            []);
    }

    /// <summary>
    ///     Checks that the names and aliases of the roots of different sources are unique, ignoring case.
    /// </summary>
    /// <remarks>
    ///     The roots of a catalog are the mount root of every mounted source and the roots of every flat source.
    ///     A name or alias that two sources both use is a <c>collision</c>. Repeats inside one flat source are
    ///     reported by the sibling check as <c>name-duplicate</c>; a mount root that repeats its own name in an
    ///     alias is reported here as <c>name-duplicate</c>.
    /// </remarks>
    /// <param name="registry">The registry whose sources are placed. Must not be null.</param>
    /// <param name="problems">Receives the problems. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static void CheckRootNames(SourceRegistry registry, ProblemCollector problems)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(problems);

        var used = new Dictionary<string, (SourceEntry Entry, string Description, string File)>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in registry.Entries.Where(entry => !entry.Excluded))
        {
            if (entry.Registration.Mounted)
            {
                var file = entry.Definition.File;
                Claim(used, problems, entry, entry.Mount!, $"the mount of the source '{entry.Name}'", file, 0, "name", true);
                foreach (var alias in entry.Definition.Aliases)
                {
                    Claim(used, problems, entry, Normalize(alias.Value), $"an alias of the mount '{entry.Mount}'", alias.File, alias.Line, "alias", true);
                }

                continue;
            }

            foreach (var root in entry.Tree.Roots)
            {
                Claim(used, problems, entry, SiblingOrder.NameOf(root), $"the root card '{root.LocalId}'", root.File, 0, "name", false);
                foreach (var alias in root.Aliases)
                {
                    Claim(used, problems, entry, Normalize(alias.Value), $"an alias of the root card '{root.LocalId}'", alias.File, alias.Line, "alias", false);
                }
            }
        }
    }

    /// <summary>
    ///     Claims a mount, or reports why it cannot be used and excludes the source.
    /// </summary>
    /// <param name="entry">The mounted source.</param>
    /// <param name="owners">The global ids claimed so far.</param>
    /// <param name="problems">Receives the problem.</param>
    /// <returns><see langword="true"/> when the mount was claimed.</returns>
    private static bool ClaimMount(
        SourceEntry entry,
        Dictionary<string, (SourceEntry Entry, string File)> owners,
        ProblemCollector problems)
    {
        var mount = entry.Mount!;
        if (entry.Registration.Mount is null && entry.Name == CardReader.UnnamedSource)
        {
            // The source has no usable name and was reported as source-invalid by Loading: one defect, one problem
            entry.Excluded = true;
            return false;
        }

        if (!CardReader.IsSegment(mount))
        {
            entry.Excluded = true;
            problems.Add(entry.Index, HelpProblem.CreateError(
                RuleIds.MountInvalid,
                entry.Name,
                string.Empty,
                0,
                $"The mount '{mount}' is not valid; use lower-case letters and digits with single dashes (1 to 48 characters)."));
            return false;
        }

        if (owners.TryGetValue(mount, out var holder))
        {
            entry.Excluded = true;
            problems.Add(entry.Index, HelpProblem.CreateError(
                RuleIds.Collision,
                entry.Name,
                entry.Definition.File,
                0,
                $"The mount '{mount}' of the source '{entry.Name}' is already used by {Describe(holder.Entry, holder.File)}; "
                + "give the source another mount."));
            return false;
        }

        owners[mount] = (entry, entry.Definition.File);
        return true;
    }

    /// <summary>
    ///     Records a root name or alias, or reports it when another source already uses it.
    /// </summary>
    /// <param name="used">The names used so far.</param>
    /// <param name="problems">Receives the problem.</param>
    /// <param name="entry">The source that uses the name.</param>
    /// <param name="key">The name or alias.</param>
    /// <param name="description">Who uses it, for the message.</param>
    /// <param name="file">The file to report.</param>
    /// <param name="line">The line to report.</param>
    /// <param name="kind">The word <c>name</c> or <c>alias</c>, for the message.</param>
    /// <param name="checkSelf">Whether a repeat inside the same source is reported here.</param>
    private static void Claim(
        Dictionary<string, (SourceEntry Entry, string Description, string File)> used,
        ProblemCollector problems,
        SourceEntry entry,
        string key,
        string description,
        string file,
        int line,
        string kind,
        bool checkSelf)
    {
        if (!used.TryGetValue(key, out var holder))
        {
            used[key] = (entry, description, file);
            return;
        }

        var sameSource = ReferenceEquals(holder.Entry, entry);
        if (sameSource && !checkSelf)
        {
            return;
        }

        problems.Add(entry.Index, HelpProblem.CreateError(
            sameSource ? RuleIds.NameDuplicate : RuleIds.Collision,
            entry.Name,
            file,
            line,
            $"The {kind} '{key}' of {description} is already used by {holder.Description} of {Describe(holder.Entry, holder.File)}; "
            + "names and aliases of roots must be unique, ignoring case."));
    }

    /// <summary>
    ///     Describes a source and file for a message.
    /// </summary>
    /// <param name="entry">The source.</param>
    /// <param name="file">The file, or an empty string.</param>
    /// <returns>The description naming the source and, when known, the file.</returns>
    private static string Describe(SourceEntry entry, string file)
    {
        return file.Length > 0 ? $"the source '{entry.Name}' ('{file}')" : $"the source '{entry.Name}'";
    }

    /// <summary>
    ///     Normalizes an alias to NFC, the form in which aliases are compared.
    /// </summary>
    /// <param name="alias">The alias.</param>
    /// <returns>The normalized alias.</returns>
    private static string Normalize(string alias)
    {
        return alias.Normalize(NormalizationForm.FormC);
    }
}
