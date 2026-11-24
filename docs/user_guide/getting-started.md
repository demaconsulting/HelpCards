# Installation and Quick Start

## Installation

Install the library using the .NET CLI:

```bash
dotnet add package DemaConsulting.HelpCards
```

The package targets .NET 8, 9, 10, and 11 and has no dependencies beyond the .NET Base Class Library. Besides the
assembly it contains two MSBuild files that embed your cards; the chapter *Packaging and Embedding* describes them.

## A First Card

Create a `Help` folder in your project. Each Markdown file in it is one card. This example has a root card and one
child card, and a `_source.md` file that names the source (every source needs a name: the `name:` line of `_source.md`,
or `HelpSource.WithName`; without one the catalog fails with the rule `source-invalid`):

```text
MyLib/
├── MyLib.csproj
└── Help/
    ├── _source.md
    ├── getting-started.md
    └── getting-started/
        └── install.md
```

The `_source.md` file describes the source as a whole:

```markdown
---
name: demo
title: MyLib
summary: Help cards of MyLib.
---
MyLib reads data files and writes reports.
```

The root card has a title, a summary, a tag, and one example:

```markdown
---
title: Getting started
summary: Install MyLib and run it for the first time.
tags:
  - tutorial
examples:
  - name: load
    kind: command
    language: shell
    content: '{{tool}} load data.csv'
    description: Load a data file.
---
Install the package first, see [[getting-started/install]], then run [[#load]].
```

The child card is the file `getting-started/install.md`:

```markdown
---
title: Install
summary: Install the package.
---
Run `dotnet add package MyLib`.
```

## Loading and Rendering

A `HelpSource` points at the cards. Creating one reads nothing; the catalog reads the files when it is built:

```csharp
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Loading;

var catalog = HelpCatalog.Create(builder => builder
    .AddSource(HelpSource.FromAssembly(typeof(Program).Assembly)));

HelpCard? card = catalog.Find("getting-started");
Console.WriteLine(catalog.Render(card!));
```

The default rendering is plain text. The output of this example is:

```text
# Getting started

Install MyLib and run it for the first time.
Tags: tutorial

Install the package first, see the card `getting-started/install`, then run the example `load`.

## Cards

- getting-started/install
  Install the package.

## Examples

- load [command]: Load a data file.
```

`catalog.RenderIndex()` lists every card with its summary. The overloads of `Render` take a `HelpRenderOptions` to
choose Markdown or JSON, the link style, and the wrapping width; see the chapter *Command-Line Integration* for the
JSON documents.

If any card is defective, `HelpCatalog.Create` throws a `HelpCatalogException` that lists the problems with their file
and line. The checks run in stages, so fixing the listed errors can reveal more; see the chapter *Composing Sources*
for the problem model.

## Where to Go Next

- To write richer cards, read *Writing Cards*.
- To ship the cards of a library, read *Packaging and Embedding*.
- To add `tool help` to a command-line tool, read *Command-Line Integration*.
- To answer questions of an AI agent, read *Agent Knowledge Tool*.
- To prove that the cards are valid in your tests, read *Testing Your Help*.

## API Reference

The package ships a Markdown API reference in its `api/` folder, generated from the XML doc comments on every build
and organized for gradual disclosure: `api/api.md` is the index (one row per namespace), each namespace has a page,
each type has a page, and each member has its own page. Read the namespace page first: it opens with an overview, the
common usage steps and a complete example, then follow the links to the types and members you need. The
`DemaConsulting.HelpCards.Integration` page, for example, shows how to wire `HelpCli` into a command-line tool and how
to expose the cards to an AI agent, and the `DemaConsulting.HelpCards.Loading` page shows how a library exposes its
embedded cards as a `HelpSource` and is also the complete card-format reference (front-matter keys, links, examples,
tokens, cultures, the hierarchy rule, and the `HelpCardsEnabled`, `HelpCardsDirectory` and `HelpCardsPrefix` MSBuild
properties). The remarks of `HelpProblem` list every rule id once.

To read it from a restored package, open the HelpCards package folder in the NuGet global-packages cache and read
`api/api.md`, or unzip the `.nupkg`. When building this
repository, the same files are written to the `api/` folder of the project under `src/` (ignored by git).
