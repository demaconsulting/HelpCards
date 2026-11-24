### HelpCardMarkdown Verification Design

#### Verification Approach

The helpers are tested directly in `HelpCardMarkdownTests.cs` with one body that has headings of several levels, bullets
with every marker, and a fence that contains lines that look like headings and bullets. No mocks.

#### Test Environment

Standard xUnit v3 runner; strings in the tests.

#### Acceptance Criteria

All tests in `HelpCardMarkdownTests.cs` pass.

#### Test Scenarios

- **Headings**: texts without marks, a closing run of marks, six levels and not seven, indentation up to three spaces,
  and no heading inside a fence or without a space.
- **Sections**: to the next heading of the same level with deeper headings and fences inside, a missing heading, the
  first of equal headings, and carriage returns.
- **Bullets**: all markers, joined continuation lines, text that ends a bullet, and a fence that ends a bullet.
- **Scan**: fence lines with their language, an unclosed fence, an inline span that is not a fence, and a run that is
  too short to close.
- **Arguments**: null body, null heading, and a white-space heading.
