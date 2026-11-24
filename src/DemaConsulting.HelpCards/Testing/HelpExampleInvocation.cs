using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Testing;

/// <summary>
///     One command example that <see cref="HelpExampleRunner"/> asks a test to run.
/// </summary>
/// <remarks>
///     <para>
///         The runner has already split the command line with <see cref="ExampleCommandLine"/> and has already saved the
///         source examples of the card into <see cref="WorkingDirectory"/>, so the test only has to run its own entry
///         point (or start its own process) with <see cref="Arguments"/> in that folder and return the result. The
///         library never starts a process or a shell.
///     </para>
///     <para>
///         Instances are created by the runner and cannot be created by callers. The type is immutable and therefore safe
///         for concurrent use.
///     </para>
/// </remarks>
public sealed class HelpExampleInvocation
{
    /// <summary>
    ///     Creates an invocation.
    /// </summary>
    /// <param name="card">The card that owns the example.</param>
    /// <param name="example">The command example.</param>
    /// <param name="arguments">The arguments after the tool name.</param>
    /// <param name="workingDirectory">The folder that holds the saved source examples of the card.</param>
    internal HelpExampleInvocation(HelpCard card, HelpExample example, IReadOnlyList<string> arguments, string workingDirectory)
    {
        Card = card;
        Example = example;
        Arguments = arguments;
        WorkingDirectory = workingDirectory;
    }

    /// <summary>
    ///     Gets the card that owns the example.
    /// </summary>
    public HelpCard Card { get; }

    /// <summary>
    ///     Gets the command example to run.
    /// </summary>
    public HelpExample Example { get; }

    /// <summary>
    ///     Gets the arguments of the command: the words after the tool name, as <see cref="ExampleCommandLine"/> read them.
    /// </summary>
    public IReadOnlyList<string> Arguments { get; }

    /// <summary>
    ///     Gets the folder that holds the saved source examples of the card; run the command with this as its working
    ///     directory so that the files it names exist.
    /// </summary>
    public string WorkingDirectory { get; }
}
