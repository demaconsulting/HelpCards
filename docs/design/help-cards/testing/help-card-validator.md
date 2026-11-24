### HelpCardValidator Design

![Testing Structure](TestingView.svg)

#### Purpose

`HelpCardValidator` is the main entry point of the Testing subsystem: it validates a help source, the
configuration of a catalog, or a catalog, and returns every problem as a list that a test can assert on, or throws one
`HelpCatalogException` on request. It also compares the files embedded in an assembly with the files of a directory.
With it go the public `HelpValidationOptions` (limits and switches) and the public interface `IHelpCardRule` (a rule
of the adopting product).

#### Data Model

| Member | Meaning |
| --- | --- |
| `MaxCardChars` | Longest text rendering of a card; 6000 by default |
| `MaxRootIndexChars` | Longest text rendering of a root card that has cards below it; 3500 by default |
| `MaxSummaryChars`, `MaxRootSummaryChars` | Longest summary of a card and of a root card; 100 and 60 by default |
| `MaxLineLength` | Longest body line, or null for no limit (the default) |
| `Rules` | The rules of the product, `IHelpCardRule`, applied to every card; none by default |
| `RequireExternalSources` | Turns every absent external source into an error; off by default |
| `WarningsAreErrors` | Makes warnings fail `AssertValid`; off by default |

`IHelpCardRule.Check(card)` returns the problems of one card, or nothing. The validator keeps no state between calls.

#### Key Methods

- **`Validate(source | callback | catalog, options)`** composes the source or the configuration with
  `HelpCatalogBuilder.Compose`, returns the errors alone when there are any, and otherwise checks every visible card of
  the catalog and the tokens of the sources. A `HelpSource` is added to a new builder together with the sources it
  imports, directly or transitively, each provider once and imports before importers (a depth-first walk of the
  imports), so a source with imports does not report `import-missing`; a callback adds exactly what it adds.
- **Quality rules** (private, in the `Rules` part): summary length, card size (the length of
  `HelpCatalog.Render(card)`), reserved headings (level one, and level two `Cards`, `Examples`, and `See also`),
  fences without a language, an unclosed fence, long lines, unused tokens (every file of the source is scanned for
  `{{name}}`), command examples (parsed with `ExampleCommandLine` for the tool name of the builder, else the name
  of the first source), and the rules of the options.
- **`Order`** sorts by severity descending, source index, file, line, and rule, with a stable sort.
- **`AssertValid(...)`** validates and throws `HelpCatalogException` with the errors, plus the warnings when
  `WarningsAreErrors` is set.
- **`CompareToDirectory(source, directory)`** lists the embedded files and the files of the directory and returns a
  `resources-mismatch` warning for each path that is on one side only, in path order.

#### Error Handling

Defects of cards are returned as problems. Null arguments throw `ArgumentNullException`; a builder without a source, a
white-space directory, and a directory that does not exist throw `ArgumentException`; limits
that are not positive throw `ArgumentOutOfRangeException` where they are set. `AssertValid` is the only member that
throws for a defect.

#### Dependencies

`HelpCatalogBuilder` (compose and sources), `HelpCatalog` (`FromData`, `Render`, cards), `HelpCard` (members and the
internal file and line of the body), `HelpProblem`, `HelpCatalogException`, `HelpSource` (provider), `TokenResolver`
(`FindNames`), `ExampleCommandLine`, and `HelpCardMarkdown`.

#### Callers

Adopters in their test projects, and the tests of this repository.
