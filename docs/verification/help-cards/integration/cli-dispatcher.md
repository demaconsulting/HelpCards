### CliDispatcher Verification Design

#### Verification Approach

The dispatcher is tested through the helper in `CliDispatcherTests.cs` and `CliDispatcherTests.Modes.cs`, with catalogs
created from the embedded `simple`, `big`, `lib-a`, and `lib-b` fixtures and from in-memory sources. A harness provides
in-memory writers, a counting factory, and a fixed console width. Documents are compared with the golden files of the
`cli` fixture, which are the recorded output of the helper, each followed by one line feed. No mocks.

#### Test Environment

Standard xUnit v3 runner; the embedded fixtures; no sleeps or console access.

#### Acceptance Criteria

All tests in the two files pass, every document equals its golden file exactly, and every error goes to the channel and
exit code of the contract.

#### Test Scenarios

- **Index**: the usage, preamble, section, topics, and footer in text; the whole tree; the JSON index with and without
  usage lines.
- **Card**: the card with command links in text and Markdown, and the path as words, slashes, alias, and other case.
- **Examples**: the examples in full with the cards below only listed, one example alone, the qualified name, a missing
  example, and an ambiguous name below a mount.
- **Not found**: the closest name, the names at the level, a card without children, and a far name at the top whose
  hint uses the command template.
- **Search and tags**: ranked results with and without matches, restriction by tag and source, an unknown tag, and a
  catalog without tags.
- **Sources**: the index restricted by source, an unknown source, and the filter of the JSON tag listing.
- **Failures and channels**: usage errors in text and JSON, a defective catalog, a factory that returns nothing, and
  line feeds only.
- **Tool contract** (`CliDispatcherTests.Contract.cs`): an envelope of the tool wraps the index, card, examples,
  example, search, and tag documents; an error body of the tool is written inside the envelope for a usage error and
  for a name that is not found, and receives the code, message, hint, and candidates of every kind of failure; the
  envelope and the error body work independently; text errors are unchanged; the exit codes of the tool apply to usage
  errors, names that are not found, and a defective or missing catalog, success and the other codes are mapped
  independently, and the exit code of a fallback answer is kept; with nothing set the documents and exit codes are
  those of the contract of the helper.
- **Extensions**: the fallback answer before the catalog is created, no answer, the language, the default and explicit
  widths, `auto` with and without a console, and the command template.
