# System Verification Design

This document describes the system-level verification strategy for HelpCards.

## Verification Approach

The HelpCards system is verified through system-level integration tests that
exercise the library as a whole from the perspective of a consumer. Tests use the packaged
MSBuild files exactly as an adopting project would, build catalogs from sources, and assert on
observable outputs, without relying on knowledge of internal implementation details. No mocking
or stubbing is required at the system level — the entire integrated system is exercised as it
would be used by a real caller.

System tests reside in `HelpCardsTests.cs` within the
`DemaConsulting.HelpCards.Tests` project. Fixture sources are embedded in that project through the
packaged targets with `HelpCardsDirectory` set to `Fixtures`, so every fixture is loaded the way an
adopter's embedded cards are loaded.

## Test Environment

- **Framework**: xUnit v3 running under the .NET SDK
- **Execution**: `dotnet test` invoked by `build.ps1` and the CI pipeline
- **Dependencies**: No external services, databases, or network access required
- **Projects**: the test project imports the packaged props and targets; the source-less project
  `DemaConsulting.HelpCards.Tests.EmbeddingDefault` keeps every embedding default and is loaded by
  assembly name
- **Isolation**: Each test method builds its own inputs; no shared mutable state between tests

## External Interface Simulation

The system has no external interfaces requiring simulation. It is a pure in-process .NET library
with no network calls or platform services. System tests call the public API directly
with controlled inputs and verify returned values and thrown exceptions. File system access is
limited to temporary directories created by the tests themselves.

## System-Level Test Scenarios

### Embedding: Default Directory Uses the Assembly Name Prefix

**Test**: `HelpCards_Embedding_DefaultDirectory_UsesAssemblyNamePrefix`

Loads the source-less project that keeps every default and lists its embedded resources. Asserts
that every file of its `Help` folder is embedded under the assembly name followed by `.Help/`.

### Embedding: Overridden Directory Embeds Every File

**Test**: `HelpCards_Embedding_OverriddenDirectory_EmbedsEveryFileUnderPrefix`

Lists the resources of the test project, which overrides `HelpCardsDirectory`. Asserts that the
files of the embedding fixture, including a nested folder and a non-Markdown data file, are embedded
with `/` separators after the loader's separator conversion.

### Embedding: Embedded Resource Reads Authored Content

**Test**: `HelpCards_Embedding_EmbeddedResource_ReadsAuthoredContent`

Reads one embedded card as UTF-8 text and asserts that it holds the authored content.

## Acceptance Criteria

A system-level test run passes when all scenarios above pass without error or exception beyond
those explicitly asserted. Any unexpected exception, wrong exception type, or wrong return value
constitutes a failure. The packed NuGet package is additionally checked by FileAssert to contain
the props and targets files under `build/`.
