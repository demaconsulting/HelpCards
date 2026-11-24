### TextRenderer Verification Design

#### Verification Approach

The renderer is tested directly in `TextRendererTests.cs`, `TextRendererTests.Examples.cs`, and
`TextRendererTests.Lists.cs`. Whole documents are compared exactly with the golden files of the `render` fixture, and
the exact layout of small documents is checked against strings in the tests, over catalogs created from the embedded
`simple` fixture and from in-memory sources. No mocks.

#### Test Environment

Standard xUnit v3 runner; the embedded fixtures with their golden files; in-memory sources.

#### Acceptance Criteria

All tests in the three files pass; every golden comparison is exact.

#### Test Scenarios

- **Card**: the text, Markdown, command, Markdown-link, example-content, and narrow-width golden files; the exact layout
  of a full card; a plain card, an empty body, see-also entries, and tags switched off.
- **Markdown and wrapping**: bold labels, no wrapping, and wrapping of generated lines only with a hanging indent.
- **Hints and fences**: the command hint only for cards with examples; a fence that is longer than a run of backticks.
- **Output rules**: LF only, no trailing line feed, no trailing spaces, tabs, or escape characters for every card.
- **Examples document**: the golden, the exact layout with descendants, the plain style without hint, a source example
  with its file name, and a card without examples.
- **Expected results**: the lines after the fence of a command in the examples document (the exit code only when it is
  not 0, one line per fragment, nothing for examples that state no result), wrapped at a narrow width with the hanging
  indent in the card document with example content, and nothing added when one example is printed alone.
- **One example**: exactly the content, found ignoring case in the subtree; an unknown example is an argument error.
- **Index**: the text, Markdown, and command golden files; the padding and indent by depth; the depth limit; a long
  summary; an empty catalog view.
- **Lists**: a card list and search results in the plain and command styles, and no results.
- **Arguments**: null arguments throw.
