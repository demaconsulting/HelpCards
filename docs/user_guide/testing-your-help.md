# Testing Your Help

HelpCards ships helpers so that every library and tool can prove in its own tests that its cards are valid, that its
examples run and give the results they state, and that its documentation matches its cards. The helpers live in the
`DemaConsulting.HelpCards.Testing` namespace and do not depend on a test framework.

## Validating Cards

`HelpCardValidator.AssertValid` builds the catalog and throws a `HelpCatalogException` that lists the problems it finds
when any card is defective (the checks run in stages, so fixing the listed errors can reveal more; see *Composing
Sources*). A single test covers all of your cards:

```csharp
using DemaConsulting.HelpCards.Testing;
using Xunit;

public class HelpTests
{
    [Fact]
    public void Help_Cards_AreValid()
    {
        HelpCardValidator.AssertValid(MyLibHelp.Source);
    }
}
```

For a command-line tool, pass `options.GetCatalogConfiguration()` of its `HelpCliOptions`: it is the composition that
the helper builds (see *Command-Line Integration*), so the test validates what users get.

The validator has three groups of overloads, each with a `Validate` method that returns the problems and an
`AssertValid` method that throws:

| Input | Builds |
| --- | --- |
| A `HelpSource` | A flat catalog of the source and, in dependency order, every source it imports |
| An `Action<HelpCatalogBuilder>` | Exactly the composition that you describe, for example the one of the application |
| A `HelpCatalog` | Nothing: the catalog already exists, and the quality rules and the external-source check run on it |

`Validate` returns the problems: the errors that building the catalog produces first (found in stages, see *Composing
Sources*), and then the warnings of the quality rules, which appear only when there is no error. It never throws for a
defect of a card. `AssertValid` fails on errors, and also on warnings when `WarningsAreErrors` is set.

## Options and Quality Rules

`HelpValidationOptions` sets the limits of the quality rules and adds your own rules.

| Option | Default | Meaning |
| --- | --- | --- |
| `MaxCardChars` | 6000 | The longest rendered text of a card |
| `MaxRootIndexChars` | 3500 | The longest rendered text of the index of the roots |
| `MaxSummaryChars` | 100 | The longest summary |
| `MaxRootSummaryChars` | 80 | The longest summary of a root card (two lines of the topic index) |
| `MaxLineLength` | Off | The longest line of a body (inside fences too, unless `MaxFenceLineLength` is set) |
| `MaxFenceLineLength` | Off | The longest line in a fenced block; alone, only fenced lines are checked |
| `Rules` | None | Your own `IHelpCardRule` objects |
| `RequireExternalSources` | `false` | Turns each absent external source into the error `external-source-missing` |
| `WarningsAreErrors` | `false` | Makes warnings fail `AssertValid` |

The quality rules, each reported as a warning with the rule id, are:

| Rule id | Meaning |
| --- | --- |
| `summary-length` | A summary that is longer than the limit or is not one line |
| `card-size` | A card whose rendered text is longer than the limit |
| `heading-reserved` | A body that has a first-level heading or one of the generated headings |
| `fence-language` | A fenced block without a language |
| `fence-unclosed` | A fenced block that never ends |
| `unused-token` | A token that is defined but never used |
| `line-length` | A body line over `MaxLineLength`, or a fenced line over `MaxFenceLineLength` |
| `command-parse` | A `command` example that `ExampleCommandLine` cannot parse; names the character and column |
| `save-as-duplicate` | Two `source` examples of one card that are saved under the same name |
| `resources-mismatch` | A file that exists on disk or in the assembly but not on the other side |

Code and command samples are usually wider than prose, so the two limits can differ:

```csharp
var options = new HelpValidationOptions { MaxLineLength = 80, MaxFenceLineLength = 120 };
```

With these options prose may have 80 characters and the lines of fenced code 120.

A domain rule implements `IHelpCardRule`, which has one method that receives a `HelpCard` and returns problems. A
library whose cards must not contain command-line text can check that with a rule, and put it in the options:

```csharp
var options = new HelpValidationOptions { Rules = [new NoCommandLineTextRule()], WarningsAreErrors = true };
HelpCardValidator.AssertValid(MyLibHelp.Source, options);
```

### Embedded Files and the Folder on Disk

The packaged build targets embed every file below the `Help` folder, so `AssertValid(MyLibHelp.Source)` and the example
runner already prove what the product ships. Do not add a unit test that compares the embedded files with the checked-in
`Help` folder of your repository: the testing standard of DEMA Consulting does not allow a requirement-linked test to
assert on checked-in source files, because such a test depends on where the repository is on the build machine and not
on what the product does.

`HelpCardValidator.CompareToDirectory(source, directory)` compares the embedded resources of an assembly source with
the files of a folder and reports the differences as `resources-mismatch`. It stays available for a check outside the
unit tests, such as a packaging or release step that compares the package with a folder it produced. It is not marked
obsolete, but the unit-test use is discouraged and the method may be deprecated in a later release.

## Running Examples

A `command` example can state the result a correct run gives, with the keys `exit` (the exit code, 0 when absent) and
`expect` (text the output must contain; see *Writing Cards*). `HelpExampleRunner` proves those statements in one test.
It saves the `source` examples of each card to a scratch folder, so a command can name them, splits every command with
`ExampleCommandLine`, and calls a delegate of your test to run it. Your delegate decides how: usually it calls the entry
point of your tool in the same process, or it starts your tool as a process. HelpCards never starts a process or a shell
itself. The delegate returns the exit code and the text the command wrote, and the runner compares them with the
example.

```csharp
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Testing;
using Xunit;

public class HelpExampleTests
{
    [Fact]
    public void Help_CommandExamples_GiveTheStatedResult()
    {
        HelpCatalog catalog = HelpCatalog.Create(builder => builder
            .AddSource(MyLibHelp.Source)
            .ToolName("mytool"));

        HelpExampleRunReport report = HelpExampleRunner.Run(catalog, invocation =>
        {
            // invocation.Arguments are the words after the tool name; invocation.WorkingDirectory holds the
            // saved source examples of the card, so run the command there.
            var output = new StringWriter();
            int exitCode = MyTool.Run(invocation.Arguments.ToArray(), invocation.WorkingDirectory, output);
            return new HelpExampleOutcome(exitCode, output.ToString());
        });

        Assert.True(report.CommandsRun > 0);
        report.AssertSucceeded();
    }
}
```

`Run(catalog, execute, toolName)` takes the cards in catalog order and gives each card with commands a fresh folder
below the temporary folder, which it deletes afterwards. `Run(card, directory, execute, toolName)` runs one card in a
folder of your choice, and `Materialize(card, directory)` only saves the files (LF line endings, one trailing line
feed, UTF-8 without a byte order mark; a snippet is a fragment and is not saved). Every difference is a problem of the
report and `AssertSucceeded` fails with all of them:

| Rule id | Meaning |
| --- | --- |
| `example-parse` | The command cannot be split into arguments, so it was not run (the run goes on) |
| `example-exit-code` | The command ended with another exit code than the example states |
| `example-output` | The output lacks a fragment of the example's `expect`; one problem for each missing fragment |

Each message names the example and the card and shows the first 400 characters of the output. An exception of your
delegate propagates after the scratch folder is deleted. Do not show the output files of your tool as examples; state
the result of the command with `exit` and `expect` instead.

## Listing Examples

`HelpExamples.Enumerate(catalog, kinds)` returns every example of a catalog together with its card, optionally limited
to some kinds, when you need to treat the examples yourself, for example to compile each `source` example:

```csharp
foreach ((HelpCard card, HelpExample example) in HelpExamples.Enumerate(catalog, [HelpExample.SourceKind]))
{
    Compile(card.Id, example.FileName!, example.Content);
}
```

## Reading Command Lines

`ExampleCommandLine.TryParse(content, toolName, out arguments, out error)` parses one command line with a restricted
grammar. The first word must be the tool name, and the remaining words are the arguments. A word is made of unquoted
characters, single-quoted text without escapes, and double-quoted text in which only a backslash and a quote can be
escaped. Unquoted characters exclude the space, the quotes, and ``| & ; < > ( ) $ ` * ? [ ] { } ~ ! # \ %``. Pipes,
redirections, variables, wildcards, comments, and line continuations are therefore rejected with a message that names
the character and the column, for example `shell operator '|' is not allowed in an example command (column 10)` for
`tool run | more`. The
`command-parse` warning of the validator wraps this message with the example and the card, and ends it with a period.
For an example `load` of the card `getting-started` whose command is `tool load {a}`, the whole message reads:

```text
The command example 'load' of 'getting-started' cannot be run as written: shell expansion character '{' is not allowed in an example command (column 11); quote it with single quotes if it is meant literally.
```

## Testing Documentation Against Cards

`HelpCardMarkdown` reads the structure of a card body, so a test can prove that a user guide and the cards say the same
thing:

| Method | Returns |
| --- | --- |
| `Headings(body)` | The headings of the body in order |
| `GetSection(body, heading)` | The text below a heading, or `null` |
| `Bullets(body, heading)` | The bullet items below a heading |

```csharp
HelpCard card = Catalog.Find("options")!;
IReadOnlyList<string> bullets = HelpCardMarkdown.Bullets(card.Body, "Options");
Assert.Contains(bullets, bullet => bullet.StartsWith("--output", StringComparison.Ordinal));
```
