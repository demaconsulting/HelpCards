### TextWrapper Verification Design

#### Verification Approach

The wrapper is tested directly in `TextWrapperTests.cs` by wrapping short texts and comparing the lines. No mocks.

#### Test Environment

Standard xUnit v3 runner.

#### Acceptance Criteria

All tests in `TextWrapperTests.cs` pass.

#### Test Scenarios

- **Width**: lines break between words and none is longer than the width.
- **Prefixes**: the prefix counts toward the width; later lines start with the hanging indent; the hanging variant
  indents by the width of the prefix.
- **Long words**: a word longer than the width is on its own line and is not split.
- **No wrapping**: a width of zero writes one line.
- **Spaces and empty text**: runs of spaces collapse; text without a word writes nothing; lines end with a line feed
  only.
- **Arguments**: null arguments throw.
