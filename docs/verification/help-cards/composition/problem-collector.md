### ProblemCollector Verification Design

#### Verification Approach

The collector is tested directly in `ProblemCollectorTests.cs` with problems created by the test, added in scrambled
order. No mocks.

#### Test Environment

Standard xUnit v3 runner; no files.

#### Acceptance Criteria

All tests in `ProblemCollectorTests.cs` pass: the order is by source, file (ordinal), line, and rule (ordinal); equal
keys keep the order found; identical problems are kept once.

#### Test Scenarios

- **Ordering**: problems of two sources, upper-case and lower-case file names, several lines, and several rules.
- **Equal keys**: two problems with the same keys keep the order in which they were added.
- **Duplicates**: an identical problem added twice is kept once, and the same problem of another source is kept.
- **Severity**: the filter returns only that severity, and the error flag follows the collected problems.
- **Arguments**: a null problem and a null list throw `ArgumentNullException`.
