# Introduction

This document provides the detailed design for HelpCards, a .NET library for authoring help and
knowledge content as Markdown cards with YAML front matter, embedded in assemblies and served as
command-line help for people and as searchable knowledge for AI agents.

## Purpose

The purpose of this document is to serve as the design entry point and provide detailed design
specifications for the HelpCards system. This documentation enables formal code
review by providing implementation specifications, supports compliance auditing by maintaining
clear traceability from requirements through design to code, aids maintenance by documenting
system structure and interactions, and ensures quality assurance through detailed technical
specifications.

This document is intended for:

- Software developers implementing and maintaining the system
- Code reviewers validating implementation against design
- Compliance auditors tracing requirements through design to implementation
- Quality assurance teams validating system behavior

## Scope

This document covers the detailed design of the HelpCards system and its constituent
software items, specifically:

- **HelpCards (System)** — The complete HelpCards .NET library system
- **Loading (Subsystem)** — Finds the files of a source, decodes front matter, and reads cards, overlays,
  translations, tokens, and bodies into definitions; its units are HelpSource, HelpFileProvider,
  FrontMatterDecoder, CardReader, TokenResolver, CultureSelector, and BodyParser
- **Composition (Subsystem)** — Registers the single source, runs the one composition pipeline, orders siblings,
  validates every reference and name, and collects all problems; its units are HelpCatalogBuilder,
  ReferenceValidator, SiblingOrder, and ProblemCollector
- **Catalog (Subsystem)** — The frozen data, the immutable model, filtered views, path and example lookup, and the
  problem model; its units are HelpCatalog, HelpCard, HelpProblem, CatalogData, CatalogScope, PathResolver, and
  ExampleResolver

The following OTS items are also covered:

- **BuildMark** — build-notes documentation tool
- **FileAssert** — document assertion tool
- **Pandoc** — Markdown-to-HTML conversion tool
- **ReqStream** — requirements traceability tool
- **ReviewMark** — file review enforcement tool
- **SarifMark** — SARIF report conversion tool
- **SonarMark** — SonarCloud quality report tool
- **SysML2Tools** — architecture model lint and diagram rendering tool
- **VersionMark** — tool-version documentation tool
- **WeasyPrint** — HTML-to-PDF conversion tool
- **xUnit** — unit-testing framework

Version applicability: This design applies to all versions of HelpCards.

The following topics are explicitly excluded from this design documentation:

- External library internals and third-party OTS components
- Build pipeline configuration and CI/CD processes
- Deployment, packaging, and distribution mechanisms
- Infrastructure and hosting environment details
- Test projects and test infrastructure

## Software Structure

The software structure is modeled in SysML2 under `docs/sysml2/` and rendered to the
diagram below by SysML2Tools as part of the build pipeline. AI agents should query the
SysML2 model directly (see the `sysml2tools-query` skill) rather than parsing this
diagram or the prose below.

![Software Structure](SoftwareStructureView.svg)

This repository organizes the software into subsystems that each own a folder of units. The system
contains the Loading, Composition, Catalog, Presentation, and Testing subsystems; each unit is a
behavior-bearing class, or a tightly coupled set of classes, and pure data types are documented as
supporting types in the unit that owns them. Subsystem and unit boundaries follow the folder and
namespace structure of the source code.

The diagram below complements the structure view by showing the library in its run-time context:
how the calling application uses the library's subsystems and units, and which inputs, outputs,
and dependencies they involve. It is a placeholder: replace its TODO boxes with the real
application, subsystems, units, dependencies, inputs, and outputs of your library.

![Placeholder architecture diagram: replace the TODO boxes for your application, library subsystems, support units, dependencies, inputs, and outputs with the real ones](architecture.svg)

## Folder Layout

The source code folder structure mirrors the software structure organization, with file paths
and descriptions as follows:

```text
src/DemaConsulting.HelpCards/
├── build/                      — Packaged MSBuild props and targets that embed help cards
├── Loading/                    — Loading subsystem units
├── Composition/                — Composition subsystem units
├── Catalog/                    — Catalog subsystem units
├── Presentation/               — Presentation subsystem units
└── Testing/                    — Testing subsystem units
```

Each subsystem folder holds one file per unit class, named after the class. Test projects mirror the
source folders; they are listed in the verification documentation, not here.

## Document Conventions

Throughout this document:

- Class names, method names, property names, and file names appear in `monospace` font.
- The word **shall** denotes a design constraint that the implementation must satisfy.
- Section headings within each unit chapter follow a consistent structure: overview, data model,
  methods/algorithms, and interactions with other units.
- Text tables are used in preference to diagrams, which may not render in all PDF viewers.

## Companion Artifact Structure

Each software item has corresponding artifacts in parallel directory trees:

- Requirements: `docs/reqstream/{system}/.../{item}.yaml` (kebab-case)
- Design docs: `docs/design/{system}/.../{item}.md` (kebab-case)
- Verification design: `docs/verification/{system}/.../{item}.md` (kebab-case)
- Source code: `src/{System}/.../{Item}.cs` (PascalCase for C#)
- Tests: `test/{System}.Tests/.../{Item}Tests.cs` (PascalCase for C#)
- SysML2 model: `docs/sysml2/model/{system}/.../{item}.sysml` (kebab-case)
- Review-sets: defined in `.reviewmark.yaml`

## References

- HelpCards User Guide — the compiled User Guide document for this repository.
- HelpCards Repository — the HelpCards source repository hosted on
  GitHub.
