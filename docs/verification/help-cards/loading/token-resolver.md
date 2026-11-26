### TokenResolver Verification Design

#### Verification Approach

The resolver is tested directly: scopes are built with `Create` from small dictionaries and texts are passed
through `Substitute`. Problems are asserted by rule, source name, file, and line.

#### Test Environment

Standard xUnit v3 runner; no files, no mocks.

#### Acceptance Criteria

All tests in `TokenResolverTests.cs` pass: known tokens are replaced exactly, unknown tokens are reported at
their lines and left in the text, and redefinitions are reported at scope creation.

#### Test Scenarios

- **Syntax**: valid and invalid token names, case sensitivity, malformed lookalikes left alone,
  replacement inside code, text without tokens unchanged.
- **Escape**: `\{{tool}}` renders `{{tool}}`; an escaped undefined name is not reported; an unknown token beside an
  escape is still reported at its line; `a\{{ b` loses only the backslash and other backslashes stay; `\\{{tool}}`
  gives `\{{tool}}`.
- **Scope**: built-in tokens, own tokens, imported tokens, tokens of a source that is not imported are unknown,
  an empty version.
- **Redefinition**: own and imported with the same name, two imports with the same name, built-in names; the
  first definition wins.
- **Unknown tokens**: a token on the third line of a text that starts on line 10 is reported at line 12 with
  file and source; several unknown tokens are all reported.
- **Names**: the distinct names of a text in order of first use, and no names for text that only looks like a
  token, has an escaped token only, or has none.
