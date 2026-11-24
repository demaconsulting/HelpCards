## Composition Subsystem Verification Design

This document describes how the Composition subsystem is verified.

### Verification Approach

The subsystem is verified by integration tests in `CompositionTests.cs` and `CompositionMultiSourceTests.cs` that use
the units through the builder on whole sources: the embedded `simple`, `order`, `lib-a`, `lib-b`, `app`, and `tool`
fixtures, and small in-memory sources for defects. The Loading
subsystem is used as it is to read the sources, and the frozen data and the problem model of the Catalog subsystem
are used as they are to assert on results. Nothing is mocked except the file provider: a counting provider (in the
test support folder) wraps in-memory files so tests can prove how often files are listed and read. Error sources live
in memory so that no malformed file is checked in.

### Test Environment

- xUnit v3 under the .NET SDK, run by `build.ps1` and CI on every supported target framework.
- The embedded fixtures are compiled into the test assembly by the packaged MSBuild targets
  (`HelpCardsDirectory` set to `Fixtures`) and read with `HelpSource.FromAssembly`.
- No temporary files, network, or services.

### Acceptance Criteria

The subsystem passes when every scenario below passes on every target framework. Problems must carry the expected rule,
file, and line, in display order, with one problem per defect; a valid source must give the same tree whatever the
order of the problems or files in the input.

### Test Scenarios

- **Pipeline produces a frozen tree**: the `simple` fixture gives five cards in depth-first order, the children in
  sibling order, the parent of each child, the resolved see-also entry, the examples, and two informational problems
  for the links to the absent source.
- **Ordering follows the one comparator**: the `order` fixture gives the roots in the order of the `children` list of
  `_source.md`, then by order value with a tie by name, then unordered cards by name; one parent lists only part of its
  children.
- **Several defects are collected once and in order**: a syntax error that other files link to, a bad tag, a bad link,
  a duplicate alias, and a missing parent each give exactly one problem, in file and line order, and no follow-up
  problem.
- **References resolve**: forward links, card and example links, an external link to the registered source, and
  see-also entries resolve; a link to an absent source degrades; `[[#name]]` looks only in the card it is written in.
- **Uniqueness**: a repeated example name in one card, a name that is also an alias, and a mismatch with the expected
  roots are all reported, in ordinal file order.
- **Mounting**: two mounted libraries give generated root cards with the mount as id, global ids with the mount as
  prefix, children in the order of `_source.md`, the landing text from `_source.md`, and an alias that finds the mount
  root; flat sources merge their roots in registration order; order values never reorder sources; a view by source
  shows one mounted source.
- **Imports**: an application that imports a library sees its tokens and bare card ids whatever the mount of the
  library; without the import the same cards report unknown tokens and links; a tool source with a schema token uses
  the tokens and bare links of its companion library; an import that is added later, is missing, or is the source
  itself is one problem and causes no follow-up problems (a later source that only names itself in its `_source.md` is
  still `import-order`); a bare id that two sources of one product have is ambiguous.
- **Overlays**: the overlay of the application replaces title and summary of a library card and appends body, see-also
  entries, and examples (whose links resolve in the application's scope and whose `[[#name]]` refers to the target
  subtree); a bare overlay extends a card of the imported library; an optional overlay of an absent library is skipped
  with an informational problem; overlays apply in registration order and then by file path.
- **Example names and links between sources**: the same example name in two libraries or two cards is allowed (the
  card's own example wins, two cards below a card with the name are ambiguous), an overlay that
  repeats a name of its target card is reported in the overlay file; links to a present library resolve with the
  global id and a missing card is an error, links to an absent library degrade to their display text.
- **Collisions**: the same global id, mount, source name, or root alias in two sources, and a card under a card of
  another source, are each one problem that names both sources, and the later card or source is left out silently.
- **Failures across sources**: problems of several sources come in registration order with no duplicates; an unsupported
  schema in one source does not hide the defects of the others; omitted, explicit 1, and 2 are supported side by side.
- **Vocabulary and roots across sources**: expected roots count mount roots and flat roots; the tag vocabulary applies
  to the cards of every source but not to the generated roots (also checked at the builder unit).
- **Failed targets and unnamed sources**: an overlay of a card that failed to load, and a mounted source without a
  name, each add no problem beyond the one that Loading reports.
- **Determinism and reading**: composing the same sources twice gives equal renderings; each file is read once and
  nothing is read after the catalog is created.
- **Reading and culture**: composing reads each file once per composition (counted after each composition), and the
  culture of the builder selects the translation.
