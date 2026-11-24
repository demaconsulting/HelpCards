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
    ///         <b>Prefer a unit test of the embedded cards.</b> The packaged build targets embed every file below the
    ///         <c>Help</c> folder, so <c>HelpCardValidator.AssertValid</c> on the embedded source, together with
    ///         <c>HelpExampleRunner</c>, proves what the product ships. This method compares the embedded files with
    ///         the files of a directory, usually the checked-in <c>Help</c> folder of the repository. A test that reads
    ///         the checked-in source files of the repository is not a unit test of the product: the DEMA testing
    ///         standard does not allow requirement-linked tests to assert on checked-in source or configuration files,
    ///         because such a test depends on where the repository is on the build machine and not on the behavior of
    ///         the product. The method stays for a check outside the unit tests, for example a packaging or release
    ///         check that compares the embedded files with a folder that the product itself exports, or a script of the
    ///         build. It is not marked obsolete, and it is not planned to be removed before a major release.
    ///     </para>
    ///     <para>
    ///         Authors edit cards in a directory while the product ships them as embedded resources. A card that
    ///         exists only on disk was forgotten by the build (for example because it sits outside the embedded
    ///         folder); a resource that exists only in the assembly was deleted or renamed on disk but the
    ///         assembly was not rebuilt. Either way the two views of the help differ, and the check should say so.
    ///     </para>
    ///     <para>
    ///         The directory is only listed, never read, and links are not resolved: pass a folder of your own
    ///         build, never a path from the input of a user.
    ///     </para>
    ///     <para>
    ///         Only the presence of each file is compared, by its path below the help root (ordinal comparison, with
    ///         <c>/</c> as the separator). Every difference is a <c>resources-mismatch</c> warning, with line 0 (see
    ///         <see cref="HelpProblem"/> for the table of rule ids). The comparison covers every file below the folder,
    ///         data files of examples included, not only the cards.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     A check in a packaging or release tool, outside the unit tests of the product, that lists every difference
    ///     between the embedded cards of <c>MyLibHelp.Source</c> and the folder of a build:
    ///     <code language="csharp">
    /// using System;
    /// using DemaConsulting.HelpCards.Catalog;
    /// using DemaConsulting.HelpCards.Testing;
    ///
    /// internal static class PackageCheck
    /// {
    ///     public static int Run(string helpFolder)
    ///     {
    ///         // Empty when both sides agree; otherwise one resources-mismatch warning per file on only one side.
    ///         var differences = HelpCardValidator.CompareToDirectory(MyLibHelp.Source, helpFolder);
    ///         foreach (HelpProblem difference in differences)
    ///         {
    ///             Console.Error.WriteLine($"{difference.File}: {difference.Message}");
    ///         }
    ///
    ///         return differences.Count == 0 ? 0 : 1;
    ///     }
    /// }
    /// </code>
    /// </example>
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
