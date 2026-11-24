### HelpProblem Design

![Catalog Structure](CatalogView.svg)

#### Purpose

`HelpProblem` is the plain value with which every part of the library reports a finding about a help source:
an error, a warning, or an informational note. `HelpSeverity` classifies it and `RuleIds` names every rule
that a problem can carry. `HelpCatalogException` carries every error of a failed catalog creation. The unit exists
so that defects can be collected, sorted, filtered, compared, and asserted on without exceptions, and so that a
caller can tell a defective source from a programming mistake.

#### Data Model

| Member | Type | Meaning |
| --- | --- | --- |
| `Severity` | `HelpSeverity` | `Info` (0), `Warning` (1), `Error` (2) |
| `RuleId` | `string` | A constant of `RuleIds`, such as `syntax` or `link-target` |
| `SourceName` | `string` | The name of the source the problem was found in |
| `File` | `string` | The path of the file inside the source, or an empty string |
| `Line` | `int` | The one-based line, or 0 when none applies |
| `Message` | `string` | What is wrong and, where possible, how to fix it |

`HelpProblem` is a sealed record, so it is immutable and compares by value. `RuleIds` is an internal static
class of string constants grouped as errors, informational notes, and validator warnings.
`HelpCatalogException` derives from `InvalidOperationException`, has one internal constructor, and exposes
`Problems`, a read-only copy of the errors in display order.

#### Key Methods

- **`CreateError(...)`** and **`CreateInfo(...)`** (internal) create an error or an informational problem so the many
  raise sites stay short.
- **`ToDisplayText()`** (internal) writes the problem as one sentence: `Help card '{file}' of source '{source}', line
  {n}: {message}.` The line is left out when it is 0; a problem without a file reads `Help source '{source}':
  {message}.`; the closing period is added only when the message does not already end a sentence.
- **`HelpCatalogException`** builds its message from the display text of the first problem and, when there are more,
  the words `(and N more)`. It refuses an empty or null list with an argument exception.

#### Error Handling

`HelpProblem` is a data type; it validates nothing and throws nothing. `HelpCatalogException` rejects an empty or null
problem list, because an exception without a problem would say nothing.

#### Dependencies

None beyond the Base Class Library.

#### Callers

`FrontMatterDecoder`, `TokenResolver`, `BodyParser`, and `CardReader` create problems; Composition collects
and orders them; `HelpCatalog.Create` throws the exception; the validator and the renderers read them.
