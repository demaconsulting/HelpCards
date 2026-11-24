# Introduction

HelpCards turns Markdown files into help for people and knowledge for AI agents. You write one Markdown file per
topic, a *card*, with a small block of front matter. The package embeds the cards in your assembly, and the library
loads them into an immutable catalog that renders text, Markdown, or JSON.

The following diagram shows how four kinds of consumers use the library: a library that ships cards, a command-line
tool, an agent host, and a test project. The cards go in as Markdown files, the Loading, Composition, and Catalog
subsystems turn them into a catalog, and Presentation, Integration, and Testing build on that catalog.

![Architecture of HelpCards: consumers use six subsystems that turn Markdown cards into a catalog](architecture.svg)

## Purpose

This document is the user guide for HelpCards, a .NET library for authoring help and knowledge content as Markdown
cards with YAML front matter, embedded in assemblies and served as command-line help for people and as searchable
knowledge for AI agents. It explains how to write cards, package them, compose the cards of several libraries,
connect them to a command line or an agent, and test them.

## Scope

This user guide covers:

- Installation of the library and a first working example
- Writing cards: file layout, front matter, links, tokens, examples, and cultures
- Packaging and embedding the cards in an assembly
- Composing the cards of several libraries: mounts, imports, overlays, and collisions
- The command-line helper, its options, its exit codes, and its JSON output
- The agent knowledge tool, its arguments, its schema, and its output budgets
- The helpers that test your cards
- Versioning, compatibility, and security notes

The guide describes version 0.1 of HelpCards. It does not describe the internals of the library; the design
document does.

## References

- [REF-1] Continuous Compliance Methodology (<https://github.com/demaconsulting/ContinuousCompliance>)
