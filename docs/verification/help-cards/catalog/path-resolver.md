### PathResolver Verification Design

#### Verification Approach

The resolver is tested directly in `PathResolverTests.cs` with scopes over a small tree composed from in-memory files,
including aliases and a translation with a decomposed alias. No mocks.

#### Test Environment

Standard xUnit v3 runner; in-memory files; the `de` culture for the alias scenario.

#### Acceptance Criteria

All tests in `PathResolverTests.cs` pass.

#### Test Scenarios

- **Path forms**: slashes, spaces, tabs, repeated and leading separators, names and aliases, and mixed case.
- **Failures**: an unknown segment names its parent and the valid names; an empty path lists the roots.
- **Suggestions**: a misspelled segment gets the closest name at its level; a far segment, an empty path, and a
  resolved path get none.
- **Hidden cards**: a hidden target is reported like an unknown segment with the visible names at its level; a hidden
  parent is walked through; a hidden nested target names its real parent; a failure below a hidden parent names the
  nearest visible ancestor.
- **Normalization**: an alias written with a combining accent is found with the composed and the decomposed form.
- **Arguments**: null arguments throw.
