### HelpAgentTool Verification Design

#### Verification Approach

The tool is tested directly in `HelpAgentToolTests.cs` over catalogs created from the embedded fixtures and from
in-memory sources. The recoverable-exception boundary is tested through the internal constructor, which takes the
function that answers a call, so that exceptions of every type can be thrown on purpose. A counting file provider proves
that no file is read. No other mocks.

#### Test Environment

Standard xUnit v3 runner; `Parallel.For` for the concurrency scenario; no sleeps or timers.

#### Acceptance Criteria

All tests in `HelpAgentToolTests.cs` pass; the schema equals the text of the contract byte for byte.

#### Test Scenarios

- **Schema**: the exact text, one line, valid JSON, no additional properties.
- **Name**: the default, a name chosen by a host, and the longest valid name.
- **Creation**: every invalid name, range, description, and tool name, and the limits of the ranges.
- **Description**: the generated text with the product and sources, and a given text.
- **Budget**: a long card cut at a line end with the omitted count, and a long single line cut without splitting a pair.
- **Boundary**: each recoverable exception becomes a message with the type name only; out-of-memory and cancellation
  pass; hostile text never throws.
- **No I/O and threads**: no listing or read after creation, and the same answers from many threads.
