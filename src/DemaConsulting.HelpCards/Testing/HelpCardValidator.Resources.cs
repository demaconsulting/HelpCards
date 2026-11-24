using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;

namespace DemaConsulting.HelpCards.Testing;

/// <summary>
///     The comparison of embedded resources with the files of a directory.
/// </summary>
public static partial class HelpCardValidator
{
    /// <summary>
    ///     Compares the files embedded in an assembly with the files of a directory.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Authors edit cards in a directory while the product ships them as embedded resources. A card that
    ///         exists only on disk was forgotten by the build (for example because it sits outside the embedded
    ///         folder); a resource that exists only in the assembly was deleted or renamed on disk but the
    ///         assembly was not rebuilt. Either way the two views of the help differ, and a test should say so.
    ///     </para>
    ///     <para>
    ///         The directory is only listed, never read, and links are not resolved: pass the source folder of your own
    ///         repository, never a path from the input of a user.
    ///     </para>
    ///     <para>
    ///         Only the presence of each file is compared, by its path below the help root (ordinal comparison, with
    ///         <c>/</c> as the separator). Every difference is a <c>resources-mismatch</c> warning.
    ///     </para>
    /// </remarks>
    /// <param name="assemblySource">The source made with <see cref="HelpSource.FromAssembly"/>. Must not be null.</param>
    /// <param name="directory">The directory that holds the cards on disk. Must exist.</param>
    /// <returns>One warning for each file that is on only one side, in path order; empty when both sides agree.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assemblySource"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="directory"/> is empty or does not exist.</exception>
    public static IReadOnlyList<HelpProblem> CompareToDirectory(HelpSource assemblySource, string directory)
    {
        ArgumentNullException.ThrowIfNull(assemblySource);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        if (!Directory.Exists(directory))
        {
            throw new ArgumentException($"The directory '{directory}' does not exist.", nameof(directory));
        }

        var embedded = assemblySource.Provider.ListFiles();
        var onDisk = new DirectoryFileProvider(directory).ListFiles();
        var sourceName = assemblySource.Name ?? Path.GetFileName(Path.GetFullPath(directory));
        var problems = new List<HelpProblem>();
        problems.AddRange(onDisk.Except(embedded, StringComparer.Ordinal).Select(path => Warning(
            RuleIds.ResourcesMismatch,
            sourceName,
            path,
            0,
            "The file is in the directory but not embedded in the assembly; rebuild, or check the folder that the project embeds.")));
        problems.AddRange(embedded.Except(onDisk, StringComparer.Ordinal).Select(path => Warning(
            RuleIds.ResourcesMismatch,
            sourceName,
            path,
            0,
            "The file is embedded in the assembly but not in the directory; rebuild the assembly, or restore the file.")));
        return problems.OrderBy(problem => problem.File, StringComparer.Ordinal).ToList().AsReadOnly();
    }
}
