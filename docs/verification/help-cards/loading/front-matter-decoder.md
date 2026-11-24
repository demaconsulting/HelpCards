### FrontMatterDecoder Verification Design

#### Verification Approach

The decoder is a pure function, so it is tested directly with text. Every accepted construct has a test named
`FrontMatterDecoder_TryDecode_{Construct}_Accepted` and every rejected construct a test named
`FrontMatterDecoder_TryDecode_{Construct}_Rejected`, so the documentation of the card format can cite the test
that proves each row of its tables. Rejected tests assert the rule, the exact line, the source name, and the
file of the problem.

#### Test Environment

Standard xUnit v3 runner; no files, no mocks.

#### Acceptance Criteria

All tests in `FrontMatterDecoderTests.cs` pass: each accepted construct decodes to the exact values and lines,
and each rejected construct yields exactly one problem with the expected rule and line and no document.

#### Test Scenarios

- **Accepted subset**: plain, single-quoted (with a doubled quote), and double-quoted (with both escapes)
  scalars; full-line comments at any indentation; an indented list; a list in the first column; a list of
  maps; a colon without a space inside a value; an empty value; an extension key; CRLF; a byte order mark; the
  complete example of the card format.
- **Literal blocks**: `|` and `|-` for the `content` and the `expect` of an example; kept blank lines, hash lines,
  deeper lines, and tabs after the indent; dropped trailing blank lines.
- **Body**: blank separator line, no body, trimmed trailing line feeds, a thematic break inside the body.
- **Rejected constructs**: flow sequences and maps (also as list items), anchors, aliases, tags, trailing
  comments (also after a quoted value), values that start with `#`, `%`, `@`, or a backtick, plain values with
  `: ` or a trailing colon, tabs, duplicate keys (also in a list item), nested mappings, a list inside a list
  item, document markers, unterminated quotes, an unsupported escape, text after a closing quote, a list item
  without a key, unexpected indentation, a key with a space before the colon, an empty list item, misaligned
  keys in a list item.
- **Block scalars where they are not allowed**: an ordinary key, a list item, a `content` key outside an example list,
  an `expect` key outside an example list, `content` and `expect` in a list that is not `examples`, and another key of
  an example (rule `block-scalar-not-allowed`); `|+`, `|2`, and `>` (rule `syntax`).
- **Delimiters and arguments**: missing opening delimiter, missing closing delimiter, a null text, and the
  source name and file in the problem.
