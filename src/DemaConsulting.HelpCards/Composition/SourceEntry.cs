using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Composition;

/// <summary>
///     One registered source while a catalog is built: what was registered, what was read, and what was decided.
/// </summary>
/// <remarks>
///     <para>
///         An entry starts with the registration and grows as the pipeline runs: the registry reads the source
///         and resolves its imports, the mounter decides its mount and claims its ids, and the tree placement
///         fills its cards. An entry that cannot take part in the catalog (a duplicate name, an invalid mount, an
///         id that is already taken) is <see cref="Excluded"/>: it contributes no cards, and references to it are
///         not reported again because the defect that excluded it is already a problem.
///     </para>
///     <para>
///         An entry belongs to one catalog build and is not safe for concurrent use.
///     </para>
/// </remarks>
internal sealed class SourceEntry
{
    /// <summary>
    ///     Creates an entry for a registration.
    /// </summary>
    /// <param name="index">The registration index.</param>
    /// <param name="registration">The registration.</param>
    public SourceEntry(int index, SourceRegistration registration)
    {
        Index = index;
        Registration = registration;
        Name = registration.Source.Name ?? CardReader.UnnamedSource;
    }

    /// <summary>
    ///     Gets the registration index, which is also the registration order.
    /// </summary>
    public int Index { get; }

    /// <summary>
    ///     Gets the registration.
    /// </summary>
    public SourceRegistration Registration { get; }

    /// <summary>
    ///     Gets the source.
    /// </summary>
    public HelpSource Source => Registration.Source;

    /// <summary>
    ///     Gets or sets the declared name of the source; known once the source file was read.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Gets or sets what the loader read; set by the registry.
    /// </summary>
    public SourceDefinition Definition { get; set; } = null!;

    /// <summary>
    ///     Gets the mount of the source, or <see langword="null"/> for a source that is added flat.
    /// </summary>
    public string? Mount => Registration.Mounted ? Registration.Mount ?? Name : null;

    /// <summary>
    ///     Gets the sources this source imports directly, in declaration order.
    /// </summary>
    public List<SourceEntry> Imports { get; } = [];

    /// <summary>
    ///     Gets or sets the sources this source imports, directly or not, in the order they are searched.
    /// </summary>
    public IReadOnlyList<SourceEntry> ImportClosure { get; set; } = [];

    /// <summary>
    ///     Gets or sets a value that says whether an import could not be resolved, so bare references that
    ///     match nothing are not reported again.
    /// </summary>
    public bool BrokenImports { get; set; }

    /// <summary>
    ///     Gets or sets a value that says whether the source takes no part in the catalog.
    /// </summary>
    public bool Excluded { get; set; }

    /// <summary>
    ///     Gets the ids of cards whose global id is already taken by another source.
    /// </summary>
    public HashSet<string> Dropped { get; } = new(StringComparer.Ordinal);

    /// <summary>
    ///     Gets or sets the placed cards of the source.
    /// </summary>
    public SourceTree Tree { get; set; } = SourceTree.Empty;

    /// <summary>
    ///     Gets the cards of the source by local id, once they are placed.
    /// </summary>
    public Dictionary<string, CardBuild> Cards { get; } = new(StringComparer.Ordinal);

    /// <summary>
    ///     Gets or sets the generated root card of a mounted source, once it is created.
    /// </summary>
    public CardNode? MountRoot { get; set; }
}
