### ExampleCommandLine Verification Design

#### Verification Approach

The parser is tested directly in `ExampleCommandLineTests.cs` with commands written in the tests. Every character of the
grammar that is an error is tried in a loop, so the error text and the column are asserted for each one. No mocks.

#### Test Environment

Standard xUnit v3 runner; no files and no processes.

#### Acceptance Criteria

All tests in `ExampleCommandLineTests.cs` pass.

#### Test Scenarios

- **Words**: several spaces between words; a command that is only the tool name.
- **Quoting**: single quotes, double quotes with both escapes, joined parts, an empty argument, and a pipe in single
  quotes.
- **Tool name**: another name and another case are rejected with both names; empty content.
- **Line break**: a line feed and a carriage return with their column.
- **Shell syntax**: each operator and each expansion character with its column; a comment, a redirection, a pipe,
  and a trailing backslash.
- **Double quotes**: a dollar sign, a backtick, an unknown escape, and quotes that are not closed.
- **Arguments**: null content, a null tool name, and an empty tool name.
