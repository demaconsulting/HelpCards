### HelpProblem Design

![HelpCards Structure](HelpCardsView.svg)

#### Purpose

`HelpProblem` is the plain value with which every part of the library reports a finding about a help source:
an error, a warning, or an informational note. `HelpSeverity` classifies it and `RuleIds` names every rule
that a problem can carry. The unit exists so that defects can be collected, sorted, filtered, compared, and
asserted on without exceptions.

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

#### Key Methods

- **`CreateError(ruleId, sourceName, file, line, message)`** (internal) creates an error problem so the many
  raise sites stay short.

#### Error Handling

N/A - the unit is a data type; it validates nothing and throws nothing.

#### Dependencies

None beyond the Base Class Library.

#### Callers

`FrontMatterDecoder`, `TokenResolver`, `BodyParser`, and `CardReader` create problems; Composition collects
and orders them; the catalog exposes them; the validator and the renderers read them.
