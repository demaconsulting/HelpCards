# System Design

This document provides the system-level design for HelpCards.

![HelpCards Structure](HelpCardsView.svg)

## Architecture

HelpCards is a .NET library for authoring help and knowledge content as Markdown cards with YAML
front matter, embedded in assemblies and served as command-line help for people and as searchable
knowledge for AI agents. It has no dependencies beyond the .NET Base Class Library, so every
adopting tool or library stays dependency-free.

A *card* is a Markdown file with restricted-subset front matter. A *source* is the set of cards of
one library or tool, found as embedded resources of an assembly, as files of a directory, or as
in-memory text. A *catalog* is the immutable, thread-safe tree of cards built from a source. The
system is organized into five subsystems that follow the life of a card from file to output:

- **Loading** finds the files of a source, decodes the front matter, reads cards, overlays,
  translations, and the source description, substitutes tokens, selects the culture variant, and
  parses bodies into text and link parts. Its public face is `HelpSource`.
- **Composition** registers the source, runs the one composition pipeline, orders siblings,
  validates every reference, and collects all problems. Its public face is `HelpCatalogBuilder`.
- **Catalog** holds the frozen data, the immutable model (`HelpCard`, `HelpExample`, and the
  result types), filtered views, path and example lookup, and the problem model. Its public face
  is `HelpCatalog`.
- **Presentation** renders cards, indexes, examples, search results, and card lists as text,
  Markdown, or JSON with three link styles. Its public face is `HelpRenderOptions`.
- **Testing** gives adopters a collect-all validator and example helpers for their own test
  suites. It has no test-framework reference.

The subsystems depend on each other in one direction for building a catalog: Composition uses
Loading and produces the data that Catalog freezes; Presentation reads the Catalog model; Testing
uses all of them through their public faces. Packaged MSBuild files (see *Resource Embedding*
below) are part of the NuGet package and run in the consuming project's build, not at run time.

## External Interfaces

| Interface | Direction | Format | Constraints |
| --- | --- | --- | --- |
| `HelpSource` factories | Inbound | Resource names, directory, or dictionary | UTF-8 text; creation does no I/O |
| `HelpCatalog.Create(configure)` | Inbound | Builder callback | Throws `HelpCatalogException` for defects |
| `HelpCatalog` lookup, views, `Render*` | Outbound | Immutable model and strings | Thread-safe; LF only |
| `HelpRenderOptions` | Inbound | Options object | `Width` 0 or 20 to 500 |
| `HelpCardValidator` and helpers | Outbound | Problem lists and values | No test-framework reference |
| Packaged props and targets | Inbound | MSBuild properties | `HelpCardsEnabled`, `HelpCardsDirectory` |

The public surface consists of the types named above and their members, plus the card, example,
link, problem, and result types they return; `HelpCardValidator` and helpers stand for the validator,
`HelpValidationOptions`, `IHelpCardRule`, `HelpExamples`, `ExampleCommandLine`, and `HelpCardMarkdown`.
Everything else is `internal`. The signature closure of `HelpSource` and `HelpCatalogBuilder`
contains only Base Class Library types.

### Resource Embedding

The package ships `build\DemaConsulting.HelpCards.props` and `build\DemaConsulting.HelpCards.targets`
(not `buildTransitive`, so only a project that references HelpCards directly embeds cards). The props
file imports early and only defaults `HelpCardsEnabled` to `true`. The targets file imports after the
project body, so a `HelpCardsDirectory` or `HelpCardsPrefix` override in the project is honored; it
defaults the directory to `Help` and the prefix to `{AssemblyName}.Help` and embeds every file below the
directory with the logical name `{prefix}/{relative path}`. MSBuild writes `RecursiveDir` with the
platform separator, so the loader converts `\` to `/` when it lists resources.

## Dependencies

HelpCards has zero runtime NuGet dependencies — it is implemented exclusively
against the .NET Base Class Library (JSON output uses `System.Text.Json`, which ships with every
supported runtime). The following OTS items are used for building and verifying
this system (not consumed at runtime); see *OTS Integration Design* (`docs/design/ots.md`) and
each item's dedicated design document for details:

- **BuildMark** — generates build-notes documentation; see *BuildMark Design*
- **FileAssert** — validates generated documents and the packed build files against acceptance
  criteria; see *FileAssert Design*
- **Pandoc** — converts Markdown documentation to HTML; see *Pandoc Design*
- **ReqStream** — enforces requirements-to-test traceability; see *ReqStream Design*
- **ReviewMark** — enforces file review coverage and currency; see *ReviewMark Design*
- **SarifMark** — converts CodeQL SARIF results to markdown; see *SarifMark Design*
- **SonarMark** — generates SonarCloud quality reports; see *SonarMark Design*
- **VersionMark** — captures and publishes tool-version information; see *VersionMark Design*
- **WeasyPrint** — converts HTML documentation to PDF; see *WeasyPrint Design*
- **xUnit** — executes unit and integration tests; see *xUnit Design*

## Risk Control Measures

N/A - HelpCards has no safety-critical functionality
requiring risk control measures (IEC 62304 §5.3.3).

## Data Flow

A catalog is built by one pipeline that runs inside `HelpCatalog.Create`. Errors are collected, the
build fails after the last step if any error exists, and each step still runs on the data that
survived earlier steps (a file with a syntax error contributes nothing, and references to its id are
not reported):

1. **Validate builder input**: names, culture, and the number of sources.
2. **Read files**: list the files of the source in ordinal order, read `_source.md` and its
   translations, and group files into cards, overlays, translations, and data files. Names that fit
   no pattern are rejected.
3. **Read cards**: choose the culture variant per file, decode the front matter, build the card
   definition, substitute tokens, and parse the body into text and link parts.
4. **Register cards**: compute ids, detect duplicates, and order siblings.
5. **Check the tree**: parent exists, `children` entries are unique and exist, example kinds and
   files are valid, tags satisfy the optional vocabulary.
6. **Apply overlays**: reserved; a catalog currently holds one source and does not merge overlays.
7. **Resolve references** in a final pass over the final tree: links, see-also entries, and examples.
8. **Check uniqueness**: example names, expected roots, and sibling names and aliases.
9. **Freeze**: build the immutable catalog data. Warnings are produced only by the validator.

Rendering is a pure function of the immutable catalog and the render options; no rendered output is
cached. A catalog never reads files after `Create` returns.

## Design Constraints

- **Dependency-free**: only Base Class Library APIs are used, and a test proves that the library
  references no assemblies beyond the framework.
- **Determinism**: all output has LF line endings, no ANSI sequences, and is byte-identical on every
  machine; comparisons of names are ordinal or ordinal-ignore-case, never culture-sensitive.
- **Thread safety**: every public member of the catalog and result types is safe for concurrent use;
  caller callbacks run on the caller's thread.
- **Failure model**: card defects throw `HelpCatalogException` carrying every problem; bad arguments
  throw `ArgumentException` or `ArgumentNullException`; the library never throws
  `InvalidOperationException` directly.
- **Single source**: a catalog currently holds exactly one source with no imports; adding a second
  source or a source with imports is rejected with `ArgumentException`. Composition of several
  sources, mounting, overlays, search, suggestions, the command-line helper, and the agent tool are
  not part of the current system.
- **Compliance**: All functionality must be traceable to requirements.
- **Quality**: Zero warnings, complete documentation of every member, and tests for every requirement.

### Platform Support

The library targets the following frameworks, enabling broad compatibility across supported .NET
runtimes:

| Target Framework | Runtime / Environment |
| --- | --- |
| `net8.0` | .NET 8 LTS |
| `net9.0` | .NET 9 |
| `net10.0` | .NET 10 LTS |
| `net11.0` | .NET 11 |

The library is supported on the following operating systems:

- **Windows** — primary developer and CI platform
- **Linux** — CI/CD and containerized environments
- **macOS** — developer workstations using Apple platforms

Portability is achieved by restricting the implementation exclusively to Base Class Library (BCL)
APIs available across all target frameworks. No platform-specific native interop, OS-specific
APIs, or framework-version-specific features are used.

### Integration Patterns

- **NuGet Packaging**: Standard .NET library packaging; the package also carries the MSBuild files
  that embed help cards in referencing projects
- **CI/CD Integration**: Automated build, test, and quality validation
- **Requirements Traceability**: All features linked to passing tests
- **Review Management**: Systematic file review using ReviewMark patterns
