### JsonDocumentWriter Verification Design

#### Verification Approach

The writer is tested directly in `JsonDocumentWriterTests.cs` and `JsonDocumentWriterTests.Contract.cs`. The seven
documents of the contract are compared character for character with the fixtures of the specification, over small
in-memory catalogs of an imaginary tool named `reqstream` (the test support class `RenderSources`) that produce each
fixture exactly. The `simple` fixture is compared with golden files. The rules of the contract are checked by parsing
the output with `System.Text.Json`. No mocks.

#### Test Environment

Standard xUnit v3 runner; the embedded fixtures and golden files; in-memory sources; the determinism scenario changes
the current culture of the test thread and restores it.

#### Acceptance Criteria

All tests in the two files pass; the contract documents are equal to the specification character for character.

#### Test Scenarios

- **Contract documents**: `index`, `card`, `examples`, `example`, `search`, `cards`, and `error` as in the
  specification.
- **Golden files**: the card, the index, and the examples document of the `simple` fixture.
- **One line, no null**: every document of every kind, for every card and link style, parses, has the contract version,
  contains no null, and has no line break.
- **ASCII**: accents, a symbol, and a surrogate pair become escapes that round-trip; quotes, backticks, apostrophes, and
  angle brackets stay readable while quotes, backslashes, and control characters are escaped.
- **Key order and optional keys**: the keys of the card; no command without the command style; no tool, version, or
  usage unless supplied; the index depth; `exitCode` and `expect` appended after `content` only when a command states
  them, and omitted (without any null) otherwise.
- **Links and culture**: the link targets of the guide body in order with their optional parts; the culture of the body
  file from the `i18n` fixture.
- **Envelope**: a callback replaces the top-level object and receives the kind; non-ASCII text it writes is escaped.
- **Errors**: candidates only for `not-found` and `ambiguous`, the optional hint, unknown codes, and null arguments.
- **Error body**: a body callback replaces the default members inside the default envelope and inside a callback
  envelope, and an unknown code is still rejected.
- **Determinism and arguments**: equal documents under two cultures; null arguments throw; an envelope callback that
  returns null gives an argument error that names the callback.
