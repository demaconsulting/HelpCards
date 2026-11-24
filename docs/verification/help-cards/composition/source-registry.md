### SourceRegistry Verification Design

#### Verification Approach

The registry is tested directly in `SourceRegistryTests.cs`. The tests register in-memory sources (and the counting file
provider, which proves how often files are listed and read), read them with the real loader, and look at the entries,
the problems, and the outcome of lookups. Cards are placed into the entries by the test the way the pipeline does it,
so lookups can be tested without running the whole pipeline. Nothing is mocked except the file provider.

#### Test Environment

Standard xUnit v3 runner; in-memory files; no temporary files.

#### Acceptance Criteria

All tests in `SourceRegistryTests.cs` pass: every source is read once in registration order, every import is resolved or
reported with the documented rule, every lookup gives the documented outcome, and the token scope is as documented.

#### Test Scenarios

- **Reading**: two sources are listed once and read once each, in order, with the name from the source file or from the
  source object; a name is found ignoring case; null arguments throw.
- **Duplicate names**: the later source is excluded, not found by name, and reported once with the position of the
  first.
- **Imports**: a copy of a source made with a `With...` call, and a source with the same declared name, match the
  registered source; imports of imports are listed once in search order.
- **Import checks**: an import that is not registered, one registered later, one registered later whose name comes
  only from its `_source.md`, and the source itself give `import-missing`, `import-order`, `import-order`, and
  `import-cycle`, one problem each, and the source is marked as having a broken import.
- **Tokens**: a source sees the tokens of every source it imports, directly or not; a token defined by a source and by
  an import is `token-redefined`; the tool token is the name of the first source unless the tool name is set.
- **Lookup**: own card before imported card, a qualified id in its source, an id that two sources have as ambiguous, an
  unknown id, an absent source, and a source that is not imported as not found; a card that failed to load, an excluded
  source, and an id after a broken import as suppressed.
