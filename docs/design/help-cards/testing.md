## Testing Subsystem Design

![Testing Structure](TestingView.svg)

### Overview

The Testing subsystem gives an adopter the helpers to test its own help in its own test project: a validator that
returns the problems of a source or catalog, a parser for the command line of an example, an enumeration of examples,
and readers for the structure of a card body. It references no test framework and starts no process; a product asserts
on the results with whatever framework it uses. Every member is a pure function of its arguments and the files of
the sources it is given.

The subsystem contains these units:

- **HelpCardValidator** (public, static) validates a source, the configuration of a catalog, or a catalog, and
  asserts on the result. It is a partial class: one part for the entry points and the ordering, one for the quality
  rules, and one for the comparison of embedded resources with a directory. With it go the public
  `HelpValidationOptions`, which holds the limits and switches, and the public interface `IHelpCardRule`, which a
  product implements to add rules of its own.
- **HelpExamples** (public, static) enumerates the examples of a catalog with their cards.
- **ExampleCommandLine** (public, static) reads the one-line content of a command example into arguments.
- **HelpCardMarkdown** (public, static) reads the headings, sections, and bullets of a card body, and classifies the
  lines of a body for the validator. With it go the internal `MarkdownLine` and `MarkdownLineKind`.

### Interfaces

| Interface | Direction | Purpose |
| --- | --- | --- |
| `Validate`, `AssertValid` | Inbound (adopters) | Get the problems of a source, or fail a test |
| `CompareToDirectory` | Inbound (adopters) | Compare embedded resources with a directory |
| `HelpValidationOptions`, `IHelpCardRule` | Inbound (adopters) | Set limits, add product rules |
| `HelpExamples.Enumerate` | Inbound (adopters) | List examples with their cards |
| `ExampleCommandLine.TryParse` | Inbound (adopters) | Read the arguments of a command |
| `HelpCardMarkdown` | Inbound (adopters) | Read headings, sections, and bullets |
| `HelpCatalogBuilder.Compose`, `Sources` | Outbound (Composition) | Compose without an exception |
| `HelpCatalog.FromData`, `Render` | Outbound (Catalog) | Read the cards as adopters see them |
| `BodyParser` fences, `FindNames` | Outbound (Loading) | Agree with the loader on code and tokens |

The Testing subsystem depends on the Loading, Composition, Catalog, and Presentation subsystems (the last one only
through `HelpCatalog.Render`) and on the Base Class Library. No other subsystem depends on it.

### Design

**Validate without an exception.** The validator creates a builder, lets the caller configure it, and calls the same
`Compose` method that `HelpCatalog.Create` uses, which returns the errors instead of throwing. When there are errors
there is no catalog to inspect, so only the errors are returned, already in display order. Otherwise the data is
wrapped in a catalog and every visible card is checked.

**Quality rules.** Each rule is a small private method that adds warnings: the summary against its limit (shorter
for a root), the length of the text rendering against the card limit (the index limit for a root with children),
the lines of the body for reserved headings, fences without a language, an unclosed fence, and long lines, and the
command examples against `ExampleCommandLine` with the tool name of the catalog. The line numbers are the line of
the first body line in the file that supplied the body plus the offset in the body, which holds because links never
change the number of lines. Front-matter findings (summary, size, command) name the file and no line. The rule for
unused tokens needs the tokens of the sources, which a catalog does not keep, so it runs only when the validator
holds the source or the builder.

**Code and tokens like the loader.** `HelpCardMarkdown.Scan` classifies every line with the fence rule of
`BodyParser` and `TokenResolver.FindNames` finds the tokens with the pattern of the resolver, so the validator never
disagrees with the loader about what is code or what is a token.

**Ordering.** All problems are sorted by severity (errors first), then by the registration index of the source, the
file (ordinal), the line, and the rule (ordinal); the sort is stable.

**Product rules and sources.** The rules of `HelpValidationOptions.Rules` run for every card in catalog order; their
problems join the result with the severity they chose. `RequireExternalSources` turns each informational note about
an absent external source into an error with the same file and line.

**Command lines.** The grammar of `ExampleCommandLine` is a single left-to-right scan: spaces separate words, quotes
group, and every character a shell would interpret is an error that names the character and the column. The first
word must equal the tool name; the rest are the arguments.

**Failure model.** Defects of cards are returned, never thrown, except by `AssertValid`, which throws one
`HelpCatalogException`. A null argument throws `ArgumentNullException`; an invalid argument, a builder without a
source, a source that imports another source (this release composes one source), and a directory that does not exist
throw `ArgumentException`. An exception thrown by a rule of a product is a defect of that rule and is not caught.

The unit design documents describe each unit in detail; the verification design describes how the subsystem is
tested with the embedded `simple` fixture and in-memory sources.
