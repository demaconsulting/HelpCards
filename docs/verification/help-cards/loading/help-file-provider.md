### HelpFileProvider Verification Design

#### Verification Approach

Each implementation is tested directly through `ListFiles` and `ReadText`: `MemoryFileProvider` with
dictionaries, `AssemblyFileProvider` with the embedded fixtures of the test assembly and of the source-less
default-embedding project, and `DirectoryFileProvider` with a temporary directory. No mocking is needed.

#### Test Environment

Standard xUnit v3 runner; a unique temporary directory per test that is deleted afterwards; the fixtures
embedded by the packaged MSBuild targets.

#### Acceptance Criteria

All tests in `HelpFileProviderTests.cs` pass: listings are relative, `/`-separated, and ordinal; text is
strict UTF-8 without a byte order mark; unlisted paths and paths outside the directory are refused.

#### Test Scenarios

- **Ordinal listing**: unsorted memory input, resources below an explicit prefix, nested directory files.
- **Assembly resources**: explicit prefix, prefix with a trailing slash, default prefix from the assembly
  name, unknown prefix gives an empty list, embedded text is the authored text, unknown path is refused.
- **UTF-8 text**: byte order mark character removed from memory text, byte order mark bytes removed from a
  resource and from a file, invalid bytes refused in a resource and in a file, a backslash in a resource name
  converted to a slash.
- **Isolation**: the memory provider copies its input and refuses unknown paths; the assembly provider refuses a path
  that is not a resource; a missing directory raises `DirectoryNotFoundException`; a parent-relative path and an
  absolute path elsewhere are refused.
