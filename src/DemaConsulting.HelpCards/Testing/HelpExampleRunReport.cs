using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Testing;

/// <summary>
///     The result of running the command examples of a card or a catalog: how many commands ran and what was wrong.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Problems"/> holds one error for every command that could not be read (<c>example-parse</c>), whose
///         exit code differed from the <c>exit</c> of the example (<c>example-exit-code</c>), or whose output lacked a
///         fragment of the <c>expect</c> of the example (<c>example-output</c>, one problem per missing fragment). The
///         problems are in the order the examples ran. Use <see cref="AssertSucceeded"/> in a test to fail with all of them.
///     </para>
///     <para>
///         Instances are created by the runner and cannot be created by callers. The type is immutable and therefore safe
///         for concurrent use.
///     </para>
/// </remarks>
public sealed class HelpExampleRunReport
{
    /// <summary>
    ///     Creates a report.
    /// </summary>
    /// <param name="commandsRun">The number of commands that were run.</param>
    /// <param name="problems">The problems, in the order the examples ran.</param>
    internal HelpExampleRunReport(int commandsRun, IReadOnlyList<HelpProblem> problems)
    {
        CommandsRun = commandsRun;
        Problems = Array.AsReadOnly(problems.ToArray());
    }

    /// <summary>
    ///     Gets the number of commands that were run through the delegate; a command that could not be read is not counted.
    /// </summary>
    public int CommandsRun { get; }

    /// <summary>
    ///     Gets every error that the run found, in the order the examples ran.
    /// </summary>
    public IReadOnlyList<HelpProblem> Problems { get; }

    /// <summary>
    ///     Gets a value that tells whether the run found no problem.
    /// </summary>
    public bool Succeeded => Problems.Count == 0;

    /// <summary>
    ///     Throws when the run found a problem.
    /// </summary>
    /// <exception cref="HelpCatalogException">Thrown when <see cref="Succeeded"/> is <see langword="false"/>; its problems are the problems of the report.</exception>
    public void AssertSucceeded()
    {
        if (!Succeeded)
        {
            throw new HelpCatalogException(Problems);
        }
    }
}
