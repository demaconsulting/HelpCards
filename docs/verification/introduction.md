# Introduction

This document provides the verification design for HelpCards, a .NET library for authoring help
and knowledge content as Markdown cards with YAML front matter, embedded in assemblies and served
as command-line help for people and as searchable knowledge for AI agents.

## Purpose

The purpose of this document is to serve as the verification design entry point and document how
requirements will be tested across all software items in the HelpCards system. This
documentation enables formal review by mapping every requirement to named test scenarios, supports
compliance auditing by providing clear traceability from requirements through verification design
to tests, and ensures test completeness can be assessed without reading implementation code.

This document is intended for:

- Software developers implementing and maintaining tests
- Code reviewers validating test completeness against requirements
- Compliance auditors tracing requirements through verification design to tests
- Quality assurance teams validating test coverage and scenario adequacy

## Scope

This document covers the verification design for the HelpCards system and its
constituent software items, specifically:

- **HelpCards (System)** — The complete HelpCards .NET library system
- **Loading (Subsystem)** — Finds the files of a source, decodes front matter, and reads cards, overlays,
  translations, tokens, and bodies into definitions; its units are HelpSource, HelpFileProvider,
  FrontMatterDecoder, CardReader, TokenResolver, CultureSelector, and BodyParser
- **Composition (Subsystem)** — Registers the sources, mounts them, resolves imports and overlays, runs the one
  composition pipeline, orders siblings, validates every reference and name, and collects all problems; its units are
  HelpCatalogBuilder, SourceRegistry, Mounter, OverlayMerger, ReferenceValidator, SiblingOrder, and ProblemCollector
- **Catalog (Subsystem)** — The frozen data, the immutable model, filtered views, path and example lookup, and the
  problem model; its units are HelpCatalog, HelpCard, HelpProblem, CatalogData, CatalogScope, PathResolver, and
  ExampleResolver
- **Presentation (Subsystem)** — Renders cards, indexes, examples, search results, and card lists as text, Markdown,
  and JSON with three link styles; its units are HelpRenderOptions, LinkFormatter, TextRenderer, JsonDocumentWriter,
  TextWrapper, and HelpStrings
- **Testing (Subsystem)** — The helpers an adopter uses to test its own help: the validator with its options and rule
  interface, the parser for the command line of an example, the enumeration of examples, and the readers for the
  structure of a card body; its units are HelpCardValidator, HelpExamples, ExampleCommandLine, and HelpCardMarkdown

The following OTS items are also covered:

- **BuildMark** — build-notes documentation tool
- **FileAssert** — document assertion tool
- **Pandoc** — Markdown-to-HTML conversion tool
- **ReqStream** — requirements traceability tool
- **ReviewMark** — file review enforcement tool
- **SarifMark** — SARIF report conversion tool
- **SonarMark** — SonarCloud quality report tool
- **VersionMark** — tool-version documentation tool
- **WeasyPrint** — HTML-to-PDF conversion tool
- **xUnit** — unit-testing framework

This verification documentation covers the same software items as the design documentation.

Version applicability: This verification design applies to all versions of
HelpCards.

The following topics are explicitly excluded from this verification documentation:

- Build pipeline and CI/CD process testing
- Infrastructure and hosting environment testing

## Companion Artifact Structure

Each software item covered by this document has corresponding artifacts in parallel directory
trees. In-house items have artifacts in these parallel locations:

- Requirements: `docs/reqstream/{system}/.../{item}.yaml` (kebab-case)
- Design docs: `docs/design/{system}/.../{item}.md` (kebab-case)
- Verification design: `docs/verification/{system}/.../{item}.md` (kebab-case)
- Source code: `src/{System}/.../{Item}.cs` (PascalCase for C#)
- Tests: `test/{System}.Tests/.../{Item}Tests.cs` (PascalCase for C#)

OTS items have parallel artifacts in:

- Requirements: `docs/reqstream/ots/{ots-name}.yaml` (kebab-case)
- Verification: `docs/verification/ots/{ots-name}.md` (kebab-case)

Review-sets: defined in `.reviewmark.yaml`

## References

- HelpCards User Guide — the compiled User Guide document for this repository.
- HelpCards Repository — the HelpCards source repository hosted on
  GitHub.
