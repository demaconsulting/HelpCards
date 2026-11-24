using System.Text;
using DemaConsulting.HelpCards.Catalog;

namespace DemaConsulting.HelpCards.Testing;

/// <summary>
///     Saves the source examples of a card to a folder and runs its command examples through a delegate of your test, then
///     compares each result with the exit code and the output that the example states.
/// </summary>
/// <remarks>
///     <para>
///         This is how a product proves that every command in its help works. HelpCards never starts a process or a
///         shell: for each <c>command</c> example the runner splits the line with <see cref="ExampleCommandLine"/> and calls
///         your delegate with a <see cref="HelpExampleInvocation"/>; your delegate runs the command, with your own entry
///         point or your own process, and returns a <see cref="HelpExampleOutcome"/>. The runner then compares the exit
///         code with the <c>exit</c> key of the example (0 when absent) and checks that the output contains every fragment
///         of its <c>expect</c> key, comparing case-sensitively after CRLF becomes LF.
///     </para>
///     <para>
///         Before a card runs, every <c>source</c> example of the card is saved to the working directory under its
///         <see cref="HelpExample.FileName"/> (LF line endings, one trailing line feed, UTF-8 without a byte order mark,
///         folders created), so a command can name the files the card shows. A later example with the same name
///         overwrites an earlier one. A <c>snippet</c> is a fragment that is never claimed to work alone, so it is not
///         saved.
///     </para>
///     <para>
///         Every difference is an error problem in the report: <c>example-parse</c> (the command could not be read; it is not
///         run and the run goes on), <c>example-exit-code</c>, and <c>example-output</c> (one problem for each missing
///         fragment). A problem names the example and the card and shows the first 400 characters of the output. An
///         exception of your delegate is not caught: the scratch folder is deleted and the exception propagates. The class
///         is stateless and safe for concurrent use; the delegate runs on the calling thread, one command after another.
///     </para>
/// </remarks>
/// <example>
///     A test that runs every command example of a catalog. The delegate stands in for the entry point of a tool
///     <c>mytool</c>: it succeeds for a file that the runner saved from a source example and fails otherwise. A real test
///     calls its own <c>Main</c> or starts its own process here:
///     <code language="csharp">
/// using System;
/// using System.IO;
/// using DemaConsulting.HelpCards.Catalog;
/// using DemaConsulting.HelpCards.Loading;
/// using DemaConsulting.HelpCards.Testing;
///
/// HelpSource cards = HelpSource.FromFiles(new System.Collections.Generic.Dictionary&lt;string, string&gt;
/// {
///     ["_source.md"] = "---\nname: mytool\nschema: 2\n---\n",
///     ["check.md"] = "---\n"
///         + "title: Check\n"
///         + "summary: Check a file.\n"
///         + "examples:\n"
///         + "  - name: config\n"
///         + "    kind: source\n"
///         + "    language: yaml\n"
///         + "    content: 'limit: 10'\n"
///         + "    save-as: config.yaml\n"
///         + "    description: A configuration file.\n"
///         + "  - name: ok\n"
///         + "    kind: command\n"
///         + "    language: shell\n"
///         + "    content: '{{tool}} check config.yaml'\n"
///         + "    expect: 'Checked config.yaml'\n"
///         + "    description: Check the file.\n"
///         + "  - name: missing\n"
///         + "    kind: command\n"
///         + "    language: shell\n"
///         + "    content: '{{tool}} check absent.yaml'\n"
///         + "    exit: 2\n"
///         + "    expect: 'Cannot open absent.yaml'\n"
///         + "    description: Check a file that does not exist.\n"
///         + "---\n"
///         + "Checks a configuration file.\n",
/// }, name: "mytool");
///
/// HelpCatalog catalog = HelpCatalog.Create(builder => builder.AddSource(cards).ToolName("mytool"));
///
/// HelpExampleRunReport report = HelpExampleRunner.Run(catalog, invocation =>
/// {
///     // A stand-in for the entry point of the tool: it succeeds for a file that exists and fails otherwise.
///     string file = invocation.Arguments[1];
///     bool exists = File.Exists(Path.Combine(invocation.WorkingDirectory, file));
///     return new HelpExampleOutcome(
///         exists ? 0 : 2,
///         exists ? $"Checked {file}" : $"Cannot open {file}");
/// });
///
/// report.AssertSucceeded();
/// Console.WriteLine($"{report.CommandsRun} commands ran, succeeded: {report.Succeeded}");
/// </code>
///     The output:
///     <code language="text">
/// 2 commands ran, succeeded: True
/// </code>
/// </example>
public static class HelpExampleRunner
{
    /// <summary>
    ///     The number of characters of the output that a problem shows.
    /// </summary>
    private const int OutputExcerptChars = 400;

    /// <summary>
    ///     Saves the source examples of a card into a folder.
    /// </summary>
    /// <remarks>
    ///     Every example of the kind <see cref="HelpExample.SourceKind"/> is written to the folder under its file name
    ///     (<c>/</c> separates folders, which are created) with LF line endings, one trailing line feed, and UTF-8 without a
    ///     byte order mark. An example with the same file name as an earlier one overwrites it. Snippets and commands are not
    ///     written. The call checks that every target stays inside the folder, so a file name can never leave it.
    /// </remarks>
    /// <param name="card">The card. Must not be null.</param>
    /// <param name="directory">The folder to write to; it is created when it does not exist. Must not be null or white space.</param>
    /// <returns>The file name of every source example that was written, in the order of the examples.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="card"/> or <paramref name="directory"/> is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="directory"/> is empty or white space, or when a file name would leave the folder.
    /// </exception>
    public static IReadOnlyList<string> Materialize(HelpCard card, string directory)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        var root = Path.GetFullPath(directory);
        var written = new List<string>();
        foreach (var example in card.Examples.Where(example => example.Kind == HelpExample.SourceKind && example.FileName is not null))
        {
            var name = example.FileName!;
            var path = Path.GetFullPath(Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar)));
            var relative = Path.GetRelativePath(root, path);
            if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            {
                throw new ArgumentException($"The example file name '{name}' of the card '{card.Id}' leaves the folder.", nameof(card));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, example.Content.TrimEnd('\n') + "\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            written.Add(name);
        }

        return written.AsReadOnly();
    }

    /// <summary>
    ///     Saves the source examples of a card to a folder, then runs its command examples in order and compares each result
    ///     with the exit code and the output the example states.
    /// </summary>
    /// <param name="card">The card. Must not be null.</param>
    /// <param name="directory">The folder for the saved source examples and the working directory of the commands. Must not be null or white space.</param>
    /// <param name="execute">Runs one command and returns its result. Must not be null; an exception it throws propagates.</param>
    /// <param name="toolName">The name every command starts with. Must not be null, empty, or white space.</param>
    /// <returns>The report of the run.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="directory"/> or <paramref name="toolName"/> is empty or white space.</exception>
    public static HelpExampleRunReport Run(
        HelpCard card,
        string directory,
        Func<HelpExampleInvocation, HelpExampleOutcome> execute,
        string toolName)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(execute);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        var problems = new List<HelpProblem>();
        var commandsRun = RunCard(card, directory, execute, toolName, problems);
        return new HelpExampleRunReport(commandsRun, problems);
    }

    /// <summary>
    ///     Runs the command examples of every visible card that has one, each card in a fresh scratch folder.
    /// </summary>
    /// <remarks>
    ///     The cards are taken in catalog order. For each card with at least one command example the runner creates a new
    ///     folder below <see cref="Path.GetTempPath"/>, saves the source examples of the card there, runs the commands, and
    ///     deletes the folder again (also when the delegate throws; a folder that cannot be deleted is left behind).
    /// </remarks>
    /// <param name="catalog">The catalog or view. Must not be null.</param>
    /// <param name="execute">Runs one command and returns its result. Must not be null; an exception it throws propagates.</param>
    /// <param name="toolName">
    ///     The name every command starts with, or <see langword="null"/> for the tool name of the catalog: the name set on
    ///     the builder, else the name of the first source.
    /// </param>
    /// <returns>The report of the run.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="catalog"/> or <paramref name="execute"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="toolName"/> is empty or white space.</exception>
    public static HelpExampleRunReport Run(
        HelpCatalog catalog,
        Func<HelpExampleInvocation, HelpExampleOutcome> execute,
        string? toolName = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(execute);
        if (toolName is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        }

        var tool = toolName ?? catalog.Scope.Data.EffectiveToolName;
        var problems = new List<HelpProblem>();
        var commandsRun = 0;
        foreach (var card in catalog.Cards.Where(card => card.Examples.Any(example => example.Kind == HelpExample.CommandKind)))
        {
            var scratch = Path.Combine(Path.GetTempPath(), "help-cards-" + Guid.NewGuid().ToString("N"));
            try
            {
                commandsRun += RunCard(card, scratch, execute, tool, problems);
            }
            finally
            {
                DeleteQuietly(scratch);
            }
        }

        return new HelpExampleRunReport(commandsRun, problems);
    }

    /// <summary>
    ///     Saves the source examples of one card and runs its command examples.
    /// </summary>
    /// <param name="card">The card.</param>
    /// <param name="directory">The working directory.</param>
    /// <param name="execute">The delegate that runs a command.</param>
    /// <param name="toolName">The tool name every command starts with.</param>
    /// <param name="problems">Receives the problems.</param>
    /// <returns>The number of commands that were run.</returns>
    private static int RunCard(
        HelpCard card,
        string directory,
        Func<HelpExampleInvocation, HelpExampleOutcome> execute,
        string toolName,
        List<HelpProblem> problems)
    {
        Materialize(card, directory);
        var commandsRun = 0;
        foreach (var example in card.Examples.Where(example => example.Kind == HelpExample.CommandKind))
        {
            if (!ExampleCommandLine.TryParse(example.Content, toolName, out var arguments, out var error))
            {
                problems.Add(CreateProblem(
                    RuleIds.ExampleParse,
                    card,
                    $"The command example '{example.Name}' of '{card.Id}' cannot be run as written: {error}."));
                continue;
            }

            var outcome = execute(new HelpExampleInvocation(card, example, arguments, Path.GetFullPath(directory)));
            commandsRun++;
            Compare(card, example, outcome, problems);
        }

        return commandsRun;
    }

    /// <summary>
    ///     Compares the result of a command with the result the example states.
    /// </summary>
    /// <param name="card">The card that owns the example.</param>
    /// <param name="example">The example.</param>
    /// <param name="outcome">The result of the run.</param>
    /// <param name="problems">Receives a problem for the exit code and one for every missing fragment.</param>
    private static void Compare(HelpCard card, HelpExample example, HelpExampleOutcome outcome, List<HelpProblem> problems)
    {
        var output = (outcome.Output ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal);
        var excerpt = output.Length <= OutputExcerptChars ? output : output[..OutputExcerptChars] + "...";
        if (outcome.ExitCode != example.ExitCode)
        {
            problems.Add(CreateProblem(
                RuleIds.ExampleExitCode,
                card,
                $"The command example '{example.Name}' of '{card.Id}' exited with {outcome.ExitCode} but its 'exit' is "
                + $"{example.ExitCode}. Output (first {OutputExcerptChars} characters): {excerpt}"));
        }

        foreach (var fragment in example.Expect.Where(fragment => !output.Contains(fragment, StringComparison.Ordinal)))
        {
            problems.Add(CreateProblem(
                RuleIds.ExampleOutput,
                card,
                $"The output of the command example '{example.Name}' of '{card.Id}' does not contain '{fragment}'. "
                + $"Output (first {OutputExcerptChars} characters): {excerpt}"));
        }
    }

    /// <summary>
    ///     Creates an error problem for a card, at the card file and without a line.
    /// </summary>
    /// <param name="ruleId">The rule.</param>
    /// <param name="card">The card.</param>
    /// <param name="message">The message.</param>
    /// <returns>The problem.</returns>
    private static HelpProblem CreateProblem(string ruleId, HelpCard card, string message)
    {
        return HelpProblem.CreateError(ruleId, card.SourceName, card.File, 0, message);
    }

    /// <summary>
    ///     Deletes a scratch folder and ignores a failure to do so.
    /// </summary>
    /// <param name="directory">The folder.</param>
    private static void DeleteQuietly(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException)
        {
            // A folder that is locked by an anti-virus scanner or an open handle is left behind
        }
        catch (UnauthorizedAccessException)
        {
            // Same: cleanup is best effort and never hides the result of the run
        }
    }
}
