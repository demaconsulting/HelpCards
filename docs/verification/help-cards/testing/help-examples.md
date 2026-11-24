### HelpExamples Verification Design

#### Verification Approach

The enumeration is tested directly in `HelpExamplesTests.cs` over the catalog of the embedded `simple` fixture and a
view of it. No mocks.

#### Test Environment

Standard xUnit v3 runner; the embedded fixtures.

#### Acceptance Criteria

All tests in `HelpExamplesTests.cs` pass.

#### Test Scenarios

- **Order**: every example is listed with its card in catalog order and in the order of the card.
- **Kinds**: one kind, two kinds, a kind with another case (not matched), and an empty list of kinds.
- **Views**: the examples of a hidden card are not listed.
- **Arguments**: a null catalog and a null kind throw when the method is called.
