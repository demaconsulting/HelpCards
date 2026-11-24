## Testing Subsystem Verification Design

This document describes how the Testing subsystem is verified.

### Verification Approach

The subsystem is verified by integration tests in `TestingTests.cs` that create catalogs with the Composition subsystem
from the embedded `simple` fixture and from in-memory sources, and then use only the public face of the Testing
subsystem: `HelpCardValidator`, `HelpValidationOptions`, `HelpExamples`, `ExampleCommandLine`, `HelpExampleRunner`, and
`HelpCardMarkdown`. Defective sources exist only in memory, so no malformed card is ever checked in. Nothing is mocked.

### Test Environment

- xUnit v3 under the .NET SDK, run by `build.ps1` and CI on every supported target framework.
- The `simple` fixture is embedded in the test assembly by the packaged MSBuild targets; the directory scenario
  writes a copy of it into a unique temporary directory that is deleted afterwards.
- The `runnable` fixture is embedded in the same way; the runner scenarios use fake delegates and never start a process.
- There are no sleeps, no timing assertions, and no network access.

### Acceptance Criteria

The subsystem passes when every scenario below passes on every target framework. A valid source must have no problem,
a defective source must give its errors without an exception, each quality rule must be reported once with the file and
line of the defect, and the helpers must agree with the loader on code and tokens.

### Test Scenarios

- **Validation**: a valid embedded source has no problems, a source with a broken link gives its error without an
  exception, and the assertion throws one exception with the same problems.
- **Quality rules**: one defective card reports every rule once: a long summary, a large card, a reserved heading with
  its file line, a fence without a language, an unclosed fence with its line, a long line, a command that redirects, and
  an unused token.
- **Options**: a rule of the product adds its problem, required external sources give errors for absent links, and
  warnings that fail make the assertion throw for the problem of the rule.
- **Examples**: the commands of the `simple` fixture are enumerated with their cards and parse into arguments.
- **Example runner**: the embedded `runnable` fixture is run through a fake delegate in catalog order with the saved
  files present, a correct run succeeds, and a run that gives other exit codes and other output reports each difference
  with its rule and fails `AssertSucceeded`.
- **Markdown**: headings, a section, and bullets are read from a card body of a catalog.
- **Resources**: a directory with one file that is not embedded is reported for that file only.
