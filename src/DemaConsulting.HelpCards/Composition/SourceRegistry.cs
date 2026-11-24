using System.Globalization;
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Composition;

/// <summary>
///     Registers the sources of a catalog, reads them in registration order, resolves their imports, and looks up
///     the cards that references name.
/// </summary>
/// <remarks>
///     <para>
///         The registry is the one place that knows which sources a catalog has. Reading happens in
///         registration order, and a source can only import a source registered before it, so when a source is
///         read every source it imports has already been read and its tokens are known. An import is matched to a
///         registered source by the files it reads (the same source, or a copy made with a <c>With...</c> call)
///         or, when the import has a declared name, by that name. An import that matches nothing is
///         <c>import-missing</c>, one that matches a source registered later is <c>import-order</c>, and one that
///         matches the importing source itself is <c>import-cycle</c>. A source name that a second source uses is
///         <c>source-name-duplicate</c> and the second source is excluded from the catalog.
///     </para>
///     <para>
///         A bare reference (<c>id</c>) looks first in the cards of the referencing source and then in the cards
///         of its imports; an id that exists in both the source and an import is ambiguous. A qualified reference
///         (<c>source:id</c>) looks only in the named source, and a source that is not registered is reported as
///         absent so the caller can degrade. A registry belongs to one catalog build and is not safe for
///         concurrent use.
///     </para>
/// </remarks>
internal sealed class SourceRegistry
{
    /// <summary>
    ///     The registered sources in registration order.
    /// </summary>
    private readonly List<SourceEntry> _entries;

    /// <summary>
    ///     The sources by declared name, ignoring case; a duplicate name is not added.
    /// </summary>
    private readonly Dictionary<string, SourceEntry> _byName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     The problems found while registering, each with the index of its source.
    /// </summary>
    private readonly List<(int SourceIndex, HelpProblem Problem)> _problems = [];

    /// <summary>
    ///     The tool name set on the builder, or <see langword="null"/>.
    /// </summary>
    private readonly string? _toolName;

    /// <summary>
    ///     The tool version set on the builder, or <see langword="null"/>.
    /// </summary>
    private readonly string? _toolVersion;

    /// <summary>
    ///     Creates a registry over registrations that are not read yet.
    /// </summary>
    /// <param name="registrations">The registrations in registration order.</param>
    /// <param name="toolName">The tool name set on the builder, or <see langword="null"/>.</param>
    /// <param name="toolVersion">The tool version set on the builder, or <see langword="null"/>.</param>
    private SourceRegistry(IReadOnlyList<SourceRegistration> registrations, string? toolName, string? toolVersion)
    {
        _entries = registrations.Select((registration, index) => new SourceEntry(index, registration)).ToList();
        _toolName = toolName;
        _toolVersion = toolVersion;
    }

    /// <summary>
    ///     Gets the sources in registration order.
    /// </summary>
    public IReadOnlyList<SourceEntry> Entries => _entries;

    /// <summary>
    ///     Gets the problems found while registering and reading, each with the index of its source.
    /// </summary>
    public IReadOnlyList<(int SourceIndex, HelpProblem Problem)> Problems => _problems;

    /// <summary>
    ///     Registers and reads the sources of a catalog (pipeline steps 1 to 3).
    /// </summary>
    /// <remarks>
    ///     The call lists and reads the files of every source through its provider. Defects never throw; they
    ///     are returned in <see cref="Problems"/> and in the definition of each source.
    /// </remarks>
    /// <param name="registrations">The registrations in registration order. Must not be null.</param>
    /// <param name="culture">The culture whose translations are applied. Must not be null.</param>
    /// <param name="toolName">The tool name set on the builder, or <see langword="null"/>.</param>
    /// <param name="toolVersion">The tool version set on the builder, or <see langword="null"/>.</param>
    /// <returns>The registry with every source read.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static SourceRegistry Read(
        IReadOnlyList<SourceRegistration> registrations,
        CultureInfo culture,
        string? toolName,
        string? toolVersion)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        ArgumentNullException.ThrowIfNull(culture);
        var registry = new SourceRegistry(registrations, toolName, toolVersion);
        foreach (var entry in registry._entries)
        {
            registry.ReadEntry(entry, culture);
        }

        return registry;
    }

    /// <summary>
    ///     Finds a registered source by its declared name, ignoring case.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The source, or <see langword="null"/> when no registered source has the name.</returns>
    public SourceEntry? FindByName(string name)
    {
        return _byName.GetValueOrDefault(name);
    }

    /// <summary>
    ///     Looks up the card a reference names.
    /// </summary>
    /// <param name="from">The source whose file contains the reference.</param>
    /// <param name="sourceName">The declared source name of a qualified reference, or <see langword="null"/>.</param>
    /// <param name="localId">The local id.</param>
    /// <returns>The outcome.</returns>
    public LookupResult Lookup(SourceEntry from, string? sourceName, string localId)
    {
        if (sourceName is not null)
        {
            return _byName.TryGetValue(sourceName, out var named)
                ? Probe(named, localId)
                : new LookupResult(LookupKind.SourceAbsent, null);
        }

        // A bare id is looked up in the source itself and then in its imports; both having it is a clash
        var own = Probe(from, localId);
        var imported = from.ImportClosure.Select(entry => (Entry: entry, Result: Probe(entry, localId))).ToList();
        var hit = imported.FirstOrDefault(item => item.Result.Kind == LookupKind.Found);
        if (own.Kind == LookupKind.Found)
        {
            return hit.Result is null
                ? own
                : new LookupResult(
                    LookupKind.Ambiguous,
                    null,
                    $"'{localId}' names a card of the source '{from.Name}' and a card of the imported source '{hit.Entry.Name}'");
        }

        if (own.Kind == LookupKind.Suppressed)
        {
            return own;
        }

        if (hit.Result is not null)
        {
            return hit.Result;
        }

        // A failed card of an import, or an import that could not be resolved, is already reported
        var unreported = from.BrokenImports || imported.Any(item => item.Result.Kind == LookupKind.Suppressed);
        return unreported ? new LookupResult(LookupKind.Suppressed, null) : own;
    }

    /// <summary>
    ///     Looks for a local id in one source.
    /// </summary>
    /// <param name="entry">The source.</param>
    /// <param name="localId">The local id.</param>
    /// <returns>Found, suppressed (the card failed or the source is excluded), or not found.</returns>
    private static LookupResult Probe(SourceEntry entry, string localId)
    {
        if (entry.Excluded)
        {
            return new LookupResult(LookupKind.Suppressed, null);
        }

        if (entry.Cards.TryGetValue(localId, out var build))
        {
            return new LookupResult(LookupKind.Found, build);
        }

        return entry.Tree.Suppressed.Contains(localId)
            ? new LookupResult(LookupKind.Suppressed, null)
            : new LookupResult(LookupKind.NotFound, null);
    }

    /// <summary>
    ///     Reads one source and registers its name.
    /// </summary>
    /// <param name="entry">The entry to read.</param>
    /// <param name="culture">The culture whose translations are applied.</param>
    private void ReadEntry(SourceEntry entry, CultureInfo culture)
    {
        var definition = CardReader.Read(entry.Source, culture, name => CreateTokenScope(entry, name));
        entry.Definition = definition;
        entry.Name = definition.Name;
        foreach (var problem in definition.Problems)
        {
            _problems.Add((entry.Index, problem));
        }

        if (definition.Name == CardReader.UnnamedSource)
        {
            return;
        }

        if (_byName.TryGetValue(definition.Name, out var first))
        {
            entry.Excluded = true;
            Report(
                entry,
                RuleIds.SourceNameDuplicate,
                definition.File,
                $"The source name '{definition.Name}' is already used by the source registered at position {first.Index + 1}"
                + $"{DescribeFile(first)}; rename one of them with HelpSource.WithName or in '_source.md'.");
        }
        else
        {
            _byName[definition.Name] = entry;
        }
    }

    /// <summary>
    ///     Resolves the imports of a source and builds its token scope, once its name is known.
    /// </summary>
    /// <param name="entry">The source that is being read.</param>
    /// <param name="name">The resolved name of the source.</param>
    /// <returns>The token scope of the source.</returns>
    private TokenResolver CreateTokenScope(SourceEntry entry, string name)
    {
        entry.Name = name;
        ResolveImports(entry);

        var closure = new List<SourceEntry>();
        AddClosure(entry, closure);
        entry.ImportClosure = closure;

        // Tokens follow what the source declares it imports, so an import that is misplaced or missing does not
        // also make every token it provides unknown
        var seen = new HashSet<HelpFileProvider> { entry.Source.Provider };
        var imported = new List<KeyValuePair<string, IReadOnlyDictionary<string, string>>>();
        CollectImportedTokens(entry.Source, seen, imported);
        return TokenResolver.Create(name, entry.Source.Tokens, imported, _toolName ?? _entries[0].Name, _toolVersion ?? string.Empty);
    }

    /// <summary>
    ///     Collects the tokens of the sources a source imports, directly or not, each source once.
    /// </summary>
    /// <param name="source">The importing source.</param>
    /// <param name="seen">The files already collected, so a source reached twice counts once.</param>
    /// <param name="tokens">Receives the name and tokens of each imported source in search order.</param>
    private void CollectImportedTokens(
        HelpSource source,
        HashSet<HelpFileProvider> seen,
        List<KeyValuePair<string, IReadOnlyDictionary<string, string>>> tokens)
    {
        foreach (var imported in source.ImportedSources.Where(imported => seen.Add(imported.Provider)))
        {
            var match = _entries.FirstOrDefault(candidate => Matches(candidate, imported));
            tokens.Add(new(match?.Name ?? imported.Name ?? "an imported source", imported.Tokens));
            CollectImportedTokens(imported, seen, tokens);
        }
    }
    /// <summary>
    ///     Matches the imports of a source to registered sources and reports those that cannot be used.
    /// </summary>
    /// <param name="entry">The source whose imports are resolved.</param>
    private void ResolveImports(SourceEntry entry)
    {
        foreach (var imported in entry.Source.ImportedSources)
        {
            var match = _entries.FirstOrDefault(candidate => Matches(candidate, imported));
            if (match is null)
            {
                entry.BrokenImports = true;
                Report(
                    entry,
                    RuleIds.ImportMissing,
                    string.Empty,
                    $"The source '{entry.Name}' imports {Describe(imported)}, which is not added to the catalog; add it before '{entry.Name}'.");
            }
            else if (match.Index == entry.Index)
            {
                entry.BrokenImports = true;
                Report(entry, RuleIds.ImportCycle, string.Empty, $"The source '{entry.Name}' imports itself, directly or through a copy.");
            }
            else if (match.Index > entry.Index)
            {
                entry.BrokenImports = true;
                Report(
                    entry,
                    RuleIds.ImportOrder,
                    string.Empty,
                    $"The source '{entry.Name}' imports {Label(match)}, which is added after it; add it before '{entry.Name}'.");
            }
            else if (!entry.Imports.Contains(match))
            {
                entry.Imports.Add(match);
            }
        }
    }

    /// <summary>
    ///     Determines whether a registered source is the source an import names.
    /// </summary>
    /// <param name="candidate">The registered source.</param>
    /// <param name="imported">The imported source.</param>
    /// <returns><see langword="true"/> when they read the same files or share the declared name.</returns>
    private static bool Matches(SourceEntry candidate, HelpSource imported)
    {
        return ReferenceEquals(candidate.Source.Provider, imported.Provider)
            || (imported.Name is not null
                && candidate.Name != CardReader.UnnamedSource
                && string.Equals(candidate.Name, imported.Name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Collects the sources a source imports, directly or not, in the order they are searched.
    /// </summary>
    /// <param name="entry">The source.</param>
    /// <param name="closure">Receives the sources without duplicates.</param>
    private static void AddClosure(SourceEntry entry, List<SourceEntry> closure)
    {
        foreach (var imported in entry.Imports.Where(imported => !closure.Contains(imported)))
        {
            closure.Add(imported);
            AddClosure(imported, closure);
        }
    }

    /// <summary>
    ///     Records an error of a source.
    /// </summary>
    /// <param name="entry">The source.</param>
    /// <param name="ruleId">The rule.</param>
    /// <param name="file">The file, or an empty string.</param>
    /// <param name="message">The message.</param>
    private void Report(SourceEntry entry, string ruleId, string file, string message)
    {
        _problems.Add((entry.Index, HelpProblem.CreateError(ruleId, entry.Name, file, 0, message)));
    }

    /// <summary>
    ///     Describes an imported source for a message.
    /// </summary>
    /// <param name="imported">The imported source.</param>
    /// <returns>The name in quotes, or a general phrase when it has none.</returns>
    private static string Describe(HelpSource imported)
    {
        return imported.Name is null ? "a source without a name set with WithName" : $"the source '{imported.Name}'";
    }

    /// <summary>
    ///     Describes a registered source for a message, by name when the name is known and by position otherwise.
    /// </summary>
    /// <param name="entry">The source.</param>
    /// <returns>The description.</returns>
    private static string Label(SourceEntry entry)
    {
        return entry.Name == CardReader.UnnamedSource ? $"the source at position {entry.Index + 1}" : $"the source '{entry.Name}'";
    }

    /// <summary>
    ///     Describes the file of a source for a message.
    /// </summary>
    /// <param name="entry">The source.</param>
    /// <returns>The description, or an empty string when the source has no source file.</returns>
    private static string DescribeFile(SourceEntry entry)
    {
        return entry.Definition.File.Length > 0 ? $" ('{entry.Definition.File}')" : string.Empty;
    }
}
