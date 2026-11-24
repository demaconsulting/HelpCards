### HelpProblem Verification Design

#### Verification Approach

The problem model is tested directly: records are constructed and compared, and the rule identifier class is
inspected through reflection over its public constants to prove that the identifiers are unique, well-formed,
and complete.

#### Test Environment

Standard xUnit v3 runner; no files, no mocks.

#### Acceptance Criteria

All tests in `HelpProblemTests.cs` pass.

#### Test Scenarios

- **Fields**: all six fields read back; equal fields give equal records and hash codes; one different field
  gives unequal records; the error factory sets severity `Error`; the informational factory sets severity `Info`.
- **Severity**: the numeric order is informational, warning, error.
- **Rule identifiers**: every identifier is a distinct lower-case dashed name and every error rule of the
  failure model has a constant.
- **Display text**: file, source, and line are named; the line is left out for 0; a problem without a file names only
  the source; the closing period is added once.
- **Exception**: the problems are exposed, the message names the first problem and counts the others, the type is an
  invalid operation exception, and an empty or null list is refused.
