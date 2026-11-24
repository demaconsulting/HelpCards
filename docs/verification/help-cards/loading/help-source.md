### HelpSource Verification Design

#### Verification Approach

`HelpSource` is tested through its public factories and `With...` methods, reading the internal properties
that expose its provider, name, tokens, and imports (the library grants its test project access). Real
providers are used (assembly resources of the test projects, a temporary directory, in-memory files); a
counting provider proves that no call is made during construction.

#### Test Environment

Standard xUnit v3 runner. The assembly case uses the source-less `EmbeddingDefault` project that the test
project references, and a temporary directory that the test removes.

#### Acceptance Criteria

All tests in `HelpSourceTests.cs` pass: argument defects throw the documented exception types, derived sources
leave the original unchanged, and no provider call happens at construction.

#### Test Scenarios

- **Factories**: null assembly, empty prefix, default prefix finds the embedded files, empty directory,
  existing directory reads files, null dictionary, duplicate paths after separator normalization, null text,
  and the name given at creation.
- **Path normalization**: a backslash path is listed with a forward slash.
- **Immutability**: changing the caller's dictionary does not change the source; `WithName`, `WithTokens`, and
  `Imports` return new sources and keep the original; tokens replace earlier tokens; imports keep order.
- **Validation**: empty name, invalid token name, null token value, null tokens, import of itself, import of
  null.
- **No I/O**: every factory and `With` method makes zero list and read calls; a missing directory does not
  fail at creation.
