using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Testing;
using DemaConsulting.HelpCards.Tests.TestSupport;

namespace DemaConsulting.HelpCards.Tests.Testing;

/// <summary>
///     Unit tests for <see cref="HelpExampleRunner"/> and its report, invocation, and outcome types.
/// </summary>
/// <remarks>
///     The tests use fake delegates and temporary folders and never start a process. The cards come from the embedded
///     <c>runnable</c> fixture and from in-memory sources.
/// </remarks>
public class HelpExampleRunnerTests
{
    /// <summary>
    ///     The outcome that the commands of the <c>check</c> card of the fixture state.
    /// </summary>
    /// <param name="invocation">The invocation.</param>
    /// <returns>The outcome that matches the example.</returns>
    private static HelpExampleOutcome Correct(HelpExampleInvocation invocation)
    {
        return invocation.Example.Name switch
        {
            "ok" => new HelpExampleOutcome(0, "Checked 1 file\nNo problems"),
            "missing" => new HelpExampleOutcome(2, "Cannot open absent.yaml"),
            _ => new HelpExampleOutcome(0, "Verbose output for demo"),
        };
    }

    /// <summary>
    ///     Creates a card with a source, a snippet, and a command example from in-memory files.
    /// </summary>
    /// <param name="extraExamples">More example items, each line indented under <c>examples:</c> and ending with a line feed.</param>
    /// <returns>The card <c>a</c> of a catalog whose tool is <c>demo</c>.</returns>
    private static HelpCard MixedCard(string extraExamples = "")
    {
        var items = "  - name: data\n    kind: source\n    language: yaml\n    file: data.yaml\n    content: 'k: v'\n    description: D.\n"
            + "  - name: part\n    kind: snippet\n    language: yaml\n    file: part.yaml\n    content: 'p: q'\n    description: D.\n"
            + "  - name: run\n    kind: command\n    language: shell\n    content: '{{tool}} run data.yaml'\n    description: D.\n"
            + extraExamples;
        var catalog = CatalogBuilding.Create(
            CatalogBuilding.Named("test", ("a.md", SourceReading.Card(extraFrontMatter: "examples:\n" + items))),
            builder => builder.ToolName("demo"));
        return catalog.Find("a")!;
    }

    /// <summary>
    ///     Proves that source examples are written with LF endings, one trailing line feed, no byte order mark, and folders.
    /// </summary>
    [Fact]
    public void HelpExampleRunner_Materialize_SourceExamples_WritesFilesWithTrailingLfAndFolders()
    {
        // Arrange: the check card has two file-backed sources (one saved under its folder, one under its last segment)
        // and an inline source
        using var directory = new TemporaryDirectory();
        var card = CatalogBuilding.Runnable().Find("check")!;

        // Act
        var written = HelpExampleRunner.Materialize(card, directory.FullPath);

        // Assert
        Assert.Equal(["check/config.yaml", "limits.yaml", "notes.txt"], written);
        var config = File.ReadAllBytes(Path.Combine(directory.FullPath, "check", "config.yaml"));
        Assert.Equal("---\nname: demo\nlimit: 10\n", System.Text.Encoding.UTF8.GetString(config));
        Assert.NotEqual(0xEF, config[0]);
        Assert.Equal("---\nlimit: 10\nmode: strict\n", File.ReadAllText(Path.Combine(directory.FullPath, "limits.yaml")));
        Assert.Equal("first line\nsecond line\n", File.ReadAllText(Path.Combine(directory.FullPath, "notes.txt")));
    }

    /// <summary>
    ///     Proves that a snippet and a command are not written.
    /// </summary>
    [Fact]
    public void HelpExampleRunner_Materialize_SnippetAndCommand_AreNotWritten()
    {
        // Arrange
        using var directory = new TemporaryDirectory();

        // Act
        var written = HelpExampleRunner.Materialize(MixedCard(), directory.FullPath);

        // Assert: only the source example is on disk
        Assert.Equal(["data.yaml"], written);
        Assert.Equal(["data.yaml"], Directory.GetFiles(directory.FullPath, "*", SearchOption.AllDirectories).Select(file => Path.GetFileName(file)));
    }

    /// <summary>
    ///     Proves that a later example with the same file name overwrites an earlier one.
    /// </summary>
    [Fact]
    public void HelpExampleRunner_Materialize_SameName_LastWins()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        var card = MixedCard("  - name: again\n    kind: source\n    language: yaml\n    file: data.yaml\n    content: 'k: w'\n    description: D.\n");

        // Act
        var written = HelpExampleRunner.Materialize(card, directory.FullPath);

        // Assert
        Assert.Equal(["data.yaml", "data.yaml"], written);
        Assert.Equal("k: w\n", File.ReadAllText(Path.Combine(directory.FullPath, "data.yaml")));
    }

    /// <summary>
    ///     Proves that a file name that would leave the folder is rejected before anything is written outside it.
    /// </summary>
    [Fact]
    public void HelpExampleRunner_Materialize_EscapingName_Throws()
    {
        // Arrange: a card whose example has a name that the loader would have rejected
        using var directory = new TemporaryDirectory();
        var catalog = CatalogBuilding.Runnable();
        var scope = catalog.Scope;
        var crafted = scope.Data.Find("check")! with
        {
            Examples = [new HelpExample("x", HelpExample.SourceKind, "text", "../escape.txt", "D.", "content", "runnable")],
        };
        var card = new HelpCard(crafted, scope);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => HelpExampleRunner.Materialize(card, Path.Combine(directory.FullPath, "inner")));
        Assert.False(File.Exists(Path.Combine(directory.FullPath, "escape.txt")));
    }

    /// <summary>
    ///     Proves that the commands run in the order of the card with the arguments after the tool name and the folder of the saved
    ///     files as working directory.
    /// </summary>
    [Fact]
    public void HelpExampleRunner_Run_Commands_RunInOrderWithParsedArguments()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        var card = CatalogBuilding.Runnable().Find("check")!;
        var seen = new List<string>();
        var directories = new List<string>();
        var filesPresent = new List<bool>();

        // Act
        var report = HelpExampleRunner.Run(card, directory.FullPath, invocation =>
        {
            seen.Add(string.Join("|", invocation.Arguments));
            directories.Add(invocation.WorkingDirectory);
            filesPresent.Add(File.Exists(Path.Combine(invocation.WorkingDirectory, "check", "config.yaml")));
            Assert.Same(card, invocation.Card);
            return Correct(invocation);
        }, "demo");

        // Assert
        Assert.Equal(["check|check/config.yaml", "check|absent.yaml"], seen);
        Assert.All(directories, working => Assert.Equal(Path.GetFullPath(directory.FullPath), working));
        Assert.All(filesPresent, present => Assert.True(present));
        Assert.Equal(2, report.CommandsRun);
        Assert.True(report.Succeeded);
        Assert.Empty(report.Problems);
        report.AssertSucceeded();
    }

    /// <summary>
    ///     Proves that a different exit code is reported at the card file with the example, both codes, and the output.
    /// </summary>
    [Fact]
    public void HelpExampleRunner_Run_ExitCodeMismatch_ReportsExampleExitCode()
    {
        // Arrange: the second command exits with 1 instead of the stated 2
        using var directory = new TemporaryDirectory();
        var card = CatalogBuilding.Runnable().Find("check")!;

        // Act
        var report = HelpExampleRunner.Run(
            card,
            directory.FullPath,
            invocation => invocation.Example.Name == "missing" ? new HelpExampleOutcome(1, "Cannot open absent.yaml") : Correct(invocation),
            "demo");

        // Assert
        var problem = Assert.Single(report.Problems);
        Assert.Equal((HelpSeverity.Error, RuleIds.ExampleExitCode, "runnable", "check.md", 0), (problem.Severity, problem.RuleId, problem.SourceName, problem.File, problem.Line));
        Assert.Contains("'missing'", problem.Message, StringComparison.Ordinal);
        Assert.Contains("'check'", problem.Message, StringComparison.Ordinal);
        Assert.Contains("exited with 1", problem.Message, StringComparison.Ordinal);
        Assert.Contains("'exit' is 2", problem.Message, StringComparison.Ordinal);
        Assert.Contains("Cannot open absent.yaml", problem.Message, StringComparison.Ordinal);
        Assert.False(report.Succeeded);
    }

    /// <summary>
    ///     Proves that one problem is reported for every fragment the output lacks, that CRLF in the output is accepted,
    ///     and that only the first 400 characters of the output are shown.
    /// </summary>
    [Fact]
    public void HelpExampleRunner_Run_MissingFragment_ReportsExampleOutputPerFragment()
    {
        // Arrange: the first command writes neither fragment, the second one's output has CRLF, a fragment is missing from the third
        using var directory = new TemporaryDirectory();
        var card = CatalogBuilding.Runnable().Find("check")!;
        var longOutput = new string('x', 1000);

        // Act
        var none = HelpExampleRunner.Run(
            card,
            directory.FullPath,
            invocation => invocation.Example.Name == "ok" ? new HelpExampleOutcome(0, longOutput) : Correct(invocation),
            "demo");
        var crlf = HelpExampleRunner.Run(
            card,
            directory.FullPath,
            invocation => invocation.Example.Name == "ok" ? new HelpExampleOutcome(0, "Checked 1 file\r\nNo problems\r\n") : Correct(invocation),
            "demo");
        var one = HelpExampleRunner.Run(
            card,
            directory.FullPath,
            invocation => invocation.Example.Name == "ok" ? new HelpExampleOutcome(0, "checked 1 file\nNo problems") : Correct(invocation),
            "demo");

        // Assert: two problems for the two fragments, with the excerpt cut; CRLF passes; the comparison is case-sensitive
        Assert.Equal([RuleIds.ExampleOutput, RuleIds.ExampleOutput], none.Problems.Select(problem => problem.RuleId));
        Assert.Contains("'Checked 1 file'", none.Problems[0].Message, StringComparison.Ordinal);
        Assert.Contains("'No problems'", none.Problems[1].Message, StringComparison.Ordinal);
        Assert.Contains(new string('x', 400) + "...", none.Problems[0].Message, StringComparison.Ordinal);
        Assert.DoesNotContain(new string('x', 401), none.Problems[0].Message, StringComparison.Ordinal);
        Assert.True(crlf.Succeeded);
        var lowerCase = Assert.Single(one.Problems);
        Assert.Equal(RuleIds.ExampleOutput, lowerCase.RuleId);
        Assert.Contains("'Checked 1 file'", lowerCase.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that a command that cannot be read is reported and not run, and that the run goes on with the next one.
    /// </summary>
    [Fact]
    public void HelpExampleRunner_Run_UnreadableCommand_ReportsExampleParseAndContinues()
    {
        // Arrange: a command with a pipe, then a valid command
        using var directory = new TemporaryDirectory();
        var card = MixedCard("  - name: piped\n    kind: command\n    language: shell\n    content: '{{tool}} run | more'\n    description: D.\n"
            + "  - name: last\n    kind: command\n    language: shell\n    content: '{{tool}} last'\n    description: D.\n");
        var seen = new List<string>();

        // Act
        var report = HelpExampleRunner.Run(card, directory.FullPath, invocation =>
        {
            seen.Add(invocation.Example.Name);
            return new HelpExampleOutcome(0, string.Empty);
        }, "demo");

        // Assert
        Assert.Equal(["run", "last"], seen);
        Assert.Equal(2, report.CommandsRun);
        var problem = Assert.Single(report.Problems);
        Assert.Equal((RuleIds.ExampleParse, "a.md", 0), (problem.RuleId, problem.File, problem.Line));
        Assert.Contains("'piped'", problem.Message, StringComparison.Ordinal);
        Assert.Contains("'|'", problem.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Proves that the catalog overload uses a fresh folder for every card with commands, saves the files there, runs
    ///     the commands of every card, and deletes the folders.
    /// </summary>
    [Fact]
    public void HelpExampleRunner_Run_Catalog_UsesFreshFolderPerCardAndDeletesIt()
    {
        // Arrange
        var catalog = CatalogBuilding.Runnable();
        var folders = new Dictionary<string, string>();
        var filesSeen = new List<bool>();

        // Act
        var report = HelpExampleRunner.Run(catalog, invocation =>
        {
            folders[invocation.Example.Name] = invocation.WorkingDirectory;
            filesSeen.Add(File.Exists(Path.Combine(invocation.WorkingDirectory, "check", "config.yaml")));
            return Correct(invocation);
        });

        // Assert: three commands in two cards; the cards have different folders; the files of the card were saved; all gone
        Assert.Equal(3, report.CommandsRun);
        Assert.True(report.Succeeded);
        Assert.Equal(folders["ok"], folders["missing"]);
        Assert.NotEqual(folders["ok"], folders["verbose"]);
        Assert.Equal([true, true, false], filesSeen);
        Assert.All(folders.Values, folder => Assert.False(Directory.Exists(folder)));
        Assert.All(folders.Values, folder => Assert.StartsWith(Path.GetFullPath(Path.GetTempPath()), folder, StringComparison.Ordinal));
    }

    /// <summary>
    ///     Proves that an exception of the delegate deletes the scratch folder and propagates.
    /// </summary>
    [Fact]
    public void HelpExampleRunner_Run_DelegateThrows_CleansUpAndPropagates()
    {
        // Arrange
        var catalog = CatalogBuilding.Runnable();
        string? folder = null;

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => HelpExampleRunner.Run(catalog, invocation =>
        {
            folder = invocation.WorkingDirectory;
            throw new InvalidOperationException("boom");
        }));

        // Assert
        Assert.Equal("boom", exception.Message);
        Assert.NotNull(folder);
        Assert.False(Directory.Exists(folder));
    }

    /// <summary>
    ///     Proves that the tool name defaults to the tool name of the catalog, then to the first source, and that an explicit
    ///     tool name wins.
    /// </summary>
    [Fact]
    public void HelpExampleRunner_Run_Catalog_ToolNameDefaultsToCatalogTool()
    {
        // Arrange: the fixture catalog names its tool demo; without a tool name the first source (runnable) is the tool
        var named = CatalogBuilding.Runnable();
        var unnamed = CatalogBuilding.Create(CatalogBuilding.Fixture("runnable"));
        static HelpExampleOutcome Fine(HelpExampleInvocation invocation) => Correct(invocation);

        // Act
        var byCatalog = HelpExampleRunner.Run(named, Fine);
        var bySource = HelpExampleRunner.Run(unnamed, Fine);
        var explicitName = HelpExampleRunner.Run(named, Fine, "other");

        // Assert: tokens are replaced with the tool name, so the commands parse only for the same tool name; for the
        // second catalog the only difference is the tool name that the fake output cannot know
        Assert.True(byCatalog.Succeeded);
        Assert.Equal(3, byCatalog.CommandsRun);
        Assert.Equal(3, bySource.CommandsRun);
        Assert.Equal(RuleIds.ExampleOutput, Assert.Single(bySource.Problems).RuleId);
        Assert.Equal(3, explicitName.Problems.Count(problem => problem.RuleId == RuleIds.ExampleParse));
        Assert.Equal(0, explicitName.CommandsRun);
    }

    /// <summary>
    ///     Proves that a failed report throws the exception of the catalog with its problems and that a successful one does not.
    /// </summary>
    [Fact]
    public void HelpExampleRunner_Report_AssertSucceeded_ThrowsWithProblems()
    {
        // Arrange
        var catalog = CatalogBuilding.Runnable();
        var failing = HelpExampleRunner.Run(catalog, _ => new HelpExampleOutcome(9, string.Empty));
        var passing = HelpExampleRunner.Run(catalog, Correct);

        // Act
        var exception = Assert.Throws<HelpCatalogException>(failing.AssertSucceeded);
        passing.AssertSucceeded();

        // Assert
        Assert.Equal(failing.Problems, exception.Problems);
        Assert.NotEmpty(failing.Problems);
        Assert.All(failing.Problems, problem => Assert.Equal(HelpSeverity.Error, problem.Severity));
        Assert.True(passing.Succeeded);
    }

    /// <summary>
    ///     Proves that a catalog without a command example runs nothing and succeeds.
    /// </summary>
    [Fact]
    public void HelpExampleRunner_Run_Catalog_WithoutCommands_RunsNothing()
    {
        // Arrange
        var catalog = CatalogBuilding.FromIds("a", "a/b");

        // Act
        var report = HelpExampleRunner.Run(catalog, _ => throw new InvalidOperationException("must not run"));

        // Assert
        Assert.Equal(0, report.CommandsRun);
        Assert.True(report.Succeeded);
    }

    /// <summary>
    ///     Creates a catalog whose only card has a command example and no source example.
    /// </summary>
    /// <param name="expect">The <c>expect</c> line of the command, or an empty text for none.</param>
    /// <returns>The catalog, whose tool is <c>demo</c> and which holds the card <c>a</c>.</returns>
    private static HelpCatalog CommandOnlyCatalog(string expect = "")
    {
        var items = "  - name: run\n    kind: command\n    language: shell\n    content: '{{tool}} run'\n"
            + expect
            + "    description: D.\n";
        return CatalogBuilding.Create(
            CatalogBuilding.Named("test", ("a.md", SourceReading.Card(extraFrontMatter: "examples:\n" + items))),
            builder => builder.ToolName("demo"));
    }

    /// <summary>
    ///     Creates a card that has a command example and no source example.
    /// </summary>
    /// <param name="expect">The <c>expect</c> line of the command, or an empty text for none.</param>
    /// <returns>The card <c>a</c>.</returns>
    private static HelpCard CommandOnlyCard(string expect = "")
    {
        return CommandOnlyCatalog(expect).Find("a")!;
    }

    /// <summary>
    ///     Proves that the folder is created even when the card has no source example to write.
    /// </summary>
    [Fact]
    public void HelpExampleRunner_Materialize_CommandOnlyCard_CreatesTheFolder()
    {
        // Arrange: a folder that does not exist yet
        using var directory = new TemporaryDirectory();
        var target = Path.Combine(directory.FullPath, "new", "work");

        // Act
        var written = HelpExampleRunner.Materialize(CommandOnlyCard(), target);

        // Assert
        Assert.Empty(written);
        Assert.True(Directory.Exists(target));
        Assert.Empty(Directory.GetFileSystemEntries(target));
    }

    /// <summary>
    ///     Proves that the working directory of a command exists when the card has no source example, for both overloads.
    /// </summary>
    [Fact]
    public void HelpExampleRunner_Run_CommandOnlyCard_WorkingDirectoryExists()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        var target = Path.Combine(directory.FullPath, "new");
        var existed = new List<bool>();
        HelpExampleOutcome Execute(HelpExampleInvocation invocation)
        {
            existed.Add(Directory.Exists(invocation.WorkingDirectory));
            return new HelpExampleOutcome(0, string.Empty);
        }

        // Act
        var cardReport = HelpExampleRunner.Run(CommandOnlyCard(), target, Execute, "demo");
        var catalogReport = HelpExampleRunner.Run(CommandOnlyCatalog(), Execute);

        // Assert
        Assert.Equal([true, true], existed);
        Assert.True(cardReport.Succeeded);
        Assert.True(catalogReport.Succeeded);
    }

    /// <summary>
    ///     Proves that a null output is treated like an empty text: no exception, no problem without an <c>expect</c>
    ///     fragment, and one problem per fragment with one.
    /// </summary>
    [Fact]
    public void HelpExampleRunner_Run_NullOutput_IsTreatedAsEmpty()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        Func<HelpExampleInvocation, HelpExampleOutcome> execute = _ => new HelpExampleOutcome(0, null);

        // Act
        var without = HelpExampleRunner.Run(CommandOnlyCard(), directory.FullPath, execute, "demo");
        var with = HelpExampleRunner.Run(CommandOnlyCard("    expect: done\n"), directory.FullPath, execute, "demo");

        // Assert
        Assert.True(without.Succeeded);
        var problem = Assert.Single(with.Problems);
        Assert.Equal(RuleIds.ExampleOutput, problem.RuleId);
        Assert.Null(new HelpExampleOutcome(0, null).Output);
    }

    /// <summary>
    ///     Proves that null and blank arguments are rejected at the call.
    /// </summary>
    [Fact]
    public void HelpExampleRunner_InvalidArguments_Throw()
    {
        // Arrange
        var catalog = CatalogBuilding.Runnable();
        var card = catalog.Find("check")!;
        Func<HelpExampleInvocation, HelpExampleOutcome> execute = Correct;

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => HelpExampleRunner.Materialize(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => HelpExampleRunner.Materialize(card, null!));
        Assert.Throws<ArgumentException>(() => HelpExampleRunner.Materialize(card, " "));
        Assert.Throws<ArgumentNullException>(() => HelpExampleRunner.Run(null!, "x", execute, "demo"));
        Assert.Throws<ArgumentNullException>(() => HelpExampleRunner.Run(card, null!, execute, "demo"));
        Assert.Throws<ArgumentException>(() => HelpExampleRunner.Run(card, "x", execute, " "));
        Assert.Throws<ArgumentNullException>(() => HelpExampleRunner.Run(card, "x", null!, "demo"));
        Assert.Throws<ArgumentNullException>(() => HelpExampleRunner.Run(card, "x", execute, null!));
        Assert.Throws<ArgumentNullException>(() => HelpExampleRunner.Run((HelpCatalog)null!, execute));
        Assert.Throws<ArgumentNullException>(() => HelpExampleRunner.Run(catalog, null!));
        Assert.Throws<ArgumentException>(() => HelpExampleRunner.Run(catalog, execute, ""));
    }

    /// <summary>
    ///     Proves that the outcome compares by value and treats a null output like an empty text.
    /// </summary>
    [Fact]
    public void HelpExampleOutcome_Equality_ComparesByValue()
    {
        // Arrange / Act
        var left = new HelpExampleOutcome(1, "a");
        var right = new HelpExampleOutcome(1, "a");
        var report = HelpExampleRunner.Run(
            CatalogBuilding.Runnable(),
            _ => new HelpExampleOutcome(0, null));

        // Assert: a null output lacks every fragment but does not throw
        Assert.Equal(left, right);
        Assert.NotEqual(left, new HelpExampleOutcome(2, "a"));
        Assert.Contains(report.Problems, problem => problem.RuleId == RuleIds.ExampleOutput);
    }
}
