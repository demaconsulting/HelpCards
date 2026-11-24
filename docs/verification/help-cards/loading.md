## Loading Subsystem Verification Design

This document describes how the Loading subsystem is verified.

### Verification Approach

The subsystem is verified by integration tests in `LoadingTests.cs` that use the Loading units together the way
Composition does: a `HelpSource` over each kind of provider is read by `CardReader`, which uses the decoder,
the token resolver, the culture selector, and the body parser. The only dependency outside the subsystem is the
problem model of the Catalog subsystem, which is used as it is. Nothing is mocked except the file provider:
a counting provider (in the test support folder) wraps in-memory files so that tests can prove how often files
are listed and read and can make a read or a listing fail on purpose.

### Test Environment

- xUnit v3 under the .NET SDK, run by `build.ps1` and CI on every supported target framework.
- The assembly source test reads the `embedding` fixture, which the test project embeds with the packaged
  MSBuild targets (`HelpCardsDirectory` set to `Fixtures`).
- The directory source test writes files to a unique temporary directory that the test deletes.
- Error sources are built in memory; no malformed file is checked in.

### Acceptance Criteria

The subsystem passes when every scenario below passes on every target framework. A problem must carry the
expected rule, file, and line; a source must give the same definitions whatever its kind; and the counting
provider must show no calls before reading, one listing, and one read per file used.

### Test Scenarios

- **Assembly source reads embedded cards**: the embedded fixture yields its two cards, no problems, and the
  example read from its data file in a subfolder.
- **Directory source equals memory source**: the same files on disk and in memory give the same ids, titles,
  cultures, example contents, and bodies.
- **Memory source reads cards, links, tokens, and examples together**: a small source with a link, a token,
  an example file, and a source description.
- **Source construction performs no I/O until read**: creating a source and applying every `With` method
  makes no provider call; reading lists once and reads each used file once.
- **Problems are collected across files and units**: a bad file name, a syntax error, an unknown key, an
  unknown token, a link typo, and a missing example file are all reported, and the files whose structure is
  invalid are listed as failed.
- **Cultures select the translation per file**: one card is translated and one falls back to neutral.
- **Imported tokens are in scope**: a card of an importing source uses a token that only the import defines.
- **Body links are recognized outside code only**: a link, a link in a span, a link in a fence, and a typo.
- **Front-matter error names file and line**: a duplicate key in a nested file.
- **Read failure is reported and other files are read**: a failing read becomes a `source-read` problem.
- **Reading is deterministic**: two reads give identical cards and problems in the same order.
- **Body file is recorded**: the file that supplied the body is the neutral file, or the selected translation file.
