# Introduction

The following diagram is a placeholder: replace its TODO boxes to show how your application calls the
library's subsystems, which inputs it takes, and which outputs it produces.

![Placeholder architecture diagram: replace the TODO boxes for your application, library subsystems, support units, dependencies, inputs, and outputs with the real ones](architecture.svg)

## Purpose

This document is the user guide for HelpCards, a .NET library for authoring help and knowledge
content as Markdown cards with YAML front matter, embedded in assemblies and served as
command-line help for people and as searchable knowledge for AI agents.

## Scope

This user guide covers:

- Installation of the library
- Basic usage and examples
- API reference

# Continuous Compliance

This repository follows the
[Continuous Compliance](https://github.com/demaconsulting/ContinuousCompliance) methodology, which ensures
compliance evidence is generated automatically on every CI run.

## Key Practices

- **Requirements Traceability**: Every requirement is linked to passing tests, and a trace matrix is
  auto-generated on each release
- **Linting Enforcement**: LintMark, TextLint, and yamllint are enforced before any build proceeds
- **Automated Audit Documentation**: Each release ships with generated requirements, justifications,
  trace matrix, and quality reports
- **CodeQL and SonarCloud**: Security and quality analysis runs on every build

# Installation

Install the library using the .NET CLI:

```bash
dotnet add package DemaConsulting.HelpCards
```

The package targets .NET 8, 9, 10, and 11 and has no dependencies beyond the .NET Base Class Library.

# Usage

## Basic Usage

Put Markdown cards in a `Help` folder of your project. The package embeds every file of that folder in
your assembly, so the cards travel with it. Then load them as a source and build a catalog:

```csharp
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;

var catalog = HelpCatalog.Create(builder => builder
    .AddSource(HelpSource.FromAssembly(typeof(Program).Assembly)));
var card = catalog.Find("getting-started");
Console.WriteLine(card is null ? catalog.RenderIndex() : catalog.Render(card));
```

## Embedding Options

Two MSBuild properties let a project change where the cards come from:

- `HelpCardsDirectory` is the folder below the project directory that is embedded (default `Help`).
- `HelpCardsPrefix` is the resource name prefix (default `{AssemblyName}.Help`).

Set `HelpCardsEnabled` to `false` to switch embedding off.

# References

- [REF-1] Continuous Compliance Methodology (<https://github.com/demaconsulting/ContinuousCompliance>)
