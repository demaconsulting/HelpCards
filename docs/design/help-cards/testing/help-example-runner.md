### HelpExampleRunner Design

![Testing Structure](TestingView.svg)

#### Purpose

`HelpExampleRunner` lets a product prove that every command in its help works. It saves the source examples of a card
to a folder, hands each command example to a delegate of the product, and compares the exit code and the output that
come back with the result the example states in its `exit` and `expect` keys. The library never starts a process or a
shell: the delegate runs the command with the entry point or the process of the product.

#### Data Model

The class is static and has no state. Three small public types go with it, all immutable and created by the runner:

| Type | Meaning |
| --- | --- |
| `HelpExampleInvocation` | The card, the command example, the arguments after the tool name, and the folder |
| `HelpExampleOutcome` | The exit code and the output of one run (a record; the output may be null) |
| `HelpExampleRunReport` | The number of commands run and the problems; `Succeeded` and `AssertSucceeded` |

The problems are ordinary `HelpProblem` errors at the card file with line 0, with the rules `example-parse`,
`example-exit-code`, and `example-output`.

#### Key Methods

- **`Materialize(card, directory)`** creates `directory` (also when the card has no source example, so that it is a
  usable working directory) and writes every example of the kind `source` to `directory` under its file name: `/`
  separates folders, which are created; the text has LF line endings and exactly one trailing line feed; the encoding is
  UTF-8 without a byte order mark. A later example with the same file name overwrites an earlier one. Snippets are
  fragments that are never claimed to work alone, and commands are not files, so neither is written. The resolved path
  of each file is checked to stay below the folder (the loader already rejects `..` and rooted names, so this is defense
  in depth) and an escaping name is an `ArgumentException` before anything is written for it. The method returns the
  names it wrote, in the order of the examples.
- **`Run(card, directory, execute, toolName)`** materializes the card, then takes its command examples in order. Each
  command is split with `ExampleCommandLine.TryParse` and the tool name; a command that cannot be split is an
  `example-parse` problem, is not run, and the loop goes on. Otherwise the delegate is called with an invocation, and
  the result is compared: a different exit code is `example-exit-code` (the expected code is the `exit` of the example,
  0 when absent), and every `expect` fragment that the output does not contain is an `example-output` problem of its
  own. The comparison is case-sensitive and ordinal after CRLF in the output becomes LF, and a null output counts as
  empty. Each message names the example and the card and shows at most the first 400 characters of the output.
- **`Run(catalog, execute, toolName)`** does the same for every visible card that has a command example, in catalog
  order, each in a fresh folder named `help-cards-{guid}` below the temporary folder of the machine. The folder is
  deleted when the card is done, also when the delegate throws, and a folder that cannot be deleted (an
  `IOException` or an `UnauthorizedAccessException`) is left behind without failing the run. A null `toolName` means
  the tool name of the catalog (`CatalogData.EffectiveToolName`: the name set on the builder, else the name of the first
  source, else `tool`).

#### Error Handling

Null arguments throw `ArgumentNullException`; a blank folder or tool name throws `ArgumentException`. Differences from
the stated results are never thrown by the runner: they are problems in the report, and the test decides to fail with
`AssertSucceeded`, which throws one `HelpCatalogException` with the same problems. An exception of the delegate is a
defect of the product, is not caught, and propagates after the scratch folder has been deleted.

#### Dependencies

`HelpCatalog`, `HelpCard`, `HelpExample`, `HelpProblem`, `HelpCatalogException`, and the internal `CatalogData` and
`RuleIds` of the Catalog subsystem; `ExampleCommandLine` of this subsystem; and the Base Class Library (`System.IO`).

#### Callers

Adopters in their test projects, and the tests of this repository.
