### HelpRenderOptions Verification Design

#### Verification Approach

The options and the context are tested directly in `HelpRenderOptionsTests.cs`: the options by constructing them with
valid and invalid values, the `RenderContext` over catalogs from the embedded `simple` fixture and small in-memory
sources. No mocks.

#### Test Environment

Standard xUnit v3 runner; the embedded fixtures; in-memory sources.

#### Acceptance Criteria

All tests in `HelpRenderOptionsTests.cs` pass.

#### Test Scenarios

- **Defaults and storage**: every default value, and every setting read back unchanged.
- **Validation**: the width values 0, 20, 78, and 500 are accepted; 1, 19, 501, and negative values are argument errors;
  undefined enumeration values and a white-space command template are argument errors.
- **Context**: the tool name from the builder and from the first source, the version, wrapping width by format, and a
  null catalog.
- **Walks**: depth-first listing with depth and a depth limit; the descendants that have examples; mapping a card of
  another view to the card of the rendering view and rejecting a hidden card; finding an example in the subtree.
