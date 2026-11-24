# Testing Your Help

HelpCards ships helpers so that every library and tool can prove in its own tests that its cards are valid, that its
examples parse, and that its documentation matches its cards. The helpers live in the `DemaConsulting.HelpCards.Testing`
namespace and do not depend on a test framework.

## Validating Cards

`HelpCardValidator.AssertValid` builds the catalog and throws a `HelpCatalogException` that lists every problem when
any card is defective. A single test covers all of your cards:

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

The validator has three groups of overloads, each with a `Validate` method that returns the problems and an
`AssertValid` method that throws:

| Input | Builds |
| --- | --- |
| A `HelpSource` | A flat catalog of the source and, in dependency order, every source it imports |
| An `Action<HelpCatalogBuilder>` | Exactly the composition that you describe, for example the one of the application |
| A `HelpCatalog` | Nothing: the catalog already exists, and the quality rules and the external-source check run on it |

`Validate` returns every problem: the errors that building the catalog produces first, and then the warnings of the
quality rules. It never throws for a defect of a card. `AssertValid` fails on errors, and also on warnings when
`WarningsAreErrors` is set.

## Options and Quality Rules

`HelpValidationOptions` sets the limits of the quality rules and adds your own rules.

| Option | Default | Meaning |
| --- | --- | --- |
| `MaxCardChars` | 6000 | The longest rendered text of a card |
| `MaxRootIndexChars` | 3500 | The longest rendered text of the index of the roots |
| `MaxSummaryChars` | 100 | The longest summary |
| `MaxRootSummaryChars` | 60 | The longest summary of a root card |
| `MaxLineLength` | Off | The longest line of a body |
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
| `line-length` | A body line that is longer than `MaxLineLength` |
| `command-parse` | A `command` example that `ExampleCommandLine` cannot parse |
| `resources-mismatch` | A file that exists on disk or in the assembly but not on the other side |

A domain rule implements `IHelpCardRule`, which has one method that receives a `HelpCard` and returns problems. A
library whose cards must not contain command-line text can check that with a rule, and put it in the options:

```csharp
var options = new HelpValidationOptions { Rules = [new NoCommandLineTextRule()], WarningsAreErrors = true };
HelpCardValidator.AssertValid(MyLibHelp.Source, options);
```

`HelpCardValidator.CompareToDirectory(source, directory)` compares the embedded resources of an assembly source with
the files of a folder and reports the differences as `resources-mismatch`.

## Testing Examples

`HelpExamples.Enumerate(catalog, kinds)` returns every example of a catalog together with its card, optionally limited
to some kinds. It feeds a theory of your test framework:

```csharp
public static IEnumerable<object[]> CommandExamples() =>
    HelpExamples.Enumerate(Catalog, ["command"]).Select(pair => new object[] { pair.Card.Id, pair.Example.Name });

[Theory]
[MemberData(nameof(CommandExamples))]
public void Help_CommandExample_RunsWithoutError(string cardId, string exampleName)
{
    var reference = Catalog.FindExamples(exampleName, cardId).Single();
    Assert.True(ExampleCommandLine.TryParse(reference.Example.Content, "tool", out var arguments, out var error), error);
    Assert.Equal(0, Program.Main([.. arguments]));
}
```

HelpCards never starts a process and never runs an example. Your test decides how a command runs, usually by calling the
entry point of the tool with the parsed arguments.

`ExampleCommandLine.TryParse(content, toolName, out arguments, out error)` parses one command line with a restricted
grammar. The first word must be the tool name, and the remaining words are the arguments. A word is made of unquoted
characters, single-quoted text without escapes, and double-quoted text in which only a backslash and a quote can be
escaped. Unquoted characters exclude the space, the quotes, and ``| & ; < > ( ) $ ` * ? [ ] { } ~ ! # \ %``. Pipes,
redirections, variables, wildcards, comments, and line continuations are therefore rejected with a message that names
the character and the column, for example `shell operator '|' is not allowed in an example command`.

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
