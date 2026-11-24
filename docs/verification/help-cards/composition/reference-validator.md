### ReferenceValidator Verification Design

#### Verification Approach

The validator works on the definitions that Loading produces and on the nodes of the tree, so it is tested through
the pipeline on small in-memory sources in `ReferenceValidatorTests.cs`; the tests look at the problems and at the
resolved data of the nodes. Loading and the other Composition units are real because the validator cannot be
exercised without their output.

#### Test Environment

Standard xUnit v3 runner; in-memory files.

#### Acceptance Criteria

All tests in `ReferenceValidatorTests.cs` pass: every reference form resolves to the expected global ids, every defect
gives exactly one problem at the expected file and line, and references to failed files are silent.

#### Test Scenarios

- **Card links**: an unknown card is reported at the line of the link with the link text; forward and backward links
  resolve; the links in the landing text of a mount root resolve in the mounted source and a broken one is reported in
  `_source.md`.
- **Example links**: `[[card#name]]` and `[[#name]]` find the card's own example first and otherwise the one example
  below it; the same name in two cards below the card is `ambiguous-reference` naming all of them; an example in a
  sibling tree is an example target problem.
- **External links**: a link to the registered source resolves like a local link, a wrong card is a link target
  problem, and a link to an absent source becomes display text with an informational problem.
- **See-also**: entries resolve in order without duplicates, an absent source is dropped with an informational problem
  that says the entry is left out (a link says it is written as its display text), and an unknown entry is reported at
  its line.
- **Suppression**: links and see-also entries that name a file that failed to load give no problem.
- **Imported sources**: a bare link and a bare see-also entry resolve to the importer's own card, a link that names a
  source resolves to that source even when the importer has a card of the same id, and a bare link or a bare see-also
  entry that names a card of the source and of an import is ambiguous.
- **Example names**: the second use in one card is reported with the first location; the same name in different cards
  (unrelated, parent and child, or siblings) is allowed; a name that an overlay repeats is reported in the overlay file.
- **Sibling names**: an alias equal to a sibling alias or name is reported at the alias line, an alias equal to the
  card's own name is reported, and equal names below different parents are allowed.
