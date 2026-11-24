## Composition Subsystem Verification Design

This document describes how the Composition subsystem is verified.

### Verification Approach

The subsystem is verified by integration tests in `CompositionTests.cs` that use the units through the builder on
whole sources: the embedded `simple` and `order` fixtures, and small in-memory sources for defects. The Loading
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
- **Uniqueness**: a repeated example name in one scope, a name that is also an alias, and a mismatch with the expected
  roots are all reported, in ordinal file order.
- **Single source and overlays**: a second source and a source with imports are argument errors; an overlay is read
  and has no effect.
- **Reading and culture**: composing reads each file once per composition, and the culture of the builder selects the
  translation.
