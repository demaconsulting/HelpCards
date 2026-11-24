### HelpCatalog Design

![Catalog Structure](CatalogView.svg)

#### Purpose

`HelpCatalog` is the public face of the library. It creates a catalog from sources, exposes the immutable tree of
cards, looks up cards and examples, and makes filtered views. It is the type that adopters, renderers, the command-line
helper, and agent tools hold.

#### Data Model

| Member | Meaning |
| --- | --- |
| `Scope` (internal) | The `CatalogScope` that decides which cards the catalog shows |
| `_byId` | The `HelpCard` objects of this view by global id |
| `Cards`, `Roots` | The visible cards in depth-first order, and the visible cards without a visible ancestor |
| `Sources` | The sources that contributed visible cards; all sources for an unfiltered catalog |
| `TagNames` | The distinct tags of the visible cards in ordinal order |
| `Diagnostics`, `Culture` | The informational problems and the configured culture, shared by all views |

The class is partial: `HelpCatalog.cs` holds creation, properties, lookup, and example search; `HelpCatalog.Views.cs`
holds the four views; `HelpCatalog.Render.cs` holds the rendering members, whose bodies live in the Presentation
subsystem. All lists are read-only collections.

#### Key Methods

- **`Create(configure)`** creates a builder, calls the callback, composes, and throws `HelpCatalogException` with the
  errors, or returns a catalog over the scope that shows everything.
- **`FromData(data)`** (internal) returns a catalog that shows everything of data that was composed beforehand; the
  validator composes first so that it can report errors without an exception.
- **`Find(path)`**, **`Resolve(path)`** use `PathResolver`; `Resolve` returns a `HelpPathResolution` with the failing
  parent, segment, and valid names and no suggestion.
- **`GetChildren(card)`**, **`GetSeeAlso(card)`** match the card by id and map its visible child and see-also ids to
  the cards of this view.
- **`FindExamples(name, scopeCardId)`**, **`FindExample(card, name)`** use `ExampleResolver`; an unknown or hidden card
  gives no result.
- **`Filter`**, **`FilterSource`**, **`Where`**, **`Without`** each restrict the scope and return a new catalog.
  `Filter` and `FilterSource` compare ignoring case, treat an empty include list as no restriction, and reject empty
  entries, tags outside a closed vocabulary, and unknown source names. `Where` calls the predicate once per card, in
  `Cards` order, at once. `Without` hides exactly the listed ids and rejects unknown ones.
- **`Render`**, **`RenderIndex`**, **`RenderExamples`**, **`RenderExample`**, **`RenderSearch`**, **`RenderCardList`**,
  and **`FormatBody`** create a `RenderContext` for the catalog view and the options, map the card to the card of this
  view (rejecting a null card with `ArgumentNullException` and a card the view does not show with `ArgumentException`),
  and call `TextRenderer` or, for the JSON format, `JsonDocumentWriter`. `RenderIndex` rejects a negative depth. Two
  internal overloads add the usage lines to the JSON index and the filter to the JSON card list for later callers.
  `RenderSearch` renders results that were found elsewhere; no search is part of the current system.

#### Error Handling

Defects throw `HelpCatalogException`; invalid arguments throw `ArgumentException` or `ArgumentNullException`. Lookups
that find nothing return `null` or an empty list. No member throws `InvalidOperationException` directly.

#### Dependencies

`HelpCatalogBuilder` of the Composition subsystem; `CatalogData`, `CatalogScope`, `PathResolver`, `ExampleResolver`,
`HelpCard`, and the problem model; and, for the `Render` members, `HelpRenderOptions`, `TextRenderer`, `LinkFormatter`,
and `JsonDocumentWriter` of the Presentation subsystem.

#### Callers

Adopters, the Presentation subsystem (it reads the catalog it renders), the Testing subsystem (validation), and later
the command-line helper and the agent tool.
