### OverlayMerger Verification Design

#### Verification Approach

The merger works on the placed cards of a registry, so it is tested in `OverlayMergerTests.cs` through the builder on
small in-memory sources: the tests look at the merged cards and at the problems of the composition. The other
Composition units are real because the merger cannot be exercised without their output.

#### Test Environment

Standard xUnit v3 runner; in-memory files; no temporary files.

#### Acceptance Criteria

All tests in `OverlayMergerTests.cs` pass: the merged cards are exactly as documented, and every defect gives one
problem with the documented rule at the file of the overlay and the line of `extends`.

#### Test Scenarios

- **Merge**: title, summary, body, see-also entries, examples, and extension keys of an overlay reach the target; an
  empty body adds no separator and an overlay body on an empty card needs none.
- **Order**: two overlays of one source apply in ordinal file order and the last title wins; overlays of two sources
  apply in the registration order of their sources, not in the order of their names.
- **Targets**: an unknown card, a generated mount root, and an unknown bare id give `overlay-target`; a source that is
  absent or registered later gives `extends-target`; a bare id that a source and its import both have gives
  `ambiguous-reference` and a qualified one does not; a target that failed to load gives only its own `syntax` error.
- **Optional**: an optional overlay of an absent source is skipped with an informational problem; an optional overlay
  with a bare target that does not exist is still an error.
- **Scope**: a link in an overlay body resolves in the source of the overlay and `[[#name]]` finds an example below the
  target; a broken link is reported in the overlay file.
- **Arguments**: a null registry and a null problem collector throw.
