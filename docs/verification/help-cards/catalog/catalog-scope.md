### CatalogScope Verification Design

#### Verification Approach

The scope is tested directly in `CatalogScopeTests.cs` with the data of a small tree composed from in-memory files
(`a` with `a/b` and `a/c`, and `d`). No mocks.

#### Test Environment

Standard xUnit v3 runner; in-memory files.

#### Acceptance Criteria

All tests in `CatalogScopeTests.cs` pass.

#### Test Scenarios

- **Everything**: the scope of everything shows every card with the real parents and children.
- **Lifting**: hiding a parent makes its children roots.
- **Intersection and immutability**: restricting twice shows the intersection and leaves the earlier scopes unchanged.
- **Evaluation**: a restriction runs once per visible card in depth-first order and never for a hidden card.
- **Arguments**: null arguments throw.
