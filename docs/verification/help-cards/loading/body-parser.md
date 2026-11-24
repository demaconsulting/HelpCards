### BodyParser Verification Design

#### Verification Approach

The parser is a pure function, tested directly with body text. Links are asserted as exact `ParsedLink`
records including their lines; problems are asserted by rule, source name, file, and line.

#### Test Environment

Standard xUnit v3 runner; no files, no mocks.

#### Acceptance Criteria

All tests in `BodyParserTests.cs` pass: every link form parses to the expected record, nothing inside code is a
link, and every link typo is reported once at its line.

#### Test Scenarios

- **Link forms**: plain text only, an empty body, a card link with its line, a card link with an example, a
  local example link, an external link with display text, an external link to an example whose display text
  contains a bar, several links with the text between them.
- **Code awareness**: a code span, a double-backtick span that contains a backtick, a span across a line
  break, an unmatched backtick that must not hide a later paragraph, a backtick fence, a tilde fence, a short
  fence inside a long fence, an unclosed fence, three backticks that close on the same line, brackets that
  do not start like a link.
- **Link errors**: upper-case names, a space, an external link without text, display text with an edge space
  or longer than 120 characters, an id of nine segments (eight are allowed), several typos reported each.
