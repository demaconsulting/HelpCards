### HelpStrings Verification Design

#### Verification Approach

The strings are tested directly in `HelpStringsTests.cs` by comparing each heading, label, and hint with its expected
English text. No mocks.

#### Test Environment

Standard xUnit v3 runner; the culture scenario sets the current culture of the test thread to Turkish and restores it.

#### Acceptance Criteria

All tests in `HelpStringsTests.cs` pass.

#### Test Scenarios

- **Chrome**: the headings, the labels in plain and bold form, the title of an examples document, and the hint
  sentences.
- **Culture**: the headings and titles are the same under a culture with unusual casing rules.
