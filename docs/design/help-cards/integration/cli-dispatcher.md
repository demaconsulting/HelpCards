### CliDispatcher Design

![Integration Structure](IntegrationView.svg)

#### Purpose

`CliDispatcher` runs one help command. It decides in which order things happen, selects the answer of the mode, and
writes it to the right writer with the right exit code.

#### Data Model

A dispatcher holds the options of the helper and the command template, and nothing else, so one dispatcher serves any
number of commands, including concurrent ones. The writers are read from the options on each command. The class is
partial: `CliDispatcher.cs` holds the flow, the writing, the width, and the creation of the catalog;
`CliDispatcher.Modes.cs` holds the answers of the modes; `CliDispatcher.Lookup.cs` holds the lookups and failures for
sources, tags, cards, and examples. `CliOutcome` is the text or the `CliFailure` (code, message, hint, candidates) of a
mode.

#### Key Methods

- **`Run(arguments)`** parses the arguments; a usage error is reported before any catalog exists, as JSON when the
  arguments ask for JSON. It creates the render options (the command link style, the command template, and the width),
  offers a card command to the fallback resolver, and then calls the catalog factory with the culture of the request. The
  fallback is offered for plain card commands only: `--examples` and `--example` with a subject that only the tool owns
  are answered by the catalog as not found. A
  `HelpCatalogException` becomes exit code 70 with `Help is unavailable: {first problem}`; so does a factory that
  returns nothing. The mode then selects the answer and the outcome is written.
- **Index** writes, in text, the usage lines under `Usage:`, the preamble, each section as its heading and indented
  lines, `Help topics:` with the index lines of the roots (the whole tree for `--all`), and the footer, separated by
  blank lines; in JSON it is the index document with the usage lines.
- **Card, examples, example** resolve the path with `HelpCatalog.Resolve`; an unknown card is `not-found` with the
  closest name, the names at that level, and the command for their summaries (built from the command template, also at
  the first level). An example name is searched in the card
  and below it with `FindExamples`; none is `not-found` with the example names, several is `ambiguous` with the
  qualified names.
- **Search and tags** validate the sources and tags against the catalog (an unknown one is `not-found` with the known
  ones), then call `Search` or filter by tag; the sources restrict the view first.
- **Width** is the width of the command, else the default of the options; `auto` is the console width, from the provider
  of the options or from the console when the helper writes to it and the output is not redirected, clamped to 40 to
  120, and 78 without one.

#### Error Handling

Failures are values (`CliFailure`) with a code; `usage-error`, `not-found`, and `ambiguous` exit with 2 and
`catalog-error` with 70. Text and Markdown errors go to the error writer as `Error: {message}` and an optional second
line; JSON errors are the error document on the output. All writing uses a single line feed at the end and line feeds
only inside. Null arguments throw `ArgumentNullException`.

#### Dependencies

`CliArgumentParser`, `HelpCatalog` (lookup, views, search, rendering), `JsonDocumentWriter` for the error document, and
`HelpRenderOptions`.

#### Callers

`HelpCli`.
