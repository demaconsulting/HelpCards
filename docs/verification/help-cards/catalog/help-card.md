### HelpCard Verification Design

#### Verification Approach

Cards are tested in `HelpCardTests.cs` through catalogs created from the embedded `simple` fixture and from in-memory
sources, because cards are created only by catalogs. The small model types (source description, example reference,
search result) are tested by construction and comparison.

#### Test Environment

Standard xUnit v3 runner; the embedded fixtures.

#### Acceptance Criteria

All tests in `HelpCardTests.cs` pass: every property reads back what the card file said, the body is exactly the
expected plain-style text, and hidden targets are written as specified.

#### Test Scenarios

- **Properties**: a root and a child card expose id, name, parent, source, title, summary, tags, aliases, culture,
  children, and see-also; extension keys are preserved.
- **Examples**: a command with its token replaced, a source example read from its data file, and a snippet with inline
  content.
- **Body**: the whole body of the `guide` card of the fixture, with every link kind, tokens, and a code span; the same
  string on repeated access; an example of a descendant written with its owner.
- **Body pieces**: the five visible links with ids, titles, display text, and source; adjacent text merged when links
  are hidden.
- **Hidden links**: a hidden card becomes its title and a hidden example becomes the words `an example`.
- **Model types**: equality of the source description and the example reference; fields of the search result.
