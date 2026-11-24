## Catalog Subsystem Verification Design

This document describes how the Catalog subsystem is verified.

### Verification Approach

The subsystem is verified by integration tests in `CatalogTests.cs` that create catalogs with the builder of the
Composition subsystem from the embedded fixtures (`simple`, `i18n`, `big`) and from in-memory sources, and then use
only the public face of the Catalog subsystem: `HelpCatalog`, `HelpCard`, the views, the lookups, and the problem
model. Nothing is mocked. Defective sources are built in memory.

### Test Environment

- xUnit v3 under the .NET SDK, run by `build.ps1` and CI on every supported target framework.
- The fixtures are embedded in the test assembly by the packaged MSBuild targets.
- The concurrency scenario uses `Parallel.For` with counted callbacks; there are no sleeps and no timing assertions.

### Acceptance Criteria

The subsystem passes when every scenario below passes on every target framework. Views must never change the catalog
they come from, a hidden card must never be returned or pointed at, and every worker of the concurrency scenario must
see the same result.

### Test Scenarios

- **Model exposes an immutable tree**: roots, cards in depth-first order, the source description, the tag names, the
  examples, read-only lists, and the same card object on repeated lookup.
- **Big fixture**: twelve cards in three groups of three below three roots, the tags, and a lookup by alias.
- **Cultures**: neutral, language, and language-and-region catalogs of the `i18n` fixture give the translated title,
  aliases, and example description per card, the culture of each body, and fall back to the language and the neutral
  text.
- **Lookup**: a path with an alias finds a card; a failed path reports the parent, the segment, and the valid names;
  examples are found by name and by card.
- **Views compose and lift**: a tag view; a source, predicate, and id view composed; hidden parents lift their children
  to the roots; the original catalog is unchanged.
- **Hidden targets**: links to hidden cards are written as titles, a link to a hidden external target as its display
  text, and see-also entries to hidden cards are dropped.
- **Failures**: a defective source throws the catalog exception with every error and a message that counts the rest;
  a null callback, an unknown id, an unknown source, and a null path are argument errors.
- **Problems**: problem records compare by value, the exception message is one sentence, and the severities are ordered.
- **Search**: the twelve-card fixture is ranked, limited by count, restricted by tag, and searched through a view that
  hides a card.
- **Suggestions**: a misspelled alias and a misspelled path segment get the closest visible name, and a hidden card is
  never suggested.
- **Concurrency**: one hundred workers use one catalog, make views, and read bodies; every result is the same and every
  predicate call is counted.
- **Rendering**: a view renders and indexes only the cards it shows; a link to a hidden card is its title, the example
  content is exact, and a hidden card cannot be rendered.
