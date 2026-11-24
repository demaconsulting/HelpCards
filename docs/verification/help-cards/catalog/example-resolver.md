### ExampleResolver Verification Design

#### Verification Approach

The resolver is tested directly in `ExampleResolverTests.cs` with scopes over trees composed from in-memory files whose
cards hold command examples. No mocks.

#### Test Environment

Standard xUnit v3 runner; in-memory files.

#### Acceptance Criteria

All tests in `ExampleResolverTests.cs` pass.

#### Test Scenarios

- **Subtree search**: an own example found ignoring case; examples of children found in order; examples of other trees
  not found; the children function decides what is searched.
- **Whole-view search**: every match in depth-first order across two scopes that use the same name.
- **Limits**: the card form, the scope card, and both together; unknown cards give no result.
- **Hidden cards**: a hidden card contributes no example and a hidden card form gives no result.
