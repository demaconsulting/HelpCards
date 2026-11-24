### AgentArgumentReader Verification Design

#### Verification Approach

The reader is tested directly in `AgentArgumentReaderTests.cs` by reading JSON text and comparing the arguments or the
message. No mocks, no catalog.

#### Test Environment

Standard xUnit v3 runner.

#### Acceptance Criteria

All tests in `AgentArgumentReaderTests.cs` pass; every row of the argument table of the contract has a test.

#### Test Scenarios

- **Nothing to ask for**: null, empty, white space, JSON null, and empty objects.
- **Valid properties**: all properties, trimmed text, absent text, repeated properties, and the limits of `maxResults`.
- **Invalid JSON**: several malformed texts, the example of what to send, and a document that is too deep.
- **Not an object**: arrays, strings, numbers, and booleans.
- **Unknown property**: the first in ordinal order, before the types.
- **Wrong types**: each text property and each wrong `maxResults` (including `5.0`), and the order in which types are
  checked.
