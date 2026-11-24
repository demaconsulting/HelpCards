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

### Sources: Assembly, Directory, and Memory

**Tests**: `HelpCards_Sources_Assembly_ReadsEmbeddedCards`, `HelpCards_Sources_Directory_ReadsFilesOnDisk`,
`HelpCards_Sources_Memory_ReadsFiles`

Reads the same kind of cards from the embedded fixture of the test assembly, from a temporary directory, and
from in-memory text, and asserts that each yields its cards, titles, and example content without problems.

### Sources: Construction Performs No I/O

**Test**: `HelpCards_Sources_Construction_PerformsNoIo`

Creates a source over a counting provider and derives other sources from it with a name, tokens, and an
import, then asserts that the provider was never asked to list or read a file.

### Front Matter: Valid Subset

**Test**: `HelpCards_FrontMatter_ValidSubset_ReadsCard`

Reads a card that uses comments, single- and double-quoted values, a list, a list of maps with a literal
block for example content, and an extension key, and asserts every value.

### Front Matter: Unsupported Constructs and Unknown Keys

**Tests**: `HelpCards_FrontMatter_UnsupportedConstruct_ReportsFileAndLine`,
`HelpCards_FrontMatter_UnknownKey_ReportsKeyAndAllowedKeys`

Reads a card with a flow sequence and a card with a misspelled key, and asserts that the problems carry the
rule, the file, and the line, and that the key problem names the key and the allowed keys.

### Tokens: Replacement and Unknown Tokens

**Tests**: `HelpCards_Tokens_BuiltInAndSourceTokens_AreReplaced`, `HelpCards_Tokens_UnknownToken_ReportsFileAndLine`

Reads cards that use the built-in tokens and a token of the source, and a card with an unknown token on its
second body line; asserts the replaced text and the exact file and line of the problem.

### Cultures: Per-File Selection With Fallback

**Test**: `HelpCards_Cultures_Translation_IsSelectedPerFileWithFallback`

Reads a source for `de-DE` and for the neutral culture and asserts that the language translation is used for
the translated card only and that the neutral culture ignores translations.

### Links: Recognition and Typos

**Test**: `HelpCards_Links_OutsideCode_AreRecognizedAndTyposReported`

Reads a body with a local link, an external link, a link in a code span, and a typo, and asserts the links and
the line of the problem.

### Diagnostics: Fields and Collection of All Defects

**Tests**: `HelpCards_Diagnostics_Defects_AreReportedWithAllFields`,
`HelpCards_Diagnostics_SeveralDefectiveFiles_AreAllCollected`

Reads a source with a bad file name and asserts all six fields of the problem; reads a source with three
different defects in three files and one valid card and asserts that all defects are reported and the valid
card is read.

### Catalog: Tree, Aliases, Tags, Examples, and Links

**Tests**: `HelpCards_Catalog_SimpleFixture_ExposesTreeAliasesTagsExamplesAndLinks`,
`HelpCards_Catalog_Create_IsImmutableAndReadsNoFilesAfterwards`

Creates a catalog from the embedded `simple` fixture and asserts the cards in depth-first order, the children, the tags,
the see-also cards, the example kinds, and a body with plain-style links, a link to an absent library written as text,
and a code span that is not a link. A second test creates a catalog over a counting provider and proves that one listing
and one read per file happen during creation and none afterwards, even after views and bodies are used.

### Ordering: Children List, Order, and Name

**Test**: `HelpCards_Ordering_OrderFixture_FollowsChildrenOrderThenName`

Creates a catalog from the `order` fixture and asserts the roots and the children of one card in the order the
children list, the order values, and the names give.

### References: Forward and Broken

**Test**: `HelpCards_References_BrokenAndForwardReferences_AreHandled`

Creates a catalog whose first card links forward, and composes a source with a broken see-also entry, card link, and
example link; asserts the plain text of the forward link and the rule and line of each broken reference.

### Lookup: Paths and Examples

**Test**: `HelpCards_Lookup_PathsAndExamples_FindCardsAndExamples`

Finds a card by alias path, finds nothing for an unqualified id, lists the valid names of a failed path, and finds
examples by name and by card form.

### Views: Subsets and Hidden Targets

**Test**: `HelpCards_Views_FilterSourceWhereWithout_ShowSubsetsAndHideTargets`

Combines a tag view with an id view, asserts the lifted roots, that the catalog is unchanged, that a hidden card is
written as its title in another card, and that an unknown id is an argument error.

### Vocabulary: Tags and Expected Roots

**Test**: `HelpCards_Tags_VocabularyAndExpectedRoots_AreEnforced`

Creates a catalog with a closed tag vocabulary, a required tag, and expected roots that the source does not match, and
asserts the rules of all problems in display order.

### Diagnostics: Failure Model

**Test**: `HelpCards_Diagnostics_CreateFailure_ThrowsCatalogExceptionForDefectsAndArgumentExceptionForMisuse`

Creates a catalog from a source with two defects and asserts that the catalog exception carries both errors, and that
a null callback and a builder without a source are argument errors.

### Thread Safety and Determinism

**Tests**: `HelpCards_ThreadSafety_ParallelUse_IsConsistent`, `HelpCards_ThreadSafety_ParallelRendering_IsConsistent`,
`HelpCards_Determinism_CreateTwice_IsEqualRegardlessOfCurrentCulture`,
`HelpCards_Determinism_RenderTwice_IsEqualRegardlessOfCurrentCulture`

Fifty parallel workers share one catalog, make views with a counted predicate, and read bodies; every result is equal
and every call is counted. Forty parallel workers render the guide card as text and JSON and with a counted link
callback in a view; every output equals the expected one and the callback is called the same number of times by every
worker. Two creations and two renderings under the current cultures `tr-TR` and `de-DE` give equal results.

### Rendering: Text, Markdown, and JSON Golden Files

**Tests**: `HelpCards_Rendering_SimpleFixtureText_EqualsGolden`,
`HelpCards_Rendering_SimpleFixtureMarkdown_EqualsGolden`, `HelpCards_Rendering_SimpleFixtureJson_EqualsGolden`

Renders the guide card and the index of the embedded `simple` fixture as text, Markdown, and JSON, and compares each
with its golden file exactly. A golden file holds the output followed by one line feed.

### Rendering: Link Styles and Examples

**Tests**: `HelpCards_Rendering_LinkStyles_WriteTargetsInEachStyle`,
`HelpCards_Rendering_Examples_ArePrintedInFullOrAlone`

Formats the guide body in the plain, command, and Markdown styles and with a callback, and asserts one link of each.
Renders the examples document of the guide card in the command style and compares it with its golden file, and prints
one example of a card below it alone, asserting that the result is exactly its content.

### JSON Contract

**Test**: `HelpCards_JsonContract_Documents_AreOneAsciiLineWithoutNull`

Renders the index, a card, examples, one example, search results, and a card list as JSON; parses each, asserts the
contract version, the success flag, the kind, that the text contains no null and no character outside printable ASCII.

### No Dependencies

**Test**: `HelpCards_NoDependencies_ReferencedAssemblies_AreFrameworkOnly`

Reads the assemblies that the library assembly references and asserts that each is part of the framework.

### Cultures: Catalog

**Test**: `HelpCards_Cultures_Catalog_ExposesTranslatedCardsWithBodyCulture`

Creates the `i18n` fixture for `de-DE` and asserts the translated title and the culture of the body of three cards: a
region translation, a language translation, and a card without translation.

## Acceptance Criteria

A system-level test run passes when all scenarios above pass without error or exception beyond
those explicitly asserted. Any unexpected exception, wrong exception type, or wrong return value
constitutes a failure. The packed NuGet package is additionally checked by FileAssert to contain
the props and targets files under `build/`.
