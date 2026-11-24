## Catalog Subsystem Design

![Catalog Structure](CatalogView.svg)

### Overview

The Catalog subsystem holds what composition produced and gives it to callers: the immutable model of cards,
examples, links, and results, the frozen data that a catalog and all of its views share, filtered views, path and
example lookup, and the problem model with the exception that carries it. The public face is `HelpCatalog`.

The subsystem contains these units:

- **HelpCatalog** (public) creates a catalog from a configuration callback, exposes the cards, roots, sources, tags,
  and diagnostics, looks up cards and examples, and makes views. The type is partial so that the Presentation
  subsystem adds the `Render` members next to it.
- **HelpCard** (public) is one card as a view shows it. With it go the public supporting types `HelpExample`,
  `HelpExampleReference`, `HelpLink`, `HelpSourceInfo`, `HelpPathResolution`, and `HelpSearchResult`, and the
  internal `BodyPiece`.
- **HelpProblem** (public) is the problem value; with it go `HelpSeverity`, `HelpCatalogException`, and the internal
  `RuleIds`.
- **CatalogData** (internal) is the frozen data. The internal `CardNode`, `ResolvedPart`, and `ResolvedLink` are its
  supporting types.
- **CatalogScope** (internal) is the set of cards a view shows and the tree they form when hidden cards are lifted out.
  The name differs from the SysML2 view `CatalogView` on purpose.
- **PathResolver** (internal) resolves a path of names and aliases; `PathMatch` is its result.
- **ExampleResolver** (internal) finds examples by name.

### Interfaces

| Interface | Direction | Purpose |
| --- | --- | --- |
| `HelpCatalog.Create(configure)` | Inbound (adopters) | Create a catalog; throws `HelpCatalogException` |
| `Cards`, `Roots`, `Sources`, `Diagnostics`, `TagNames` | Inbound | Read the catalog |
| `Find`, `Resolve`, `GetChildren`, `GetSeeAlso` | Inbound | Look up cards |
| `FindExamples`, `FindExample` | Inbound | Look up examples |
| `Filter`, `FilterSource`, `Where`, `Without` | Inbound | Make views |
| `Render*`, `FormatBody` | Inbound | Render the view (bodies in the Presentation subsystem) |
| `HelpCatalogBuilder.Compose()` | Outbound (Composition) | Obtain the frozen data |
| `HelpCard.BodyPieces`, `HelpCatalog.Scope` | Inbound (Presentation) | Render cards in several styles |

The Catalog subsystem and the Composition subsystem depend on each other in one direction per use: the catalog calls
the builder to create its data, and the builder fills the data types of the catalog. Search and suggestions are not
part of the current system; `HelpPathResolution.Suggestion` is always empty and `HelpSearchResult` is data only.

### Design

**Frozen data and views.** Composition creates one `CatalogData` with a `CardNode` per card in depth-first order. A
catalog is a `CatalogData` seen through a `CatalogScope`. The scope of a new catalog shows everything; a view restricts
the scope of its parent by intersection, so views of views compose. Everything a scope needs is computed when the scope
is created, so views are immutable and cheap to share between threads.

**Hiding.** A hidden card does not hide its children: each visible descendant is lifted to its nearest visible
ancestor, or to the roots, and keeps its order. `Find` and `Resolve` walk the real tree through hidden cards but return
nothing for a hidden target, and report it like an unknown segment. See-also entries to hidden cards are dropped, a
link to a hidden card is written as its title, a link to a hidden example as `an example`, and an external link to a
hidden or absent target as its display text.

**Cards per view.** Each catalog creates its own `HelpCard` objects, because a card's parent, children, see-also list,
and body depend on the view. The body is computed on first access with a thread-safe `Lazy`; no rendered output of any
other style is cached. The plain-style body is written by `HelpCard` itself; the Presentation subsystem formats the
same `BodyPieces` in other styles.

**Lookup.** A path is split on `/` and white space; the first segment matches a root name or alias, each later
segment a child name or alias, ignoring case, after NFC normalization. Examples are found depth first, a card's own
examples before those of its children.

**Failure model.** Defective sources throw `HelpCatalogException` with every error; argument errors throw
`ArgumentException` or `ArgumentNullException`; unknown tags (only for a closed vocabulary), sources, and ids in a
view are argument errors. Callbacks, such as `Where` predicates, run on the calling thread, are called at once, and
are not kept.

The unit design documents describe each unit in detail; the verification design describes how the subsystem is tested
through the public face and the builder.
