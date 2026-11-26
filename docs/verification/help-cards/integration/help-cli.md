### HelpCli Verification Design

#### Verification Approach

The helper is tested directly in `HelpCliTests.cs` with a harness that wires in-memory writers and a counting catalog
factory. Factories that throw prove that the catalog is not created. No mocks.

#### Test Environment

Standard xUnit v3 runner; the embedded `simple` fixture.

#### Acceptance Criteria

All tests in `HelpCliTests.cs` and `HelpCliTests.Contract.cs` pass.

#### Test Scenarios

- **Creation and footer**: creating the helper and reading the footer never call the factory; the footer is constant.
- **Gate**: no arguments, an option, a word, a similar word, and a different position of the verb return false without
  writing; the verb in any case runs the command; the verb alone writes the index.
- **Verb**: another verb replaces `help` in the gate and the footer.
- **Run**: the arguments after the verb, and null arguments.
- **Options**: invalid tool names, verbs, templates, and widths are rejected; options with neither or both of the
  catalog configuration and the factory, and a writer together with its provider, are rejected.
- **Catalog configuration**: the catalog is built only after the verb is seen; the tool name, the version, and the
  culture of the language option reach the cards, and the action of the tool may override them;
  `GetCatalogConfiguration` applies the same settings and throws `InvalidOperationException` without a configuration.
- **Writers**: a provider is called when the helper writes (not at creation), so the writer can change between commands;
  a provider that returns null writes to the console (the console writers are replaced for the test and restored); a
  provider is not the console for automatic width.
- **Contract settings** (`HelpCliTests.Contract.cs`): the envelope, the error body, and the exit codes are returned as
  set; the defaults of the options (no envelope, no error body, exit codes 0, 2, and 70); an exit code outside 0 to 255
  and a null set of exit codes are rejected; an exception of the error body or the envelope callback, and an envelope
  that returns null, propagate out of `Run` and `TryRun`.
