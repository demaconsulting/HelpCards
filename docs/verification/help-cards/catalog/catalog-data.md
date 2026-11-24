### CatalogData Verification Design

#### Verification Approach

The data is tested directly in `CatalogDataTests.cs` with the data that the real builder produces from the embedded
`simple` fixture. No mocks.

#### Test Environment

Standard xUnit v3 runner; the embedded fixtures.

#### Acceptance Criteria

All tests in `CatalogDataTests.cs` pass.

#### Test Scenarios

- **Content**: cards in depth-first order and the root ids; lookup by id ignoring case and null for an unknown id; the
  resolved ids of the see-also entries and of every link of a body.
- **Tags**: the distinct tags in ordinal order.
- **Settings**: tool name, tool version, and vocabulary as configured, and null when not configured.
- **Immutability**: every list refuses writes through the collection interface; the freeze helpers copy their input.
