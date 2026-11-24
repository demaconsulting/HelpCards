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
  resolve.
- **Example links**: `[[card#name]]` and `[[#name]]` find the owner in the subtree depth first; an example in a sibling
  tree is an example target problem.
- **External links**: a link to the registered source resolves like a local link, a wrong card is a link target
  problem, and a link to an absent source becomes display text with an informational problem.
- **See-also**: entries resolve in order without duplicates, an absent source is dropped with an informational problem,
  and an unknown entry is reported at its line.
- **Suppression**: links and see-also entries that name a file that failed to load give no problem.
- **Example names**: the second use in one scope is reported with the first location; the same name in different scopes
  is allowed.
- **Sibling names**: an alias equal to a sibling alias or name is reported at the alias line, an alias equal to the
  card's own name is reported, and equal names below different parents are allowed.
