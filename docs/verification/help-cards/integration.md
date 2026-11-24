## Integration Subsystem Verification Design

This document describes how the Integration subsystem is verified.

### Verification Approach

The subsystem is verified by integration tests in `IntegrationTests.cs` that create catalogs with the builder of the
Composition subsystem from the embedded fixtures and from in-memory sources, create the command-line helper and the
knowledge tool over them, and then use only their public faces: `HelpCli`, `HelpCliOptions`, `HelpAgentTools`, and
`HelpAgentTool`. The helper writes to in-memory writers and a counting catalog factory records when the catalog is
created; a counting file provider records every listing and read. Nothing is mocked. The golden files of the `cli`
fixture are compared at unit level and in the system tests.

### Test Environment

- xUnit v3 under the .NET SDK, run by `build.ps1` and CI on every supported target framework.
- The fixtures and golden files are embedded in the test assembly by the packaged MSBuild targets; golden files use LF
  only.
- The width of the console is supplied by a function of the test, never read from the machine; there are no sleeps and
  no timing assertions.

### Acceptance Criteria

The subsystem passes when every scenario below passes on every target framework. A command that is not a help command
must never create the catalog or read a resource; every exit code and error channel must follow the contract; every
wrong argument of the tool must be answered with text; and no answer may be longer than the limit plus the notice.

### Test Scenarios

- **Gate**: no arguments, an option, another command, and a different verb return false without creating the catalog or
  reading a resource, and the footer is constant; the help verb creates the catalog once.
- **Option table**: the index, a card, examples, one example, a search, a tag listing, and a source restriction each
  answer in text, Markdown, and JSON, and the JSON documents have the expected kinds.
- **Failures**: a usage error exits with 2 on the error writer, an unknown card exits with 2, and a defective catalog
  exits with 70; the JSON forms are documents on the output, for a not-found card and for an ambiguous example (with
  its candidates).
- **Restrictions and misses**: a source restriction hides the other source; an unknown source, tag, card, and example
  exit with 2 and name the valid names or the closest name; one example prints as exactly its content.
- **Extensions**: a preamble and sections in the index, a fallback answer with its exit code, the width of the console,
  the culture passed to the factory, and a command template.
- **Tool creation**: the default name and a name chosen by a host, the schema text, the generated description, and
  options that are checked at once (a bad name, a limit out of range, null catalog).
- **Tool calls**: over a catalog of two sources, the index with one block for each source, a card, an example, and a
  search with a limit.
- **Tool arguments**: invalid JSON, a non-object, an unknown property, a wrong type, an out-of-range maximum, unknown
  names, and a lone surrogate are all answered with text and never throw.
- **Budget**: a long card is cut at a line end with the notice of the omitted characters; using the tool reads no file
  after the catalog is created and hostile text never makes it throw. The exceptions of the tool boundary and the
  pass-through of unrecoverable ones are verified at the unit level (`HelpAgentTool`).
